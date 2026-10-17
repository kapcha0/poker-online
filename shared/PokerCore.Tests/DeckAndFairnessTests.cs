using System.Collections.Generic;
using System.Linq;
using PokerCore;
using Xunit;

namespace PokerCore.Tests
{
    public class DeckAndFairnessTests
    {
        [Fact]
        public void FullDeck_Has52UniqueCards()
        {
            List<Card> deck = Deck.CreateFullDeck();
            Assert.Equal(52, deck.Count);
            Assert.Equal(52, deck.Distinct().Count());
        }

        [Fact]
        public void Shuffle_WithSameSeed_IsDeterministic()
        {
            var d1 = new Deck();
            d1.Shuffle(new SeededRandomSource(42));
            var d2 = new Deck();
            d2.Shuffle(new SeededRandomSource(42));
            Assert.Equal(d1.Snapshot, d2.Snapshot);
        }

        [Fact]
        public void ProvablyFair_SameSeeds_SameOrder()
        {
            Deck a = ProvablyFair.CreateShuffledDeck("server-seed", "client-seed", 1);
            Deck b = ProvablyFair.CreateShuffledDeck("server-seed", "client-seed", 1);
            Assert.Equal(a.Snapshot, b.Snapshot);
        }

        [Fact]
        public void ProvablyFair_DifferentNonce_DifferentOrder()
        {
            Deck a = ProvablyFair.CreateShuffledDeck("server-seed", "client-seed", 1);
            Deck b = ProvablyFair.CreateShuffledDeck("server-seed", "client-seed", 2);
            Assert.NotEqual(a.Snapshot, b.Snapshot);
        }

        [Fact]
        public void ProvablyFair_Hash_IsStableHex()
        {
            string hash = ProvablyFair.HashServerSeed("abc");
            Assert.Equal(64, hash.Length);
            Assert.Equal(hash, ProvablyFair.HashServerSeed("abc"));
            Assert.NotEqual(hash, ProvablyFair.HashServerSeed("abd"));
        }

        [Fact]
        public void CryptoShuffle_KeepsAllCards()
        {
            var deck = new Deck();
            deck.Shuffle();
            Assert.Equal(52, deck.Snapshot.Count);
            Assert.Equal(52, deck.Snapshot.Distinct().Count());
        }
    }
}
