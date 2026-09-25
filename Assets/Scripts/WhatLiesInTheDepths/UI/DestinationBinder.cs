// What Lies In The Depths — what a bar item does. Kept off the bar prefabs because what an item does
// is a fact about the screen, not about the bar.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using System.Linq;
using UnityEngine;
using Ursine.Combat;
using Ursine.Economy;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>A center destination. It takes the underline when the center is showing it,
    /// and carries the dot when it is holding something the player has not seen.</summary>
    public sealed class DestinationBinder : MonoBehaviour
    {
        public Destination destination;
        public BarItem item;

        void Start()
        {
            if (item == null) item = GetComponent<BarItem>();
            if (item == null) return;

            item.Configure(
                () => Router.I != null && Router.I.IsLit(destination),
                Unseen,
                () => Router.I?.Open(destination));

            if (Router.I != null) Router.I.Changed += OnRouted;
            if (GameState.I != null) GameState.I.Changed += Sync;
            _wasLit = Router.I != null && Router.I.IsLit(destination);
            Sync();
        }

        void OnDestroy()
        {
            if (Router.I != null) Router.I.Changed -= OnRouted;
            if (GameState.I != null) GameState.I.Changed -= Sync;
        }

        bool _wasLit;

        void OnRouted()
        {
            bool lit = Router.I != null && Router.I.IsLit(destination);
            // Leaving a destination means its new things have been looked at: every card in it
            // wore its own dot while the player was there, and the bar's dot should not greet
            // them again on the way out. Some new things have no card to rest on at all (The
            // Silent Altar is a landmark), so a visit is the only way they could ever be seen.
            if (_wasLit && !lit) MarkVisited();
            _wasLit = lit;
            item.Refresh();
        }

        void MarkVisited()
        {
            var s = GameState.I;
            if (s == null) return;
            switch (destination)
            {
                case Destination.Focus:
                    foreach (var t in s.ShownTasks) t.seen = true;
                    break;
                case Destination.Constructs:
                    foreach (var c in s.ShownConstructs) c.seen = true;
                    break;
            }
        }

        /// <summary>A destination that has nothing in it does not appear, and a new one simply
        /// appears in its fixed slot, wearing its dot. No fade, no announcement. The bar's row
        /// is a layout group, so the others close up around an absent one.</summary>
        void Sync()
        {
            var s = GameState.I;
            if (s == null || item == null) return;
            bool open = s.MenuOpen(destination.ToString());
            if (item.gameObject.activeSelf != open) item.gameObject.SetActive(open);
            if (open) item.Refresh();
        }

        /// <summary>The dot means new data inside, and it is the same dot every time. Never
        /// a rate change and never a resource filling — the ledger already says those.</summary>
        bool Unseen()
        {
            var s = GameState.I;
            if (s == null) return false;
            switch (destination)
            {
                case Destination.Journal:
                    return s.journal.Any(e => !e.seen);
                case Destination.Focus:
                    // A task with free Oneiri sitting idle, or a card never looked at.
                    return s.ShownTasks.Any(t => !t.seen)
                        || (s.BindingOpen && s.OneiriFree > 0 && s.ShownTasks.Any(t => t.w < t.cap));
                case Destination.Revelations:
                    return s.ShownRevelations.Any(r => s.Judge(r.cost) == Refusal.None);
                case Destination.Constructs:
                    return s.ShownConstructs.Any(c => !c.seen);
                case Destination.Visions:
                    return s.visions.Any(v => s.Shown(v) && v.of != null && v.of.Count > 0
                                              && s.Held(v.of[Mathf.Clamp(v.sel, 0, v.of.Count - 1)].r)
                                                 >= v.OfferCost(v.of[Mathf.Clamp(v.sel, 0, v.of.Count - 1)]));
                case Destination.Assault:
                    var next = s.road.FirstOrDefault(l => !l.won);
                    return next != null && Odds.Chance(s.Dream.Army, next.en) > 0 && s.Open(next);
            }
            return false;
        }
    }
}
