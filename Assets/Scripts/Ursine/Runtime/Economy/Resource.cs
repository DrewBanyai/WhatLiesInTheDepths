// Ursine — the resource, the cost, and why a purchase is refused.
//
// Field names are deliberately terse (k, n, c, m, r, g) because they are what a design
// document and a save file tend to call them. The readable properties beside them are the
// surface to prefer in new code.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ursine.Economy
{
    /// <summary>One resource: what is held, the ceiling it is held against, and the rate
    /// it is moving at.</summary>
    [Serializable]
    public class Res
    {
        public string k;        // id
        public string n;        // display name
        public double c;        // current
        public double m;        // ceiling
        public double r;        // rate per tick, as displayed: everything moving it
        /// <summary>The part of the rate the ledger applies on its own every tick (a
        /// building's yield, a slow drain). Work that pays out at completion — a task, a dive —
        /// is not in here, so it is never counted twice; it only shows in <see cref="r"/>.</summary>
        public double passive;
        public int g;           // group index — decides where it is listed and nothing else
        public string glyph;    // icon key
        /// <summary>What must have happened before this resource is shown at all. Read by
        /// the game against its own record of what has happened; empty means always.</summary>
        public List<string> requires;

        public string Id { get => k; set => k = value; }
        public string Name { get => n; set => n = value; }
        public double Current { get => c; set => c = value; }
        public double Ceiling { get => m; set => m = value; }
        public double Rate { get => r; set => r = value; }
        public int Group { get => g; set => g = value; }

        /// <summary>Held over ceiling, for a fill bar.</summary>
        public float Fill => m <= 0 ? 0f : Mathf.Clamp01((float)(c / m));

        /// <summary>At the ceiling. Usually drawn differently, because producing past a
        /// ceiling costs a player nothing but time and they should be able to see it.</summary>
        public bool Full => m > 0 && c >= m;

        /// <summary>A resource that works against the player, where the sign convention
        /// inverts: a positive rate is the bad one.</summary>
        public bool hostile;
    }

    /// <summary>An amount of one resource: a cost, or a gain.</summary>
    [Serializable]
    public class Amount
    {
        public string k;
        public double n;

        public Amount() { }
        public Amount(string id, double amount) { k = id; n = amount; }
    }

    /// <summary>Why a purchase cannot be made. The distinction matters to a player:
    /// being short resolves itself while they watch, but a cost above a ceiling never
    /// will — only a bigger reservoir fixes that, so the two should not look alike.</summary>
    public enum Refusal
    {
        None = 0,
        /// <summary>Short of a resource. Time fixes this.</summary>
        Short = 1,
        /// <summary>The cost is larger than the ceiling. Time will never fix this.</summary>
        AboveCeiling = 2
    }
}
