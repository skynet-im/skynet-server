using SkynetServer.Database;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal abstract class PacketHandler<T> : IPacketHandler where T : Packet
    {
        protected Client Client { get; private set; }
        protected DatabaseContext Database { get; private set; }
        protected PacketService Packets { get; private set; }
        protected DeliveryService Delivery { get; private set; }

        public void Init(Client client, DatabaseContext database, PacketService packets, DeliveryService delivery)
        {
            Client = client;
            Database = database;
            Packets = packets;
            Delivery = delivery;
        }

        public ValueTask Handle(Packet packet)
        {
            return Handle((T)packet);
        }
        public abstract ValueTask Handle(T packet);
    }
}
