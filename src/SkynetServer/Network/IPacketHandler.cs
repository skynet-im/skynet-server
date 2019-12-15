using SkynetServer.Database;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    interface IPacketHandler
    {
        void Init(Client client, DatabaseContext database, PacketService packets, DeliveryService delivery);
        ValueTask Handle(Packet packet);
    }
}
