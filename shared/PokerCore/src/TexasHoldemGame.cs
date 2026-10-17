using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerCore
{
    /// <summary>
    /// Движок одной раздачи Техасского Холдема за столом.
    /// Серверо-авторитетная логика: клиент только отображает состояние и отправляет действия.
    /// </summary>
    public sealed class TexasHoldemGame
    {
        private readonly List<Player> _players;
        private readonly List<string> _log = new List<string>();
        private readonly List<PotAward> _lastAwards = new List<PotAward>();
        private readonly List<Card> _community = new List<Card>(5);
        private Deck? _deck;
        private long _lastPotTotal;

        public TexasHoldemGame(IEnumerable<Player> players, long smallBlind, long bigBlind)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            _players = players.OrderBy(p => p.Seat).ToList();
            if (_players.Count < 2)
            {
                throw new ArgumentException("Нужно минимум 2 игрока.", nameof(players));
            }

            if (_players.Select(p => p.Seat).Distinct().Count() != _players.Count)
            {
                throw new ArgumentException("Места (seat) игроков должны быть уникальны.", nameof(players));
            }

            if (smallBlind <= 0 || bigBlind <= 0 || bigBlind < smallBlind)
            {
                throw new ArgumentException("Некорректные размеры блайндов.");
            }

            SmallBlind = smallBlind;
            BigBlind = bigBlind;
            DealerSeat = _players[0].Seat;
            ActiveSeat = -1;
            Street = Street.HandComplete;
        }

        public long SmallBlind { get; }

        public long BigBlind { get; }

        public Street Street { get; private set; }

        public int DealerSeat { get; private set; }

        /// <summary>Чей сейчас ход (-1, если раздача завершена/ожидание).</summary>
        public int ActiveSeat { get; private set; }

        public long CurrentBet { get; private set; }

        public long MinRaise { get; private set; }

        public IReadOnlyList<Player> Players => _players;

        public IReadOnlyList<Card> CommunityCards => _community;

        public IReadOnlyList<string> Log => _log;

        public IReadOnlyList<PotAward> LastAwards => _lastAwards;

        public bool HandInProgress => Street != Street.HandComplete;

        /// <summary>Общий размер банка (все вложенные фишки, включая ставки текущей улицы).</summary>
        public long Pot => _players.Sum(p => p.TotalCommitted);

        /// <summary>Банк последней завершённой раздачи (учёт раздачи после неё обнуляется).</summary>
        public long LastPot => _lastPotTotal;

        public Player GetPlayer(int seat) => _players.First(p => p.Seat == seat);

        /// <summary>Начать новую раздачу.</summary>
        public void StartHand(IRandomSource? randomSource = null)
        {
            if (HandInProgress)
            {
                throw new InvalidOperationException("Раздача уже идёт.");
            }

            _log.Clear();
            _lastAwards.Clear();
            _community.Clear();

            foreach (Player p in _players)
            {
                p.ResetForNewHand();
            }

            var inPlay = _players.Where(p => p.Chips > 0).ToList();
            if (inPlay.Count < 2)
            {
                throw new InvalidOperationException("Недостаточно игроков с фишками.");
            }

            // Ротация дилерской кнопки.
            DealerSeat = NextOccupied(DealerSeat, p => p.Chips > 0, includeCurrent: false, fallback: DealerSeat);

            var deck = new Deck();
            if (randomSource != null)
            {
                deck.Shuffle(randomSource);
            }
            else
            {
                deck.Shuffle();
            }

            _deck = deck;
            AssignPositions(inPlay);

            // Раздача карт по одной, начиная слева от дилера.
            var order = PlayersInOrderAfter(DealerSeat, p => p.Chips > 0);
            for (int round = 0; round < 2; round++)
            {
                foreach (Player p in order)
                {
                    Card card = deck.Draw();
                    if (round == 0)
                    {
                        p.Hole1 = card;
                    }
                    else
                    {
                        p.Hole2 = card;
                    }

                    p.HasCards = true;
                }
            }

            CurrentBet = 0;
            MinRaise = BigBlind;
            Street = Street.Preflop;

            PostBlind(FirstByPosition(p => p.IsSmallBlind), SmallBlind, "малый блайнд");
            PostBlind(FirstByPosition(p => p.IsBigBlind), BigBlind, "большой блайнд");

            CurrentBet = _players.Max(p => p.BetThisRound);
            ActiveSeat = FirstToActPreflop();

            LogLine($"Раздача началась. Дилер: {GetPlayer(DealerSeat).Name}, банк: {Pot}.");
            AfterActionCheck();
        }

        /// <summary>Легальные действия для места.</summary>
        public IReadOnlyList<PlayerAction> GetLegalActions(int seat)
        {
            var result = new List<PlayerAction>();
            if (!HandInProgress || seat != ActiveSeat)
            {
                return result;
            }

            Player p = GetPlayer(seat);
            if (!p.CanAct)
            {
                return result;
            }

            long toCall = CurrentBet - p.BetThisRound;
            long allInTotal = p.BetThisRound + p.Chips;

            result.Add(PlayerAction.Fold());

            if (toCall <= 0)
            {
                result.Add(PlayerAction.Check());
            }
            else
            {
                result.Add(PlayerAction.Call());
            }

            if (CurrentBet == 0)
            {
                // Первая ставка на улице.
                long minBetTo = Math.Min(BigBlind, allInTotal);
                if (minBetTo > 0)
                {
                    result.Add(PlayerAction.BetTo(minBetTo));
                }
            }
            else
            {
                long minRaiseTo = CurrentBet + MinRaise;
                if (allInTotal >= minRaiseTo)
                {
                    result.Add(PlayerAction.RaiseTo(minRaiseTo));
                }
            }

            if (allInTotal > p.BetThisRound)
            {
                result.Add(PlayerAction.AllIn());
            }

            return result;
        }

        /// <summary>Применить действие игрока. Бросает InvalidOperationException при нелегальном действии.</summary>
        public void ApplyAction(int seat, PlayerAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!HandInProgress)
            {
                throw new InvalidOperationException("Раздача не идёт.");
            }

            if (seat != ActiveSeat)
            {
                throw new InvalidOperationException("Сейчас не ход этого игрока.");
            }

            Player p = GetPlayer(seat);
            if (!p.CanAct)
            {
                throw new InvalidOperationException("Игрок не может действовать.");
            }

            long toCall = CurrentBet - p.BetThisRound;

            switch (action.Type)
            {
                case ActionType.Fold:
                    p.Folded = true;
                    p.HasActed = true;
                    LogLine($"{p.Name} — фолд.");
                    break;

                case ActionType.Check:
                    if (toCall > 0)
                    {
                        throw new InvalidOperationException("Нельзя чекнуть: есть ставка для колла.");
                    }

                    p.HasActed = true;
                    LogLine($"{p.Name} — чек.");
                    break;

                case ActionType.Call:
                    if (toCall <= 0)
                    {
                        throw new InvalidOperationException("Нечего коллировать.");
                    }

                    long paid = Commit(p, p.BetThisRound + toCall);
                    p.HasActed = true;
                    LogLine(paid < toCall
                        ? $"{p.Name} — колл all-in {paid}."
                        : $"{p.Name} — колл {paid}.");
                    break;

                case ActionType.Bet:
                    if (CurrentBet != 0)
                    {
                        throw new InvalidOperationException("Ставка невозможна: уже есть ставка (используйте рейз).");
                    }

                    if (action.Amount < BigBlind && action.Amount < p.BetThisRound + p.Chips)
                    {
                        throw new InvalidOperationException($"Минимальная ставка — {BigBlind}.");
                    }

                    if (action.Amount > p.BetThisRound + p.Chips)
                    {
                        throw new InvalidOperationException("Недостаточно фишек.");
                    }

                    Commit(p, action.Amount);
                    CurrentBet = p.BetThisRound;
                    MinRaise = CurrentBet;
                    ReopenAction(p.Seat);
                    p.HasActed = true;
                    LogLine($"{p.Name} — бет {p.BetThisRound}.");
                    break;

                case ActionType.Raise:
                    if (CurrentBet == 0)
                    {
                        throw new InvalidOperationException("Рейз невозможен: ставок нет (используйте бет).");
                    }

                    long raiseDelta = action.Amount - CurrentBet;
                    bool isAllInShort = action.Amount == p.BetThisRound + p.Chips && raiseDelta < MinRaise;
                    if (raiseDelta < MinRaise && !isAllInShort)
                    {
                        throw new InvalidOperationException($"Минимальный рейз — до {CurrentBet + MinRaise}.");
                    }

                    if (action.Amount > p.BetThisRound + p.Chips)
                    {
                        throw new InvalidOperationException("Недостаточно фишек.");
                    }

                    Commit(p, action.Amount);
                    CurrentBet = p.BetThisRound;
                    if (raiseDelta >= MinRaise)
                    {
                        MinRaise = raiseDelta;
                        ReopenAction(p.Seat);
                    }

                    p.HasActed = true;
                    LogLine($"{p.Name} — рейз до {p.BetThisRound}.");
                    break;

                case ActionType.AllIn:
                    long target = p.BetThisRound + p.Chips;
                    long allInDelta = target - CurrentBet;
                    Commit(p, target);
                    p.HasActed = true;
                    if (p.BetThisRound > CurrentBet)
                    {
                        if (allInDelta >= MinRaise)
                        {
                            MinRaise = allInDelta;
                            ReopenAction(p.Seat);
                        }

                        CurrentBet = p.BetThisRound;
                    }

                    LogLine($"{p.Name} — all-in {p.BetThisRound}.");
                    break;

                default:
                    throw new InvalidOperationException("Неизвестное действие.");
            }

            AdvanceAfterAction(seat);
            AfterActionCheck();
        }

        // ---------- внутреннее ----------

        private void AssignPositions(List<Player> inPlay)
        {
            int count = inPlay.Count;
            if (count == 2)
            {
                // Heads-up: дилер = малый блайнд.
                foreach (Player p in _players)
                {
                    p.IsDealer = p.Seat == DealerSeat;
                    p.IsSmallBlind = p.Seat == DealerSeat;
                    p.IsBigBlind = p.Seat != DealerSeat && p.Chips > 0;
                }
            }
            else
            {
                int sbSeat = NextOccupied(DealerSeat, p => p.Chips > 0, includeCurrent: false, fallback: DealerSeat);
                int bbSeat = NextOccupied(sbSeat, p => p.Chips > 0, includeCurrent: false, fallback: sbSeat);
                foreach (Player p in _players)
                {
                    p.IsDealer = p.Seat == DealerSeat;
                    p.IsSmallBlind = p.Seat == sbSeat;
                    p.IsBigBlind = p.Seat == bbSeat;
                }
            }
        }

        private void PostBlind(Player? p, long amount, string label)
        {
            if (p == null)
            {
                return;
            }

            long paid = Commit(p, Math.Min(amount, p.Chips));
            LogLine($"{p.Name} ставит {label} {paid}.");
        }

        private Player? FirstByPosition(Func<Player, bool> predicate) =>
            _players.FirstOrDefault(p => predicate(p) && p.Chips >= 0 && p.HasCards);

        private int FirstToActPreflop()
        {
            int start = FirstByPosition(p => p.IsBigBlind)?.Seat ?? DealerSeat;
            if (_players.Count(p => p.Chips > 0) == 2)
            {
                // Heads-up: первым действует дилер (малый блайнд).
                return DealerSeat;
            }

            return NextOccupied(start, p => p.CanAct, includeCurrent: false, fallback: -1);
        }

        private int FirstToActPostflop()
        {
            return NextOccupied(DealerSeat, p => p.CanAct, includeCurrent: false, fallback: -1);
        }

        private long Commit(Player p, long toTotal)
        {
            long need = toTotal - p.BetThisRound;
            if (need < 0)
            {
                need = 0;
            }

            long actual = Math.Min(need, p.Chips);
            p.Chips -= actual;
            p.BetThisRound += actual;
            p.TotalCommitted += actual;
            if (p.Chips == 0)
            {
                p.AllIn = true;
            }

            return actual;
        }

        private void ReopenAction(int raiserSeat)
        {
            foreach (Player p in _players)
            {
                if (p.Seat != raiserSeat && p.CanAct)
                {
                    p.HasActed = false;
                }
            }
        }

        private void AdvanceAfterAction(int actorSeat)
        {
            int next = NextOccupied(actorSeat, p => p.CanAct, includeCurrent: false, fallback: -1);
            ActiveSeat = next;
        }

        private void AfterActionCheck()
        {
            int inHand = _players.Count(p => p.InHand);
            if (inHand <= 1)
            {
                // Все, кроме одного, сбросили — он забирает банк.
                FinishWithoutShowdown();
                return;
            }

            var canAct = _players.Where(p => p.CanAct).ToList();
            bool roundComplete;
            if (canAct.Count == 0)
            {
                roundComplete = true;
            }
            else
            {
                roundComplete = canAct.All(p => p.HasActed && p.BetThisRound == CurrentBet);
            }

            if (!roundComplete)
            {
                if (ActiveSeat < 0)
                {
                    ActiveSeat = NextOccupied(DealerSeat, p => p.CanAct, includeCurrent: false, fallback: -1);
                }

                return;
            }

            CollectBetsAndAdvanceStreet();
        }

        private void CollectBetsAndAdvanceStreet()
        {
            // Ставки улицы остаются в TotalCommitted; BetThisRound обнуляется.
            foreach (Player p in _players)
            {
                p.BetThisRound = 0;
                p.HasActed = false;
            }

            CurrentBet = 0;
            MinRaise = BigBlind;

            switch (Street)
            {
                case Street.Preflop:
                    DealCommunity(3);
                    Street = Street.Flop;
                    LogLine("Флоп.");
                    break;
                case Street.Flop:
                    DealCommunity(1);
                    Street = Street.Turn;
                    LogLine("Тёрн.");
                    break;
                case Street.Turn:
                    DealCommunity(1);
                    Street = Street.River;
                    LogLine("Ривер.");
                    break;
                case Street.River:
                    DoShowdown();
                    return;
                default:
                    return;
            }

            var canAct = _players.Where(p => p.CanAct).ToList();
            if (canAct.Count <= 1)
            {
                // Все оставшиеся all-in — доигрываем до вскрытия.
                RunOutBoard();
                DoShowdown();
                return;
            }

            ActiveSeat = FirstToActPostflop();
            if (ActiveSeat < 0)
            {
                RunOutBoard();
                DoShowdown();
            }
        }

        private void DealCommunity(int count)
        {
            if (_deck == null)
            {
                throw new InvalidOperationException("Колода не инициализирована.");
            }

            for (int i = 0; i < count; i++)
            {
                _community.Add(_deck.Draw());
            }
        }

        private void RunOutBoard()
        {
            if (_deck == null)
            {
                throw new InvalidOperationException("Колода не инициализирована.");
            }

            while (_community.Count < 5)
            {
                _community.Add(_deck.Draw());
            }
        }

        private void DoShowdown()
        {
            Street = Street.Showdown;
            AwardPots(reveal: true);
            Street = Street.HandComplete;
            ActiveSeat = -1;
        }

        private void FinishWithoutShowdown()
        {
            Street = Street.Showdown;
            AwardPots(reveal: false);
            Street = Street.HandComplete;
            ActiveSeat = -1;
        }

        private void AwardPots(bool reveal)
        {
            long potTotal = Pot;
            var pots = PotManager.Build(_players);
            var totals = new Dictionary<int, long>();
            var hands = new Dictionary<int, HandRank>();

            // Оценивать руку можно только когда на столе хватает карт (2 в руке + 3 флоп).
            bool canEvaluate = reveal && _community.Count >= 3;

            foreach (Pot pot in pots)
            {
                var eligible = pot.EligibleSeats.Count > 0
                    ? pot.EligibleSeats
                    : _players.Where(p => p.InHand).Select(p => p.Seat).ToList();

                var contenders = new List<Player>();
                foreach (int seat in eligible)
                {
                    Player p = GetPlayer(seat);
                    if (p.InHand)
                    {
                        contenders.Add(p);
                    }
                }

                if (contenders.Count == 0)
                {
                    continue;
                }

                List<Player> winners;
                if (canEvaluate)
                {
                    HandRank? best = null;
                    winners = new List<Player>();
                    foreach (Player p in contenders)
                    {
                        var cards = new List<Card> { p.Hole1, p.Hole2 };
                        cards.AddRange(_community);
                        HandRank rank = HandEvaluator.Evaluate(cards);
                        hands[p.Seat] = rank;
                        if (best == null || rank.CompareTo(best) > 0)
                        {
                            best = rank;
                            winners.Clear();
                            winners.Add(p);
                        }
                        else if (rank.CompareTo(best) == 0)
                        {
                            winners.Add(p);
                        }
                    }
                }
                else
                {
                    // Вскрытия не было (все сбросили) — забирает единственный остающийся.
                    winners = new List<Player>(contenders);
                }

                long share = pot.Amount / winners.Count;
                long remainder = pot.Amount % winners.Count;

                foreach (Player w in winners)
                {
                    totals.TryGetValue(w.Seat, out long existing);
                    totals[w.Seat] = existing + share;
                }

                // Остаток от деления раздаётся по одному, начиная слева от дилера.
                if (remainder > 0)
                {
                    var ordered = winners.OrderBy(w => SeatDistanceAfterDealer(w.Seat)).ToList();
                    for (int i = 0; i < remainder; i++)
                    {
                        Player w = ordered[i % ordered.Count];
                        totals[w.Seat] = totals[w.Seat] + 1;
                    }
                }
            }

            foreach (var kv in totals)
            {
                Player p = GetPlayer(kv.Key);
                p.Chips += kv.Value;
                HandRank? hand = hands.TryGetValue(kv.Key, out HandRank? found) ? found : null;
                _lastAwards.Add(new PotAward(kv.Key, kv.Value, hand));
                LogLine(hand != null
                    ? $"{p.Name} выигрывает {kv.Value} ({hand.Type})."
                    : $"{p.Name} забирает банк {kv.Value}.");
            }

            _lastPotTotal = potTotal;

            // Раздача завершена: вложенные фишки уже в стеках, сбрасываем учёт раздачи.
            foreach (Player p in _players)
            {
                p.TotalCommitted = 0;
                p.BetThisRound = 0;
                p.HasActed = false;
            }
        }

        private int SeatDistanceAfterDealer(int seat)
        {
            int distance = 0;
            int current = DealerSeat;
            for (int i = 0; i < _players.Count; i++)
            {
                current = NextOccupied(current, _ => true, includeCurrent: false, fallback: current);
                distance++;
                if (current == seat)
                {
                    break;
                }
            }

            return distance;
        }

        /// <summary>Публичный метод досдачи карт доски при all-in (используется клиентом/сервером).</summary>
        public void DealBoardCards(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            int needed = 5 - _community.Count;
            for (int i = 0; i < cards.Count && i < needed; i++)
            {
                _community.Add(cards[i]);
            }
        }

        private IEnumerable<Player> PlayersInOrderAfter(int seat, Func<Player, bool> predicate)
        {
            var result = new List<Player>();
            int current = seat;
            for (int i = 0; i < _players.Count; i++)
            {
                current = NextOccupied(current, _ => true, includeCurrent: false, fallback: current);
                Player? p = _players.FirstOrDefault(x => x.Seat == current);
                if (p != null && predicate(p))
                {
                    result.Add(p);
                }
            }

            return result;
        }

        private int NextOccupied(int fromSeat, Func<Player, bool> predicate, bool includeCurrent, int fallback)
        {
            var ordered = _players.OrderBy(p => p.Seat).ToList();
            int startIndex = ordered.FindIndex(p => p.Seat == fromSeat);
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            for (int step = includeCurrent ? 0 : 1; step <= ordered.Count; step++)
            {
                if (includeCurrent && step == 0)
                {
                    // уже проверено условием ниже
                }

                int idx = (startIndex + step) % ordered.Count;
                if (predicate(ordered[idx]))
                {
                    return ordered[idx].Seat;
                }
            }

            return fallback;
        }

        private void LogLine(string message)
        {
            _log.Add(message);
        }
    }
}
