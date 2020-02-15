using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SkynetServer.Configuration
{
    public class FcmOptions
    {
        public bool NotifyForEveryMessage { get; set; }

        public bool DeleteSessionOnError { get; set; }

        [Range(0, 1800000)]
        public int PriorityMessageAckTimeout { get; set; }
    }
}
