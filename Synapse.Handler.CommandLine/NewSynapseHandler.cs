using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

using Synapse.Core;

namespace Synapse.Handler.CommandLine
{
    public class NewSynapseHandler : HandlerRuntimeBase
    {
        override public ExecuteResult Execute(HandlerStartInfo startInfo)
        {
            return new ExecuteResult() { Status = StatusType.Complete };
        }
    }
}
