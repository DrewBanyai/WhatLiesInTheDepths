// What Lies In The Depths — the game's own data.
//
// The generic pieces — a resource, an amount, why a purchase is refused, odds from a
// strength ratio — live in Ursine. What is here is what this game specifically has:
// veils, visions, revelations, constructs, a road and a journal.
//
// Field names follow the brief's section 5 so they can be grepped against the spec files.
//
// EVERY NUMBER THAT REACHES THESE TYPES FROM THE SPEC SET IS A PLACEHOLDER. Costs, rates,
// ceilings, enemy strengths, unit powers, Oneiri counts, day counts and percentages were
// chosen to make the interface legible at a representative mid-game state. None of it is
// balanced. Treat each as an example of the shape a number takes in that slot.
using System;
using System.Collections.Generic;
using UnityEngine;
using Ursine.Economy;

namespace WhatLiesInTheDepths.Data
{
    /// <summary>A resource's group decides where it sits in the ledger and nothing else.
    /// Maps onto Ursine's Res.g.</summary>
    public enum ResGroup { Gathered = 0, Yours = 1, AgainstYou = 2 }

    [Serializable]
    public class FocusTask
    {
        public string id;
        public string g;              // glyph key
        public string n;              // name
        public string b;              // blurb
        public List<Amount> cost;     // per completion, may be null
        public List<Amount> gain;
        public double baseSeconds;    // seconds of work for one completion at one Oneiri
        public int w;                 // Oneiri bound
        public int cap;               // maximum bindable
        public float p;               // progress 0-100
        public string section;        // Labor, Tending, Listening
        public bool seen = true;      // a newly unlocked card wears the iris dot
        /// <summary>Conditions, all of which must hold before the card exists (see
        /// <see cref="Conditions"/>). Once it has appeared it never goes away.</summary>
        public List<string> requires;

        /// <summary>The player's own attention is worth two Oneiri laid on top of whatever
        /// is bound, which is why the rate line moves when they arrive or leave.</summary>
        public double Period(bool attended)
        {
            int effective = w + (attended ? 2 : 0);
            return effective <= 0 || speed <= 0 ? double.PositiveInfinity : baseSeconds / effective / speed;
        }

        /// <summary>How much faster than its base it runs, from everything that speeds work up
        /// and Dread slowing it down. Written by the dream every recompute; 1 is untouched.</summary>
        [NonSerialized] public double speed = 1.0;
        /// <summary>The authored gain. <see cref="gain"/> is this scaled by every effect on
        /// the task, rewritten each recompute, so a card always shows what it will pay.</summary>
        public List<Amount> baseGain;
    }

    public enum ConstructKind { Works = 0, Dwellings = 1, Reservoirs = 2, Wards = 3 }

    [Serializable]
    public class ConstructDef
    {
        public string g;              // glyph / plate key
        public string n;
        public ConstructKind kind;    // also the section, and its quarter of the grounds
        public int owned;
        public string b;              // blurb
        public List<string> fx;       // final values, never deltas over the previous tier
        public List<Amount> cost;
        public Refusal state;
        public bool seen = true;
        public Vector2 palacePos;     // position on the Mind Palace map
        public bool built = true;     // false draws the dashed unbuilt plot

        public List<string> requires;
        /// <summary>Unlocks set the first time one is built.</summary>
        public List<string> grants;
        /// <summary>Built once and never again: no count, no second price.</summary>
        public bool once;
        /// <summary>Stands at the center of the grounds rather than in a quarter, comes first
        /// in the list on its own row, and has no card on the map — one line on hover.</summary>
        public bool landmark;
        /// <summary>The landmark's hover line once built.</summary>
        public string line;
        /// <summary>The caption under the name, when it is not the section's own name.</summary>
        public string kindLabel;
        /// <summary>Oneiri housed by each one built.</summary>
        public int housing;
        /// <summary>The art it is drawn with — plate, map form and glyph — when an upgrade has
        /// given it new art. Its id stays <see cref="g"/> either way, so every condition that
        /// names it (owned:, built:) keeps working after it changes.</summary>
        public string art;
        public string ArtKey => string.IsNullOrEmpty(art) ? g : art;
        /// <summary>What each one standing does. Counted once per owned.</summary>
        public List<Effect> effects;
        /// <summary>The first price. <see cref="cost"/> is the next price, and rises by
        /// <see cref="growth"/> with every one built.</summary>
        public List<Amount> baseCost;
        public double growth = 1.15;
    }

    [Serializable]
    public class RevelationDef
    {
        public string k;
        public string n;
        public string kind;           // "unlocks a construct"
        public string bl;             // the realization, in serif italic
        public List<string> fx;
        public List<Amount> cost;
        public bool great;            // the greater tier — gold, absorbed into the mass
        public Refusal state;
        public float ring;            // 306 or 388
        public float angle;           // radians on that ring
        public bool realized;
        public bool seen = true;      // a newly arrived sigil lights the Revelations dot
        public string g;              // sigil art; the id when empty
        public List<string> requires;
        /// <summary>Unlocks set when it is realized, beside rev:&lt;k&gt; itself.</summary>
        public List<string> grants;
        /// <summary>Realizations this one takes off the field when it is realized, by key. Two
        /// that name each other are a choice: realizing either withdraws the other for good,
        /// and sets withdrawn:&lt;k&gt; for it, so a condition can read which way it went.</summary>
        public List<string> withdraws;
        /// <summary>What it does once realized.</summary>
        public List<Effect> effects;
        /// <summary>The authored price; <see cref="cost"/> is after effects.</summary>
        public List<Amount> baseCost;
    }

    [Serializable] public class Offer { public string r; public double n; public double g; }

    [Serializable]
    public class VisionDef
    {
        public string k;
        public string n;
        public string bl;
        public List<string> fx;
        public List<Offer> of;        // each offer: n of resource r buys g percent
        public int sel;               // which offer is selected
        public float p;               // percent complete 0-100
        public bool a;                // channelling: submits the selected offer every second
        public bool rep;              // repeatable
        public int done;
        public bool great;
        public float angle;

        /// <summary>Each completion of a repeatable multiplies its offer costs by 1.2.</summary>
        public double Escalated(double baseCost) => baseCost * Math.Pow(1.2, done);
        /// <summary>What pouring an offer costs now: escalated if repeatable, and cut by every
        /// effect that cheapens Visions.</summary>
        public double OfferCost(Offer o) => Math.Ceiling((rep ? Escalated(o.n) : o.n) * costScale - 1e-9);
        [NonSerialized] public double costScale = 1.0;
        public List<string> requires;
        /// <summary>Unlocks set the first time it completes, beside vision:&lt;k&gt;.</summary>
        public List<string> grants;
        /// <summary>What each completion does. A repeatable one stacks them.</summary>
        public List<Effect> effects;
    }

    [Serializable]
    public class RoadLocation
    {
        public string k;
        public string n;
        public float t;               // position along the road, 0-1
        public bool won;
        public double en;             // enemy strength
        public string bl;
        public string ben;            // what taking it unlocked
        public double tookWith;       // the army that took it, once taken

        /// <summary>What winning here does to the price of mustering, as a multiplier: 1 changes
        /// nothing, 0.85 is the "Assault costs -15%" this road's last bell promises. A discount
        /// is written the way the card reads it, so the two can never drift apart.</summary>
        public double costScale = 1.0;

        /// <summary>What must hold before battle can be given here. The place still shows on
        /// the road when it is next; it simply cannot be entered until these hold.</summary>
        public List<string> requires;
        /// <summary>Unlocks set the moment it is taken, beside won:&lt;k&gt; itself. This is what
        /// makes <see cref="ben"/> true: the line says what winning does, the grants do it.</summary>
        public List<string> grants;
        /// <summary>What taking it does, for good.</summary>
        public List<Effect> effects;
        /// <summary>Authored strength; <see cref="en"/> is after effects. Negative until seen.</summary>
        public double baseEn = -1;
    }

    [Serializable]
    public class UnitDef
    {
        public string k;
        public string n;
        public double p;              // army power each
        public int c;                 // how many you have
        public string bl;
        public List<Amount> cost;     // escalates 1.15x per muster
        public string sec;            // roster section: Levied, Sworn
        public bool isNew;            // iris dot until the pointer first rests on the card
        public List<string> requires;
        /// <summary>The portrait it is drawn with once an upgrade has changed it. The id stays
        /// <see cref="k"/>.</summary>
        public string art;
        public string ArtKey => string.IsNullOrEmpty(art) ? k : art;
        /// <summary>Strength each before effects; <see cref="p"/> is after. Negative until
        /// the dream first sees the unit and takes p as authored.</summary>
        public double baseP = -1;
        /// <summary>How many have been mustered, all time. Each one raises the next price.</summary>
        public int mustered;
        /// <summary>How much dearer each one makes the next.</summary>
        public double growth = 1.15;
    }

    [Serializable]
    public class JournalEntry
    {
        public int d;                 // the dream-day. Authored, never computed, and fiction.
        public int ch;                // chapter 1-5, authored with the entry
        public string src;            // revelation | vision | assault | veil
        public bool great;            // written by a greater Revelation or Vision
        public string n;              // the triggering thing's own name — never a new one
        public bool seen;             // cleared when the entry has been on screen
        public List<string> p;        // one to three paragraphs
        public string id;
        /// <summary>Written into the Journal the moment these all hold.</summary>
        public List<string> requires;
    }

    [Serializable]
    public class VeilDef
    {
        public string n;
        public int ord;
        public double sunk;
        public double need;           // fathoms
        public List<Amount> spend;    // per dive
        public List<Amount> bring;
        public int w;                 // Oneiri bound to diving
        public int cap;
        public double baseSeconds;    // seconds of work for one dive at one Oneiri
        public float p;               // progress toward the next dive, 0-1

        /// <summary>Diving is a Focus in its own place: the player's attention is worth two
        /// Oneiri on top of whatever is bound, exactly as on a Focus card.</summary>
        public double Period(bool attended)
        {
            int effective = w + (attended ? 2 : 0);
            return effective <= 0 || speed <= 0 ? double.PositiveInfinity : baseSeconds / effective / speed;
        }
        [NonSerialized] public double speed = 1.0;
        /// <summary>The authored price and yield of a dive; <see cref="spend"/> and
        /// <see cref="bring"/> are these after every effect, rewritten each recompute.</summary>
        public List<Amount> baseSpend, baseBring;
        public List<string> entry;    // revealed a paragraph at a time as fathoms are sunk
        public int revealed;          // how many paragraphs are written so far
        /// <summary>Fathoms at which each paragraph of <see cref="entry"/> is written.</summary>
        public List<double> reveal;
        /// <summary>Unlocks set when the veil is parted.</summary>
        public List<string> grants;

        public float Fill => need <= 0 ? 0f : Mathf.Clamp01((float)(sunk / need));
        public bool AtFull => need > 0 && sunk >= need;
    }

    /// <summary>A step on the path: when every condition holds, its unlocks are set, once.</summary>
    [Serializable]
    public class Rule
    {
        public string id;
        public List<string> when;
        public List<string> grants;
    }

    /// <summary>One mark on the Achievements page. Earned once, the moment every condition
    /// in <see cref="when"/> holds, and kept. The page shows <see cref="hint"/> while it is
    /// blank and <see cref="req"/> once earned; the name is withheld until then.</summary>
    [Serializable]
    public class AchievementDef
    {
        public string k;
        public string group;          // the group's display name
        public string n;
        public string hint;
        public string req;
        public string glyph;          // Glyphs/Achievement/<glyph>
        public List<string> when;
    }

    /// <summary>What an effect changes. Each sums with every other effect of its kind.</summary>
    public enum Fx
    {
        /// <summary>Resource <c>of</c> moves by n a second on its own.</summary>
        Rate = 0,
        /// <summary>Resource <c>of</c>'s ceiling rises by n.</summary>
        Cap = 1,
        /// <summary>Resource <c>of</c>'s ceiling rises by n as a fraction, after every Cap.</summary>
        CapPct = 2,
        /// <summary>Focus task <c>of</c> ("*" for all, "dive" for what a dive brings up) gives n more, as a fraction.</summary>
        Gain = 3,
        /// <summary>Focus task <c>of</c> ("*" for all) runs n faster, as a fraction.</summary>
        Speed = 4,
        /// <summary>A dive runs n faster, as a fraction.</summary>
        DiveSpeed = 5,
        /// <summary>A dive costs n less, as a fraction (never below a tenth of its price).</summary>
        DiveCost = 6,
        /// <summary>Units in section <c>of</c> ("*" for all) are n stronger, as a fraction.</summary>
        Power = 7,
        /// <summary>Every place on the road fields n less, as a fraction (never below a fifth).</summary>
        Enemy = 8,
        /// <summary>Mustering costs n less, as a fraction (never below a fifth).</summary>
        Muster = 9,
        /// <summary>Each construct <c>of</c> houses n more Oneiri.</summary>
        Housing = 10,
        /// <summary>No Oneiri come any more, and the unbound ones leave. Those already bound stay.</summary>
        Exodus = 11,
        /// <summary>A Vision's offers cost n less, as a fraction (never below a fifth).</summary>
        VisionCost = 12,
        /// <summary>Realizations cost n less, as a fraction (never below a fifth).</summary>
        RevCost = 13
    }

    /// <summary>One thing a construct, Realization, Vision or place does. See <see cref="Fx"/>.</summary>
    [Serializable]
    public class Effect
    {
        public Fx kind;
        public string of;
        public double n;
        public Effect() { }
        public Effect(Fx kind, string of, double n) { this.kind = kind; this.of = of; this.n = n; }
    }

    public enum UpgradeTarget { Construct = 0, Unit = 1, Resource = 2, Focus = 3 }

    /// <summary>
    /// A thing becoming something else, in place: a Warren becoming an Abode, Echo becoming
    /// Whispers, a Sworn unit becoming its nightmare. Applied once, the first time every
    /// condition in <see cref="when"/> holds (usually rev:&lt;k&gt;), and recorded as
    /// upgrade:&lt;id&gt;. The thing keeps its id, its count and what it holds, so costs,
    /// bindings and conditions that name it are untouched; only what it is called, how it is
    /// drawn, what it says and what it does change.
    ///
    /// Every field left empty (null, or the -1 sentinel) is left as it was. A list replaces
    /// the whole list: effect lines are final values, never deltas.
    /// </summary>
    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public UpgradeTarget target;
        /// <summary>The id of the thing upgraded: a construct's g, a unit's k, a resource's k,
        /// a Focus task's id.</summary>
        public string of;
        public List<string> when;

        public string n;              // new name
        public string art;            // construct art key, unit portrait, resource glyph, Focus glyph
        public string b;              // new blurb: a construct's b, a unit's bl, a Focus task's b
        public string kindLabel;      // construct only
        public List<string> fx;       // construct only: the effect lines, final values

        /// <summary>A construct's first price (those already standing reprice from it), a unit's
        /// muster cost, or a Focus task's cost per completion.</summary>
        public List<Amount> cost;
        public List<Amount> gain;     // Focus only
        public int housing = -1;      // construct only: Oneiri housed by each
        public double power = -1;     // unit only: strength each
        public double powerScale = 1; // unit only: applied after power, e.g. 2.5 for a nightmare
        public string sec;            // unit only: roster section
        public double ceiling = -1;   // resource only
        /// <summary>Construct only: replaces what each one does.</summary>
        public List<Effect> effects;
        /// <summary>The upgraded thing wears the iris dot again, so the change is noticed.</summary>
        public bool marksNew = true;
    }

    /// <summary>Why a piece of work is standing still.</summary>
    public enum Hold { None = 0, Short = 1, Full = 2 }
}
