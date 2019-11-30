using SkynetServer.Network.Attributes;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkynetServer.Network
{
    internal abstract class Packet
    {
        static Packet()
        {
            List<Packet> packets = new List<Packet>(capacity: byte.MaxValue);
            int max = 0;

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsSubclassOf(typeof(Packet))) continue;
                if (type == typeof(Packet) || type == typeof(ChannelMessage)) continue;

                PacketAttribute attribute = type.GetCustomAttribute<PacketAttribute>();
                Packet instance = (Packet)Activator.CreateInstance(type);
                instance.Id = attribute.PacketId;
                instance.Policies = attribute.PacketPoliies;

                if (instance is ChannelMessage message)
                {
                    object[] flags = type.GetCustomAttributes(typeof(MessageFlagsAttribute), inherit: true);
                    MessageFlagsAttribute messageFlags = (MessageFlagsAttribute)flags.FirstOrDefault(x => x.GetType() == typeof(MessageFlagsAttribute));
                    RequiredFlagsAttribute requiredFlags = (RequiredFlagsAttribute)flags.FirstOrDefault(x => x.GetType() == typeof(RequiredFlagsAttribute));
                    AllowedFlagsAttribute allowedFlags = (AllowedFlagsAttribute)flags.FirstOrDefault(x => x.GetType() == typeof(AllowedFlagsAttribute));
                    if (messageFlags != null)
                    {
                        message.RequiredFlags = messageFlags.Flags;
                        message.AllowedFlags = messageFlags.Flags;
                    }
                    else
                    {
                        if (requiredFlags != null) message.RequiredFlags = requiredFlags.Flags;
                        if (allowedFlags != null) message.AllowedFlags = allowedFlags.Flags;
                    }
                }

                packets.Add(instance);
                max = Math.Max(max, attribute.PacketId);
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
            foreach (Packet packet in Packets)
            {
                if (packet is T prototype)
                {
                    T instance = (T)prototype.Create();
                    return instance;
                }
            }

            throw new ArgumentException($"Unknown packet type {typeof(T).Name}");
        }

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
