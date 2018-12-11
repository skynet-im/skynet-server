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

            foreach ((Type type, PacketAttribute attribute) packet in packets)
            {
                Packet instance = (Packet)packet.type.GetConstructor(new Type[0]).Invoke(new object[0]);
                instance.PacketId = packet.attribute.PacketId;
                instance.PacketPolicy = packet.attribute.PacketPolicy;
                Packets[instance.PacketId] = instance;
            }
        }

        public static Packet[] Packets { get; }

        public byte PacketId { get; set; }
        public PacketPolicy PacketPolicy { get; set; }
        public abstract void ReadPacket(PacketBuffer buffer);
        public abstract void WritePacket(PacketBuffer buffer);
        public abstract Packet Create();

        protected Packet Init(Packet source)
        {
            PacketId = source.PacketId;
            PacketPolicy = source.PacketPolicy;
            return this;
        }
    }
}
