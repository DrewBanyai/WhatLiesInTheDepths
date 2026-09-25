// Ursine — the set of things that have happened.
//
// An unlock is a string. Setting one is the only way anything in a game built on this opens
// up, and nothing ever unsets one except starting over. What a screen shows is then a pure
// question asked of this set ("are my requirements met?") rather than a listener that has to
// be told, in the right order, at the right time — which is why a load needs no catch-up:
// restore the set and every screen is already right.
using System;
using System.Collections.Generic;

namespace Ursine
{
    public sealed class Unlocks
    {
        readonly HashSet<string> _set = new HashSet<string>();

        /// <summary>Raised once, the moment an id is set for the first time.</summary>
        public event Action<string> Unlocked;

        public int Count => _set.Count;
        public IEnumerable<string> All => _set;

        public bool Has(string id) => !string.IsNullOrEmpty(id) && _set.Contains(id);

        /// <summary>Sets an id. Returns true only the first time; setting it again changes
        /// nothing and announces nothing.</summary>
        public bool Add(string id)
        {
            if (string.IsNullOrEmpty(id) || !_set.Add(id)) return false;
            Unlocked?.Invoke(id);
            return true;
        }

#if UNITY_EDITOR
        /// <summary>Editor only, and only for a debugger: the game never unsets an unlock, and
        /// nothing in a build can call this. Returns true if the id was set.</summary>
        public bool EditorRemove(string id) => !string.IsNullOrEmpty(id) && _set.Remove(id);

        /// <summary>Editor only: forget everything. For putting a dream back to its opening.</summary>
        public void EditorClear() => _set.Clear();
#endif

        public void AddRange(IEnumerable<string> ids)
        {
            if (ids == null) return;
            foreach (var id in ids) Add(id);
        }

        /// <summary>Every id set. An empty or missing list is always met.</summary>
        public bool AllOf(IEnumerable<string> ids)
        {
            if (ids == null) return true;
            foreach (var id in ids) if (!Has(id)) return false;
            return true;
        }

        /// <summary>A hard reset: forget everything. Announces nothing.</summary>
        public void Clear() => _set.Clear();
    }
}
