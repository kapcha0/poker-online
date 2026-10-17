using System.Collections.Generic;
using System.Linq;
using PokerCore;
using UnityEngine;

namespace PokerOnline
{
    /// <summary>
    /// Локальный игровой стол MVP: игрок против бота.
    /// UI на IMGUI — намеренно простой, чтобы прототип гарантированно запускался
    /// и в Play Mode, и в собранном билде. Позже заменяется на uGUI/UI Toolkit.
    /// </summary>
    public sealed class PokerTableController : MonoBehaviour
    {
        private const long StartingStack = 1000;
        private const long SmallBlind = 10;
        private const long BigBlind = 20;

        private TexasHoldemGame _game = null!;
        private int _humanSeat;
        private int _botSeat;
        private float _botDelay;
        private Vector2 _logScroll;

        private void Awake()
        {
            CreateGame();
        }

        private void CreateGame()
        {
            var players = new List<Player>
            {
                new Player(0, L.T("you"), StartingStack),
                new Player(1, L.T("bot"), StartingStack)
            };

            _game = new TexasHoldemGame(players, SmallBlind, BigBlind);
            _humanSeat = 0;
            _botSeat = 1;
        }

        private void Update()
        {
            if (!_game.HandInProgress || _game.ActiveSeat != _botSeat)
            {
                return;
            }

            _botDelay -= Time.deltaTime;
            if (_botDelay > 0f)
            {
                return;
            }

            _botDelay = 0.7f;
            PlayerAction action = Bot.Decide(_game, _botSeat);
            SafeApply(_botSeat, action);
        }

        private void SafeApply(int seat, PlayerAction action)
        {
            try
            {
                _game.ApplyAction(seat, action);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Не удалось применить действие: {ex.Message}");
            }
        }

        private void NewHand()
        {
            if (_game.HandInProgress)
            {
                return;
            }

            if (_game.Players.Any(p => p.Chips <= 0))
            {
                ResetStacks();
                return;
            }

            _botDelay = 0.8f;
            _game.StartHand();
        }

        private void ResetStacks()
        {
            // Проще всего пересоздать игру с полными стеками.
            CreateGame();
        }

        private void OnGUI()
        {
            EnsureSkin();

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("title"), GUILayout.Width(360));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L.T("langToggle"), GUILayout.Width(60)))
            {
                L.Toggle();
                RenamePlayers();
            }

            if (GUILayout.Button(L.T("resetStacks"), GUILayout.Width(140)))
            {
                ResetStacks();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            Player bot = _game.GetPlayer(_botSeat);
            Player human = _game.GetPlayer(_humanSeat);

            // --- Бот ---
            GUILayout.Label($"{L.T("bot")} — {L.T("stack")}: {bot.Chips}");
            bool revealBot = !_game.HandInProgress;
            GUILayout.Label(revealBot && bot.HasCards
                ? $"{L.T("yourCards")}: {CardText(bot.Hole1)} {CardText(bot.Hole2)}"
                : "🂠 🂠");

            GUILayout.Space(10);

            // --- Стол ---
            GUILayout.Label($"{L.T("board")} ({L.T(_game.Street.ToString().ToLowerInvariant())}): {BoardText()}");
            GUILayout.Label($"{L.T("pot")}: {(_game.HandInProgress ? _game.Pot : _game.LastPot)}");
            if (_game.HandInProgress)
            {
                long toCall = _game.CurrentBet - human.BetThisRound;
                GUILayout.Label($"{L.T("toCall")}: {System.Math.Max(0, toCall)}");
            }

            GUILayout.Space(10);

            // --- Человек ---
            GUILayout.Label($"{L.T("you")} — {L.T("stack")}: {human.Chips}");
            GUILayout.Label(human.HasCards
                ? $"{L.T("yourCards")}: {CardText(human.Hole1)} {CardText(human.Hole2)}"
                : "🂠 🂠");

            GUILayout.Space(10);

            // --- Журнал ---
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(150));
            foreach (string line in _game.Log.Reverse())
            {
                GUILayout.Label(line);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // --- Управление ---
            if (!_game.HandInProgress)
            {
                if (GUILayout.Button(L.T("newHand"), GUILayout.Height(40)))
                {
                    NewHand();
                }

                foreach (PotAward award in _game.LastAwards)
                {
                    GUILayout.Label($"{_game.GetPlayer(award.Seat).Name}: +{award.Amount}");
                }
            }
            else if (_game.ActiveSeat == _humanSeat)
            {
                GUILayout.Label(L.T("yourTurn"));
                DrawHumanActions(human);
            }
            else
            {
                GUILayout.Label(L.T("botThinking"));
            }

            GUILayout.EndArea();
        }

        private void DrawHumanActions(Player human)
        {
            var legal = _game.GetLegalActions(_humanSeat);
            if (legal.Count == 0)
            {
                return;
            }

            long toCall = _game.CurrentBet - human.BetThisRound;
            long allInTotal = human.BetThisRound + human.Chips;

            GUILayout.BeginHorizontal();

            if (legal.Any(a => a.Type == ActionType.Fold) &&
                GUILayout.Button(L.T("fold"), GUILayout.Height(36)))
            {
                SafeApply(_humanSeat, PlayerAction.Fold());
                return;
            }

            if (legal.Any(a => a.Type == ActionType.Check) &&
                GUILayout.Button(L.T("check"), GUILayout.Height(36)))
            {
                SafeApply(_humanSeat, PlayerAction.Check());
                return;
            }

            if (toCall > 0 && legal.Any(a => a.Type == ActionType.Call) &&
                GUILayout.Button($"{L.T("call")} ({System.Math.Min(toCall, human.Chips)})", GUILayout.Height(36)))
            {
                SafeApply(_humanSeat, PlayerAction.Call());
                return;
            }

            GUILayout.EndHorizontal();

            // Ставки/рейзы: минимальный, полбанка, банк, олл-ин.
            GUILayout.BeginHorizontal();

            bool canBet = _game.CurrentBet == 0;
            long minTarget = canBet ? BigBlind : _game.CurrentBet + _game.MinRaise;
            long halfPotTarget = _game.CurrentBet + System.Math.Max(_game.MinRaise, (_game.Pot + toCall) / 2);
            long potTarget = _game.CurrentBet + System.Math.Max(_game.MinRaise, _game.Pot + toCall);

            DrawAggressiveButton(canBet, minTarget, allInTotal, canBet ? "bet" : "raise", null);
            DrawAggressiveButton(canBet, halfPotTarget, allInTotal, canBet ? "bet" : "raise", "/2");
            DrawAggressiveButton(canBet, potTarget, allInTotal, canBet ? "bet" : "raise", "pot");

            if (legal.Any(a => a.Type == ActionType.AllIn) &&
                GUILayout.Button($"{L.T("allin")} ({human.Chips})", GUILayout.Height(36)))
            {
                SafeApply(_humanSeat, PlayerAction.AllIn());
                return;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawAggressiveButton(bool isBet, long target, long allInTotal, string labelKey, string? suffix)
        {
            if (target < allInTotal && target <= 0)
            {
                return;
            }

            long clamped = System.Math.Min(target, allInTotal);
            if (clamped <= 0)
            {
                return;
            }

            string label = $"{L.T(labelKey)} {clamped}" + (suffix == null ? string.Empty : $" {suffix}");
            if (GUILayout.Button(label, GUILayout.Height(36)))
            {
                PlayerAction action = isBet
                    ? PlayerAction.BetTo(clamped)
                    : PlayerAction.RaiseTo(clamped);
                SafeApply(_humanSeat, action);
            }
        }

        private void RenamePlayers()
        {
            _game.GetPlayer(_humanSeat).Name = L.T("you");
            _game.GetPlayer(_botSeat).Name = L.T("bot");
        }

        private string BoardText()
        {
            if (_game.CommunityCards.Count == 0)
            {
                return "—";
            }

            return string.Join(" ", _game.CommunityCards.Select(CardText));
        }

        private static string CardText(Card c)
        {
            string rank;
            switch (c.Rank)
            {
                case Rank.Ten: rank = "10"; break;
                case Rank.Jack: rank = "J"; break;
                case Rank.Queen: rank = "Q"; break;
                case Rank.King: rank = "K"; break;
                case Rank.Ace: rank = "A"; break;
                default: rank = ((int)c.Rank).ToString(); break;
            }

            string suit;
            switch (c.Suit)
            {
                case Suit.Spades: suit = "♠"; break;
                case Suit.Hearts: suit = "♥"; break;
                case Suit.Diamonds: suit = "♦"; break;
                default: suit = "♣"; break;
            }

            return rank + suit;
        }

        private static GUIStyle? _label;

        private static void EnsureSkin()
        {
            if (_label != null)
            {
                return;
            }

            _label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            GUI.skin.label.fontSize = 16;
            GUI.skin.button.fontSize = 16;
        }
    }
}
