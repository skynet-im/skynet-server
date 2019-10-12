using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SkynetServer.Sockets
{
    internal sealed class PacketBuffer
    {
        private const int DefaultCapacity = 65536;
        private static readonly Encoding encoding = Encoding.UTF8;

        private Memory<byte> buffer;
        private int position;

        public PacketBuffer() : this(DefaultCapacity) { }

        public PacketBuffer(int capacity)
        {
            buffer = new byte[capacity];
        }

        public PacketBuffer(Memory<byte> buffer)
        {
            this.buffer = buffer;
        }

        public int Capacity => buffer.Length;

        public int Position
        {
            get => position;
            set
            {
                if (value < 0 || value >= Capacity) throw new ArgumentOutOfRangeException(nameof(value));
                position = value;
            }
        }

        public Memory<byte> GetRawBuffer() => buffer;
        public Memory<byte> GetBuffer() => buffer.Slice(0, Position);

        private void EnsureSpace(int length)
        {
            if (Position + length > Capacity)
            {
                int newSize = Math.Max(buffer.Length * 2, Position + length);
                byte[] newBuffer = new byte[newSize];
                buffer.CopyTo(newBuffer);
                buffer = newBuffer;
            }
        }

        public ReadOnlyMemory<byte> ReadRawByteArray(int count)
        {
            if (Position + count > Capacity) throw new EndOfStreamException();
            ReadOnlyMemory<byte> value = buffer.Slice(Position, count);
            position += count;
            return value;
        }
        public void WriteRawByteArray(ReadOnlySpan<byte> array)
        {
            EnsureSpace(array.Length);
            array.CopyTo(buffer.Slice(Position).Span);
            position += array.Length;
        }

        #region primitives
        public bool ReadBoolean()
        {
            if (Position + sizeof(byte) > Capacity) throw new EndOfStreamException();
            bool value = buffer.Span[Position] != 0;
            position++;
            return value;
        }
        public void WriteBoolean(bool value)
        {
            EnsureSpace(sizeof(byte));
            buffer.Span[Position] = value ? (byte)1 : (byte)0;
            position++;
        }

        public byte ReadByte()
        {
            if (Position + sizeof(byte) > Capacity) throw new EndOfStreamException();
            byte value = buffer.Span[Position];
            position += sizeof(byte);
            return value;
        }
        public void WriteByte(byte value)
        {
            EnsureSpace(sizeof(byte));
            buffer.Span[Position] = value;
            position += sizeof(byte);
        }
        
        public ushort ReadUInt16()
        {
            if (Position + sizeof(ushort) > Capacity) throw new EndOfStreamException();
            ushort value = Unsafe.ReadUnaligned<ushort>(ref buffer.Span[Position]);
            position += sizeof(ushort);
            return value;
        }
        public void WriteUInt16(ushort value)
        {
            EnsureSpace(sizeof(ushort));
            Unsafe.WriteUnaligned(ref buffer.Span[Position], value);
            position += sizeof(ushort);
        }

        public int ReadInt32()
        {
            if (Position + sizeof(int) > Capacity) throw new EndOfStreamException();
            int value = Unsafe.ReadUnaligned<int>(ref buffer.Span[Position]);
            position += sizeof(int);
            return value;
        }
        public void WriteInt32(int value)
        {
            EnsureSpace(sizeof(int));
            Unsafe.WriteUnaligned(ref buffer.Span[Position], value);
            position += sizeof(int);
        }

        public long ReadInt64()
        {
            if (Position + sizeof(long) > Capacity) throw new EndOfStreamException();
            long value = Unsafe.ReadUnaligned<long>(ref buffer.Span[Position]);
            position += sizeof(long);
            return value;
        }
        public void WriteInt64(long value)
        {
            EnsureSpace(sizeof(long));
            Unsafe.WriteUnaligned(ref buffer.Span[Position], value);
            position += sizeof(long);
        }
        #endregion

        #region arrays
        public ReadOnlyMemory<byte> ReadShortByteArray()
        {
            if (Position + sizeof(byte) > Capacity) throw new EndOfStreamException();
            int length = buffer.Span[Position];
            if (Position + sizeof(byte) + length > Capacity) throw new EndOfStreamException();
            ReadOnlyMemory<byte> value = buffer.Slice(Position + sizeof(byte), length);
            position += sizeof(byte) + length;
            return value;
        }
        public void WriteShortByteArray(ReadOnlySpan<byte> array)
        {
            if (array.Length > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(array));

            Span<byte> buffer = stackalloc byte[sizeof(byte) + array.Length];
            buffer[0] = (byte)array.Length;
            array.CopyTo(buffer.Slice(sizeof(byte)));
            WriteRawByteArray(buffer);
        }

        public ReadOnlyMemory<byte> ReadByteArray()
        {
            if (Position + sizeof(ushort) > Capacity) throw new EndOfStreamException();
            int length = Unsafe.ReadUnaligned<ushort>(ref buffer.Span[Position]);
            if (Position + sizeof(ushort) + length > Capacity) throw new EndOfStreamException();
            ReadOnlyMemory<byte> value = buffer.Slice(Position + sizeof(ushort), length);
            position += sizeof(ushort) + length;
            return value;
        }
        public void WriteByteArray(ReadOnlySpan<byte> array)
        {
            if (array.Length > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(array));

            Span<byte> buffer = stackalloc byte[sizeof(ushort) + array.Length];
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), (ushort)array.Length);
            array.CopyTo(buffer.Slice(sizeof(ushort)));
            WriteRawByteArray(buffer);
        }

        public ReadOnlyMemory<byte> ReadLongByteArray()
        {
            if (Position + sizeof(int) > Capacity) throw new EndOfStreamException();
            int length = Unsafe.ReadUnaligned<int>(ref buffer.Span[Position]);
            if (length < 0) throw new InvalidDataException();
            if (Position + sizeof(int) + length > Capacity) throw new EndOfStreamException();
            ReadOnlyMemory<byte> value = buffer.Slice(Position + sizeof(int), length);
            position += sizeof(int) + length;
            return value;
        }
        public void WriteLongByteArray(ReadOnlySpan<byte> array)
        {
            if (array.Length > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(array));

            Span<byte> buffer = stackalloc byte[sizeof(int) + array.Length];
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), array.Length);
            array.CopyTo(buffer.Slice(sizeof(int)));
            WriteRawByteArray(buffer);
        }
        #endregion
        #region strings
        // TODO: Implement strings
        #endregion
    }
}
