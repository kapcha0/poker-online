using System.Collections.Generic;

namespace PokerCore
{
    /// <summary>Банк (в т.ч. сайд-пот): сумма и места, претендующие на него.</summary>
    public sealed class Pot
    {
        public Pot(long amount, List<int> eligibleSeats)
        {
            Amount = amount;
            EligibleSeats = eligibleSeats;
        }

        public long Amount { get; }

        public List<int> EligibleSeats { get; }
    }

    /// <summary>Выплата по итогам раздачи.</summary>
    public sealed class PotAward
    {
        public PotAward(int seat, long amount, HandRank? hand)
        {
            Seat = seat;
            Amount = amount;
            Hand = hand;
        }

        public int Seat { get; }

        public long Amount { get; }

        public HandRank? Hand { get; }
    }

    /// <summary>Разбиение общего банка на основной и сайд-поты.</summary>
    public static class PotManager
    {
        public static List<Pot> Build(IReadOnlyList<Player> players)
        {
            var pots = new List<Pot>();

            // Уникальные уровни вложений (без нуля), по возрастанию.
            var levels = new List<long>();
            foreach (Player p in players)
            {
                if (p.TotalCommitted > 0 && !levels.Contains(p.TotalCommitted))
                {
                    levels.Add(p.TotalCommitted);
                }
            }

            levels.Sort();

            long previous = 0;
            foreach (long level in levels)
            {
                long amount = 0;
                foreach (Player p in players)
                {
                    long contribution = p.TotalCommitted;
                    if (contribution > previous)
                    {
                        amount += System.Math.Min(contribution, level) - previous;
                    }
                }

                var eligible = new List<int>();
                foreach (Player p in players)
                {
                    if (p.InHand && p.TotalCommitted >= level)
                    {
                        eligible.Add(p.Seat);
                    }
                }

                if (amount > 0)
                {
                    if (eligible.Count == 0 && pots.Count > 0)
                    {
                        // Невостребованная часть присоединяется к предыдущему банку.
                        Pot last = pots[pots.Count - 1];
                        pots[pots.Count - 1] = new Pot(last.Amount + amount, last.EligibleSeats);
                    }
                    else
                    {
                        pots.Add(new Pot(amount, eligible));
                    }
                }

                previous = level;
            }

            // Если все вложенные фишки уже учтены — ок; иначе объединяем в общий банк.
            long totalCommitted = 0;
            foreach (Player p in players)
            {
                totalCommitted += p.TotalCommitted;
            }

            long accounted = 0;
            foreach (Pot pot in pots)
            {
                accounted += pot.Amount;
            }

            if (totalCommitted > accounted)
            {
                var eligibleAll = new List<int>();
                foreach (Player p in players)
                {
                    if (p.InHand)
                    {
                        eligibleAll.Add(p.Seat);
                    }
                }

                pots.Add(new Pot(totalCommitted - accounted, eligibleAll));
            }

            return pots;
        }
    }
}
