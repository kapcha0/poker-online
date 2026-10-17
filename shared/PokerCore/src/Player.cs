using System;
using System.Collections.Generic;

namespace PokerCore
{
    /// <summary>Место (seat) за столом и состояние стека/ставок игрока.</summary>
    public sealed class Player
    {
        public Player(int seat, string name, long chips)
        {
            Seat = seat;
            Name = name;
            Chips = chips;
            StartingChips = chips;
        }

        public int Seat { get; }

        public string Name { get; set; }

        public long StartingChips { get; }

        /// <summary>Текущий стек (не включая уже поставленные в банк фишки).</summary>
        public long Chips { get; internal set; }

        /// <summary>Внесено в банк в текущей улице (BetThisRound).</summary>
        public long BetThisRound { get; internal set; }

        /// <summary>Внесено в банк за всю раздачу (для сайд-потов).</summary>
        public long TotalCommitted { get; internal set; }

        public Card Hole1 { get; internal set; }

        public Card Hole2 { get; internal set; }

        public bool HasCards { get; internal set; }

        public bool Folded { get; internal set; }

        public bool AllIn { get; internal set; }

        public bool HasActed { get; internal set; }

        /// <summary>В игре (не сбросил карты).</summary>
        public bool InHand => HasCards && !Folded;

        /// <summary>Может делать ставки (в раздаче и не all-in).</summary>
        public bool CanAct => InHand && !AllIn;

        /// <summary>Дилерская кнопка.</summary>
        public bool IsDealer { get; internal set; }

        public bool IsSmallBlind { get; internal set; }

        public bool IsBigBlind { get; internal set; }

        public IReadOnlyList<Card> HoleCards => new[] { Hole1, Hole2 };

        /// <summary>Сброс состояния перед новой раздачей (стек сохраняется).</summary>
        internal void ResetForNewHand()
        {
            BetThisRound = 0;
            TotalCommitted = 0;
            Folded = false;
            AllIn = false;
            HasActed = false;
            HasCards = false;
            Hole1 = default;
            Hole2 = default;
            IsDealer = false;
            IsSmallBlind = false;
            IsBigBlind = false;
        }

        public override string ToString() => $"{Name} (seat {Seat}, chips {Chips})";
    }
}
