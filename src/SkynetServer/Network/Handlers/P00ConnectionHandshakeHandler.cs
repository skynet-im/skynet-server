using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P00ConnectionHandshakeHandler : PacketHandler<P00ConnectionHandshake>
    {
        private readonly IOptions<ProtocolOptions> options;

        public P00ConnectionHandshakeHandler(IOptions<ProtocolOptions> options)
        {
            this.options = options;
        }

        public override async ValueTask Handle(P00ConnectionHandshake packet)
        {
            ProtocolOptions config = options.Value;
            var response = Packet.New<P01ConnectionResponse>();
            ProtocolOptions.Platform platform = config.Platforms.SingleOrDefault(p => p.Name == packet.ApplicationIdentifier);
            if (platform == null)
                throw new ProtocolException($"Unsupported client {packet.ApplicationIdentifier}");
            response.LatestVersion = platform.VersionName;
            response.LatestVersionCode = platform.VersionCode;
            if (packet.ProtocolVersion != config.ProtocolVersion || packet.VersionCode < platform.ForceUpdateThreshold)
                response.ConnectionState = ConnectionState.MustUpgrade;
            else if (packet.VersionCode < platform.RecommendUpdateThreshold)
                response.ConnectionState = ConnectionState.CanUpgrade;
            else
                response.ConnectionState = ConnectionState.Valid;

            Client.Initialize(packet.ApplicationIdentifier, packet.VersionCode);

            await Client.SendPacket(response);
        }
    }
}
