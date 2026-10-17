using System;
using System.Collections.Generic;

namespace PokerCore
{
    /// <summary>Колода из 52 карт. Тасование — Фишер-Йетс через переданный источник случайности.</summary>
    public sealed class Deck
    {
        private readonly List<Card> _cards = new List<Card>(52);
        private int _index;

        public Deck()
        {
            Reset();
        }

        /// <summary>Полный набор 52 карт в фиксированном порядке (без тасования).</summary>
        public static List<Card> CreateFullDeck()
        {
            var list = new List<Card>(52);
            foreach (Suit suit in new[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades })
            {
                for (int r = (int)Rank.Two; r <= (int)Rank.Ace; r++)
                {
                    list.Add(new Card(suit, (Rank)r));
                }
            }

            return list;
        }

        public void Reset()
        {
            _cards.Clear();
            _cards.AddRange(CreateFullDeck());
            _index = 0;
        }

        /// <summary>Криптостойкое тасование (Фишер-Йетс).</summary>
        public void Shuffle()
        {
            Shuffle(new CryptoRandomSource());
        }

        public void Shuffle(IRandomSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = source.Next(i + 1);
                Card tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }

            _index = 0;
        }

        public int Remaining => _cards.Count - _index;

        public Card Draw()
        {
            if (_index >= _cards.Count)
            {
                throw new InvalidOperationException("Колода исчерпана.");
            }

            return _cards[_index++];
        }

        /// <summary>Раздать n карт.</summary>
        public List<Card> Draw(int count)
        {
            var result = new List<Card>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(Draw());
            }

            return result;
        }

        /// <summary>Текущий порядок карт (для провери / отладки).</summary>
        public IReadOnlyList<Card> Snapshot => _cards;
    }
}
