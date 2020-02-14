using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Sockets
{
    /// <summary>
    /// Provides a reading and writing wrapper to reassemble packets with their ID from a segmented stream.
    /// </summary>
    internal class PacketStream : IAsyncDisposable
    {
        private readonly Stream innerStream;
        private readonly bool leaveInnerStreamOpen;

        public PacketStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            this.innerStream = innerStream;
            this.leaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        /// <summary>
        /// Reads an entire packet from the underlying stream.
        /// </summary>
        /// <exception cref="IOException">Failed to read from the underlying stream.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="PacketStream"/> has been disposed.</exception>
        public async ValueTask<(byte id, ReadOnlyMemory<byte> buffer)> ReadAsync(CancellationToken ct = default)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(PacketStream));

            byte[] buffer = new byte[4];
            await ReadInternal(buffer, ct).ConfigureAwait(false);
            int packetMeta = BitConverter.ToInt32(buffer);
            if (!BitConverter.IsLittleEndian)
                packetMeta = BinaryPrimitives.ReverseEndianness(packetMeta);

            byte id = (byte)(packetMeta & 0x000000FF);
            int length = packetMeta >> 8;

            buffer = new byte[length];
            await ReadInternal(buffer, ct).ConfigureAwait(false);

            return (id, buffer);
        }

        /// <summary>
        /// Writes a packet to the underlying stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The packet payload is larger than 0x00ffffff bytes.</exception>
        /// <exception cref="IOException">Failed to write on the underlying stream.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="PacketStream"/> has been disposed.</exception>
        public ValueTask WriteAsync(IPacket packet, CancellationToken ct = default)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (disposedValue) throw new ObjectDisposedException(nameof(PacketStream));

            // Create buffer with four bytes alignment for packet meta
            var buffer = new PacketBuffer { Position = sizeof(int) };
            packet.WritePacket(buffer);

            int length = buffer.Position - sizeof(int);
            if (length > 0x00ffffff) throw new ArgumentOutOfRangeException(nameof(packet), "The packet payload is too large");

            buffer.Position = 0;
            buffer.WriteInt32(length << 8 | packet.Id);
            buffer.Position = length + sizeof(int);

            return innerStream.WriteAsync(buffer.GetBuffer(), ct);
        }

        private async ValueTask ReadInternal(Memory<byte> buffer, CancellationToken ct)
        {
            int read = 0;
            do
            {
                read += await innerStream.ReadAsync(buffer.Slice(read), ct);
            } while (read < buffer.Length);
        }

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                if (!leaveInnerStreamOpen)
                    await innerStream.DisposeAsync().ConfigureAwait(false);

                disposedValue = true;
            }
        }
        #endregion
    }
}
