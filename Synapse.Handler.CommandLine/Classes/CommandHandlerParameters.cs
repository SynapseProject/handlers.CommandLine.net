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

namespace Synapse.Handlers.CommandLine
{
    public class CommandHandlerParameters
    {
        [XmlElement]
        public String Arguments { get; set; }
        [XmlArrayItem(ElementName = "Expression")]
        public List<RegexArguments> Expressions { get; set; }
    }
}
