using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Skynet.Server.Network
{
    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Not accessible to other assemblies.")]
    internal class ProtocolException : Exception
    {
        public ProtocolException() : this("Client violation of Skynet Protocol rules") { }

        public ProtocolException(string message) : base(message) { }

        public ProtocolException(string message, Exception innerException) : base(message, innerException) { }
    }
}
