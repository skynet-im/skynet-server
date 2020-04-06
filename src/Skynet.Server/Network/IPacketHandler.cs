using Skynet.Protocol;
using Skynet.Server.Database;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Network
{
    interface IPacketHandler
    {
        void Init(IClient client, DatabaseContext database, PacketService packets, DeliveryService delivery);
        ValueTask Handle(Packet packet);
    }
}
