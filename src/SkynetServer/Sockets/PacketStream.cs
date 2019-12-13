using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Sockets
{
    internal class PacketStream : IAsyncDisposable
    {
        private readonly Stream innerStream;
        private readonly bool leaveInnerStreamOpen;

        public PacketStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            this.innerStream = innerStream;
            this.leaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        public async ValueTask<(byte id, ReadOnlyMemory<byte> buffer)> ReadAsync(CancellationToken ct = default)
        {
            byte[] buffer = new byte[4];
            await ReadInternal(buffer, ct);
            int packetMeta = BitConverter.ToInt32(buffer);
            if (!BitConverter.IsLittleEndian)
                packetMeta = BinaryPrimitives.ReverseEndianness(packetMeta);

            byte id = (byte)(packetMeta & 0x000000FF);
            int length = packetMeta >> 8;

            buffer = new byte[length];
            await ReadInternal(buffer, ct);

            return (id, buffer);
        }

        public async ValueTask WriteAsync(byte id, ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            if (buffer.Length > 0x00ffffff) throw new ArgumentOutOfRangeException(nameof(buffer));

            int packetMeta = buffer.Length << 8 | id;
            if (!BitConverter.IsLittleEndian)
                packetMeta = BinaryPrimitives.ReverseEndianness(packetMeta);

            Memory<byte> sendBuffer = new byte[sizeof(int) + buffer.Length];
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(sendBuffer.Span), packetMeta);
            buffer.CopyTo(sendBuffer.Slice(sizeof(int)));
            await innerStream.WriteAsync(sendBuffer, ct);
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
