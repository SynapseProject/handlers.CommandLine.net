using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

using YamlDotNet.Serialization;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Handlers.CommandLine;

public class CommandLineHandler : HandlerRuntimeBase
{
    HandlerConfig config = null;
    HandlerParameters parameters = null;

    public override IHandlerRuntime Initialize(string configStr)
    {
        config = HandlerUtils.Deserialize<HandlerConfig>(configStr);
        Console.WriteLine(config);

        return base.Initialize(configStr);
    }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        parameters = HandlerUtils.Deserialize<HandlerParameters>(startInfo.Parameters);
        Console.WriteLine(parameters);

        StatusType status = StatusType.Complete;

        try
        {
            String args = ProcessArguments(parameters);
            if (String.IsNullOrEmpty(config.RunOn))
                LocalProcess.RunCommand(config.Command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutAction, null, null, startInfo.IsDryRun);
            else
                WMIUtil.RunCommand(config.Command, args, config.RunOn, config.WorkingDirectory, config.TimeoutMills, config.TimeoutAction, null, null, startInfo.IsDryRun);
        }
        catch (Exception)
        {
            status = StatusType.Failed;
        }

        return new ExecuteResult() { Status = status };
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
}
