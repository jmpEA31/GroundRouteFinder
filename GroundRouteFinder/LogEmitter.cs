using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class LogEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public LogEventArgs(string message)
        {
            Message = message;
        }
    }

    public class LogEmitter
    {
        public event EventHandler<LogEventArgs> LogMessage;

        public LogEmitter()
        {

        }

        protected void RelayMessage(object sender, LogEventArgs e)
        {
            LogMessage?.Invoke(this, e);
        }

        protected void Log(string message)
        {
            LogMessage?.Invoke(this, new LogEventArgs(message));
        }
    }
}
