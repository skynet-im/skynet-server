using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VSL;

namespace SkynetServer.Packets
{
    internal abstract class Packet
    {
        static Packet()
        {
            IEnumerable<(Type type, PacketAttribute attribute)> packets = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetCustomAttributes(typeof(PacketAttribute), false).Length > 0)
                .Select(x => (x, x.GetCustomAttribute<PacketAttribute>()));

            int max = packets.Max(x => x.attribute.PacketId);

            Packets = new Packet[max + 1];

            foreach ((Type type, PacketAttribute attribute) in packets)
            {
                Packet instance = (Packet)Activator.CreateInstance(type);
                instance.Id = attribute.PacketId;
                instance.Policy = attribute.PacketPolicy;
                Packets[instance.Id] = instance;
            }
        }

        public static Packet[] Packets { get; }

        public byte Id { get; set; }
        public PacketPolicy Policy { get; set; }

        public abstract Packet Create();
        public abstract void Handle(IPacketHandler handler);
        public abstract void ReadPacket(PacketBuffer buffer);
        public abstract void WritePacket(PacketBuffer buffer);

        protected Packet Init(Packet source)
        {
            Id = source.Id;
            Policy = source.Policy;
            return this;
        }
    }
}
