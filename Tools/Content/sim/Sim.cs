// A headless player for What Lies In The Depths: plays the Dream from the first journal entry to
// the last battle with a simple greedy policy, and reports when each milestone falls and, if it
// stalls, exactly what it is waiting on.
using System;
using System.Collections.Generic;
using System.Linq;
using WhatLiesInTheDepths.Data;
using Ursine.Economy;
using Ursine.Combat;

static class Sim
{
    static Dream d;
    static double t;
    static Random rng = new Random(7);
    static string path = "good";
    static bool verbose;
    static double dumpAt = -1;
    static readonly HashSet<string> seen = new HashSet<string>();

    static string Clock(double s) => $"{(int)(s / 3600)}h{(int)(s % 3600 / 60):00}m";

    static void Log(string m) => Console.WriteLine($"[{Clock(t),8}] L{d.veilIndex + 1,-3} {m}");

    static void Main(string[] args)
    {
        try { Run(args); } catch (System.IO.IOException) { }
    }

    static void Run(string[] args)
    {
        if (args.Length > 0) path = args[0];
        verbose = args.Contains("-v");
        var da = args.FirstOrDefault(a => a.StartsWith("--dump="));
        if (da != null) dumpAt = double.Parse(da.Substring(7)) * 60;
        var sa = args.FirstOrDefault(a => a.StartsWith("--seed="));
        if (sa != null) rng = new Random(int.Parse(sa.Substring(7)));
        double limit = 40 * 3600;
        d = new Dream(false);
        double lastProgress = 0, lastArmy = 0; int lastCount = 0;
        double tickAcc = 0, thinkAcc = 0, bindAcc = 0;
        const double dt = 0.5;
        while (t < limit)
        {
            double siltBefore = d.Held("silt");
            bool diving = d.GaugeOpen && d.veil != null && !d.veil.AtFull;
            if (diving && d.HoldOfDive == Hold.Full)
            {
                string k = d.veil.bring[0].k;
                heldBy[k] = (heldBy.TryGetValue(k, out var h) ? h : 0) + dt;
                if (k == "silt") siltLocked += dt;
            }
            d.Step((float)dt);
            double siltAfter = d.Held("silt");
            if (siltAfter > siltBefore && d.veilIndex < 100) siltEarned += siltAfter - siltBefore;
            if (siltAfter < siltBefore && d.veilIndex < 100) siltFocus += siltBefore - siltAfter;
            t += dt; tickAcc += dt; thinkAcc += dt; bindAcc += dt;
            if (tickAcc >= 1) { tickAcc -= 1; d.Tick(); }
            if (thinkAcc >= 2)
            {
                thinkAcc = 0;
                var cis = d.constructs.FirstOrDefault(c => c.g == "cistern");
                int cisBefore = cis?.owned ?? 0; double sb = d.Held("silt");
                Think();
                if (d.veilIndex < 100)
                {
                    double spent = Math.Max(0, sb - d.Held("silt"));
                    int built = (cis?.owned ?? 0) - cisBefore;
                    double onCis = 0;
                    for (int i = 0; i < built; i++) onCis += Math.Ceiling(12 * Math.Pow(1.02, cisBefore + i) - 1e-9);
                    siltCistern += Math.Min(spent, onCis); siltOther += Math.Max(0, spent - onCis);
                }
            }
            if (bindAcc >= 4) { bindAcc = 0; Bind(); }
            Milestones();
            if (dumpAt > 0 && t >= dumpAt && t - dt < dumpAt) { Log("DUMP"); Dump(); }
            if (d.unlocks.Has("gameover")) { Log("THE END (" + (d.unlocks.Has("ending:bad") ? "bad" : "good") + ")"); Summary(); return; }
            if (d.unlocks.Count != lastCount || d.Army > lastArmy * 1.03 + 1) { lastCount = d.unlocks.Count; lastArmy = d.Army; lastProgress = t; }
            if (t - lastProgress > 7200) { Log("STALLED"); Dump(); return; }
        }
        Log("TIME LIMIT"); Dump();
    }

    // Silt bookkeeping: how much the dives brought up, how long the dive stood still because
    // what it brings was full, and how many Cisterns were built to make room.
    static double siltEarned, siltLocked, siltFocus, siltCistern, siltOther;
    static readonly Dictionary<string, double> heldBy = new Dictionary<string, double>();

    static void Milestones()
    {
        foreach (var u in d.unlocks.All.ToList())
        {
            if (seen.Contains(u)) continue;
            seen.Add(u);
            if (u.StartsWith("rev:") || u.StartsWith("vision:") || u.StartsWith("won:") || u.StartsWith("built:") ||
                u.StartsWith("menu:") || u.StartsWith("upgrade:") || u.StartsWith("lost:any") || (u.StartsWith("parted:") && int.Parse(u.Substring(7)) % 5 == 0))
                Log(u + (u.StartsWith("won:") ? $"  army {d.Army:0}" : "")
                    + (u.StartsWith("parted:") ? $"  silt {siltEarned:0} earned, {siltLocked / 60:0} min held full, {d.constructs.FirstOrDefault(c => c.g == "cistern")?.owned ?? 0} cisterns" : ""));
        }
    }

    static HashSet<string> missing = new HashSet<string>();
    static string targetName = "";

    // What the player is saving for: the cheapest shown thing they can ever afford, Warrens and
    // Realizations first. Everything it is short of is "missing".
    static void Target()
    {
        missing.Clear(); targetName = "";
        var cands = new List<Tuple<string, List<Amount>, double>>();
        foreach (var r in d.ShownRevelations.Where(x => d.MenuOpen("revelations")))
        {
            if (r.k == "fear" && path != "bad") continue;
            if (r.k == "shut" && path == "bad") continue;
            if (d.Judge(r.cost) == Refusal.AboveCeiling) continue;
            cands.Add(Tuple.Create("rev " + r.k, r.cost, 0.5));
        }
        foreach (var c in d.ShownConstructs.Where(x => d.MenuOpen("constructs")))
        {
            if (c.once && c.owned > 0) continue;
            if (!Wanted(c)) continue;
            if (d.Judge(c.cost) == Refusal.AboveCeiling) continue;
            cands.Add(Tuple.Create("build " + c.g, c.cost, c.housing > 0 ? 0.6 : 1.0));
        }
        foreach (var v in d.visions.Where(x => d.Shown(x) && d.MenuOpen("visions") && !(x.rep && x.done >= 3)))
        {
            var o = v.of[v.sel];
            if (d.Ceiling(o.r) < v.OfferCost(o)) continue;
            cands.Add(Tuple.Create("vision " + v.k, new List<Amount> { new Amount(o.r, v.OfferCost(o)) }, v.great ? 0.4 : 0.8));
        }
        var best = cands.OrderBy(x => x.Item3 * x.Item2.Sum(a => Math.Max(0, a.n - d.Held(a.k)) / Math.Max(1, d.Ceiling(a.k)))).FirstOrDefault();
        if (best == null) return;
        targetName = best.Item1;
        foreach (var a in best.Item2) if (d.Held(a.k) < a.n) missing.Add(a.k);
        // what the makers of the missing are themselves short of is missing too
        for (int pass = 0; pass < 3; pass++)
            foreach (var tk in d.ShownTasks.Where(x => missing.Any(m => Makes(x, m)) && d.HoldOf(x) == Hold.Short).ToList())
                foreach (var c in tk.cost) if (d.Held(c.k) < c.n) missing.Add(c.k);
    }

    // A reservoir is only worth building when something it holds is (nearly) full.
    static bool Wanted(ConstructDef c)
    {
        if (c.once || c.housing > 0 || c.effects == null || c.effects.Count == 0) return true;
        bool storageOnly = c.effects.All(e => e.kind == Fx.Cap || e.kind == Fx.CapPct);
        if (!storageOnly) return true;
        var e0 = c.effects[0]; return d.Ceiling(e0.of) > 0 && d.Held(e0.of) >= d.Ceiling(e0.of) * 0.95;
    }

    // A gate veil's price (Dread, Chorus, Mettle) is saved for the dive, not spent elsewhere.
    static bool Reserved(List<Amount> cost) => d.GaugeOpen && d.veil != null && !d.veil.AtFull && d.HasNextVeil
        && cost.Any(a => a.k != "reverie" && d.veil.spend.Any(v => v.k == a.k));

    static bool Makes(FocusTask t, string r) => t.baseGain != null && t.baseGain.Any(a => a.k == r);
    static bool Needs(List<Amount> cost, string r) => cost != null && cost.Any(a => a.k == r);
    static bool DiveUseful => d.GaugeOpen && d.veil != null && !d.veil.AtFull && d.HoldOfDive == Hold.None
                              && !d.veil.spend.Any(a => missing.Contains(a.k) && !d.veil.bring.Any(b => b.k == a.k));

    static void Think()
    {
        Target();
        // descend
        if (d.veil != null && d.veil.AtFull && d.HasNextVeil) d.PartVeil();

        // realize — the choice by path
        foreach (var r in d.ShownRevelations.ToList())
        {
            if (r.k == "fear" && path != "bad") continue;
            if (r.k == "shut" && path == "bad") continue;
            if (d.MenuOpen("revelations") && d.Judge(r.cost) == Refusal.None) d.Realize(r);
        }

        // build: cheapest first, a few a think
        for (int i = 0; i < 3; i++)
        {
            var c = d.ShownConstructs.Where(x => !(x.once && x.owned > 0) && Wanted(x) && !Reserved(x.cost) && d.Judge(x.cost) == Refusal.None)
                     .OrderBy(x => x.cost.Sum(a => a.n / Math.Max(1, d.Ceiling(a.k)))).FirstOrDefault();
            if (c == null) break;
            // keep the kiln for the nightmare path only; it exists only there anyway
            if (!d.MenuOpen("constructs")) break;
            d.Build(c);
        }

        // visions: channel everything, choosing the offer we can best afford
        foreach (var v in d.visions.Where(x => d.Shown(x)).ToList())
        {
            int best = 0; double bestScore = -1;
            for (int i = 0; i < v.of.Count; i++)
            {
                var o = v.of[i]; double cost = v.OfferCost(o);
                if (d.Ceiling(o.r) < cost) continue;
                double score = d.Held(o.r) / cost;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            v.sel = best; v.a = d.MenuOpen("visions");
        }

        // the army
        if (d.MenuOpen("assault"))
        {
            for (int i = 0; i < 20; i++)
            {
                var u = d.units.Where(x => d.Shown(x) && !Reserved(d.MusterCost(x)) && d.Judge(d.MusterCost(x)) == Refusal.None)
                         .OrderByDescending(x => x.p).FirstOrDefault();
                if (u == null || !d.Muster(u)) break;
            }
            var next = d.NextPlace;
            if (next != null && d.Open(next))
            {
                double c = Odds.Chance(d.Army, next.en);
                if (c >= 0.7)
                {
                    if (rng.NextDouble() < c) d.TakePlace(next, d.Army);
                    else { var loss = d.LoseBattle(next, rng); Log($"lost at {next.k} ({loss.pct}%)"); }
                }
            }
        }

        // attention: on whatever makes what the target is short of; else the dive; else rotate
        var makers = d.ShownTasks.Where(x => d.HoldOf(x) == Hold.None && missing.Any(m => Makes(x, m))).OrderBy(x => x.w).ToList();
        // A player tries every new Focus once, out of curiosity, before settling into a routine.
        var untried = d.ShownTasks.FirstOrDefault(x => d.HoldOf(x) == Hold.None && !d.done.ContainsKey(x.id));
        if (untried != null) { d.attendingDive = false; d.attendedTaskId = untried.id; }
        else if (makers.Count > 0) { d.attendingDive = false; d.attendedTaskId = makers[0].id; }
        else if (DiveUseful) { d.attendingDive = true; d.attendedTaskId = null; }
        else
        {
            var any = d.ShownTasks.Where(x => d.HoldOf(x) == Hold.None).ToList();
            d.attendingDive = false;
            d.attendedTaskId = any.Count > 0 ? any[turn++ % any.Count].id : null;
        }
    }
    static int turn;

    // Oneiri: the dive first, then every task that can run, round-robin up to caps, and
    // weighted toward whatever is scarcest.
    static void Bind()
    {
        if (!d.BindingOpen) return;
        foreach (var tk in d.tasks) tk.w = 0;
        if (d.veil != null) d.veil.w = 0;
        d.Dirty();
        int free = d.OneiriTotal;
        if (DiveUseful)
        {
            int dive = Math.Min(d.veil.cap, (int)Math.Ceiling(free * (missing.Count == 0 ? 0.5 : 0.3)));
            d.veil.w = dive; free -= dive;
        }
        // makers of what is missing first, then everything else that can run and does not eat
        // what is missing
        var live = d.ShownTasks.Where(x => d.HoldOf(x) != Hold.Full && !missing.Any(m => Needs(x.cost, m))).ToList();
        live = live.OrderByDescending(x => missing.Any(m => Makes(x, m)) ? 1 : 0)
                   .ThenBy(x => x.gain.Min(a => d.Ceiling(a.k) <= 0 ? 1 : d.Held(a.k) / d.Ceiling(a.k))).ToList();
        foreach (var tk in live.Where(x => missing.Any(m => Makes(x, m))))
        { int n = Math.Min(free, tk.cap); tk.w = n; free -= n; }
        bool any = true;
        while (free > 0 && any)
        {
            any = false;
            foreach (var tk in live)
            {
                if (free <= 0) break;
                if (tk.w >= tk.cap) continue;
                tk.w++; free--; any = true;
            }
        }
        if (free > 0 && DiveUseful) { int more = Math.Min(free, d.veil.cap - d.veil.w); d.veil.w += more; }
        d.Dirty();
    }

    static void Summary()
    {
        Console.WriteLine($"army {d.Army:0}; units: " + string.Join(", ", d.units.Where(u => u.c > 0).Select(u => $"{u.n} x{u.c} ({u.p:0})")));
        Console.WriteLine("never realized: " + string.Join(", ", d.revelations.Where(r => !r.realized && !d.Withdrawn(r)).Select(r => r.k)));
        Console.WriteLine("visions left: " + string.Join(", ", d.visions.Select(v => v.k + (v.rep ? "(rep x" + v.done + ")" : ""))));
        Console.WriteLine("journal: " + d.journal.Count + " of " + d.journalDefs.Count + "; missing " + string.Join(", ", d.journalDefs.Where(j => !d.journal.Contains(j)).Select(j => j.id)));
        Console.WriteLine("resources never shown: " + string.Join(", ", d.resources.Where(r => !d.Shown(r)).Select(r => r.k)));
        Console.WriteLine("dive held full, by what filled: " + string.Join(", ", heldBy.Select(kv => $"{kv.Key} {kv.Value / 60:0} min")));
        Console.WriteLine($"silt spent to the bottom: Focus {siltFocus:0}, Cisterns {siltCistern:0}, everything else {siltOther:0}");
        Console.WriteLine($"silt: earned to the bottom {siltEarned:0}, ceiling {d.Ceiling("silt"):0}, dive held full {siltLocked / 60:0} min, cisterns {d.constructs.FirstOrDefault(c => c.g == "cistern")?.owned ?? 0}");
        Console.WriteLine("constructs: " + string.Join(", ", d.constructs.Where(c => c.owned > 0).Select(c => $"{c.g}x{c.owned}")));
    }

    static void Dump()
    {
        Summary();
        Console.WriteLine("target " + targetName + " missing " + string.Join(",", missing));
        Console.WriteLine($"veil {d.veil?.n} ord {d.veil?.ord} sunk {d.veil?.sunk}/{d.veil?.need} hold {d.HoldOfDive} spend {Str(d.veil?.spend)}");
        foreach (var r in d.resources.Where(r => d.Shown(r)))
            Console.WriteLine($"  {r.k,-11} {r.c,9:0.0} / {r.m,-7:0} {r.r,8:0.00}/s");
        foreach (var r in d.ShownRevelations) Console.WriteLine($"  REV {r.k}: {Str(r.cost)} -> {d.Judge(r.cost)}");
        var hidden = d.revelations.Where(r => !r.realized && !d.Shown(r) && !d.Withdrawn(r)).Take(6);
        foreach (var r in hidden) Console.WriteLine($"  (waiting) REV {r.k}: {string.Join(" & ", r.requires)}");
        foreach (var c in d.ShownConstructs) Console.WriteLine($"  BUILD {c.g} x{c.owned}: {Str(c.cost)} -> {d.Judge(c.cost)}");
        foreach (var v in d.visions.Where(x => d.Shown(x))) Console.WriteLine($"  VISION {v.k} {v.p:0}% offers {string.Join(",", v.of.Select(o => o.r + " " + v.OfferCost(o)))}");
        foreach (var tk in d.ShownTasks) Console.WriteLine($"  TASK {tk.id} w{tk.w}/{tk.cap} hold {d.HoldOf(tk)} speed {tk.speed:0.00}");
        foreach (var u in d.units) Console.WriteLine($"  UNIT {u.k} x{u.c} shown {d.Shown(u)} cost {Str(d.MusterCost(u))} -> {d.Judge(d.MusterCost(u))}");
        var nx = d.NextPlace;
        if (nx != null) Console.WriteLine($"  NEXT {nx.k} en {nx.en} open {d.Open(nx)} army {d.Army:0} req {string.Join("&", nx.requires ?? new List<string>())}");
    }

    static string Str(List<Amount> a) => a == null ? "-" : string.Join(" ", a.Select(x => $"{x.k}:{x.n:0.#}"));
}
