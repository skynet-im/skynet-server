using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SkynetServer.Tests
{
    [TestClass]
    public class PacketBufferTests
    {
        [TestMethod]
        public void TestReadBoundry()
        {
            var buffer = new PacketBuffer(new byte[16]);
            buffer.ReadInt64();
            buffer.ReadInt32();
            buffer.ReadUInt16();
            buffer.ReadBoolean();
            Assert.ThrowsException<EndOfStreamException>(() => buffer.ReadInt32());
            Assert.ThrowsException<EndOfStreamException>(() => buffer.ReadByteArray());
            Assert.ThrowsException<EndOfStreamException>(() => buffer.ReadRawByteArray(13));
        }

        [TestMethod]
        public void TestWriteExpansion()
        {
            var buffer = new PacketBuffer(8);
            buffer.WriteInt32(-1);
            buffer.WriteBoolean(false);
            buffer.WriteByte(0xf3);
            buffer.WriteByteArray(stackalloc byte[10]);
        }

        [TestMethod]
        public void TestPrimitives()
        {
            var write = new PacketBuffer();
            write.WriteBoolean(true);
            write.WriteByte(0x7f);
            write.WriteUInt16(0x900a);
            write.WriteInt32(0x6502a14c);
            write.WriteInt64(0x1a41a174a64c91aa);

            var read = new PacketBuffer(write.GetBuffer());
            Assert.AreEqual(true, read.ReadBoolean());
            Assert.AreEqual(0x7f, read.ReadByte());
            Assert.AreEqual(0x900a, read.ReadUInt16());
            Assert.AreEqual(0x6502a14c, read.ReadInt32());
            Assert.AreEqual(0x1a41a174a64c91aa, read.ReadInt64());
        }

        [TestMethod]
        public void TestStructs()
        {
            DateTime date1 = default;
            DateTime date2 = DateTime.Now;
            Guid uuid1 = default;
            Guid uuid2 = Guid.NewGuid();

            var write = new PacketBuffer();
            write.WriteDateTime(date1);
            write.WriteDateTime(date2);
            write.WriteUuid(uuid1);
            write.WriteUuid(uuid2);

            var read = new PacketBuffer(write.GetBuffer());
            Assert.AreEqual(date1, read.ReadDateTime());
            Assert.AreEqual(date2, read.ReadDateTime());
            Assert.AreEqual(uuid1, read.ReadUuid());
            Assert.AreEqual(uuid2, read.ReadUuid());
        }

        [TestMethod]
        public void TestArrays()
        {
            byte[] random1 = new byte[128];
            byte[] random2 = new byte[3072];
            byte[] random3 = new byte[262144];
            Random gen = new Random();
            gen.NextBytes(random1);
            gen.NextBytes(random2);
            gen.NextBytes(random3);

            var write = new PacketBuffer();
            write.WriteShortByteArray(random1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => write.WriteShortByteArray(random2));
            write.WriteByteArray(random2);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => write.WriteByteArray(random3));
            write.WriteLongByteArray(random3);

            var read = new PacketBuffer(write.GetBuffer());
            Assert.IsTrue(new Span<byte>(random1).SequenceEqual(read.ReadShortByteArray().Span));
            Assert.IsTrue(new Span<byte>(random2).SequenceEqual(read.ReadByteArray().Span));
            Assert.IsTrue(new Span<byte>(random3).SequenceEqual(read.ReadLongByteArray().Span));
        }
    }
}
