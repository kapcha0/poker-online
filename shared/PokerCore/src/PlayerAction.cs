namespace PokerCore
{
    /// <summary>Действие игрока.</summary>
    public sealed class PlayerAction
    {
        public PlayerAction(ActionType type, long amount = 0)
        {
            Type = type;
            Amount = amount;
        }

        public ActionType Type { get; }

        /// <summary>
        /// Для Bet/Raise — итоговая сумма ставки этого игрока в текущей улице (BetThisRound).
        /// Для остальных действий значение не используется.
        /// </summary>
        public long Amount { get; }

        public override string ToString() =>
            Amount > 0 ? $"{Type}({Amount})" : Type.ToString();

        public static PlayerAction Fold() => new PlayerAction(ActionType.Fold);

        public static PlayerAction Check() => new PlayerAction(ActionType.Check);

        public static PlayerAction Call() => new PlayerAction(ActionType.Call);

        public static PlayerAction BetTo(long total) => new PlayerAction(ActionType.Bet, total);

        public static PlayerAction RaiseTo(long total) => new PlayerAction(ActionType.Raise, total);

        public static PlayerAction AllIn() => new PlayerAction(ActionType.AllIn);
    }
}
