﻿using Microsoft.Extensions.Options;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
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
            var response = Packets.New<P01ConnectionResponse>();
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

            await Client.Send(response).ConfigureAwait(false);
        }
    }
}
