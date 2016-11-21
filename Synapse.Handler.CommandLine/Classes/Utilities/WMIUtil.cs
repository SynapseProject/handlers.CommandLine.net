using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace Synapse.CommandLine.Handler
{
    public static class WMIUtil
    {
        #region Public Methods - RunCommand

        public static Int32 RunCommand(String command, String args, String server, String remoteWorkingDirectory, long timeoutMills, TimeoutActionType actionOnTimeout, Action<string, string> callback = null, string callbackLabel = null)
        {
            if (callback == null)
                callback = LogTailer.ConsoleWriter;

            Int32 exitStatus = 0;
            String rwd = remoteWorkingDirectory;
            
            if (string.IsNullOrWhiteSpace(rwd))
                rwd = @"C:\Temp";

            String cmd = command;
            if (!String.IsNullOrWhiteSpace(args))
                cmd = command + " " + args;

            if (callback != null)
                callback(callbackLabel, "Starting Command : " + cmd);

            try
            {
                // Create the process 
                using (ManagementClass process = new ManagementClass("Win32_Process"))
                {
                    ManagementScope scope = GetManagementScope(server);
                    process.Scope = scope;

                    ManagementBaseObject inParams = process.GetMethodParameters("Create");

                    String stdOutErrFile = System.IO.Path.GetRandomFileName();
                    stdOutErrFile = stdOutErrFile.Replace(".", "") + ".log";

                    inParams["CurrentDirectory"] = rwd;
                    inParams["CommandLine"] = @"cmd.exe /c " + cmd + @" 1> " + stdOutErrFile + @" 2>&1";

                    ManagementBaseObject mbo = process.InvokeMethod("Create", inParams, null);
                    
                    UInt32 exitCode = (uint)mbo["ReturnValue"];
                    UInt32 processId = 0;
                    

                    if (exitCode == 0)
                    {
                        processId = (uint)mbo["ProcessId"];

                        // Start Tailing Output Log
                        LogTailer tailer = new LogTailer(server, Path.Combine(remoteWorkingDirectory, stdOutErrFile), callback, callbackLabel);
                        tailer.Start();

                        // Wait For Process To Finish or Timeout To Be Reached
                        ManagementEventWatcher w = new ManagementEventWatcher(scope, new WqlEventQuery("select * from Win32_ProcessStopTrace where ProcessId=" + processId));
                        if (timeoutMills > 0)
                            w.Options.Timeout = new TimeSpan(0, 0, 0, 0, (int)timeoutMills);
                        try
                        {
                            ManagementBaseObject mboEvent = w.WaitForNextEvent();
                            UInt32 uExitStatus = (UInt32)mboEvent.Properties["ExitStatus"].Value;
                            exitStatus = unchecked((int)uExitStatus);
                        } 
                        catch (ManagementException ex)
                        {
                         if (ex.Message.Contains("Timed out"))
                            {
                                StringBuilder rc = new StringBuilder();
                                String processName = @"cmd.exe";
                                String timeoutMessage = "TIMEOUT : Process [" + processName + "] With Id [" + processId + "] Failed To Stop In [" + timeoutMills + "] Milliseconds.";
                                if (actionOnTimeout == TimeoutActionType.KillProcessAndContinue || actionOnTimeout == TimeoutActionType.KillProcessAndError)
                                {
                                    String queryStr = String.Format("SELECT * FROM Win32_Process Where Name = '{0}' AND ProcessId = '{1}'", processName, processId);
                                    ObjectQuery Query = new ObjectQuery(queryStr);
                                    ManagementObjectSearcher Searcher = new ManagementObjectSearcher(scope, Query);

                                    foreach (ManagementObject thisProcess in Searcher.Get())
                                        rc.Append(KillProcess(scope, thisProcess));

                                    using (StringReader procStr = new StringReader(rc.ToString()))
                                    {
                                        String procLine;
                                        while ((procLine = procStr.ReadLine()) != null)
                                        {
                                            if (callback != null)
                                                callback(callbackLabel, procLine);
                                        }
                                    }

                                    timeoutMessage = "TIMEOUT : Process [" + processName + "] With Id [" + processId + "] Failed To Stop In [" + timeoutMills + "] Milliseconds And Was Remotely Termintated.";
                                }
                                tailer.Stop(60, true);

                                throw new Exception(timeoutMessage);
                            }
                            else
                            {
                                tailer.Stop(60, true);
                                throw ex;
                            }
                        }

                        tailer.Stop(300, true);

                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(callbackLabel, "Return Value : " + exitCode);
                            callback(callbackLabel, mbo.GetText(TextFormat.Mof));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (callback != null)
                {
                    String errorMsg = e.Message;

                    if (errorMsg.StartsWith("TIMEOUT"))
                    {
                        callback(callbackLabel, e.Message);
                    }
                    else
                    {
                        callback(callbackLabel, "Error Occured In WMIUtils.RunCommand : ");
                        callback(callbackLabel, e.Message);
                        callback(callbackLabel, e.StackTrace);
                        throw e;
                    }
                }

                if (actionOnTimeout == TimeoutActionType.Error)
                    throw e;
            }

            if (callback != null)
                callback(callbackLabel, "Command Completed.  Exit Code = " + exitStatus);

            return exitStatus;
        }

        #endregion


        #region Private Methods
        
        static ManagementScope GetManagementScope(String server)
        {
            return GetManagementScope(server, null, null, null);
        }
        
        static ManagementScope GetManagementScope(String server, String domain, String userName, String password)
        {
            ConnectionOptions options = new ConnectionOptions();

            if (server != null)
            {
                if (!("localhost".Equals(server.Trim().ToLower())) && !("127.0.0.1".Equals(server.Trim())))
                {
                    if (domain != null && userName != null && password != null)
                    {
                        options.Impersonation = ImpersonationLevel.Impersonate;
                        options.Username = domain + @"\" + userName;
                        options.Password = password;
                    }
                }
                else
                    server = null;
            }
            options.Authentication = AuthenticationLevel.Default;
            options.Authority = null;
            options.EnablePrivileges = true;

            // Note: The ConnectionOptions object is not necessary 
            // if we are connecting to local machine & the account has privileges 
            ManagementScope scope = null;
            if (server == null)
                scope = new ManagementScope(@"\ROOT\CIMV2", options);
            else
                scope = new ManagementScope(@"\\" + server + @"\ROOT\CIMV2", options);
            scope.Connect();

            return scope;
        }

        static String KillProcess(ManagementScope scope, ManagementObject process)
        {
            StringBuilder rc = new StringBuilder();
            String queryStr = String.Format("SELECT * FROM Win32_Process Where ParentProcessId = '{0}'", process.GetPropertyValue("ProcessId"));
            ObjectQuery Query = new ObjectQuery(queryStr);
            ManagementObjectSearcher Searcher = new ManagementObjectSearcher(scope, Query);

            foreach (ManagementObject childProc in Searcher.Get())
                rc.Append(KillProcess(scope, childProc));

            rc.AppendLine("Terminated Process : [" + process.GetPropertyValue("Name") + "] With PID [" + process.GetPropertyValue("ProcessId") + "]");
            process.InvokeMethod("Terminate", null);

            return rc.ToString();
        }

        #endregion
    }
}
