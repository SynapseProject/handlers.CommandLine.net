using System;
using System.Xml;

using Synapse.Core;
using Synapse.Handlers.CommandLine;

public class CommandHandler : HandlerRuntimeBase
{
    HandlerConfig config = null;
    HandlerParameters parameters = null;

    public override IHandlerRuntime Initialize(string configStr)
    {
        config = HandlerUtils.Deserialize<HandlerConfig>(configStr);
        return base.Initialize(configStr);
    }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = null;
        if (startInfo.Parameters != null)
            parameters = HandlerUtils.Deserialize<HandlerParameters>(startInfo.Parameters);

        try
        {
            String args = ProcessArguments(parameters);
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

    private String ProcessArguments(HandlerParameters parms)
    {
        String args = String.Empty;

        if (parms != null)
        {
            if (parms.Parser == ArgumentParserType.None)
            {
                if (parms.Arguments?.GetType() == typeof(System.Xml.XmlNode[]))
                {
                    XmlNode[] nodes = (System.Xml.XmlNode[])(parms.Arguments);
                    args = nodes[0].InnerText;
                }
                else
                    args = parms.Arguments?.ToString();
            }
            else if (parms.Parser == ArgumentParserType.Regex)
            {
                if (parms.Arguments != null)
                {
                    args = HandlerUtils.Serialize<RegexArgumentParser>(parms.Arguments);
                    RegexArgumentParser parser = HandlerUtils.Deserialize<RegexArgumentParser>(args);
                    args = parser.Parse();
                }
            }
        }

        return args;
    }

    public void SynapseLogger(String label, String message)
    {
        OnLogMessage(label, message);
    }

}
