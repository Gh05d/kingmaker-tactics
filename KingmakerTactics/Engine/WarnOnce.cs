using System.Collections.Generic;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// One WARN per (unit, rule) until Reset. A pack rule the unit cannot perform (no Rage,
    /// no Haste slot) matches every 3 s tick; without this the log drowns in identical lines.
    /// </summary>
    public class WarnOnce {
        readonly HashSet<(string, string)> warned = new HashSet<(string, string)>();

        public bool ShouldWarn(string unitId, string ruleId) => warned.Add((unitId, ruleId));

        public void Reset() => warned.Clear();
    }
}
