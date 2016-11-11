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
using Synapse.CommandLine.Handler;

public class CommandLineHandler : HandlerRuntimeBase
{
    HandlerConfig config = null;
    HandlerParameters parameters = null;

    //TODO : Debug - Remove Enternal Program Debugger In Project > Properties > Debug Section
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

        String args = ProcessArguments(parameters);
        LocalProcess.RunCommand(config.Command, args, config.WorkingDirectory, config.TimeoutMills, config.TimeoutAction);

        //TODO : Debug - Delete Me
        Console.WriteLine("Press <ENTER> To Continue.");
        Console.ReadLine();

        return new ExecuteResult() { Status = StatusType.Complete };
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
