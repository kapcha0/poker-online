using System;
using System.Security.Cryptography;

namespace PokerCore
{
    /// <summary>Источник случайности для тасования колоды.</summary>
    public interface IRandomSource
    {
        /// <summary>Возвращает случайное целое в диапазоне [0, exclusiveMax).</summary>
        int Next(int exclusiveMax);
    }

    /// <summary>Криптостойкий источник случайности (по умолчанию).</summary>
    public sealed class CryptoRandomSource : IRandomSource
    {
        private readonly RandomNumberGenerator _rng;

        public CryptoRandomSource()
        {
            _rng = RandomNumberGenerator.Create();
        }

        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            // Отбраковка выборки, чтобы избежать смещения по модулю.
            uint range = (uint)exclusiveMax;
            uint limit = uint.MaxValue - (uint.MaxValue % range);
            byte[] buffer = new byte[4];
            while (true)
            {
                _rng.GetBytes(buffer);
                uint value = BitConverter.ToUInt32(buffer, 0);
                if (value < limit)
                {
                    return (int)(value % range);
                }
            }
        }
    }

    /// <summary>Детерминированный источник (только для тестов/воспроизводимости).</summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int exclusiveMax) => _random.Next(exclusiveMax);
    }
}
