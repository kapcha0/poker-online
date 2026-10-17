using System.Collections.Generic;
using System.Linq;
using PokerCore;
using Xunit;

namespace PokerCore.Tests
{
    public class GameFlowTests
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

        [Fact]
        public void StartHand_PostsBlinds_AndDealsHoleCards()
        {
            var game = NewGame(200, 200, 200);
            game.StartHand(new SeededRandomSource(7));

            Assert.Equal(Street.Preflop, game.Street);
            Assert.Equal(3, game.Pot); // SB 1 + BB 2
            Assert.All(game.Players, p => Assert.True(p.HasCards));
            Assert.Equal(2, game.CurrentBet);
            Assert.NotEqual(-1, game.ActiveSeat);
        }

        [Fact]
        public void Fold_WinsWithoutShowdown()
        {
            var game = NewGame(200, 200);
            game.StartHand(new SeededRandomSource(1));
            // Heads-up: первым действует дилер (малый блайнд).
            int first = game.ActiveSeat;
            game.ApplyAction(first, PlayerAction.Fold());

            Assert.Equal(Street.HandComplete, game.Street);
            Assert.Contains(game.LastAwards, a => a.Amount == 3);
        }

        [Fact]
        public void CheckCheck_AdvancesToFlop_AndDealsThreeCards()
        {
            var game = NewGame(200, 200);
            game.StartHand(new SeededRandomSource(3));

            // Доводим торги префлопа до конца коллами.
            while (game.Street == Street.Preflop)
            {
                var legal = game.GetLegalActions(game.ActiveSeat);
                PlayerAction action = legal.FirstOrDefault(a => a.Type == ActionType.Call)
                    ?? legal.First(a => a.Type == ActionType.Check);
                game.ApplyAction(game.ActiveSeat, action);
                if (game.Street == Street.HandComplete)
                {
                    break;
                }
            }

            if (game.Street != Street.HandComplete)
            {
                Assert.Equal(Street.Flop, game.Street);
                Assert.Equal(3, game.CommunityCards.Count);
            }
        }

        [Fact]
        public void FullHand_Completes_AndChipsAreConserved()
        {
            var game = NewGame(100, 100, 100);
            game.StartHand(new SeededRandomSource(99));

            // Инвариант: стеки + вложения в банк за раздачу не меняются.
            long totalBefore = game.Players.Sum(p => p.Chips + p.TotalCommitted);

            int guard = 0;
            while (game.Street != Street.HandComplete && guard++ < 200)
            {
                var legal = game.GetLegalActions(game.ActiveSeat);
                Assert.NotEmpty(legal);

                // Простая стратегия: чек/колл, изредка рейз.
                PlayerAction action;
                if (guard % 7 == 0 && legal.Any(a => a.Type == ActionType.Raise))
                {
                    action = legal.First(a => a.Type == ActionType.Raise);
                }
                else
                {
                    action = legal.FirstOrDefault(a => a.Type == ActionType.Check)
                        ?? legal.FirstOrDefault(a => a.Type == ActionType.Call)
                        ?? legal.First(a => a.Type == ActionType.Fold);
                }

                game.ApplyAction(game.ActiveSeat, action);
            }

            Assert.Equal(Street.HandComplete, game.Street);
            long totalAfter = game.Players.Sum(p => p.Chips + p.TotalCommitted);
            Assert.Equal(totalBefore, totalAfter); // фишки не теряются и не создаются
            Assert.Equal(0, game.Players.Sum(p => p.TotalCommitted)); // учёт раздачи обнулён
            Assert.Equal(5, game.CommunityCards.Count);
        }

        [Fact]
        public void IllegalAction_Throws()
        {
            var game = NewGame(200, 200);
            game.StartHand(new SeededRandomSource(5));
            Assert.Throws<System.InvalidOperationException>(() =>
                game.ApplyAction(game.ActiveSeat, PlayerAction.Check())); // есть ставка BB — чек нельзя
        }

        [Fact]
        public void AllIn_ShortStack_GoesToShowdown()
        {
            var game = NewGame(1000, 10);
            game.StartHand(new SeededRandomSource(11));

            int guard = 0;
            while (game.Street != Street.HandComplete && guard++ < 50)
            {
                var legal = game.GetLegalActions(game.ActiveSeat);
                PlayerAction action = legal.FirstOrDefault(a => a.Type == ActionType.AllIn)
                    ?? legal.FirstOrDefault(a => a.Type == ActionType.Call)
                    ?? legal.First(a => a.Type == ActionType.Check);
                game.ApplyAction(game.ActiveSeat, action);
            }

            Assert.Equal(Street.HandComplete, game.Street);
            Assert.Equal(5, game.CommunityCards.Count);
        }
    }
}
