using System;
using System.Collections.Generic;
using System.Linq;
using PokerCore;
using PokerOnline;

namespace ClientSim
{
    /// <summary>
    /// Headless integration test for the client's own bot.
    ///
    /// Bot.cs and L.cs depend only on PokerCore, so they run on the desktop
    /// runtime without Unity. This catches problems that unit tests on the
    /// engine alone cannot: the bot proposing an action the engine rejects!
    ///
    /// Exit code 0 = everything is fine.
    /// </summary>
    internal static class Program
    {
        private const long Stack = 1000;
        private const int SeedCount = 400;
        private const int HandsPerSeed = 30;

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            int failures = 0;

            failures += CheckEncoding();
            failures += CheckLocalisationKeys();
            failures += CheckBotIntegration();
            failures += CheckBotMultiway();

            Console.WriteLine(failures == 0 ? "RESULT: OK" : "RESULT: FAILED");
            return failures == 0 ? 0 : 1;
        }

        /// <summary>
        /// The client sources are UTF-8 without a BOM. Roslyn decodes such files
        /// as UTF-8, but that is worth asserting rather than assuming, because
        /// silent mojibake in the UI would be hard to trace back.
        /// </summary>
        private static int CheckEncoding()
        {
            string title = L.T("title");
            Console.WriteLine("L.T(\"title\") = " + title);

            bool hasCyrillic = title.Any(ch => ch >= '\u0400' && ch <= '\u04FF');
            if (!hasCyrillic)
            {
                Console.WriteLine("ENCODING FAILURE: Russian text was not decoded correctly");
                return 1;
            }

            L.Toggle();
            string en = L.T("title");
            L.Toggle();
            Console.WriteLine("L.T(\"title\") EN = " + en);

            if (en == title)
            {
                Console.WriteLine("LOCALISATION FAILURE: language toggle did not change the string");
                return 1;
            }

            return 0;
        }

        /// <summary>Every key the table UI asks for must resolve.</summary>
        private static int CheckLocalisationKeys()
        {
            string[] usedKeys =
            {
                "title", "langToggle", "resetStacks", "bot", "stack", "yourCards",
                "board", "pot", "toCall", "you", "newHand", "fold", "check",
                "call", "allin", "bet", "raise", "yourTurn", "botThinking"
            };

            int missing = 0;
            foreach (string key in usedKeys)
            {
                if (L.T(key) == key)
                {
                    Console.WriteLine("MISSING localization key: " + key);
                    missing++;
                }
            }

            // The UI renders the street via Street.ToString().ToLowerInvariant().
            foreach (Street street in (Street[])Enum.GetValues(typeof(Street)))
            {
                string key = street.ToString().ToLowerInvariant();
                if (L.T(key) == key)
                {
                    Console.WriteLine("MISSING street key: " + key);
                    missing++;
                }
            }

            return missing;
        }

        /// <summary>
        /// Plays out many hands with the client's bot on every seat and checks
        /// that no action is rejected and that chips are never created or lost.
        /// </summary>
        private static int CheckBotIntegration()
        {
            long total = Stack * 2;
            int handsPlayed = 0;
            int illegal = 0;
            int stuck = 0;

            for (int seed = 0; seed < SeedCount; seed++)
            {
                var players = new List<Player>
                {
                    new Player(0, L.T("you"), Stack),
                    new Player(1, L.T("bot"), Stack),
                };
                var game = new TexasHoldemGame(players, 10, 20);

                for (int hand = 0; hand < HandsPerSeed; hand++)
                {
                    if (game.Players.Count(p => p.Chips > 0) < 2)
                    {
                        break;
                    }

                    game.StartHand(new SeededRandomSource(seed * 1000 + hand));

                    int guard = 0;
                    while (game.Street != Street.HandComplete && guard++ < 500)
                    {
                        int seat = game.ActiveSeat;
                        if (seat < 0)
                        {
                            Console.WriteLine($"seed={seed} hand={hand}: no active seat");
                            stuck++;
                            break;
                        }

                        try
                        {
                            PlayerAction action = Bot.Decide(game, seat);
                            game.ApplyAction(seat, action);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"seed={seed} hand={hand} seat={seat}: bot returned an illegal action -> {ex.Message}");
                            illegal++;
                            break;
                        }

                        long live = game.Players.Sum(p => p.Chips + p.TotalCommitted);
                        if (live != total)
                        {
                            Console.WriteLine($"seed={seed} hand={hand}: chip leak, live={live} expected={total}");
                            return 1;
                        }
                    }

                    if (game.Street != Street.HandComplete)
                    {
                        Console.WriteLine($"seed={seed} hand={hand}: hand stuck on {game.Street} after {guard} actions");
                        stuck++;
                        break;
                    }

                    long settled = game.Players.Sum(p => p.Chips);
                    if (settled != total)
                    {
                        Console.WriteLine($"seed={seed} hand={hand}: chips not conserved, settled={settled} expected={total}");
                        return 1;
                    }

                    handsPlayed++;
                }
            }

            Console.WriteLine($"hands played: {handsPlayed}");
            Console.WriteLine($"illegal bot actions: {illegal}");
            Console.WriteLine($"stuck/aborted hands: {stuck}");

            return illegal + stuck;
        }

        /// <summary>
        /// The bot does not only play heads-up: a real table seats several players.
        /// This plays full six-handed tables with the bot on every seat, which is
        /// where multi-level side pots actually occur.
        /// </summary>
        private static int CheckBotMultiway()
        {
            const int Seats = 6;
            long total = Stack * Seats;
            int handsPlayed = 0;
            int illegal = 0;
            int stuck = 0;

            for (int seed = 0; seed < 200; seed++)
            {
                var players = new List<Player>();
                for (int seat = 0; seat < Seats; seat++)
                {
                    players.Add(new Player(seat, "P" + (seat + 1), Stack));
                }

                var game = new TexasHoldemGame(players, 10, 20);

                for (int hand = 0; hand < HandsPerSeed; hand++)
                {
                    if (game.Players.Count(p => p.Chips > 0) < 2)
                    {
                        break;
                    }

                    game.StartHand(new SeededRandomSource(seed * 77 + hand));

                    int guard = 0;
                    while (game.Street != Street.HandComplete && guard++ < 500)
                    {
                        int active = game.ActiveSeat;
                        if (active < 0)
                        {
                            Console.WriteLine($"six-max seed={seed} hand={hand}: no active seat");
                            stuck++;
                            break;
                        }

                        try
                        {
                            game.ApplyAction(active, Bot.Decide(game, active));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"six-max seed={seed} hand={hand} seat={active}: bot returned an illegal action -> {ex.Message}");
                            illegal++;
                            break;
                        }

                        long live = game.Players.Sum(p => p.Chips + p.TotalCommitted);
                        if (live != total)
                        {
                            Console.WriteLine($"six-max seed={seed} hand={hand}: chip leak, live={live} expected={total}");
                            return 1;
                        }
                    }

                    if (game.Street != Street.HandComplete)
                    {
                        Console.WriteLine($"six-max seed={seed} hand={hand}: hand stuck on {game.Street}");
                        stuck++;
                        break;
                    }

                    if (game.Players.Sum(p => p.Chips) != total)
                    {
                        Console.WriteLine($"six-max seed={seed} hand={hand}: chips not conserved after the award");
                        return 1;
                    }

                    handsPlayed++;
                }
            }

            Console.WriteLine($"six-max hands played: {handsPlayed}");
            Console.WriteLine($"six-max illegal bot actions: {illegal}");
            Console.WriteLine($"six-max stuck/aborted hands: {stuck}");

            return illegal + stuck;
        }
    }
}
