using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal abstract class PacketHandler<T> where T : Packet
    {
        protected Client Client { get; private set; }

        public ValueTask Handle(Packet packet)
        {
            return Handle((T)packet);
        }
        public abstract ValueTask Handle(T packet);
    }
}
