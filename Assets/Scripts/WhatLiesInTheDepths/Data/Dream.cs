// What Lies In The Depths — the dream itself: the one purse, everything the dream currently holds, and the
// record of everything that has happened in it.
//
// Three rules hold the whole progression together (Progression Plan, section 4):
//   1. Unlocks only get set. Nothing unsets one but starting over.
//   2. Whether a thing is on screen is read from state, never pushed at a view. A view asks
//      Shown(...) whenever the state changes, and rebuilds when its answer changes.
//   3. Appearing latches. The first time a thing's requirements hold it is recorded as shown
//      (and wears its dot); it never disappears again except by being used up.
//
// The simulation lives here too — Focus progress, the dive, channelling — so that switching
// the center destination touches nothing else (spec section 15). Views only draw it.
//
// Spec section 15: every menu that spends spends out of the ResourceLedger in the left
// column, and nothing keeps a private total. It is why the ledger is pinned where it is.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Ursine;
using Ursine.Economy;

namespace WhatLiesInTheDepths.Data
{
    public sealed class Dream
    {
        

        public bool startMidGame;

        /// <summary>The purse. Nothing else in the game may hold a resource total.</summary>
        public readonly Ledger ledger = new Ledger();

        /// <summary>Everything that has happened. See <see cref="Holds"/> for how it is read.</summary>
        public readonly Unlocks unlocks = new Unlocks();

        public readonly List<FocusTask> tasks = new List<FocusTask>();
        public readonly List<ConstructDef> constructs = new List<ConstructDef>();
        public readonly List<RevelationDef> revelations = new List<RevelationDef>();
        /// <summary>Greater realizations the mass has kept, oldest first. They sit inside it
        /// and, now and then, remember.</summary>
        public readonly List<string> absorbed = new List<string>();
        public readonly List<VisionDef> visions = new List<VisionDef>();
        /// <summary>Golden Visions already finished. Their sigils are in the iris and never
        /// leave it; resting on one reads back what it changed.</summary>
        public readonly List<VisionDef> visionsAbsorbed = new List<VisionDef>();
        public readonly List<RoadLocation> road = new List<RoadLocation>();
        public readonly List<UnitDef> units = new List<UnitDef>();
        /// <summary>One-off Visions already finished, in the order they finished. They have left
        /// the field, but what they did still counts.</summary>
        public readonly List<VisionDef> visionsDone = new List<VisionDef>();

        /// <summary>Things becoming other things, each waiting on its own conditions.</summary>
        public readonly List<UpgradeDef> upgrades = new List<UpgradeDef>();
        /// <summary>Goes up by one every time an upgrade is applied. A view that builds its
        /// cards once folds this into its signature, so a renamed or redrawn thing is rebuilt.</summary>
        public int Version { get; private set; }

        /// <summary>Units that taking a place hands over, keyed by the place.</summary>
        public readonly Dictionary<string, UnitDef> unitsUnlockedBy = new Dictionary<string, UnitDef>();

        /// <summary>The Journal as written so far. Entries arrive from <see cref="journalDefs"/>.</summary>
        public readonly List<JournalEntry> journal = new List<JournalEntry>();
        /// <summary>Every entry the dream can write, each waiting on its own requirements.</summary>
        public readonly List<JournalEntry> journalDefs = new List<JournalEntry>();

        /// <summary>The veils, in order. The count is never disclosed anywhere on screen.</summary>
        public readonly List<VeilDef> veils = new List<VeilDef>();
        public VeilDef veil;
        public int veilIndex;

        /// <summary>Steps on the path that are not a thing appearing: a menu opening, a column
        /// arriving, a control growing onto every card.</summary>
        public readonly List<Rule> rules = new List<Rule>();

        /// <summary>The Achievements page, in its authored order.</summary>
        public readonly List<AchievementDef> achievements = new List<AchievementDef>();
        public bool Earned(AchievementDef a) => a != null && unlocks.Has("ach:" + a.k);
        /// <summary>The Achievements button exists from the first mark earned.</summary>
        public bool AchievementsOpen => _showAll || unlocks.Has("ach:any");

        /// <summary>Completions of each Focus, all time.</summary>
        public readonly Dictionary<string, int> done = new Dictionary<string, int>();
        /// <summary>What a resource holds the moment it is first shown (Dread arrives with 20).</summary>
        public readonly Dictionary<string, double> startOnShow = new Dictionary<string, double>();
        /// <summary>Fathoms sunk across every veil, all time.</summary>
        public double fathomsTotal;

        /// <summary>At most one Focus is yours at a time, and the dive is part of the same
        /// single choice. Null means idleness, which is a position you can hold.</summary>
        public string attendedTaskId;
        public bool attendingDive;

        /// <summary>Every discount a won place has handed over, multiplied together. Mustering
        /// reads it; nothing else does.</summary>
        public double musterCostScale = 1.0;

        /// <summary>The mid-game snapshot shows everything; a real dream shows what it has earned.</summary>
        bool _showAll;

        // ---- the ledger, forwarded so a view never has to reach through ----------

        public List<Res> resources => ledger.resources;

        public event Action Changed
        {
            add => ledger.Changed += value;
            remove => ledger.Changed -= value;
        }

        public Res Find(string k) => ledger.Find(k);
        public double Held(string k) => ledger.Held(k);
        public double Ceiling(string k) => ledger.Ceiling(k);

        public bool Short(string k, double n) => ledger.Short(k, n);
        public bool AboveCeiling(string k, double n) => ledger.AboveCeiling(k, n);
        public Refusal Judge(IEnumerable<Amount> cost) => ledger.Judge(cost);
        public string ReasonLine(IEnumerable<Amount> cost) => ledger.ReasonLine(cost);
        public bool Spend(IEnumerable<Amount> cost) => ledger.Spend(cost);
        public void Grant(IEnumerable<Amount> gain) => ledger.Grant(gain);

        // ---- Oneiri ---------------------------------------------------------------

        /// <summary>One pool, shared with the dive. Binding an Oneiri to a task is unbinding
        /// it from somewhere else, which is why the plus can die for two different reasons.</summary>
        public int OneiriTotal => (int)(Find("oneiri")?.c ?? 0);

        public int OneiriBound
        {
            get
            {
                int n = veil != null ? veil.w : 0;
                foreach (var t in tasks) n += t.w;
                return n;
            }
        }

        public int OneiriFree => Mathf.Max(0, OneiriTotal - OneiriBound);

        // ---- what is on screen ------------------------------------------------------

        public bool Shown(Res r) => r != null && (_showAll || unlocks.Has("shown:res:" + r.k));
        public bool Shown(FocusTask t) => t != null && (_showAll || unlocks.Has("shown:focus:" + t.id));
        public bool Shown(ConstructDef c) => c != null && (_showAll || unlocks.Has("shown:construct:" + c.g));
        public bool Shown(RevelationDef r) => r != null && (_showAll || unlocks.Has("shown:rev:" + r.k));
        public bool Shown(VisionDef v) => v != null && (_showAll || unlocks.Has("shown:vision:" + v.k));
        public bool Shown(UnitDef u) => u != null && (_showAll || unlocks.Has("shown:unit:" + u.k));

        /// <summary>The left column's ledger.</summary>
        public bool LedgerOpen => _showAll || unlocks.Has("col:ledger");
        /// <summary>The right column's veil panel.</summary>
        public bool GaugeOpen => _showAll || unlocks.Has("col:gauge");
        /// <summary>Whether the Oneiri stepper exists on the Focus cards and the dive.</summary>
        public bool BindingOpen => _showAll || unlocks.Has("bind:oneiri");

        /// <summary>A center destination appears when its own unlock is set. The Journal is
        /// there from the first frame.</summary>
        public bool MenuOpen(string destination)
        {
            if (_showAll) return true;
            string d = destination.ToLowerInvariant();
            return d == "journal" || unlocks.Has("menu:" + d);
        }

        public IEnumerable<Res> ShownResources => resources.Where(Shown);
        public IEnumerable<FocusTask> ShownTasks => tasks.Where(Shown);
        public IEnumerable<ConstructDef> ShownConstructs => constructs.Where(Shown);
        public IEnumerable<RevelationDef> ShownRevelations => revelations.Where(r => !r.realized && !Withdrawn(r) && Shown(r));

        /// <summary>Taken off the field by another Realization. It never comes back.</summary>
        public bool Withdrawn(RevelationDef r) => r != null && unlocks.Has("withdrawn:" + r.k);

        /// <summary>Battle can be given here: it is the next place and what it waits on holds.</summary>
        public bool Open(RoadLocation l) => l != null && !l.won && l == NextPlace && AllHold(l.requires);
        public RoadLocation NextPlace => road.FirstOrDefault(l => !l.won);

        // ---- conditions -------------------------------------------------------------

        /// <summary>
        /// One condition. Either an unlock id, set when something happened —
        ///   start · max:&lt;res&gt; (first time full) · zero:&lt;res&gt; (first time run dry after
        ///   having some) · got:&lt;res&gt; · rev:&lt;id&gt; · built:&lt;id&gt; · parted:&lt;n&gt; ·
        ///   or any id a rule or a grant sets —
        /// or a count compared with >= :
        ///   done:&lt;focus&gt;>=n · owned:&lt;construct&gt;>=n · fathoms>=n (all time) ·
        ///   held:&lt;res&gt;>=n · sunk>=n (the current veil) · won>=n (places taken).
        /// Battles set won:&lt;place&gt;, lost:&lt;place&gt; and lost:any; upgrades set
        /// upgrade:&lt;id&gt;; a withdrawn Realization sets withdrawn:&lt;k&gt;.
        /// </summary>
        public bool Holds(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            int at = condition.IndexOf(">=", StringComparison.Ordinal);
            if (at < 0) return unlocks.Has(condition.Trim());

            string lhs = condition.Substring(0, at).Trim();
            if (!double.TryParse(condition.Substring(at + 2).Trim(), NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out double n))
            {
                Debug.LogWarning($"[What Lies In The Depths] Unreadable condition '{condition}'.");
                return false;
            }
            return Count(lhs) >= n;
        }

        double Count(string what)
        {
            int colon = what.IndexOf(':');
            string head = colon < 0 ? what : what.Substring(0, colon);
            string id = colon < 0 ? null : what.Substring(colon + 1);
            switch (head)
            {
                case "done": return done.TryGetValue(id, out int d) ? d : 0;
                case "owned": return constructs.FirstOrDefault(c => c.g == id)?.owned ?? 0;
                case "fathoms": return fathomsTotal;
                case "sunk": return veil != null ? veil.sunk : 0;
                case "held": return Held(id);
                case "won": return road.Count(l => l.won);
                case "vdone": { var v = FindVision(id); return v != null ? v.done : 0; }
                case "parted": return veilIndex;
            }
            Debug.LogWarning($"[What Lies In The Depths] Unknown count '{what}'.");
            return 0;
        }

        public bool AllHold(List<string> conditions)
        {
            if (conditions == null) return true;
            foreach (var c in conditions) if (!Holds(c)) return false;
            return true;
        }

        // ---- actions ----------------------------------------------------------------

        /// <summary>Builds one. Returns false, having taken nothing, if it cannot be afforded
        /// or is a once-only thing already standing.</summary>
        public bool Build(ConstructDef c)
        {
            if (c == null || (c.once && c.owned > 0)) return false;
            if (!Spend(c.cost)) return false;
            c.owned += 1;
            c.built = true;
            if (c.owned == 1)
            {
                unlocks.Add("built:" + c.g);
                unlocks.AddRange(c.grants);
            }
            Reprice(c);
            Dirty();
            return true;
        }

        /// <summary>The next price: the first price, risen once for every one already built.</summary>
        static void Reprice(ConstructDef c)
        {
            if (c.baseCost == null || c.once) return;
            double k = Math.Pow(c.growth <= 0 ? 1.0 : c.growth, c.owned);
            c.cost = c.baseCost.Select(a => new Amount(a.k, Math.Ceiling(a.n * k - 1e-9))).ToList();
        }

        /// <summary>Realizes one. It leaves the field; a greater one is kept by the mass.</summary>
        public bool Realize(RevelationDef r)
        {
            if (r == null || r.realized || Withdrawn(r)) return false;
            if (!Spend(r.cost)) return false;
            r.realized = true;
            if (r.great && !absorbed.Contains(r.k)) absorbed.Add(r.k);
            unlocks.Add("rev:" + r.k);
            unlocks.AddRange(r.grants);
            Withdraw(r);
            Dirty();
            return true;
        }

        /// <summary>Takes every Realization <paramref name="r"/> names off the field. Only ever
        /// one that is not realized: a choice already made is not unmade by its twin.</summary>
        public void Withdraw(RevelationDef r)
        {
            if (r?.withdraws == null) return;
            foreach (var k in r.withdraws)
            {
                if (string.IsNullOrEmpty(k) || k == r.k) continue;
                var other = revelations.FirstOrDefault(x => x.k == k);
                if (other != null && other.realized) continue;
                unlocks.Add("withdrawn:" + k);
            }
        }

        // ---- the road -----------------------------------------------------------

        /// <summary>A place taken. Everything winning it does happens here, once: the war gets
        /// cheaper if it says so, the unit it hands over joins the roster, and its grants are set
        /// beside won:&lt;k&gt; — which is what makes the "When won" line true.</summary>
        public void TakePlace(RoadLocation place, double army)
        {
            if (place == null || place.won) return;
            place.won = true;
            place.tookWith = army;
            if (place.costScale > 0 && place.costScale != 1.0) musterCostScale *= place.costScale;
            if (unitsUnlockedBy.TryGetValue(place.k, out var unit))
            {
                if (!units.Contains(unit)) units.Add(unit);
                unitsUnlockedBy.Remove(place.k);
            }
            unlocks.Add("won:" + place.k);
            unlocks.AddRange(place.grants);
            Dirty();
        }

        /// <summary>Driven back. The losses are the view's to take; this records that it happened,
        /// so a Realization can wait on a first defeat.</summary>
        public void LosePlace(RoadLocation place)
        {
            if (place == null) return;
            unlocks.Add("lost:" + place.k);
            unlocks.Add("lost:any");
            Dirty();
        }

        // ---- upgrades -----------------------------------------------------------

        /// <summary>Applies one upgrade to the thing it names. See <see cref="UpgradeDef"/>.</summary>
        bool Upgrade(UpgradeDef up)
        {
            switch (up.target)
            {
                case UpgradeTarget.Construct:
                {
                    var c = constructs.FirstOrDefault(x => x.g == up.of);
                    if (c == null) return false;
                    if (!string.IsNullOrEmpty(up.n)) c.n = up.n;
                    if (!string.IsNullOrEmpty(up.art)) c.art = up.art;
                    if (!string.IsNullOrEmpty(up.b)) c.b = up.b;
                    if (!string.IsNullOrEmpty(up.kindLabel)) c.kindLabel = up.kindLabel;
                    if (up.fx != null) c.fx = new List<string>(up.fx);
                    if (up.effects != null) c.effects = new List<Effect>(up.effects);
                    if (up.housing >= 0) c.housing = up.housing;
                    if (up.cost != null)
                    {
                        if (c.once) c.cost = Copy(up.cost);
                        else { c.baseCost = Copy(up.cost); Reprice(c); }
                    }
                    if (up.marksNew && Shown(c)) c.seen = false;
                    break;
                }
                case UpgradeTarget.Unit:
                {
                    var u = units.FirstOrDefault(x => x.k == up.of)
                         ?? unitsUnlockedBy.Values.FirstOrDefault(x => x.k == up.of);
                    if (u == null) return false;
                    if (!string.IsNullOrEmpty(up.n)) u.n = up.n;
                    if (!string.IsNullOrEmpty(up.art)) u.art = up.art;
                    if (!string.IsNullOrEmpty(up.b)) u.bl = up.b;
                    if (!string.IsNullOrEmpty(up.sec)) u.sec = up.sec;
                    if (up.cost != null) u.cost = Copy(up.cost);
                    if (u.baseP < 0) u.baseP = u.p;
                    if (up.power >= 0) u.baseP = up.power;
                    if (up.powerScale > 0 && up.powerScale != 1.0) u.baseP *= up.powerScale;
                    u.p = u.baseP;
                    if (up.marksNew) u.isNew = true;
                    break;
                }
                case UpgradeTarget.Resource:
                {
                    var r = Find(up.of);
                    if (r == null) return false;
                    if (!string.IsNullOrEmpty(up.n)) r.n = up.n;
                    if (!string.IsNullOrEmpty(up.art)) r.glyph = up.art;
                    if (up.ceiling >= 0) { r.m = up.ceiling; _baseCeiling[r.k] = up.ceiling; }
                    break;
                }
                case UpgradeTarget.Focus:
                {
                    var t = tasks.FirstOrDefault(x => x.id == up.of);
                    if (t == null) return false;
                    if (!string.IsNullOrEmpty(up.n)) t.n = up.n;
                    if (!string.IsNullOrEmpty(up.art)) t.g = up.art;
                    if (!string.IsNullOrEmpty(up.b)) t.b = up.b;
                    if (up.cost != null) t.cost = Copy(up.cost);
                    if (up.gain != null) { t.gain = Copy(up.gain); t.baseGain = Copy(up.gain); }
                    if (up.marksNew && Shown(t)) t.seen = false;
                    break;
                }
            }
            Version++;
            return true;
        }

        static List<Amount> Copy(List<Amount> a) => a == null ? null : a.Select(x => new Amount(x.k, x.n)).ToList();

        // ---- effects --------------------------------------------------------------

        // Everything that changes a number reads from here. The authored number is kept as a
        // base (a resource's ceiling and drift, a task's gain, a dive's price and yield, a unit's
        // strength, a place's army, a Realization's price) and the number a view reads is
        // rewritten from it on every recompute, so nothing ever compounds by accident and a
        // debugger that un-builds something gets the old number straight back.

        readonly Dictionary<string, double> _sums = new Dictionary<string, double>();
        readonly Dictionary<string, double> _baseCeiling = new Dictionary<string, double>();
        readonly Dictionary<string, double> _basePassive = new Dictionary<string, double>();
        int _exodusAt = -1;

        /// <summary>Every place's discount multiplied together, and every Muster effect, as one
        /// multiplier on the price of mustering.</summary>
        public double MusterScale { get; private set; } = 1.0;

        /// <summary>How much Dread slows the work: one percent for every ten held.</summary>
        public double Drag => 1.0 + Held("dread") * 0.001;

        /// <summary>The total of every active effect of a kind on a thing. <paramref name="of"/>
        /// null asks for effects that name nothing.</summary>
        public double Sum(Fx kind, string of)
        {
            double n;
            return _sums.TryGetValue(((int)kind) + "|" + (of ?? ""), out n) ? n : 0;
        }

        void Add(Effect e, double times)
        {
            if (e == null || times == 0) return;
            string key = ((int)e.kind) + "|" + (e.of ?? "");
            double n;
            _sums.TryGetValue(key, out n);
            _sums[key] = n + e.n * times;
        }

        void AddAll(List<Effect> list, double times)
        {
            if (list == null || times == 0) return;
            foreach (var e in list) Add(e, times);
        }

        /// <summary>Takes the authored numbers as bases the first time each thing is seen.</summary>
        void Snapshot()
        {
            foreach (var r in resources)
            {
                if (!_baseCeiling.ContainsKey(r.k)) _baseCeiling[r.k] = r.m;
                if (!_basePassive.ContainsKey(r.k)) _basePassive[r.k] = r.passive;
            }
            foreach (var t in tasks) if (t.baseGain == null) t.baseGain = Copy(t.gain);
            foreach (var v in veils)
            {
                if (v.baseSpend == null) v.baseSpend = Copy(v.spend);
                if (v.baseBring == null) v.baseBring = Copy(v.bring);
            }
            foreach (var u in units) if (u.baseP < 0) u.baseP = u.p;
            foreach (var u in unitsUnlockedBy.Values) if (u.baseP < 0) u.baseP = u.p;
            foreach (var l in road) if (l.baseEn < 0) l.baseEn = l.en;
            foreach (var r in revelations) if (r.baseCost == null) r.baseCost = Copy(r.cost);
        }

        static List<Amount> Scaled(List<Amount> a, double k, bool ceil)
        {
            if (a == null) return null;
            return a.Select(x => new Amount(x.k, ceil ? Math.Ceiling(x.n * k - 1e-9) : x.n * k)).ToList();
        }

        void ApplyEffects()
        {
            if (_showAll) return;       // the spec's snapshot keeps its own numbers
            Snapshot();

            _sums.Clear();
            foreach (var c in constructs) AddAll(c.effects, c.owned);
            foreach (var r in revelations) if (r.realized) AddAll(r.effects, 1);
            foreach (var v in visions) AddAll(v.effects, v.done);
            foreach (var v in visionsDone) AddAll(v.effects, Math.Max(1, v.done));
            foreach (var l in road) if (l.won) AddAll(l.effects, 1);

            foreach (var r in resources)
            {
                if (r.k == "oneiri") continue;
                double b = _baseCeiling[r.k];
                r.m = Math.Floor((b + Sum(Fx.Cap, r.k)) * (1.0 + Sum(Fx.CapPct, r.k)));
                if (r.c > r.m) r.c = r.m;
                r.passive = _basePassive[r.k] + Sum(Fx.Rate, r.k);
            }

            double drag = Drag, allSpeed = Sum(Fx.Speed, "*"), allGain = Sum(Fx.Gain, "*");
            foreach (var t in tasks)
            {
                t.speed = (1.0 + allSpeed + Sum(Fx.Speed, t.id)) / drag;
                t.gain = Scaled(t.baseGain, 1.0 + allGain + Sum(Fx.Gain, t.id), false);
            }

            double diveCost = Math.Max(0.1, 1.0 - Sum(Fx.DiveCost, null));
            double diveGain = 1.0 + Sum(Fx.Gain, "dive");
            double diveSpeed = 1.0 + Sum(Fx.DiveSpeed, null);
            foreach (var v in veils)
            {
                v.speed = diveSpeed;
                v.spend = Scaled(v.baseSpend, diveCost, true);
                v.bring = Scaled(v.baseBring, diveGain, false);
            }

            double allPower = Sum(Fx.Power, "*");
            foreach (var u in units.Concat(unitsUnlockedBy.Values))
                u.p = u.baseP * (1.0 + allPower + Sum(Fx.Power, u.sec));

            double enemy = Math.Max(0.2, 1.0 - Sum(Fx.Enemy, null));
            foreach (var l in road) l.en = Math.Round(l.baseEn * enemy);

            MusterScale = musterCostScale * Math.Max(0.2, 1.0 - Sum(Fx.Muster, null));

            double revCost = Math.Max(0.2, 1.0 - Sum(Fx.RevCost, null));
            foreach (var r in revelations) r.cost = Scaled(r.baseCost, revCost, true);

            double visionCost = Math.Max(0.2, 1.0 - Sum(Fx.VisionCost, null));
            foreach (var v in visions) v.costScale = visionCost;
        }

        // ---- Visions --------------------------------------------------------------

        /// <summary>Pours the selected offer once, if it can be paid. Returns whether it poured.</summary>
        public bool Pour(VisionDef v)
        {
            if (v == null || v.of == null || v.of.Count == 0 || v.p >= 100f) return false;
            var offer = v.of[Mathf.Clamp(v.sel, 0, v.of.Count - 1)];
            double n = v.OfferCost(offer);
            if (Held(offer.r) < n) return false;
            var res = Find(offer.r);
            if (res != null) res.c -= n;
            // Progress never falls. No decay, no refund, no way to take a pour back.
            v.p = Mathf.Min(100f, v.p + (float)offer.g);
            if (v.p >= 100f) CompleteVision(v);
            Dirty();
            return true;
        }

        /// <summary>A Vision reaching its end. A repeatable one counts the completion and begins
        /// again, dearer; a one-off leaves the field, and a golden one is kept by the eye.</summary>
        public void CompleteVision(VisionDef v)
        {
            if (v == null) return;
            v.done += 1;
            if (unlocks.Add("vision:" + v.k)) unlocks.AddRange(v.grants);
            if (v.rep) { v.p = 0f; }
            else
            {
                v.a = false;
                v.p = 100f;
                visions.Remove(v);
                if (!visionsDone.Contains(v)) visionsDone.Add(v);
                if (v.great && !visionsAbsorbed.Contains(v)) visionsAbsorbed.Add(v);
            }
            Dirty();
        }

        VisionDef FindVision(string k) => visions.FirstOrDefault(x => x.k == k) ?? visionsDone.FirstOrDefault(x => x.k == k);

        // ---- the army -------------------------------------------------------------

        /// <summary>Everything on the roster, at its strength after every effect.</summary>
        public double Army => units.Where(Shown).Sum(u => u.p * u.c);

        /// <summary>The next one's price: dearer for every one standing (15% unless the unit says
        /// otherwise), then every discount the road and the Palace have given. It follows the
        /// count, not the history, so an army cut down by a defeat is cheaper to raise again.</summary>
        public List<Amount> MusterCost(UnitDef u)
        {
            if (u?.cost == null) return new List<Amount>();
            double k = Math.Pow(u.growth <= 0 ? 1.15 : u.growth, u.c) * MusterScale;
            return u.cost.Select(a => new Amount(a.k, Math.Round(a.n * k))).ToList();
        }

        public bool Muster(UnitDef u)
        {
            if (u == null || !Shown(u) || !Spend(MusterCost(u))) return false;
            u.c += 1;
            u.mustered += 1;
            Dirty();
            return true;
        }

        /// <summary>What a defeat cost, for the band to tell.</summary>
        public sealed class Loss { public double before, after; public int pct; }

        /// <summary>Driven back: a quarter to nearly half the army, taken out in bodies, picked
        /// in proportion to how many of each there are. Recorded, so a Realization can wait on a
        /// first defeat.</summary>
        public Loss LoseBattle(RoadLocation place, System.Random rng)
        {
            double before = Army, want = before * Ursine.Combat.Odds.LossFraction(rng), gone = 0;
            int guard = 0;
            while (gone < want && guard++ < 100000)
            {
                var live = units.Where(u => u.c > 0 && Shown(u)).ToList();
                if (live.Count == 0) break;
                int pick = rng.Next(live.Sum(u => u.c));
                foreach (var u in live)
                {
                    pick -= u.c;
                    if (pick >= 0) continue;
                    u.c -= 1; gone += u.p;
                    break;
                }
            }
            LosePlace(place);
            double after = Army;
            return new Loss { before = before, after = after, pct = (int)Math.Round(gone / Math.Max(1.0, before) * 100.0) };
        }

        public bool HasNextVeil => veilIndex + 1 < veils.Count;

        /// <summary>One-way. The count of veils is never disclosed, so the next one is simply
        /// the next one.</summary>
        public bool PartVeil()
        {
            if (veil == null || !veil.AtFull || !HasNextVeil) return false;
            unlocks.AddRange(veil.grants);
            unlocks.Add("parted:" + veil.ord);
            veilIndex += 1;
            int bound = veil.w;
            veil = veils[veilIndex];
            // Oneiri belong to the dive, not the veil: the binding carries across.
            veil.w = Mathf.Min(bound, veil.cap);
            Dirty();
            return true;
        }

        // ---- the check: everything that follows from what just happened --------------

        bool _checking;

        /// <summary>Call after anything changes. Works out what has now happened, what now
        /// appears, and what the ledger now reads, then tells every view once.</summary>
        public void Dirty()
        {
            if (!_checking)
            {
                _checking = true;
                try
                {
                    // A step can make the next one true (a grant that opens a menu that shows
                    // its first item), so settle until a pass adds nothing.
                    for (int pass = 0; pass < 12; pass++)
                    {
                        int before = unlocks.Count;
                        Check();
                        Recompute();
                        if (unlocks.Count == before) break;
                    }
                }
                finally { _checking = false; }
            }
            ledger.Dirty();
        }

        void Check()
        {
            // What the purse has done: full for the first time, dry for the first time.
            foreach (var r in resources)
            {
                if (r.c > 0) unlocks.Add("got:" + r.k);
                if (r.m > 0 && r.c >= r.m) unlocks.Add("max:" + r.k);
                if (r.c <= 0 && unlocks.Has("got:" + r.k)) unlocks.Add("zero:" + r.k);
            }

            foreach (var rule in rules)
            {
                if (unlocks.Has("rule:" + rule.id) || !AllHold(rule.when)) continue;
                unlocks.Add("rule:" + rule.id);
                unlocks.AddRange(rule.grants);
            }

            // Things becoming other things. Before the showing below, so a thing that appears
            // and is upgraded on the same pass appears already as what it has become.
            foreach (var up in upgrades)
            {
                if (unlocks.Has("upgrade:" + up.id) || !AllHold(up.when)) continue;
                if (Upgrade(up)) unlocks.Add("upgrade:" + up.id);
            }

            // Marks are earned on the same pass that sets what earned them.
            foreach (var a in achievements)
            {
                if (unlocks.Has("ach:" + a.k) || !AllHold(a.when)) continue;
                unlocks.Add("ach:" + a.k);
                unlocks.Add("ach:any");
            }

            if (_showAll) return;

            // Appearing latches, and a thing appearing wears its dot.
            foreach (var r in resources)
                if (!unlocks.Has("shown:res:" + r.k) && AllHold(r.requires))
                {
                    unlocks.Add("shown:res:" + r.k);
                    if (startOnShow.TryGetValue(r.k, out double start) && r.c < start)
                        r.c = r.m > 0 ? Math.Min(start, r.m) : start;
                }
            foreach (var t in tasks)
                if (!unlocks.Has("shown:focus:" + t.id) && AllHold(t.requires))
                { unlocks.Add("shown:focus:" + t.id); t.seen = false; }
            foreach (var c in constructs)
                if (!unlocks.Has("shown:construct:" + c.g) && AllHold(c.requires))
                { unlocks.Add("shown:construct:" + c.g); c.seen = false; }
            foreach (var r in revelations)
                if (!unlocks.Has("shown:rev:" + r.k) && !Withdrawn(r) && AllHold(r.requires))
                { unlocks.Add("shown:rev:" + r.k); r.seen = false; }
            foreach (var v in visions)
                if (!unlocks.Has("shown:vision:" + v.k) && AllHold(v.requires))
                    unlocks.Add("shown:vision:" + v.k);
            foreach (var u in units)
                if (!unlocks.Has("shown:unit:" + u.k) && AllHold(u.requires))
                { unlocks.Add("shown:unit:" + u.k); u.isNew = true; }

            // The Journal writes itself.
            foreach (var e in journalDefs)
            {
                if (unlocks.Has("journal:" + e.id) || !AllHold(e.requires)) continue;
                unlocks.Add("journal:" + e.id);
                e.seen = false;
                journal.Add(e);
            }
        }

        /// <summary>Everything derived: housing, the veil's written paragraphs, and every
        /// rate the ledger shows.</summary>
        void Recompute()
        {
            ApplyEffects();

            // Every Oneiri housed is present at once.
            var oneiri = Find("oneiri");
            if (oneiri != null && !_showAll)
            {
                int housed = constructs.Sum(c => c.owned * (c.housing + (int)Sum(Fx.Housing, c.g)));
                // The Night Kiln: from the moment it stands, only the Oneiri already at work stay.
                if (Sum(Fx.Exodus, null) > 0)
                {
                    if (_exodusAt < 0) _exodusAt = OneiriBound;
                    housed = Mathf.Min(housed, _exodusAt);
                }
                oneiri.m = housed;
                oneiri.c = housed;
                // Should the pool ever shrink, the last bindings are released first.
                int over = OneiriBound - housed;
                for (int i = tasks.Count - 1; i >= 0 && over > 0; i--)
                {
                    int take = Mathf.Min(over, tasks[i].w);
                    tasks[i].w -= take; over -= take;
                }
                if (over > 0 && veil != null) veil.w = Mathf.Max(0, veil.w - over);
            }

            // What of the veil's story has been written. Read from the fathoms every time, so
            // it goes back as readily as it goes forward — a debugger that raises the shore
            // un-writes the last paragraph, which is the honest answer to a smaller number.
            if (veil != null && veil.entry != null && veil.entry.Count > 0)
            {
                if (veil.reveal != null && veil.reveal.Count > 0)
                    veil.revealed = veil.reveal.Count(f => veil.sunk >= f);
                else
                {
                    // No authored thresholds: the first paragraph is the opening beat and the
                    // last is the floor, with the rest spread evenly between.
                    int n = veil.entry.Count;
                    veil.revealed = Mathf.Clamp(1 + Mathf.FloorToInt(veil.Fill * (n - 1)), 1, n);
                }
            }

            // The rate column reports everything moving a resource: what the ledger applies
            // on its own, plus each task and the dive at the interval they are actually running.
            foreach (var r in resources) r.r = r.passive;
            foreach (var t in tasks)
            {
                if (!Shown(t) || HoldOf(t) != Hold.None) continue;
                double period = t.Period(attendedTaskId == t.id);
                if (double.IsInfinity(period)) continue;
                AddFlow(t.gain, 1.0 / period);
                AddFlow(t.cost, -1.0 / period);
            }
            if (veil != null && GaugeOpen && !veil.AtFull && HoldOfDive == Hold.None)
            {
                double period = veil.Period(attendingDive);
                if (!double.IsInfinity(period))
                {
                    AddFlow(DiveBring, 1.0 / period);
                    AddFlow(veil.spend, -1.0 / period);
                }
            }
        }

        /// <summary>Work stands still when it cannot be paid for, or when everything it would
        /// give is already at its ceiling — there is nothing to earn. It holds where it is and
        /// carries on the moment that changes.</summary>
        public Hold HoldOf(List<Amount> cost, List<Amount> gain)
        {
            if (Judge(cost) != Refusal.None) return Hold.Short;
            if (gain != null && gain.Count > 0)
            {
                bool allFull = true;
                foreach (var a in gain)
                {
                    var r = Find(a.k);
                    if (r == null || r.m <= 0 || r.c < r.m) { allFull = false; break; }
                }
                if (allFull) return Hold.Full;
            }
            return Hold.None;
        }

        public Hold HoldOf(FocusTask t) => t == null ? Hold.None : HoldOf(t.cost, t.gain);
        /// <summary>A dive holds when it cannot be paid for, or when its primary yield — the
        /// first thing it brings up, the topmost line on the gauge — is already full. What else
        /// it brings may overflow; only the primary stops the descent, so a reach can be written
        /// to force spending (make its primary something you must spend to go on) or not (make
        /// it something that drains on its own). The bottom never finishes and never holds.</summary>
        public Hold HoldOfDive
        {
            get
            {
                if (veil == null) return Hold.None;
                bool primaryOnly = HasNextVeil && veil.bring != null && veil.bring.Count > 0;
                return HoldOf(veil.spend, primaryOnly ? veil.bring.GetRange(0, 1) : null);
            }
        }

        /// <summary>What a dive actually brings up: its primary (first) line always, and every
        /// other line only once that resource is shown. A resource you have not unlocked does
        /// not pile up out of sight — Echo from The Drift waits until Conjure has shown it, so
        /// it starts from nothing when it appears.</summary>
        public List<Amount> DiveBring
        {
            get
            {
                if (veil == null || veil.bring == null) return null;
                var l = new List<Amount>(veil.bring.Count);
                for (int i = 0; i < veil.bring.Count; i++)
                    if (i == 0 || Shown(Find(veil.bring[i].k))) l.Add(veil.bring[i]);
                return l;
            }
        }

        void AddFlow(List<Amount> amounts, double perSecond)
        {
            if (amounts == null) return;
            foreach (var a in amounts)
            {
                var r = Find(a.k);
                if (r != null) r.r += a.n * perSecond;
            }
        }

        // ---- lifetime -------------------------------------------------------------

        /// <summary>Starts a dream: the opening, or the spec's mid-game snapshot with
        /// everything shown.</summary>
        public Dream(bool midGame)
        {
            startMidGame = midGame;
            if (midGame)
            {
                PlaceholderContent.Fill(this);
                _showAll = true;
                if (veil != null && !veils.Contains(veil)) veils.Add(veil);
            }
            else
            {
                OpeningContent.Fill(this);
            }
            if (veil == null && veils.Count > 0) veil = veils[0];
            foreach (var c in constructs) Reprice(c);

            unlocks.Add("start");
            Dirty();
        }

        /// <summary>Work that fills continuously: every Focus with anything on it, and the dive.
        /// A thing with nothing on it holds its progress; it never resets.</summary>

        /// <summary>Debug only: how many times faster Focus work and the dive run. The Dream
        /// Debugger sets it; nothing in the game does. Resources, Visions and everything else on
        /// the second tick keep their own pace.</summary>
        public float workSpeed = 1f;

        public void Step(float dt)
        {
            bool changed = false;
            dt *= workSpeed <= 0f ? 1f : workSpeed;

            foreach (var t in tasks)
            {
                if (!Shown(t)) continue;
                double period = t.Period(attendedTaskId == t.id);
                if (double.IsInfinity(period)) continue;
                // Nothing to pay with, or nothing left to earn: it stands where it is.
                if (HoldOf(t) != Hold.None) continue;

                t.p += (float)(dt / period * 100.0);
                if (t.p < 100f) continue;

                // The spend is taken at the finish, not the start.
                t.p = 0f;
                Spend(t.cost);
                Grant(t.gain);
                done[t.id] = (done.TryGetValue(t.id, out int n) ? n : 0) + 1;
                changed = true;
            }

            if (veil != null && GaugeOpen && !veil.AtFull && HoldOfDive == Hold.None)
            {
                double period = veil.Period(attendingDive);
                if (!double.IsInfinity(period))
                {
                    veil.p += (float)(dt / period);
                    if (veil.p >= 1f)
                    {
                        // A dive is paid for when it lands, like a Focus.
                        veil.p = 0f;
                        Spend(veil.spend);
                        Grant(DiveBring);
                        veil.sunk = Math.Min(veil.need, veil.sunk + 1);
                        fathomsTotal += 1;
                        if (veil.AtFull) attendingDive = false;
                        changed = true;
                    }
                }
            }

            if (changed) Dirty();
        }

        readonly List<VisionDef> _channelled = new List<VisionDef>();

        public void Tick()
        {
            // Resources accrue on the second. Everything in this game happens on the second.
            ledger.Tick();

            // A channelling Vision submits its selected offer every second, out of this same
            // ledger — which is why the mark pulses and why the drain is never invisible.
            foreach (var v in visions)
            {
                if (!v.a || v.of == null || v.of.Count == 0) continue;

                var offer = v.of[Mathf.Clamp(v.sel, 0, v.of.Count - 1)];
                if (Held(offer.r) < v.OfferCost(offer)) continue;
                _channelled.Add(v);
            }
            // Poured after the loop: a one-off that completes leaves the list it is in.
            foreach (var v in _channelled) Pour(v);
            _channelled.Clear();

            Dirty();
        }
    }
}
