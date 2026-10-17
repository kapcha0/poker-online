using System;

namespace PokerCore
{
    /// <summary>Игральная карта: масть + достоинство. Неизменяемая структура.</summary>
    public readonly struct Card : IEquatable<Card>, IComparable<Card>
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>Текстовое представление, например "As", "Td", "2c".</summary>
        public override string ToString() => RankSymbol(Rank) + SuitSymbol(Suit);

        public bool Equals(Card other) => Suit == other.Suit && Rank == other.Rank;

        public override bool Equals(object? obj) => obj is Card other && Equals(other);

        public override int GetHashCode() => ((int)Suit << 8) | (int)Rank;

        public int CompareTo(Card other)
        {
            int byRank = ((int)Rank).CompareTo((int)other.Rank);
            return byRank != 0 ? byRank : ((int)Suit).CompareTo((int)other.Suit);
        }

        public static bool operator ==(Card a, Card b) => a.Equals(b);

        public static bool operator !=(Card a, Card b) => !a.Equals(b);

        /// <summary>Разбор строки вида "As", "td", "10h", "2c".</summary>
        public static Card Parse(string text)
        {
            if (!TryParse(text, out Card card))
            {
                throw new FormatException($"Некорректная карта: '{text}'");
            }

            return card;
        }

        public static bool TryParse(string? text, out Card card)
        {
            card = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string s = text.Trim();
            if (!TryParseSuit(s[s.Length - 1], out Suit suit))
            {
                return false;
            }

            string rankPart = s.Substring(0, s.Length - 1);
            if (!TryParseRank(rankPart, out Rank rank))
            {
                return false;
            }

            card = new Card(suit, rank);
            return true;
        }

        private static bool TryParseSuit(char c, out Suit suit)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'c': suit = Suit.Clubs; return true;
                case 'd': suit = Suit.Diamonds; return true;
                case 'h': suit = Suit.Hearts; return true;
                case 's': suit = Suit.Spades; return true;
                default: suit = default; return false;
            }
        }

        private static bool TryParseRank(string part, out Rank rank)
        {
            switch (part.ToUpperInvariant())
            {
                case "2": rank = Rank.Two; return true;
                case "3": rank = Rank.Three; return true;
                case "4": rank = Rank.Four; return true;
                case "5": rank = Rank.Five; return true;
                case "6": rank = Rank.Six; return true;
                case "7": rank = Rank.Seven; return true;
                case "8": rank = Rank.Eight; return true;
                case "9": rank = Rank.Nine; return true;
                case "10":
                case "T": rank = Rank.Ten; return true;
                case "J": rank = Rank.Jack; return true;
                case "Q": rank = Rank.Queen; return true;
                case "K": rank = Rank.King; return true;
                case "A": rank = Rank.Ace; return true;
                default: rank = default; return false;
            }
        }

        private static string RankSymbol(Rank rank)
        {
            switch (rank)
            {
                case Rank.Ten: return "T";
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                case Rank.Ace: return "A";
                default: return ((int)rank).ToString();
            }
        }

        private static string SuitSymbol(Suit suit)
        {
            switch (suit)
            {
                case Suit.Clubs: return "c";
                case Suit.Diamonds: return "d";
                case Suit.Hearts: return "h";
                default: return "s";
            }
        }
    }
}
