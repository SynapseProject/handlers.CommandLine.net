﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

using Synapse.Core;

using YamlDotNet.Serialization;

using Synapse.Core.Utilities;

namespace Synapse.Handlers.CommandLine
{
    public partial class HandlerConfig
    {
        [XmlElement]
        public String RunOn { get; set; }
        [XmlElement]
        public String WorkingDirectory { get; set; }
        [XmlElement]
        public String Command { get; set; }
        [XmlElement]
        public long TimeoutMills { get; set; }
        [XmlElement]
        public StatusType TimeoutStatus { get; set; }
        [XmlElement]
        public bool KillRemoteProcessOnTimeout { get; set; }
        [XmlArrayItem(ElementName = "ExitCode")]
        public List<String> ValidExitCodes { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(RunOn)) { sb.AppendLine("RunOn            : " + RunOn); }
            if (!String.IsNullOrWhiteSpace(WorkingDirectory)) { sb.AppendLine("WorkingDirectory : " + WorkingDirectory); }
            if (!String.IsNullOrWhiteSpace(Command)) { sb.AppendLine("Command          : " + Command); }
            sb.AppendLine("TimeoutMills     : " + TimeoutMills);
            sb.AppendLine("TimeoutStatus    : " + TimeoutStatus);
            sb.AppendLine("KillOnTimeout    : " + KillRemoteProcessOnTimeout);
            if (ValidExitCodes!= null)
            {
                sb.Append("ValidExitCodes   : ");
                foreach (String code in ValidExitCodes)
                    sb.Append("[" + code + "] ");
                sb.AppendLine("");
            }

            return sb.ToString();
        }

    }

}
