namespace PokerCore
{
    /// <summary>Масть карты.</summary>
    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }

    /// <summary>Достоинство карты (2..14, где 14 = туз).</summary>
    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    /// <summary>Тип покерной комбинации (сила poker hand).</summary>
    public enum HandType
    {
        HighCard = 0,
        Pair = 1,
        TwoPair = 2,
        ThreeOfAKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourOfAKind = 7,
        StraightFlush = 8
    }

    /// <summary>Улица (этап) раздачи.</summary>
    public enum Street
    {
        Preflop = 0,
        Flop = 1,
        Turn = 2,
        River = 3,
        Showdown = 4,
        HandComplete = 5
    }

    /// <summary>Действие игрока за столом.</summary>
    public enum ActionType
    {
        Fold = 0,
        Check = 1,
        Call = 2,
        Bet = 3,
        Raise = 4,
        AllIn = 5
    }
}
