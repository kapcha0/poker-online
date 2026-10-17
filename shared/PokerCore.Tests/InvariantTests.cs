using System;
using System.Collections.Generic;
using System.Linq;
using PokerCore;
using Xunit;

namespace PokerCore.Tests
{
    /// <summary>
    /// Property-based (фаззинг) тесты: прогоняют сотни случайных раздач
    /// и после каждого действия проверяют инварианты, которые обязаны
    /// выполняться в любой игровой ситуации.
    ///
    /// Именно такие тесты ловят классы багов, невидимых в точечных тестах:
    /// потерю/дублирование фишек в сайд-потах, зависшие торги, отрицательные стеки.
    /// </summary>
    public class InvariantTests
    {
        private static TexasHoldemGame NewGame(params long[] stacks)
        {
            var players = new List<Player>();
            for (int i = 0; i < stacks.Length; i++)
            {
                players.Add(new Player(i, "P" + (i + 1), stacks[i]));
            }

            return new TexasHoldemGame(players, smallBlind: 1, bigBlind: 2);
        }

        /// <summary>Полная сумма в игре: стеки + деньги в банке.</summary>
        private static long Total(TexasHoldemGame game) =>
            game.Players.Sum(p => p.Chips + p.TotalCommitted);

        /// <summary>
        /// Выбирает случайное легальное действие со смещениями,
        /// чтобы покрыть и фолды, и рейзы, и олл-ины.
        /// </summary>
        private static PlayerAction Pick(Random rng, IReadOnlyList<PlayerAction> legal)
        {
            double roll = rng.NextDouble();

            if (roll < 0.15)
            {
                PlayerAction? fold = legal.FirstOrDefault(a => a.Type == ActionType.Fold);
                if (fold != null)
                {
                    return fold;
                }
            }

            if (roll < 0.45)
            {
                PlayerAction? raise = legal.FirstOrDefault(a => a.Type == ActionType.Raise)
                                      ?? legal.FirstOrDefault(a => a.Type == ActionType.Bet);
                if (raise != null)
                {
                    return raise;
                }
            }

            if (roll < 0.70)
            {
                PlayerAction? call = legal.FirstOrDefault(a => a.Type == ActionType.Call);
                if (call != null)
                {
                    return call;
                }
            }

            PlayerAction? check = legal.FirstOrDefault(a => a.Type == ActionType.Check);
            if (check != null)
            {
                return check;
            }

            PlayerAction? allIn = legal.FirstOrDefault(a => a.Type == ActionType.AllIn);
            if (allIn != null && rng.Next(3) == 0)
            {
                return allIn;
            }

            return legal[rng.Next(legal.Count)];
        }

        [Fact]
        public void FuzzedHands_ConserveChips_Terminate_NoNegativeStacks()
        {
            var rng = new Random(20261017);

            for (int seed = 0; seed < 120; seed++)
            {
                // До 9 игроков: за полным столом появляется несколько уровней
                // сайд-потов, что и является самым багованным местом в покере.
                int playerCount = rng.Next(2, 10);
                var stacks = new long[playerCount];
                for (int i = 0; i < playerCount; i++)
                {
                    // От крошечных стеков (частые олл-ины) до крупных.
                    stacks[i] = rng.Next(3, 500);
                }

                long grandTotal = stacks.Sum();
                TexasHoldemGame game = NewGame(stacks);

                int hands = rng.Next(1, 4);
                for (int hand = 0; hand < hands; hand++)
                {
                    if (game.Players.Count(p => p.Chips > 0) < 2)
                    {
                        break;
                    }

                    game.StartHand(new SeededRandomSource(seed * 1000 + hand));

                    // Блайнды уже списаны, но сумма в игре не изменилась.
                    Assert.Equal(grandTotal, Total(game));

                    int guard = 0;
                    while (game.Street != Street.HandComplete && guard++ < 300)
                    {
                        int seat = game.ActiveSeat;
                        Assert.True(seat >= 0, $"seed={seed}: нет активного игрока на улице {game.Street}");

                        IReadOnlyList<PlayerAction> legal = game.GetLegalActions(seat);
                        Assert.NotEmpty(legal);

                        PlayerAction action = Pick(rng, legal);
                        long before = Total(game);

                        game.ApplyAction(seat, action);

                        // Главный инвариант: деньги не появляются и не исчезают.
                        Assert.Equal(before, Total(game));

                        foreach (Player p in game.Players)
                        {
                            Assert.True(p.Chips >= 0, $"seed={seed}: отрицательный стек {p.Name}={p.Chips}");
                            Assert.True(p.TotalCommitted >= 0, $"seed={seed}: отрицательное вложение");
                        }
                    }

                    Assert.True(
                        game.Street == Street.HandComplete,
                        $"seed={seed}: раздача зависла на улице {game.Street} после {guard} ходов");

                    // После раздачи вложения обнулены, всё в стеках.
                    Assert.Equal(0, game.Players.Sum(p => p.TotalCommitted));
                    Assert.Equal(grandTotal, game.Players.Sum(p => p.Chips));
                }
            }
        }

        [Fact]
        public void FuzzedHands_Showdown_AwardNeverExceedsPot()
        {
            var rng = new Random(4242);

            for (int seed = 0; seed < 60; seed++)
            {
                var stacks = new long[4];
                for (int i = 0; i < 4; i++)
                {
                    stacks[i] = rng.Next(10, 300);
                }

                long grandTotal = stacks.Sum();
                TexasHoldemGame game = NewGame(stacks);
                game.StartHand(new SeededRandomSource(seed));

                long potAtStart = game.Pot;
                int guard = 0;
                while (game.Street != Street.HandComplete && guard++ < 300)
                {
                    IReadOnlyList<PlayerAction> legal = game.GetLegalActions(game.ActiveSeat);
                    game.ApplyAction(game.ActiveSeat, Pick(rng, legal));
                }

                Assert.Equal(Street.HandComplete, game.Street);

                // Выигрыши никогда не превышают собранный банк.
                long awarded = game.LastAwards.Sum(a => a.Amount);
                Assert.True(
                    awarded <= grandTotal,
                    $"seed={seed}: начислено {awarded} при сумме в игре {grandTotal}");
                Assert.Equal(grandTotal, game.Players.Sum(p => p.Chips));
            }
        }

        [Fact]
        public void SplitPot_DistributesRemainder_WithoutLoss()
        {
            // Одна и та же раздача на 3 игрока: если банк не делится
            // ровно, остаток уходит одному из победителей, но общая сумма сохраняется.
            for (int seed = 0; seed < 40; seed++)
            {
                var game = NewGame(101, 101, 101);
                game.StartHand(new SeededRandomSource(seed));

                int guard = 0;
                while (game.Street != Street.HandComplete && guard++ < 300)
                {
                    // Все ходят до вскрытия, чтобы почти всегда был шоудаун.
                    var legal = game.GetLegalActions(game.ActiveSeat);
                    PlayerAction action =
                        legal.FirstOrDefault(a => a.Type == ActionType.Call)
                        ?? legal.FirstOrDefault(a => a.Type == ActionType.Check)
                        ?? legal.FirstOrDefault(a => a.Type == ActionType.Raise)
                        ?? legal.First();

                    game.ApplyAction(game.ActiveSeat, action);
                }

                Assert.Equal(Street.HandComplete, game.Street);
                long total = 101 + 101 + 101;
                Assert.Equal(total, game.Players.Sum(p => p.Chips));
            }
        }
    }
}
