using System;
using System.Xml;

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

        try
        {
            Console.WriteLine(parameters);

            String command = "@cmd.exe";
            String args = null;
            switch (config.Type)
            {
                case ScriptType.Powershell:
                    command = "powershell.exe";
                    args = config.Args + @" Invoke-Command -ScriptBlock {" + parameters + "}";
                    if (!String.IsNullOrWhiteSpace(config.ScriptArgs))
                        args += " -ArgumentList " + config.ScriptArgs;
                    break;
            }

            if (String.IsNullOrEmpty(config.RunOn))
                result = LocalProcess.RunCommand(command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutAction, SynapseLogger, null, startInfo.IsDryRun);
            else
                result = WMIUtil.RunCommand(command, args, config.RunOn, config.WorkingDirectory, config.TimeoutMills, config.TimeoutAction, SynapseLogger, config.RunOn, startInfo.IsDryRun);

            result.Status = HandlerUtils.GetStatusType(int.Parse(result.ExitData.ToString()), config.ValidExitCodes);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            throw e;
        }

        OnLogMessage(config.RunOn, "Command " + result.Status + " with Exit Code = " + result.ExitData);

        return result;
    }

    private String ProcessArguments(HandlerParameters parms)
    {
        String args = String.Empty;

        if (parms.Parser == ArgumentParserType.None)
        {
            if (parms.Arguments.GetType() == typeof(System.Xml.XmlNode[]))
            {
                XmlNode[] nodes = (System.Xml.XmlNode[])(parms.Arguments);
                args = nodes[0].InnerText;
            }
            else
                args = parms.Arguments.ToString();
        }
        else if (parms.Parser == ArgumentParserType.Regex)
        {
            args = HandlerUtils.Serialize<RegexArgumentParser>(parms.Arguments);
            RegexArgumentParser parser = HandlerUtils.Deserialize<RegexArgumentParser>(args);
            args = parser.Parse();
        }

        return args;
    }

    public void SynapseLogger(String label, String message)
    {
        OnLogMessage(label, message);
    }

}
