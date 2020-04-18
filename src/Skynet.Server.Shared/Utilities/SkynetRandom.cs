using System;
using System.Security.Cryptography;
using Wiry.Base32;

namespace Skynet.Server.Utilities
{
    public static class SkynetRandom
    {
        public static long Id()
        {
            long result;
            Span<byte> value = stackalloc byte[8];
            do
            {
                RandomNumberGenerator.Fill(value);
                result = BitConverter.ToInt64(value);
            } while (result == 0);
            return result;
        }

        public static byte[] Bytes(int length)
        {
            byte[] value = new byte[length];
            RandomNumberGenerator.Fill(value);
            return value;
        }

        public static string String(int length)
        {
            byte[] value = new byte[length];
            RandomNumberGenerator.Fill(value);
            return Base32Encoding.Standard.GetString(value).ToLowerInvariant();
        }
    }
}
