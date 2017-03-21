using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

using Synapse.Core;

using YamlDotNet.Serialization;

using Synapse.Core.Utilities;

namespace Synapse.Handlers.CommandLine
{
    public class ScriptHandlerConfig
    {
        [XmlElement]
        public String RunOn { get; set; }
        [XmlElement]
        public String WorkingDirectory { get; set; }
        [XmlElement]
        public ScriptType Type { get; set; }
        [XmlElement]
        public String Arguments { get; set; }
        [XmlElement]
        public long TimeoutMills { get; set; }
        [XmlElement]
        public StatusType TimeoutStatus { get; set; }
        [XmlElement]
        public bool KillRemoteProcessOnTimeout { get; set; }
        [XmlArrayItem(ElementName = "ExitCode")]
        public List<String> ValidExitCodes { get; set; }

    }

}
