using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

using YamlDotNet.Serialization;

using Synapse.Core.Utilities;

namespace Synapse.CommandLine.Handler
{
    public partial class HandlerConfig
    {
        [XmlArrayItem(ElementName = "Server")]
        public List<String> Servers { get; set; }
        [XmlElement]
        public String WorkingDirectory { get; set; }
        [XmlElement]
        public String Command { get; set; }
        [XmlElement]
        public long TimeoutMills { get; set; }
        [XmlElement]
        public TimeoutActionType TimeoutAction { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Servers?.Count > 0)
            {
                sb.AppendLine("Servers : ");
                foreach (String server in Servers)
                    sb.AppendLine("  - " + server);
            }
            if (!String.IsNullOrWhiteSpace(WorkingDirectory)) { sb.AppendLine("WorkingDirectory : " + WorkingDirectory); }
            if (!String.IsNullOrWhiteSpace(Command)) { sb.AppendLine("Command          : " + Command); }
            sb.AppendLine("TimeoutMills     : " + TimeoutMills);
            sb.AppendLine("TimeoutAction    : " + TimeoutAction);

            return sb.ToString();
        }

    }

}
