using System;

namespace MBTiles
{
    public class StatusMessageEventArgs : EventArgs
    {
        public StatusMessageEventArgs(string message) :
            base()
        {
            Status = message;
        }
        public string Status
        {
            get; set;
        }
    }
}
