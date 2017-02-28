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
            String args = this.ArgString;

            foreach (RegexSubstitutionType replacement in this.Expressions)
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
