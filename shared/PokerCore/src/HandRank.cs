using System;
using System.Collections.Generic;

namespace PokerCore
{
    /// <summary>
    /// Оценка покерной комбинации. Сравнение — сначала по типу, затем по тайбрейкерам
    /// (достоинства карт в порядке значимости).
    /// </summary>
    public sealed class HandRank : IComparable<HandRank>, IEquatable<HandRank>
    {
        public HandType Type { get; }

        /// <summary>Тайбрейкеры в порядке значимости (например для Full House: [трипс, пара]).</summary>
        public IReadOnlyList<Rank> Kickers { get; }

        /// <summary>Пять карт, образующих комбинацию.</summary>
        public IReadOnlyList<Card> Cards { get; }

        public HandRank(HandType type, IReadOnlyList<Rank> kickers, IReadOnlyList<Card> cards)
        {
            Type = type;
            Kickers = kickers;
            Cards = cards;
        }

        public int CompareTo(HandRank? other)
        {
            if (other == null)
            {
                return 1;
            }

            int byType = Type.CompareTo(other.Type);
            if (byType != 0)
            {
                return byType;
            }

            int count = Math.Min(Kickers.Count, other.Kickers.Count);
            for (int i = 0; i < count; i++)
            {
                int byKicker = Kickers[i].CompareTo(other.Kickers[i]);
                if (byKicker != 0)
                {
                    return byKicker;
                }
            }

            return 0;
        }

        public bool Equals(HandRank? other) => other != null && CompareTo(other) == 0;

        public override bool Equals(object? obj) => Equals(obj as HandRank);

        public override int GetHashCode()
        {
            int hash = (int)Type;
            foreach (Rank r in Kickers)
            {
                hash = (hash * 397) ^ (int)r;
            }

            return hash;
        }

        public static bool operator >(HandRank a, HandRank b) => a.CompareTo(b) > 0;

        public static bool operator <(HandRank a, HandRank b) => a.CompareTo(b) < 0;

        public static bool operator >=(HandRank a, HandRank b) => a.CompareTo(b) >= 0;

        public static bool operator <=(HandRank a, HandRank b) => a.CompareTo(b) <= 0;

        public override string ToString() => Type.ToString();
    }
}
