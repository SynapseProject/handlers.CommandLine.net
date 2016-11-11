using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.CommandLine.Handler
{
    public enum TimeoutActionType
    {
        Continue,
        Error,
        KillProcessAndContinue,
        KillProcessAndError
    }
}
