using KingmakerTactics.UI;
using UnityEngine;
using Xunit;

namespace KingmakerTactics.Tests {
    public class RectSizeTrackerTests {
        [Fact]
        public void FirstSightingNeedsRebuild() {
            var t = new RectSizeTracker<object>();
            Assert.True(t.NeedsRebuild(new object(), new Vector2(100, 20)));
        }

        [Fact]
        public void UnchangedSizeDoesNotRebuild() {
            var t = new RectSizeTracker<object>();
            var label = new object();
            t.NeedsRebuild(label, new Vector2(100, 20));
            Assert.False(t.NeedsRebuild(label, new Vector2(100, 20)));
        }

        // The Kingmaker bug: layout shrinks the rect after TMP built its mesh.
        [Fact]
        public void ChangedSizeRebuildsOnce() {
            var t = new RectSizeTracker<object>();
            var label = new object();
            t.NeedsRebuild(label, new Vector2(1600, 20));
            Assert.True(t.NeedsRebuild(label, new Vector2(1404, 20)));
            Assert.False(t.NeedsRebuild(label, new Vector2(1404, 20)));
        }

        [Fact]
        public void PruneForgetsUnseenKeys() {
            var t = new RectSizeTracker<object>();
            var gone = new object();
            var kept = new object();
            t.NeedsRebuild(gone, new Vector2(10, 10));
            t.NeedsRebuild(kept, new Vector2(10, 10));
            t.BeginScan();
            t.NeedsRebuild(kept, new Vector2(10, 10));
            t.EndScan();
            Assert.Equal(1, t.Count);
        }
    }
}
