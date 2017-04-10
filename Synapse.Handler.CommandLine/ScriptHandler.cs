using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Synapse.Core;
using Synapse.Handlers.CommandLine;

public class ScriptHandler : HandlerRuntimeBase
{
    ScriptHandlerConfig config = null;
    ScriptHandlerParameters parameters = null;
    Dictionary<string, string> variables = null;

    public override IHandlerRuntime Initialize(string configStr)
    {
        config = HandlerUtils.Deserialize<ScriptHandlerConfig>(configStr);
        return base.Initialize(configStr);
    }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = null;
        if (startInfo.Parameters != null)
            parameters = HandlerUtils.Deserialize<ScriptHandlerParameters>(startInfo.Parameters);
        String script = null;

        try
        {
            String command = null;
            String args = null;
            bool isTempScript = false;

            Validate();

            // Replace Any "Special" Handler Variables In Arguments or ReplaceWith elements
            variables = HandlerUtils.GatherVariables(this, startInfo);
            parameters.Arguments = HandlerUtils.ReplaceHandlerVariables(parameters.Arguments, variables);
            if (parameters.Expressions != null)
                foreach (RegexArguments expression in parameters.Expressions)
                    expression.ReplaceWith = HandlerUtils.ReplaceHandlerVariables(expression.ReplaceWith, variables);

            switch (config.Type)
            {
                case ScriptType.Powershell:
                    command = "powershell.exe";
                    if (!String.IsNullOrWhiteSpace(parameters.Script))
                    {
                        isTempScript = false;
                        script = parameters.Script;
                    }
                    else
                    {
                        isTempScript = true;
                        script = CreateTempScriptFile(parameters.ScriptBlock, "ps1");
                    }
                    args = config.Arguments + @" -File """ + script + @"""";
                    if (!String.IsNullOrWhiteSpace(parameters.Arguments))
                    {
                        String scriptArgs = RegexArguments.Parse(parameters.Arguments, parameters.Expressions);
                        args += " " + scriptArgs;
                    }
                    break;

                case ScriptType.Batch:
                    command = "cmd.exe";
                    if (!String.IsNullOrWhiteSpace(parameters.Script))
                    {
                        isTempScript = false;
                        script = parameters.Script;
                    }
                    else
                    {
                        isTempScript = true;
                        script = CreateTempScriptFile(parameters.ScriptBlock, "bat");
                    }
                    args = config.Arguments + " " + script;
                    if (!String.IsNullOrWhiteSpace(parameters.Arguments))
                    {
                        String scriptArgs = RegexArguments.Parse(parameters.Arguments, parameters.Expressions);
                        args += " " + scriptArgs;
                    }

                    break;

                default:
                    throw new Exception("Unknown ScriptType [" + config.Type.ToString() + "] Received.");
            }

            if (String.IsNullOrEmpty(config.RunOn))
                result = LocalProcess.RunCommand(command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, SynapseLogger, null, startInfo.IsDryRun, config.ReturnStdout);
            else
                result = WMIUtil.RunCommand(command, args, config.RunOn, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, config.KillRemoteProcessOnTimeout, SynapseLogger, config.RunOn, startInfo.IsDryRun, config.ReturnStdout);

            if (result.Status == StatusType.None)
                result.Status = HandlerUtils.GetStatusType(result.ExitCode, config.ValidExitCodes);

            if (File.Exists(script) && isTempScript)
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

    public string CreateTempScriptFile(String script, String extension)
    {
        String fileName = null;

        fileName = FileUtils.GetTempFileUNC(config.RunOn, config.WorkingDirectory, extension);
        if (fileName == null)
            fileName = FileUtils.GetTempFileUNC(config.RunOn, Path.GetTempPath(), extension);
        File.WriteAllText(fileName, script);

        return fileName;
    }

    public void SynapseLogger(String label, String message)
    {
        OnLogMessage(label, message);
    }

    private void Validate()
    {
        List<String> errors = new List<String>();

        if ((!String.IsNullOrWhiteSpace(parameters.Script)) && (!String.IsNullOrWhiteSpace(parameters.ScriptBlock)))
        {
            errors.Add("Script and ScriptBlock Exist In Same Action.");
        }

        if (!String.IsNullOrWhiteSpace(config.WorkingDirectory))
        {
            String path = FileUtils.GetUNCPath(config.RunOn, config.WorkingDirectory);
            if (!Directory.Exists(path))
            {
                errors.Add("Working Directory Not Found.  " + path);
            }
        }

        if (!String.IsNullOrWhiteSpace(parameters.Script))
        {
            String file = FileUtils.GetUNCPath(config.RunOn, parameters.Script);
            if (!File.Exists(file))
            {
                errors.Add("Script Not Found.  " + file);
            }
        }

        if (errors.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Invalid Plan Specified :");
            foreach (String error in errors)
                sb.AppendLine("ERROR : " + error);
            throw new Exception(sb.ToString());
        }
        else
            OnLogMessage("Validate", "Plan Sucessfully Validated");
    }

}
