// What Lies In The Depths — whether there is a resource in the ledger you have not looked at.
//
// "A row appearing is the only announcement that a resource exists" (LedgerView). That is
// true and it is enough while the ledger is a column, because a column is simply there. In
// the short stage it is in a drawer, and a row appearing inside a shut drawer announces
// nothing at all — so the drawer's own handle has to carry the news instead.
//
// Seen-ness is kept as an unlock per resource, because an unlock is exactly "a thing that has
// happened" and the player having had a resource in front of them is one. It comes with the
// save, so it survives a reload; it is add-only, so it can never un-see something; and a hard
// reset clears it with everything else, which is right — a new dream's resources are new.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;

namespace WhatLiesInTheDepths.UI
{
    public static class LedgerAttention
    {
        /// <summary>One id per resource the player has had in front of them. The prefix keeps
        /// them together and tells anyone reading a save what they are.</summary>
        public const string SeenPrefix = "ledger:seen:";

        /// <summary>A resource is shown that the player has not had in front of them. Never a
        /// count: one waiting and nine waiting mean the same thing, which is "look".</summary>
        public static bool Unseen { get; private set; }

        /// <summary>Whether the ledger is in front of the player right now. In Full and in
        /// Compact 2 it always is — it is a column, and a column is simply there. In Compact 1
        /// it is in the drawer, so only while that is open.</summary>
        public static bool Watching
        {
            get
            {
                var d = ColumnDrawer.I;
                return d == null || !d.Holds(Folded.Ledger) || d.IsOpen;
            }
        }

        /// <summary>Marks what is on screen as seen, or counts what is not. Called when the
        /// ledger is rebuilt, when the drawer opens or shuts, and when the stage changes shape —
        /// which between them is every moment either half of the answer can have changed.</summary>
        public static void Sync()
        {
            var s = GameState.I;
            if (s == null) { Unseen = false; return; }

            // Not until the stage has chosen a shape. Start order among components is not
            // defined, so the ledger can be built before the director has run — and at that
            // moment the drawer has not been told anything yet and reads as a standing column,
            // which would mark everything seen before the player had a chance to see it. The
            // director's own Sync, at the end of SetProfile, is the first one that counts.
            if (StageDirector.I == null || StageDirector.I.Profile == null) return;

            bool watching = Watching;
            bool waiting = false;

            foreach (var r in s.ShownResources)
            {
                if (r == null || string.IsNullOrEmpty(r.k)) continue;
                string id = SeenPrefix + r.k;
                if (watching) s.unlocks.Add(id);
                else if (!s.unlocks.Has(id)) waiting = true;
            }

            Unseen = waiting;
        }
    }
}
