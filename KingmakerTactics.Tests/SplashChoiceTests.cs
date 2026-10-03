using System.Collections.Generic;
using KingmakerTactics.Engine;
using KingmakerTactics.Models;
using Xunit;

namespace KingmakerTactics.Tests {
    // Kingmaker IL: ItemEntity.IsUsableFromInventory == !Player.IsInCombat for usable items,
    // so in combat only quick-slot flasks can be thrown. Candidates arrive quick slots first.
    public class SplashChoiceTests {
        const string AcidFlask = "4639724c4a9cc9544a2f622b66931658";
        const string AlchemistsFire = "fd56596e273d1ff49a8c29cc9802ae6e";

        [Fact]
        public void Unusable_candidates_are_never_chosen() {
            var c = new List<(string, bool)> { (AlchemistsFire, false), (AcidFlask, false) };
            Assert.Null(SplashItemResolver.ChooseIndex(c, ThrowSplashMode.Any));
        }

        [Fact]
        public void Any_takes_the_first_usable() {
            var c = new List<(string, bool)> { (AlchemistsFire, false), (AcidFlask, true), (AlchemistsFire, true) };
            Assert.Equal(1, SplashItemResolver.ChooseIndex(c, ThrowSplashMode.Any));
        }

        [Fact]
        public void Strongest_prefers_alchemists_fire_among_usable() {
            var c = new List<(string, bool)> { (AcidFlask, true), (AlchemistsFire, true) };
            Assert.Equal(1, SplashItemResolver.ChooseIndex(c, ThrowSplashMode.Strongest));
        }

        [Fact]
        public void Non_splash_items_are_ignored() {
            var c = new List<(string, bool)> { ("00000000000000000000000000000000", true) };
            Assert.Null(SplashItemResolver.ChooseIndex(c, ThrowSplashMode.Any));
        }

        // Holy Water does not exist in Kingmaker (blueprint index): no dead registry entries.
        [Fact]
        public void Registry_holds_only_kingmaker_items() =>
            Assert.False(SplashItemRegistry.IsSplashItem("a8bc157a846e2d64498915cadd026aef"));
    }
}
