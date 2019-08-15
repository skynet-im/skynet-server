using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    public class FcmOptions
    {
        public bool NotifyAllDevices { get; set; }

        public bool NotifyForEveryMessage { get; set; }

        public bool DeleteSessionOnError { get; set; }
    }
}
