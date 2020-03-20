using SkynetServer.Sockets;
using System;

namespace SkynetServer.Network
{
    internal abstract class Packet : IPacket
    {
        public byte Id { get; set; }
        public PacketPolicies Policies { get; set; }

        public abstract Packet Create();

        public virtual void ReadPacket(PacketBuffer buffer)
        {
            if (!Policies.HasFlag(PacketPolicies.Receive))
                throw new InvalidOperationException();
        }

        public virtual void WritePacket(PacketBuffer buffer)
        {
            if (!Policies.HasFlag(PacketPolicies.Send))
                throw new InvalidOperationException();
        }

        protected Packet Init(Packet source)
        {
            Id = source.Id;
            Policies = source.Policies;
            return this;
        }

        public override string ToString()
        {
            return $"{{{GetType().Name}}}";
        }
    }
}
