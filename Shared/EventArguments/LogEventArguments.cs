using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.Shared.EventArguments
{
    public class LogEventArguments : EventArgs
    {
        public string LogMessage { get; set; }

        public LogEventArguments(string logMessage)
        {
            this.LogMessage = logMessage;
        }
    }
}
