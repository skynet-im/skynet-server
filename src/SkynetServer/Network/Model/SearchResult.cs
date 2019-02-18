using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    internal struct SearchResult
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }
        public List<(byte PacketId, byte[] PacketContent)> ForwardedPackets { get; set; }

        public SearchResult(long accountId, string accountName)
        {
            AccountId = accountId;
            AccountName = accountName;
            ForwardedPackets = new List<(byte PacketId, byte[] PacketContent)>();
        }
    }
}
