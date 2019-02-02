using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network
{
    internal abstract class Packet
    {
        static Packet()
        {
            List<Packet> packets = new List<Packet>();
            int max = 0;

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                object[] attributes = type.GetCustomAttributes(typeof(PacketAttribute), false);
                if (attributes.Length > 0 && attributes[0] is PacketAttribute attribute)
                {
                    Packet instance = (Packet)Activator.CreateInstance(type);
                    instance.Id = attribute.PacketId;
                    instance.Policy = attribute.PacketPolicy;
                    packets.Add(instance);
                    if (attribute.PacketId > max) max = attribute.PacketId;
                }
            }

            Packets = new Packet[max + 1];

            foreach (Packet packet in packets)
            {
                Packets[packet.Id] = packet;
            }
        }

        public static Packet[] Packets { get; }

        public static T New<T>() where T : Packet
        {
            return Packets.Where(p => p is T).Select(packet => (T) packet.Create()).FirstOrDefault();
        }

        public byte Id { get; set; }
        public PacketPolicy Policy { get; set; }

        public abstract Packet Create();
        public abstract Task Handle(IPacketHandler handler);
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
