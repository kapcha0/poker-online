using System.Collections.Generic;
using System.Linq;
using PokerCore;
using Xunit;

namespace PokerCore.Tests
{
    public class PotManagerTests
    {
        private static Player Make(int seat, long committed, bool folded)
        {
            var p = new Player(seat, "P" + seat, 1000);
            // Короткий all-in: фишки уходят в банк, стек обнуляется.
            p.Chips = 1000 - committed;
            p.HasCards = true;
            p.Folded = folded;
            p.TotalCommitted = committed;
            return p;
        }

        [Fact]
        public void SingleLevel_SinglePot()
        {
            var players = new List<Player> { Make(0, 50, false), Make(1, 50, false) };
            List<Pot> pots = PotManager.Build(players);

            Assert.Single(pots);
            Assert.Equal(100, pots[0].Amount);
            Assert.Equal(2, pots[0].EligibleSeats.Count);
        }

        [Fact]
        public void ShortStack_CreatesMainAndSidePot()
        {
            // A короткий all-in (50), B и C вносят по 200.
            var players = new List<Player> { Make(0, 50, false), Make(1, 200, false), Make(2, 200, false) };
            List<Pot> pots = PotManager.Build(players);

            Assert.Equal(2, pots.Count);
            Assert.Equal(150, pots[0].Amount); // основной банк: 3 игрока по 50
            Assert.Equal(3, pots[0].EligibleSeats.Count);
            Assert.Equal(300, pots[1].Amount); // сайд-пот: только B и C
            Assert.Equal(2, pots[1].EligibleSeats.Count);

            long total = pots.Sum(p => p.Amount);
            Assert.Equal(450, total); // 50 + 200 + 200
        }

        [Fact]
        public void FoldedPlayer_FundsPot_ButCantWin()
        {
            var players = new List<Player> { Make(0, 100, true), Make(1, 100, false), Make(2, 100, false) };
            List<Pot> pots = PotManager.Build(players);

            Assert.Single(pots);
            Assert.Equal(300, pots[0].Amount);
            Assert.DoesNotContain(0, pots[0].EligibleSeats);
            Assert.Contains(1, pots[0].EligibleSeats);
            Assert.Contains(2, pots[0].EligibleSeats);
        }

        [Fact]
        public void UncalledBet_IsHandledWithoutLostChips()
        {
            // A поставил 300, никто не согласился — уровень покрыт только им.
            var players = new List<Player> { Make(0, 300, false), Make(1, 100, false) };
            List<Pot> pots = PotManager.Build(players);

            long total = pots.Sum(p => p.Amount);
            Assert.Equal(400, total);
            Assert.All(pots, p => Assert.True(p.EligibleSeats.Count > 0));
        }

        [Fact]
        public void AmountsAlways_SumToTotalCommitted()
        {
            var players = new List<Player>
            {
                Make(0, 30, false),
                Make(1, 175, true),
                Make(2, 400, false),
                Make(3, 400, false),
                Make(4, 90, false),
            };
            List<Pot> pots = PotManager.Build(players);

            long expected = players.Sum(p => p.TotalCommitted);
            Assert.Equal(expected, pots.Sum(p => p.Amount));
        }
    }
}
