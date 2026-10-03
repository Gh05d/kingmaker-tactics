using System;
using System.Collections.Generic;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Shared "seed once" planning for default presets and default packs. A sentinel file
    /// lists every id ever seeded, so user deletions and edits are never undone and a new
    /// mod version only adds ids it has not seeded before. Pure — unit-tested.
    /// </summary>
    internal static class DefaultSeeding {
        /// <summary>
        /// Returns the default ids to write now and adds every default id to <paramref name="seeded"/>.
        /// Ids whose file already exists (upgrade from a pre-sentinel version) are only marked.
        /// </summary>
        public static List<string> Plan(IEnumerable<string> defaultIds, ISet<string> seeded, Func<string, bool> exists) {
            var write = new List<string>();
            foreach (var id in defaultIds) {
                if (seeded.Contains(id)) continue;
                if (!exists(id)) write.Add(id);
                seeded.Add(id);
            }
            return write;
        }

        /// <summary>
        /// Writes the planned ids; a failed save is removed from <paramref name="seeded"/> again
        /// so the next load retries it instead of treating it as a user deletion. Returns the
        /// number written.
        /// </summary>
        public static int Write(IEnumerable<string> toWrite, ISet<string> seeded, Func<string, bool> save) {
            int written = 0;
            foreach (var id in toWrite) {
                if (save(id)) written++;
                else seeded.Remove(id);
            }
            return written;
        }
    }
}
