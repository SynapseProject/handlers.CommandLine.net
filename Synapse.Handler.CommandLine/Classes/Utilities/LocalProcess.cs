using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Synapse.Core;

namespace Synapse.Handlers.CommandLine
{
    class LocalProcess
    {
        public static ExecuteResult RunCommand(String command, String args, String remoteWorkingDirectory, long timeoutMills = 0, StatusType timeoutStatus = StatusType.Failed, Action<string, string> callback = null, String callbackLabel = null, bool dryRun = false)
        {
            StringBuilder stdout = new StringBuilder();
            ExecuteResult result = new ExecuteResult();
            int exitCode = 0;
            if (callback == null)
                callback = LogTailer.ConsoleWriter;

            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = remoteWorkingDirectory;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            callback?.Invoke(callbackLabel, "Starting Command : " + command + " " + args);

            if (!dryRun)
            {
                process.Start();

                Thread stdOutReader = new Thread(delegate ()
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        String line = process.StandardOutput.ReadLine();
                        lock (stdout)
                        {
                            stdout.AppendLine(line);
                        }
                        callback?.Invoke(callbackLabel, line);
                    }
                });
                stdOutReader.Start();

                Thread stdErrReader = new Thread(delegate ()
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        String line = process.StandardError.ReadLine();
                        lock (stdout)
                        {
                            stdout.AppendLine(line);
                        }
                        callback?.Invoke(callbackLabel, line);
                    }
                });
                stdErrReader.Start();

                bool timeoutReached = false;
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (stdOutReader.IsAlive && stdErrReader.IsAlive && !(timeoutReached))
                {
                    if (timeoutMills > 0)
                    {
                        if (timer.ElapsedMilliseconds > timeoutMills)
                            timeoutReached = true;
                    }
                    Thread.Sleep(500);
                }

                timer.Stop();

                if (timeoutReached)
                {
                    result.Status = timeoutStatus;
                    String timeoutMessage = "TIMEOUT : Process [" + process.ProcessName + "] With Id [" + process.Id + "] Failed To Complete In [" + timeoutMills + "] Milliseconds And Was Termintated.";

                    if (!process.HasExited)
                    {
                        process.Kill();
                        callback?.Invoke(callbackLabel, timeoutMessage);
                    }
                    else
                    {
                        timeoutMessage = "TIMEOUT : Process [" + process.ProcessName + "] With Id [" + process.Id + "] Failed To Complete In [" + timeoutMills + "] Milliseconds But May Have Completed.";
                        callback?.Invoke(callbackLabel, timeoutMessage);
                    }

                    callback?.Invoke(callbackLabel, "TIMEOUT : Returning Timeout Stauts [" + result.Status + "].");
                }

                exitCode = process.ExitCode;
            }
            else
            {
                callback?.Invoke(callbackLabel, "Dry Run Flag Set.  Execution Skipped");
            }

            result.ExitCode = exitCode;
            result.ExitData = stdout.ToString();
            result.Message = "Exit Code = " + exitCode;
            callback?.Invoke(callbackLabel, result.Message);

            return result;
            
        }
    }
}
