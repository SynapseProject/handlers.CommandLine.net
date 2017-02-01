using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;
using System.Xml;

namespace Synapse.Handlers.CommandLine
{
    public partial class RegexArgumentParser
    {
        [XmlElement]
        public String ArgString { get; set; }
        [XmlArrayItem(ElementName = "Expression")]
        public List<RegexSubstitutionType> Expressions { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }
    }

    public class RegexSubstitutionType
    {
        [XmlElement]
        public String Find { get; set; }
        [XmlElement]
        public String ReplaceWith { get; set; }
    }

}
