using System;
using System.Numerics;

namespace Mosquito.Core
{
    // Version 1: xorshift64*, shifts 12/25/27, multiplier 2685821657736338717.
    // Visuals never use this stream. State is persisted BEFORE the next draw.
    public sealed class DeterministicRandom
    {
        public ulong State { get; private set; }
        public DeterministicRandom(ulong state) { State = state == 0 ? 0x9E3779B97F4A7C15UL : state; }
        public ulong NextUInt64()
        {
            var x = State;
            x ^= x >> 12; x ^= x << 25; x ^= x >> 27;
            State = x;
            return unchecked(x * 2685821657736338717UL);
        }
        public bool NextBool() => (NextUInt64() & 1UL) != 0;

        public BigInteger Below(BigInteger exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            if (exclusiveMax == 1) return BigInteger.Zero;
            byte[] top = (exclusiveMax - 1).ToByteArray();
            int length = top.Length;
            while (length > 1 && top[length - 1] == 0) length--;
            int mask = 1;
            while (mask < top[length - 1]) mask = (mask << 1) | 1;
            // Extra zero byte keeps BigInteger positive on every framework.
            byte[] sample = new byte[length + 1];
            while (true)
            {
                for (int i = 0; i < length;)
                {
                    ulong bits = NextUInt64();
                    for (int b = 0; b < 8 && i < length; b++, i++) { sample[i] = (byte)bits; bits >>= 8; }
                }
                sample[length - 1] &= (byte)mask;
                var value = new BigInteger(sample);
                if (value < exclusiveMax) return value;
            }
        }
    }
}
