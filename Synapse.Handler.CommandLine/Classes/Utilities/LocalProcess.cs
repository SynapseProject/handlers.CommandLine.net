using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Synapse.CommandLine.Handler
{
    class LocalProcess
    {
        public static Int32 RunCommand(String command, String args, String remoteWorkingDirectory, long timeoutMills = 0, TimeoutActionType actionOnTimeout = TimeoutActionType.Error, Action<string, string> callback = null, String callbackLabel = null, bool dryRun = false)
        {
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
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;

            if (callback != null)
                callback(callbackLabel, "Starting Command : " + command + " " + args);

            if (!dryRun)
            {
                process.Start();

                Thread stdOutReader = new Thread(delegate ()
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        String line = process.StandardOutput.ReadLine();
                        if (callback != null)
                            callback(callbackLabel, line);
                    }
                });
                stdOutReader.Start();

                Thread stdErrReader = new Thread(delegate ()
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        String line = process.StandardError.ReadLine();
                        if (callback != null)
                            callback(callbackLabel, line);
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
                    String timeoutMessage = "TIMEOUT : Process [" + process.ProcessName + "] With Id [" + process.Id + "] Failed To Stop In [" + timeoutMills + "] Milliseconds And Was Remotely Termintated.";

                    if (!process.HasExited)
                    {
                        process.Kill();
                        if (callback != null)
                            callback(callbackLabel, timeoutMessage);
                    }
                    else
                    {
                        timeoutMessage = "TIMEOUT : Process [" + process.ProcessName + "] With Id [" + process.Id + "] Failed To Stop In [" + timeoutMills + "] Milliseconds But May Have Completed.";
                        if (callback != null)
                            callback(callbackLabel, timeoutMessage);
                    }
                    if (actionOnTimeout == TimeoutActionType.Error || actionOnTimeout == TimeoutActionType.KillProcessAndError)
                    {
                        throw new Exception(timeoutMessage);
                    }
                }

                exitCode = process.ExitCode;
            }

            if (callback != null)
                callback(callbackLabel, "Command Completed.  Exit Code = " + exitCode);

            return exitCode;
            
        }
    }
}
