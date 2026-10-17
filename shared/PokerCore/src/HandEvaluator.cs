using System;
using System.Collections.Generic;

namespace PokerCore
{
    /// <summary>Оценщик покерных комбинаций (Техасский Холдем, до 7 карт).</summary>
    public static class HandEvaluator
    {
        /// <summary>Оценка ровно 5 карт.</summary>
        public static HandRank EvaluateFive(IReadOnlyList<Card> cards)
        {
            if (cards == null || cards.Count != 5)
            {
                throw new ArgumentException("Ожидается ровно 5 карт.", nameof(cards));
            }

            var sorted = new List<Card>(cards);
            sorted.Sort((a, b) => ((int)b.Rank).CompareTo((int)a.Rank));

            bool isFlush = true;
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Suit != sorted[0].Suit)
                {
                    isFlush = false;
                    break;
                }
            }

            // Группировка по достоинству: rank -> количество.
            var counts = new Dictionary<Rank, int>();
            foreach (Card c in sorted)
            {
                counts.TryGetValue(c.Rank, out int n);
                counts[c.Rank] = n + 1;
            }

            // Упорядоченные группы: сначала по количеству (убыв.), затем по достоинству (убыв.).
            var groups = new List<KeyValuePair<Rank, int>>(counts);
            groups.Sort((a, b) =>
            {
                int byCount = b.Value.CompareTo(a.Value);
                return byCount != 0 ? byCount : ((int)b.Key).CompareTo((int)a.Key);
            });

            bool isStraight = TryGetStraightHigh(sorted, out Rank straightHigh);

            if (isFlush && isStraight)
            {
                return new HandRank(HandType.StraightFlush, new[] { straightHigh }, sorted);
            }

            if (groups[0].Value == 4)
            {
                Rank quad = groups[0].Key;
                Rank kicker = groups[1].Key;
                return new HandRank(HandType.FourOfAKind, new[] { quad, kicker }, sorted);
            }

            if (groups[0].Value == 3 && groups[1].Value == 2)
            {
                return new HandRank(HandType.FullHouse, new[] { groups[0].Key, groups[1].Key }, sorted);
            }

            if (isFlush)
            {
                var kickers = new List<Rank>(5);
                foreach (Card c in sorted)
                {
                    kickers.Add(c.Rank);
                }

                return new HandRank(HandType.Flush, kickers, sorted);
            }

            if (isStraight)
            {
                return new HandRank(HandType.Straight, new[] { straightHigh }, sorted);
            }

            if (groups[0].Value == 3)
            {
                return new HandRank(
                    HandType.ThreeOfAKind,
                    new[] { groups[0].Key, groups[1].Key, groups[2].Key },
                    sorted);
            }

            if (groups[0].Value == 2 && groups[1].Value == 2)
            {
                return new HandRank(
                    HandType.TwoPair,
                    new[] { groups[0].Key, groups[1].Key, groups[2].Key },
                    sorted);
            }

            if (groups[0].Value == 2)
            {
                var kickers = new List<Rank>(4) { groups[0].Key };
                for (int i = 1; i < groups.Count; i++)
                {
                    kickers.Add(groups[i].Key);
                }

                return new HandRank(HandType.Pair, kickers, sorted);
            }

            var highCardKickers = new List<Rank>(5);
            foreach (Card c in sorted)
            {
                highCardKickers.Add(c.Rank);
            }

            return new HandRank(HandType.HighCard, highCardKickers, sorted);
        }

        /// <summary>Лучшая комбинация из 5..7 карт.</summary>
        public static HandRank Evaluate(IReadOnlyList<Card> cards)
        {
            if (cards == null || cards.Count < 5 || cards.Count > 7)
            {
                throw new ArgumentException("Ожидается от 5 до 7 карт.", nameof(cards));
            }

            if (cards.Count == 5)
            {
                return EvaluateFive(cards);
            }

            HandRank best = null!;
            foreach (int[] combo in Combinations(cards.Count, 5))
            {
                var five = new List<Card>(5);
                foreach (int idx in combo)
                {
                    five.Add(cards[idx]);
                }

                HandRank rank = EvaluateFive(five);
                if (best == null || rank.CompareTo(best) > 0)
                {
                    best = rank;
                }
            }

            return best;
        }

        private static bool TryGetStraightHigh(IReadOnlyList<Card> sortedDesc, out Rank high)
        {
            var ranks = new List<int>(sortedDesc.Count);
            foreach (Card c in sortedDesc)
            {
                ranks.Add((int)c.Rank);
            }

            bool consecutive = true;
            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] != ranks[i - 1] - 1)
                {
                    consecutive = false;
                    break;
                }
            }

            if (consecutive)
            {
                high = (Rank)ranks[0];
                return true;
            }

            // "Колесо": A-5-4-3-2 (туз играет как единица).
            if (ranks[0] == (int)Rank.Ace &&
                ranks[1] == (int)Rank.Five &&
                ranks[2] == (int)Rank.Four &&
                ranks[3] == (int)Rank.Three &&
                ranks[4] == (int)Rank.Two)
            {
                high = Rank.Five;
                return true;
            }

            high = default;
            return false;
        }

        /// <summary>Все сочетания по k индексов из n.</summary>
        private static IEnumerable<int[]> Combinations(int n, int k)
        {
            int[] result = new int[k];
            Stack<int> stack = new Stack<int>();
            stack.Push(0);
            while (stack.Count > 0)
            {
                int index = stack.Count - 1;
                int value = stack.Pop();
                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index == k)
                    {
                        int[] copy = new int[k];
                        Array.Copy(result, copy, k);
                        yield return copy;
                        break;
                    }
                }
            }
        }
    }
}
