using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Synapse.Handlers.CommandLine
{
    public partial class RegexArgumentParser
    {
        public String Parse()
        {
            return Parse(this.ArgString, this.Expressions);
        }

        public static String Parse(String argumentString, List<RegexSubstitutionType> expressions)
        {
            String args = argumentString;

            foreach (RegexSubstitutionType replacement in expressions)
            {
                String replaceWith = replacement.ReplaceWith;
                if (replacement.Encoding == EncodingType.Base64)
                {
                    var bytes = Encoding.UTF8.GetBytes(replaceWith);
                    replaceWith = Convert.ToBase64String(bytes);
                }

                Regex regex = new Regex(replacement.Find);
                args = regex.Replace(args, replaceWith);
            }

            return args;
        }
    }
}
