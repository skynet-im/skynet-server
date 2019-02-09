using SkynetServer.Network.Packets;
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
                if (!type.IsSubclassOf(typeof(Packet))) continue;

                object[] attributes = type.GetCustomAttributes(typeof(PacketAttribute), inherit: true);
                if (attributes.Length == 1)
                {
                    PacketAttribute attribute = (PacketAttribute)attributes[0];
                    Packet instance = (Packet)Activator.CreateInstance(type);
                    instance.Id = attribute.PacketId;
                    instance.Policy = attribute.PacketPolicy;
                    packets.Add(instance);
                    max = Math.Max(max, attribute.PacketId);
                }
                else if (attributes.Length == 2)
                {
                    MessageAttribute messageAttribute = (MessageAttribute)attributes[0];
                    PacketAttribute packetAttribute = (PacketAttribute)attributes[1];
                    P0BChannelMessage instance = (P0BChannelMessage)Activator.CreateInstance(type);
                    instance.Id = packetAttribute.PacketId;
                    instance.Policy = packetAttribute.PacketPolicy;
                    instance.ContentPacketId = messageAttribute.PacketId;
                    instance.ContentPacketPolicy = messageAttribute.PacketPolicy;
                    packets.Add(instance);
                    max = Math.Max(max, messageAttribute.PacketId);
                }
            }

            Packets = new Packet[max + 1];

            foreach (Packet packet in packets)
            {
                int index = packet is P0BChannelMessage message && message.GetType() != typeof(P0BChannelMessage) ?
                    message.ContentPacketId : packet.Id;

                Packets[index] = packet;
            }
        }

        public static Packet[] Packets { get; }

        public static T New<T>() where T : Packet
        {
            return Packets.Where(p => p is T).Select(packet => (T)packet.Create()).FirstOrDefault();
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
