using System.Collections.Generic;
using PokerCore;
using Xunit;

namespace PokerCore.Tests
{
    public class HandEvaluatorTests
    {
        private static List<Card> Cards(string s)
        {
            var list = new List<Card>();
            foreach (string part in s.Split(' '))
            {
                list.Add(Card.Parse(part));
            }

            return list;
        }

        [Fact]
        public void RoyalFlush_Beats_StraightFlush()
        {
            HandRank royal = HandEvaluator.Evaluate(Cards("As Ks Qs Js Ts 2c 3d"));
            HandRank straightFlush = HandEvaluator.Evaluate(Cards("9h 8h 7h 6h 5h 2c 3d"));
            Assert.True(royal > straightFlush);
            Assert.Equal(HandType.StraightFlush, royal.Type);
        }

        [Fact]
        public void WheelStraight_IsRecognized_AndAceIsLow()
        {
            HandRank wheel = HandEvaluator.Evaluate(Cards("Ah 2c 3d 4s 5h Kd Qc"));
            HandRank sixHigh = HandEvaluator.Evaluate(Cards("2h 3c 4d 5s 6h Kd Qc"));
            Assert.Equal(HandType.Straight, wheel.Type);
            Assert.True(sixHigh > wheel);
        }

        [Fact]
        public void FullHouse_Beats_Flush()
        {
            HandRank fullHouse = HandEvaluator.Evaluate(Cards("9h 9c 9d 4s 4h Kd 2c"));
            HandRank flush = HandEvaluator.Evaluate(Cards("Ah Kh 8h 5h 2h 9c 9d"));
            Assert.Equal(HandType.FullHouse, fullHouse.Type);
            Assert.True(fullHouse > flush);
        }

        [Fact]
        public void KickerMatters_ForPair()
        {
            HandRank pairAcesKingKicker = HandEvaluator.Evaluate(Cards("Ah Ac 7d 5s 4h Kd 2c"));
            HandRank pairAcesQueenKicker = HandEvaluator.Evaluate(Cards("Ah Ac 7d 5s 4h Qd 2c"));
            Assert.True(pairAcesKingKicker > pairAcesQueenKicker);
        }

        [Fact]
        public void BestFive_ChosenFromSeven()
        {
            // На доске 4 карты к стриту; берём лучшие 5.
            HandRank rank = HandEvaluator.Evaluate(Cards("Ah Kd Qc Js Ts 3h 2d"));
            Assert.Equal(HandType.Straight, rank.Type);
            Assert.Equal(Rank.Ace, rank.Kickers[0]);
        }

        [Fact]
        public void FourOfAKind_Beats_FullHouse()
        {
            HandRank quads = HandEvaluator.Evaluate(Cards("5h 5c 5d 5s Ah Kd 2c"));
            HandRank boat = HandEvaluator.Evaluate(Cards("Ah Ac Ad Ks Kh 2c 3d"));
            Assert.Equal(HandType.FourOfAKind, quads.Type);
            Assert.True(quads > boat);
        }

        [Fact]
        public void EqualHands_CompareEqual()
        {
            HandRank a = HandEvaluator.Evaluate(Cards("Ah Kd 9c 5s 3h 2c 7d"));
            HandRank b = HandEvaluator.Evaluate(Cards("As Kc 9d 5h 3s 2d 7c"));
            Assert.Equal(0, a.CompareTo(b));
        }
    }
}
