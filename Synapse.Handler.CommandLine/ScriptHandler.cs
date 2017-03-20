using System;
using System.Xml;
using System.IO;

using Synapse.Core;
using Synapse.Handlers.CommandLine;

public class ScriptHandler : HandlerRuntimeBase
{
    ScriptHandlerConfig config = null;
    String parameters = null;

    public override IHandlerRuntime Initialize(string configStr)
    {
        config = HandlerUtils.Deserialize<ScriptHandlerConfig>(configStr);
        return base.Initialize(configStr);
    }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = null;
        parameters = startInfo.Parameters;
        String script = null;

        try
        {
            String command = null;
            String args = null;

            switch (config.Type)
            {
                case ScriptType.Powershell:
                    command = "powershell.exe";
                    script = GetScript(config, "ps1");
                    args = config.Args + @" -File """ + script + @"""";
                    if (!String.IsNullOrWhiteSpace(config.ScriptArgs))
                    {
                        String scriptArgs = RegexArgumentParser.Parse(config.ScriptArgs, config.Expressions);
                        args += " " + scriptArgs;
                    }
                    break;

                case ScriptType.Batch:
                    command = "cmd.exe";
                    script = GetScript(config, "bat");
                    args = config.Args + " " + script;
                    if (!String.IsNullOrWhiteSpace(config.ScriptArgs))
                    {
                        String scriptArgs = RegexArgumentParser.Parse(config.ScriptArgs, config.Expressions);
                        args += " " + scriptArgs;
                    }

                    break;

                default:
                    throw new Exception("Unknown ScriptType [" + config.Type.ToString() + "] Received.");
            }

            if (String.IsNullOrEmpty(config.RunOn))
                result = LocalProcess.RunCommand(command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, SynapseLogger, null, startInfo.IsDryRun);
            else
                result = WMIUtil.RunCommand(command, args, config.RunOn, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, config.KillRemoteProcessOnTimeout, SynapseLogger, config.RunOn, startInfo.IsDryRun);

            if (result.Status == StatusType.None)
                result.Status = HandlerUtils.GetStatusType(result.ExitCode, config.ValidExitCodes);

            if (File.Exists(script) && config.ParameterType == ParameterTypeType.Script)
                File.Delete(script);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            if (File.Exists(script))
                File.Delete(script);
            throw e;
        }

        OnLogMessage(config.RunOn, "Command finished with exit code = " + result.ExitCode + ".  Returning status [" + result.Status + "].");
        return result;
    }

    public string GetScript(ScriptHandlerConfig config, String extension)
    {
        String script = null;
        if (config.ParameterType == ParameterTypeType.Script)
        {
            script = FileUtils.GetTempFileUNC(config.RunOn, config.WorkingDirectory, extension);
            if (script == null)
                script = FileUtils.GetTempFileUNC(config.RunOn, Path.GetTempPath(), extension);
            File.WriteAllText(script, parameters);
        }
        else
            script = parameters;

        return script;
    }

    public void SynapseLogger(String label, String message)
    {
        OnLogMessage(label, message);
    }

}
