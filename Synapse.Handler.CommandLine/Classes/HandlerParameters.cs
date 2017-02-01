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
    public partial class HandlerParameters
    {
        [XmlElement]
        public ArgumentParserType Parser { get; set; }
        [XmlElement]
        public Object Arguments { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Arguments : " + Arguments);
            sb.AppendLine("Parser    : " + Parser);
            return sb.ToString();
        }
    }
}
