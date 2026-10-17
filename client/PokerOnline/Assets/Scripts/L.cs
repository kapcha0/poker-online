using System.Collections.Generic;

namespace PokerOnline
{
    /// <summary>Локализация ru/en (система заложена с этапа 2).</summary>
    public static class L
    {
        public enum Lang
        {
            Ru = 0,
            En = 1
        }

        public static Lang Current { get; set; } = Lang.Ru;

        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>
        {
            { "title", "Poker Онлайн — локальный прототип" },
            { "pot", "Банк" },
            { "yourCards", "Ваши карты" },
            { "bot", "Бот" },
            { "you", "Вы" },
            { "toCall", "До колла" },
            { "fold", "Фолд" },
            { "check", "Чек" },
            { "call", "Колл" },
            { "bet", "Бет" },
            { "raise", "Рейз" },
            { "allin", "Олл-ин" },
            { "newHand", "Новая раздача" },
            { "yourTurn", "Ваш ход" },
            { "botThinking", "Бот думает…" },
            { "stack", "Стек" },
            { "board", "Стол" },
            { "preflop", "Префлоп" },
            { "flop", "Флоп" },
            { "turn", "Тёрн" },
            { "river", "Ривер" },
            { "showdown", "Вскрытие" },
            { "handOver", "Раздача завершена" },
            { "langToggle", "EN" },
            { "resetStacks", "Сброс стеков" },
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "title", "Poker Online — local prototype" },
            { "pot", "Pot" },
            { "yourCards", "Your cards" },
            { "bot", "Bot" },
            { "you", "You" },
            { "toCall", "To call" },
            { "fold", "Fold" },
            { "check", "Check" },
            { "call", "Call" },
            { "bet", "Bet" },
            { "raise", "Raise" },
            { "allin", "All-in" },
            { "newHand", "New hand" },
            { "yourTurn", "Your turn" },
            { "botThinking", "Bot is thinking…" },
            { "stack", "Stack" },
            { "board", "Board" },
            { "preflop", "Preflop" },
            { "flop", "Flop" },
            { "turn", "Turn" },
            { "river", "River" },
            { "showdown", "Showdown" },
            { "handOver", "Hand complete" },
            { "langToggle", "RU" },
            { "resetStacks", "Reset stacks" },
        };

        public static string T(string key)
        {
            Dictionary<string, string> dict = Current == Lang.Ru ? Ru : En;
            return dict.TryGetValue(key, out string value) ? value : key;
        }

        public static void Toggle()
        {
            Current = Current == Lang.Ru ? Lang.En : Lang.Ru;
        }
    }
}
