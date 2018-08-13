using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

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
                    Regex regex = new Regex(replacement.Find);

                    if (replaceWith == null)
                        args = regex.Replace(args, "");
                    else
                    {
                        if (replacement.Encoding == EncodingType.Base64)
                            replaceWith = HandlerUtils.Base64Encode(replaceWith);

                        args = regex.Replace(args, replaceWith);
                    }
                }
            }

            return args;
        }

    }
}
