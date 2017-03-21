using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using System.Xml.Serialization;
using System.Xml;

namespace Synapse.Handlers.CommandLine
{
    public class RegexArguments
    {
        [XmlElement]
        public String Find { get; set; }
        [XmlElement]
        public String ReplaceWith { get; set; }
        [XmlElement]
        public EncodingType Encoding { get; set; }


        public static String Parse(String argumentString, List<RegexArguments> expressions)
        {
            String args = argumentString;

            if (expressions != null)
            {
                foreach (RegexArguments replacement in expressions)
                {
                    String replaceWith = replacement.ReplaceWith;
                    if (replacement.Encoding == EncodingType.Base64)
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(replaceWith);
                        replaceWith = Convert.ToBase64String(bytes);
                    }

                    Regex regex = new Regex(replacement.Find);
                    args = regex.Replace(args, replaceWith);
                }
            }

            return args;
        }

    }
}
