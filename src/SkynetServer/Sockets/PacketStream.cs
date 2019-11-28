using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;

namespace SkynetServer.Sockets
{
    internal class PacketStream
    {
        private readonly Stream stream;

        public PacketStream(Stream stream)
        {
            this.stream = stream;
        }

        public async ValueTask<(byte id, ReadOnlyMemory<byte> buffer)> ReadAsync()
        {
            byte[] buffer = new byte[4];
            await ReadInternal(buffer);
            int packetMeta = BitConverter.ToInt32(buffer);
            if (!BitConverter.IsLittleEndian)
                packetMeta = BinaryPrimitives.ReverseEndianness(packetMeta);

            byte id = (byte)(packetMeta & 0x000000FF);
            int length = packetMeta >> 8;

            buffer = new byte[length];
            await ReadInternal(buffer);

            return (id, buffer);
        }

        public async ValueTask WriteAsync(byte id, ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length > 0x00ffffff) throw new ArgumentOutOfRangeException(nameof(buffer));

            int packetMeta = buffer.Length << 8 | id;
            if (!BitConverter.IsLittleEndian)
                packetMeta = BinaryPrimitives.ReverseEndianness(packetMeta);

            await stream.WriteAsync(BitConverter.GetBytes(packetMeta));
            await stream.WriteAsync(buffer);
        }

        private async ValueTask ReadInternal(Memory<byte> buffer)
        {
            int read = 0;
            do
            {
                read += await stream.ReadAsync(buffer.Slice(read));
            } while (read < buffer.Length);
        }
    }
}
