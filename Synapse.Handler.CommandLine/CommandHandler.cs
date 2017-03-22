using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Synapse.Core;
using Synapse.Handlers.CommandLine;

public class CommandHandler : HandlerRuntimeBase
{
    CommandHandlerConfig config = null;
    CommandHandlerParameters parameters = null;
    Dictionary<string, string> variables = null;

    public override IHandlerRuntime Initialize(string configStr)
    {
        config = HandlerUtils.Deserialize<CommandHandlerConfig>(configStr);
        return base.Initialize(configStr);
    }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = null;
        if (startInfo.Parameters != null)
            parameters = HandlerUtils.Deserialize<CommandHandlerParameters>(startInfo.Parameters);

        try
        {
            Validate();

            // Replace Any "Special" Handler Variables In Arguments or ReplaceWith elements
            variables = HandlerUtils.GatherVariables(this, startInfo);
            parameters.Arguments = HandlerUtils.ReplaceHandlerVariables(parameters.Arguments, variables);
            if (parameters.Expressions != null)
                foreach (RegexArguments expression in parameters.Expressions)
                    expression.ReplaceWith = HandlerUtils.ReplaceHandlerVariables(expression.ReplaceWith, variables);

            String args = RegexArguments.Parse(parameters.Arguments, parameters.Expressions);
            if (String.IsNullOrEmpty(config.RunOn))
                result = LocalProcess.RunCommand(config.Command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, SynapseLogger, null, startInfo.IsDryRun);
            else
                result = WMIUtil.RunCommand(config.Command, args, config.RunOn, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, config.KillRemoteProcessOnTimeout, SynapseLogger, config.RunOn, startInfo.IsDryRun);

            if (result.Status == StatusType.None)
                result.Status = HandlerUtils.GetStatusType(result.ExitCode, config.ValidExitCodes);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            throw e;
        }

        OnLogMessage(config.RunOn, "Command finished with exit code = " + result.ExitCode + ".  Returning status [" + result.Status + "].");

        return result;
    }

    public void SynapseLogger(String label, String message)
    {
        OnLogMessage(label, message);
    }

    private void Validate()
    {
        List<String> errors = new List<String>();

        if (!String.IsNullOrWhiteSpace(config.WorkingDirectory))
        {
            String path = FileUtils.GetUNCPath(config.RunOn, config.WorkingDirectory);
            if (!Directory.Exists(path))
            {
                errors.Add("Working Directory Not Found.");
            }
        }

        if (errors.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Invalid Plan Specified :");
            foreach (String error in errors)
                sb.AppendLine(error);
            throw new Exception(sb.ToString());
        }
        else
            OnLogMessage("Validate", "Plan Sucessfully Validated");
    }

}
