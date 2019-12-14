using SkynetServer.Network;
using SkynetServer.Network.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkynetServer.Services
{
    internal class PacketService
    {
        public PacketService()
        {
            Packets = MapPackets();
            Handlers = MapHandlers(Packets);
        }

        public Packet[] Packets { get; }
        public Type[] Handlers { get; }

        public T New<T>() where T : Packet
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

        private static Packet[] MapPackets()
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
                    var messageFlags = (MessageFlagsAttribute)flags.FirstOrDefault(x => x.GetType() == typeof(MessageFlagsAttribute));
         
                    var requiredFlags = (RequiredFlagsAttribute)flags.FirstOrDefault(x => x.GetType() == typeof(RequiredFlagsAttribute));
                    var allowedFlags = (AllowedFlagsAttribute)flags.FirstOrDefault(x => x.GetType() == typeof(AllowedFlagsAttribute));
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

            var result = new Packet[max + 1];

            foreach (Packet packet in packets)
            {
                result[packet.Id] = packet;
            }

            return result;
        }

        private static Type[] MapHandlers(Packet[] packets)
        {
            Type[] result = new Type[packets.Length];

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsSubclassOf(typeof(PacketHandler<>))) continue;
                
                Type packetType = type.GetGenericArguments().Single();
                bool found = false;
                
                for (int i = 0; i < packets.Length; i++)
                {
                    if (packets[i]?.GetType() == packetType)
                    {
                        result[i] = type;
                        found = true;
                        break;
                    }
                }

                if (!found) 
                    throw new ArgumentException($"Could not find a matching packet for handler {type.Name}");
            }

            return result;
        }
    }
}
