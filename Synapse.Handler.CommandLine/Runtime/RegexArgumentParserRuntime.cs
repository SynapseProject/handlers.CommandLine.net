using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Synapse.CommandLine.Handler
{
    public partial class RegexArgumentParser
    {
        public String Parse()
        {
            String args = this.ArgString;

            foreach (RegexSubstitutionType replacement in this.Expressions)
            {
                Regex regex = new Regex(replacement.Find);
                args = regex.Replace(args, replacement.ReplaceWith);
            }

            return args;
        }
    }
}
