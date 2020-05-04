using Skynet.Model;
using Skynet.Protocol;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    /// <summary>
    /// Provides an asynchronous stream of a necessary packets to create a direct channel.
    /// It waits until the channel has been created before emitting the first packet.
    /// </summary>
    internal class CreateChannelEnumerable : IAsyncEnumerable<Packet>
    {
        private readonly PacketService packets;
        private readonly Task<Channel> channelCreation;
        private readonly long otherId;
        private readonly long otherChannelId;
        private readonly long tempChannelId;

        /// <summary>
        /// Creates a new asynchronous stream of a necessary packets to create a direct channel which waits for channel creation.
        /// </summary>
        /// <param name="packets">An instance of packet factory.</param>
        /// <param name="channelCreation">A <see cref="Task{TResult}"/> representing a database operation which will be awaited before emitting the first packet.</param>
        /// <param name="otherId">Bob's account ID which will be shared with Alice.</param>
        /// <param name="otherChannelId">Bob's account data channel ID which will be shared with Alice.</param>
        /// <param name="tempChannelId">If specified the stream returns a <see cref="P2FCreateChannelResponse"/> packet instead of <see cref="P0ACreateChannel"/>.</param>
        public CreateChannelEnumerable(PacketService packets, Task<Channel> channelCreation, long otherId, long otherChannelId, long tempChannelId = default)
        {
            this.packets = packets;
            this.channelCreation = channelCreation;
            this.otherId = otherId;
            this.otherChannelId = otherChannelId;
            this.tempChannelId = tempChannelId;
        }

        public IAsyncEnumerator<Packet> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(this);
        }

        private struct Enumerator : IAsyncEnumerator<Packet>
        {
            private readonly CreateChannelEnumerable enumerable;
            private int index;
            private Channel channel;

            public Enumerator(CreateChannelEnumerable enumerable)
            {
                this.enumerable = enumerable;
                index = 0;
                channel = null;
                Current = null;
            }

            public Packet Current { get; private set; }

            public ValueTask DisposeAsync() => default;

            public async ValueTask<bool> MoveNextAsync()
            {
                switch (index)
                {
                    case 0:
                        channel = await enumerable.channelCreation.ConfigureAwait(false);

                        if (channel == null)
                        {
                            if (enumerable.tempChannelId != default)
                            {
                                var response = enumerable.packets.New<P2FCreateChannelResponse>();
                                response.TempChannelId = enumerable.tempChannelId;
                                response.StatusCode = CreateChannelStatus.AlreadyExists;
                                Current = response;
                                index = -1;
                                return true;
                            }
                            else
                            {
                                index = -1;
                                return false;
                            }
                        }
                        else
                        {
                            var createAccountData = enumerable.packets.New<P0ACreateChannel>();
                            createAccountData.ChannelId = enumerable.otherChannelId;
                            createAccountData.ChannelType = ChannelType.AccountData;
                            createAccountData.OwnerId = enumerable.otherId;
                            Current = createAccountData;
                            index++;
                            return true;
                        }

                    case 1:
                        if (enumerable.tempChannelId != default)
                        {
                            var response = enumerable.packets.New<P2FCreateChannelResponse>();
                            response.TempChannelId = enumerable.tempChannelId;
                            response.StatusCode = CreateChannelStatus.Success;
                            response.ChannelId = channel.ChannelId;
                            Current = response;
                        }
                        else
                        {
                            var create = enumerable.packets.New<P0ACreateChannel>();
                            create.ChannelId = channel.ChannelId;
                            create.ChannelType = ChannelType.Direct;
                            create.OwnerId = channel.OwnerId.Value;
                            create.CounterpartId = enumerable.otherId;
                            Current = create;
                        }
                        index++;
                        return true;

                    default:
                        return false;
                }
            }
        }
    }
}
