using System.Collections.Generic;
using System.Linq;
using PokerCore;

namespace PokerOnline
{
    /// <summary>
    /// Простой бот с эвристикой: сила руки + пот-оддсы.
    /// Позже заменяется более сильным ИИ (этап 2 дорожной карты).
    /// </summary>
    public static class Bot
    {
        public static PlayerAction Decide(TexasHoldemGame game, int seat)
        {
            var legal = game.GetLegalActions(seat);
            if (legal.Count == 0)
            {
                return PlayerAction.Check();
            }

            Player me = game.GetPlayer(seat);
            long toCall = game.CurrentBet - me.BetThisRound;
            double strength = EstimateStrength(game, me, toCall);

            bool canCheck = legal.Any(a => a.Type == ActionType.Check);
            bool canRaise = legal.Any(a => a.Type == ActionType.Raise || a.Type == ActionType.Bet);
            PlayerAction? raiseAction = legal.FirstOrDefault(a => a.Type == ActionType.Raise || a.Type == ActionType.Bet);
            PlayerAction allIn = legal.FirstOrDefault(a => a.Type == ActionType.AllIn);

            if (toCall <= 0)
            {
                if (canRaise && raiseAction != null && strength > 0.62)
                {
                    return strength > 0.9 && allIn != null ? allIn : raiseAction;
                }

                return canCheck ? PlayerAction.Check() : PlayerAction.Fold();
            }

            double potOdds = (double)toCall / (game.Pot + toCall);
            if (strength > 0.85 && canRaise && raiseAction != null)
            {
                return raiseAction;
            }

            if (strength >= potOdds * 1.05)
            {
                PlayerAction? call = legal.FirstOrDefault(a => a.Type == ActionType.Call);
                return call ?? PlayerAction.Fold();
            }

            return PlayerAction.Fold();
        }

        private static double EstimateStrength(TexasHoldemGame game, Player me, long toCall)
        {
            if (game.Street == Street.Preflop)
            {
                return PreflopStrength(me.Hole1, me.Hole2);
            }

            var cards = new List<Card> { me.Hole1, me.Hole2 };
            cards.AddRange(game.CommunityCards);
            HandRank rank = HandEvaluator.Evaluate(cards);
            return PostflopStrength(rank.Type);
        }

        private static double PreflopStrength(Card a, Card b)
        {
            int hi = System.Math.Max((int)a.Rank, (int)b.Rank);
            int lo = System.Math.Min((int)a.Rank, (int)b.Rank);

            if (hi == lo)
            {
                // Пара: от 0.5 (22) до 1.0 (AA).
                return 0.5 + (hi - 2) / 24.0;
            }

            double strength = (hi + lo) / 28.0 * 0.7;
            if (a.Suit == b.Suit)
            {
                strength += 0.06;
            }

            if (hi - lo == 1)
            {
                strength += 0.05;
            }

            return System.Math.Min(1.0, strength);
        }

        private static double PostflopStrength(HandType type)
        {
            switch (type)
            {
                case HandType.HighCard: return 0.15;
                case HandType.Pair: return 0.42;
                case HandType.TwoPair: return 0.62;
                case HandType.ThreeOfAKind: return 0.75;
                case HandType.Straight: return 0.85;
                case HandType.Flush: return 0.9;
                case HandType.FullHouse: return 0.95;
                case HandType.FourOfAKind: return 0.99;
                case HandType.StraightFlush: return 1.0;
                default: return 0.2;
            }
        }
    }
}
