using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    internal class ProtocolException : Exception
    {
        public ProtocolException() : this("Client violation of Skynet Protocol rules") { }

        public ProtocolException(string message) : base(message) { }
    }
}
