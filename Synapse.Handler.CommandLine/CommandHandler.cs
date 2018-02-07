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

    public override object GetConfigInstance()
    {
        CommandHandlerConfig config = new CommandHandlerConfig();

        config.RunOn = @"someserver.domain.com";
        config.WorkingDirectory = @"C:\Temp";
        config.Command = @"powershell.exe";
        config.TimeoutMills = 60000;
        config.TimeoutStatus = StatusType.Failed;
        config.KillRemoteProcessOnTimeout = false;
        config.ReturnStdout = true;
        config.ValidExitCodes = new List<string>();

        config.ValidExitCodes.Add("EQ 0 Success");
        config.ValidExitCodes.Add("NE 0 Failure");

        return config;
    }

    public override object GetParametersInstance()
    {
        CommandHandlerParameters parms = new CommandHandlerParameters();

        parms.Arguments = @"-ExecutionPolicy Bypass -File C:\Temp\test.ps1 -p1 ""Hello World"" -p2 ""bbb"" -p3 ""ccc""";

        parms.Expressions = new List<RegexArguments>();
        RegexArguments args = new RegexArguments();
        args.Find = "bbb";
        args.ReplaceWith = "yyy";
        args.Encoding = EncodingType.None;
        parms.Expressions.Add(args);

        RegexArguments args2 = new RegexArguments();
        args2.Find = "ccc";
        args2.ReplaceWith = "zzz";
        args2.Encoding = EncodingType.Base64;
        parms.Expressions.Add(args2);


        return parms;
    }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = null;
        if (startInfo.Parameters != null)
            parameters = HandlerUtils.Deserialize<CommandHandlerParameters>(startInfo.Parameters);

        try
        {
            OnLogMessage( "Execute", $"Running Handler As User [{System.Security.Principal.WindowsIdentity.GetCurrent().Name}]" );

            Validate();

            // Replace Any "Special" Handler Variables In Arguments or ReplaceWith elements
            variables = HandlerUtils.GatherVariables(this, startInfo);
            parameters.Arguments = HandlerUtils.ReplaceHandlerVariables(parameters.Arguments, variables);
            if (parameters.Expressions != null)
                foreach (RegexArguments expression in parameters.Expressions)
                    expression.ReplaceWith = HandlerUtils.ReplaceHandlerVariables(expression.ReplaceWith, variables);

            String args = RegexArguments.Parse(parameters.Arguments, parameters.Expressions);

            bool isDryRun = startInfo.IsDryRun && !(config.SupportsDryRun);
            if (startInfo.IsDryRun && config.SupportsDryRun)
                OnLogMessage("Execute", "DryRun Flag is set, but plan config indicates the command supports DryRun.  Command will execute.");

            if (String.IsNullOrEmpty(config.RunOn))
            {
                SecurityContext runAs = startInfo.RunAs;
                if (runAs != null && runAs.HasCrypto)
                    runAs = startInfo.RunAs.GetCryptoValues(startInfo.RunAs.Crypto, false);
                result = LocalProcess.RunCommand(config.Command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, SynapseLogger, null, isDryRun, config.ReturnStdout, runAs?.Domain, runAs?.UserName, runAs?.Password);
            }
            else
            {
                result = WMIUtil.RunCommand(config.Command, args, config.RunOn, config.WorkingDirectory, config.TimeoutMills, config.TimeoutStatus, config.KillRemoteProcessOnTimeout, SynapseLogger, config.RunOn, isDryRun, config.ReturnStdout);
            }

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
            if (!Directory.Exists(path) && !Directory.Exists(config.WorkingDirectory))
            {
                errors.Add("Working Directory Not Found.");
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
