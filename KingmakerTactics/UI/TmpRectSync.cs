using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KingmakerTactics.UI {
    // Kingmaker's TextMeshPro (old build in Assembly-CSharp-firstpass) keeps a label's mesh
    // from an earlier, wider rect when layout resizes the RectTransform afterwards: centered
    // text looks right, left-aligned text renders left of its rect and right-aligned text
    // right of it, by ~7% of the rect width. Measured on the deck 2026-10-03: TMP's computed
    // textBounds sit inside the rect, the drawn mesh does not. This component regenerates
    // every label under its GameObject whose rect size changed since its last mesh build.
    class TmpRectSync : MonoBehaviour {
        readonly RectSizeTracker<TMP_Text> tracker = new RectSizeTracker<TMP_Text>();
        readonly List<TMP_Text> buffer = new List<TMP_Text>();

        public static void Attach(GameObject root) {
            if (root.GetComponent<TmpRectSync>() == null) root.AddComponent<TmpRectSync>();
        }

        void LateUpdate() {
            GetComponentsInChildren(false, buffer);
            tracker.BeginScan();
            foreach (var label in buffer) {
                if (tracker.NeedsRebuild(label, label.rectTransform.rect.size)) label.ForceMeshUpdate();
            }
            tracker.EndScan();
            buffer.Clear();
        }
    }

    // Pure bookkeeping for TmpRectSync (unit-tested): remembers the rect size each key's mesh
    // was last built for; keys not seen during a scan are forgotten so destroyed labels do
    // not accumulate.
    internal class RectSizeTracker<T> where T : class {
        readonly Dictionary<T, Vector2> built = new Dictionary<T, Vector2>();
        readonly HashSet<T> seen = new HashSet<T>();
        bool scanning;

        public int Count => built.Count;

        public void BeginScan() {
            scanning = true;
            seen.Clear();
        }

        public bool NeedsRebuild(T key, Vector2 size) {
            if (scanning) seen.Add(key);
            if (built.TryGetValue(key, out var last) && last == size) return false;
            built[key] = size;
            return true;
        }

        public void EndScan() {
            scanning = false;
            if (seen.Count == built.Count) return;
            var stale = new List<T>();
            foreach (var key in built.Keys) {
                if (!seen.Contains(key)) stale.Add(key);
            }
            foreach (var key in stale) built.Remove(key);
        }
    }
}
