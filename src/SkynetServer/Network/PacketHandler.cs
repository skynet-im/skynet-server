using SkynetServer.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal abstract class PacketHandler<T> : IAsyncDisposable where T : Packet
    {
        protected Client Client { get; private set; }
        protected DatabaseContext Database { get; private set; }

        public void Init(Client client, DatabaseContext database)
        {
            Client = client;
            Database = database;
        }

        public ValueTask Handle(Packet packet)
        {
            return Handle((T)packet);
        }
        public abstract ValueTask Handle(T packet);

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                await Database.DisposeAsync().ConfigureAwait(false);

                disposedValue = true;
            }
        }
        #endregion
    }
}
