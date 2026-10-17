using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PokerCore
{
    /// <summary>
    /// Provably fair: перемешивание колоды детерминированной последовательностью,
    /// полученной из HMAC-SHA256(serverSeed, clientSeed:nonce:counter).
    /// Сервер публикует хэш serverSeed до раздачи и раскрывает seed после.
    /// </summary>
    public static class ProvablyFair
    {
        public static string HashServerSeed(string serverSeed)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(serverSeed ?? string.Empty));
                return ToHex(hash);
            }
        }

        /// <summary>Детерминированно тасует свежую колоду по seed'ам.</summary>
        public static Deck CreateShuffledDeck(string serverSeed, string clientSeed, long nonce)
        {
            var deck = new Deck();
            deck.Shuffle(new HmacRandomSource(serverSeed, clientSeed, nonce));
            return deck;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        /// <summary>Поток случайных чисел из цепочки HMAC-SHA256.</summary>
        private sealed class HmacRandomSource : IRandomSource
        {
            private readonly byte[] _key;
            private readonly byte[] _messageBase;
            private readonly List<byte> _buffer = new List<byte>();
            private int _counter;

            public HmacRandomSource(string serverSeed, string clientSeed, long nonce)
            {
                _key = Encoding.UTF8.GetBytes(serverSeed ?? string.Empty);
                _messageBase = Encoding.UTF8.GetBytes((clientSeed ?? string.Empty) + ":" + nonce + ":");
            }

            public int Next(int exclusiveMax)
            {
                if (exclusiveMax <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
                }

                uint range = (uint)exclusiveMax;
                uint limit = uint.MaxValue - (uint.MaxValue % range);
                while (true)
                {
                    uint value = NextUInt32();
                    if (value < limit)
                    {
                        return (int)(value % range);
                    }
                }
            }

            private uint NextUInt32()
            {
                while (_buffer.Count < 4)
                {
                    byte[] block = NextBlock();
                    _buffer.AddRange(block);
                }

                uint value = (uint)(_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24));
                _buffer.RemoveRange(0, 4);
                return value;
            }

            private byte[] NextBlock()
            {
                using (var hmac = new HMACSHA256(_key))
                {
                    var message = new byte[_messageBase.Length + 4];
                    Array.Copy(_messageBase, message, _messageBase.Length);
                    message[_messageBase.Length] = (byte)(_counter & 0xFF);
                    message[_messageBase.Length + 1] = (byte)((_counter >> 8) & 0xFF);
                    message[_messageBase.Length + 2] = (byte)((_counter >> 16) & 0xFF);
                    message[_messageBase.Length + 3] = (byte)((_counter >> 24) & 0xFF);
                    _counter++;
                    return hmac.ComputeHash(message);
                }
            }
        }
    }
}
