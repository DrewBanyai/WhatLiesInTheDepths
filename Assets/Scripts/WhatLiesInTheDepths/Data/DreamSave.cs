// What Lies In The Depths — a dream, written down and read back.
//
// Only what the player has changed is saved: the unlocks, what is held, what is built, bound,
// realized, poured, mustered and taken, how deep the dive has gone, and what has been read.
// Everything authored (names, costs, requirements, paragraphs, effects) comes from the content
// every time the game starts, so a save made before a balance change or in another language
// still loads into the current game. Every piece is keyed by its id; a saved id the content no
// longer has is skipped, and a thing the save does not mention keeps its starting state.
//
// Upgrades are the one thing not restored field by field: their unlocks are left out when the
// save is read, so the dream applies them again from its own content on the first check, the
// same way it did when they first happened.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ursine.Text;

namespace WhatLiesInTheDepths.Data
{
    public static class DreamSave
    {
        public const int Version = 1;

        // ---- writing ---------------------------------------------------------------

        public static string Write(Dream d)
        {
            var o = new JsonObject();
            o["version"] = (double)Version;
            o["savedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            o["unlocks"] = d.unlocks.All.OrderBy(s => s, StringComparer.Ordinal).Cast<object>().ToList();

            var res = new JsonObject();
            foreach (var r in d.resources) res[r.k] = r.c;
            o["resources"] = res;

            var tasks = new JsonObject();
            foreach (var t in d.tasks)
                tasks[t.id] = Obj(("w", t.w), ("p", t.p), ("seen", t.seen));
            o["tasks"] = tasks;

            var cons = new JsonObject();
            foreach (var c in d.constructs)
                cons[c.g] = Obj(("owned", c.owned), ("built", c.built), ("seen", c.seen));
            o["constructs"] = cons;

            var revs = new JsonObject();
            foreach (var r in d.revelations)
                revs[r.k] = Obj(("realized", r.realized), ("seen", r.seen));
            o["revelations"] = revs;
            o["absorbed"] = d.absorbed.Cast<object>().ToList();

            var visions = new JsonObject();
            foreach (var v in d.visions.Concat(d.visionsDone))
                visions[v.k] = Obj(("p", v.p), ("a", v.a), ("sel", v.sel), ("done", v.done), ("seen", v.seen));
            o["visions"] = visions;
            o["visionsDone"] = d.visionsDone.Select(v => (object)v.k).ToList();
            o["visionsAbsorbed"] = d.visionsAbsorbed.Select(v => (object)v.k).ToList();

            var road = new JsonObject();
            foreach (var l in d.road)
                road[l.k] = Obj(("won", l.won), ("tookWith", l.tookWith));
            o["road"] = road;

            var units = new JsonObject();
            foreach (var u in d.units.Concat(d.unitsUnlockedBy.Values))
                units[u.k] = Obj(("c", u.c), ("mustered", u.mustered), ("isNew", u.isNew));
            o["units"] = units;

            // The Journal in the order it was written, and which entries have been read.
            var journal = new List<object>();
            foreach (var e in d.journal)
                if (!string.IsNullOrEmpty(e.id)) journal.Add(Obj(("id", e.id), ("seen", e.seen)));
            o["journal"] = journal;

            var veils = new JsonObject();
            foreach (var v in d.veils)
                veils[v.ord.ToString(CultureInfo.InvariantCulture)] = Obj(("sunk", v.sunk), ("w", v.w), ("p", v.p));
            o["veils"] = veils;
            o["veilIndex"] = (double)d.veilIndex;

            var done = new JsonObject();
            foreach (var kv in d.done) done[kv.Key] = (double)kv.Value;
            o["done"] = done;
            o["fathomsTotal"] = d.fathomsTotal;
            o["attendedTaskId"] = d.attendedTaskId;
            o["attendingDive"] = d.attendingDive;
            o["musterCostScale"] = d.musterCostScale;
            o["exodusAt"] = (double)d.ExodusAt;

            return MiniJson.Write(o);
        }

        static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs)
                o[k] = v is int i ? (double)i : v is float f ? (double)f : v;
            return o;
        }

        // ---- reading ---------------------------------------------------------------

        /// <summary>Lays a save over a freshly started dream. Returns false if the text is not a
        /// save this game can read; the dream may then be half restored and should be dropped.</summary>
        public static bool Read(Dream d, string json, out string error)
        {
            error = null;
            JsonObject o;
            try { o = MiniJson.Parse(json) as JsonObject; }
            catch (Exception e) { error = e.Message; return false; }
            if (o == null) { error = "not a save"; return false; }
            int version = (int)Num(o["version"]);
            if (version < 1 || version > Version) { error = "save version " + version + " is not readable"; return false; }

            try { Apply(d, o); }
            catch (Exception e) { error = e.Message; return false; }
            return true;
        }

        static void Apply(Dream d, JsonObject o)
        {
            // Everything but the upgrades, which the dream applies again itself.
            if (o["unlocks"] is List<object> ul)
                foreach (var u in ul)
                    if (u is string s && !s.StartsWith("upgrade:", StringComparison.Ordinal)) d.unlocks.Add(s);

            if (o["resources"] is JsonObject res)
                foreach (var r in d.resources)
                    if (res.Has(r.k)) r.c = Num(res[r.k]);

            var tasks = o["tasks"] as JsonObject;
            if (tasks != null)
                foreach (var t in d.tasks)
                    if (tasks[t.id] is JsonObject to)
                    {
                        t.w = (int)Num(to["w"]);
                        t.p = (float)Num(to["p"]);
                    }

            var cons = o["constructs"] as JsonObject;
            if (cons != null)
                foreach (var c in d.constructs)
                    if (cons[c.g] is JsonObject co)
                    {
                        c.owned = (int)Num(co["owned"]);
                        c.built = Bool(co["built"], c.built);
                    }

            var revs = o["revelations"] as JsonObject;
            if (revs != null)
                foreach (var r in d.revelations)
                    if (revs[r.k] is JsonObject ro) r.realized = Bool(ro["realized"], false);
            d.absorbed.Clear();
            d.absorbed.AddRange(Ids(o["absorbed"]));

            // Visions: progress on all of them; the finished one-offs leave the field for the
            // list of finished ones, as completing them did.
            var all = d.visions.Concat(d.visionsDone).Distinct().ToList();
            if (o["visions"] is JsonObject vis)
                foreach (var v in all)
                    if (vis[v.k] is JsonObject vo)
                    {
                        v.p = (float)Num(vo["p"]);
                        v.a = Bool(vo["a"], false);
                        v.sel = (int)Num(vo["sel"]);
                        v.done = (int)Num(vo["done"]);
                    }
            var vdone = Ids(o["visionsDone"]);
            foreach (var v in all.Where(v => vdone.Contains(v.k)))
            {
                d.visions.Remove(v);
                if (!d.visionsDone.Contains(v)) d.visionsDone.Add(v);
            }
            var vabs = Ids(o["visionsAbsorbed"]);
            d.visionsAbsorbed.Clear();
            d.visionsAbsorbed.AddRange(all.Where(v => vabs.Contains(v.k)));

            // The road: a place taken hands its unit to the roster, as taking it did.
            if (o["road"] is JsonObject road)
                foreach (var l in d.road)
                    if (road[l.k] is JsonObject lo && Bool(lo["won"], false))
                    {
                        l.won = true;
                        l.tookWith = Num(lo["tookWith"]);
                        if (d.unitsUnlockedBy.TryGetValue(l.k, out var unit))
                        {
                            if (!d.units.Contains(unit)) d.units.Add(unit);
                            d.unitsUnlockedBy.Remove(l.k);
                        }
                    }

            var units = o["units"] as JsonObject;
            if (units != null)
                foreach (var u in d.units.Concat(d.unitsUnlockedBy.Values))
                    if (units[u.k] is JsonObject uo)
                    {
                        u.c = (int)Num(uo["c"]);
                        u.mustered = (int)Num(uo["mustered"]);
                    }

            if (o["journal"] is List<object> jl)
            {
                d.journal.Clear();
                foreach (var item in jl)
                {
                    if (!(item is JsonObject jo) || !(jo["id"] is string id)) continue;
                    var e = d.journalDefs.FirstOrDefault(x => x.id == id);
                    if (e == null || d.journal.Contains(e)) continue;
                    e.seen = Bool(jo["seen"], true);
                    d.journal.Add(e);
                }
            }

            if (o["veils"] is JsonObject veils)
                foreach (var v in d.veils)
                    if (veils[v.ord.ToString(CultureInfo.InvariantCulture)] is JsonObject vo)
                    {
                        v.sunk = Num(vo["sunk"]);
                        v.w = (int)Num(vo["w"]);
                        v.p = (float)Num(vo["p"]);
                    }
            int vi = (int)Num(o["veilIndex"]);
            if (vi >= 0 && vi < d.veils.Count) { d.veilIndex = vi; d.veil = d.veils[vi]; }

            d.done.Clear();
            if (o["done"] is JsonObject done)
                foreach (var k in done.Keys) d.done[k] = (int)Num(done[k]);
            d.fathomsTotal = Num(o["fathomsTotal"]);
            d.attendedTaskId = o["attendedTaskId"] as string;
            if (d.attendedTaskId != null && !d.tasks.Any(t => t.id == d.attendedTaskId)) d.attendedTaskId = null;
            d.attendingDive = Bool(o["attendingDive"], false);
            d.musterCostScale = o.Has("musterCostScale") ? Num(o["musterCostScale"]) : 1.0;
            d.ExodusAt = o.Has("exodusAt") ? (int)Num(o["exodusAt"]) : -1;

            // Works everything out again, and applies the upgrades.
            d.Restored();

            // The dots last: re-applying an upgrade can mark a thing new, and a new dream marks
            // what it shows as new; the save knows which of them the player had already seen.
            if (tasks != null)
                foreach (var t in d.tasks)
                    if (tasks[t.id] is JsonObject to) t.seen = Bool(to["seen"], true);
            if (cons != null)
                foreach (var c in d.constructs)
                    if (cons[c.g] is JsonObject co) c.seen = Bool(co["seen"], true);
            if (revs != null)
                foreach (var r in d.revelations)
                    if (revs[r.k] is JsonObject ro) r.seen = Bool(ro["seen"], true);
            if (units != null)
                foreach (var u in d.units.Concat(d.unitsUnlockedBy.Values))
                    if (units[u.k] is JsonObject uo) u.isNew = Bool(uo["isNew"], false);
            if (o["visions"] is JsonObject vseen)
                foreach (var v in d.visions.Concat(d.visionsDone))
                    if (vseen[v.k] is JsonObject vo) v.seen = Bool(vo["seen"], true);
            d.Dirty();
        }

        static double Num(object v) => v is double x ? x : 0.0;
        static bool Bool(object v, bool fallback) => v is bool b ? b : fallback;
        static HashSet<string> Ids(object v)
            => v is List<object> l ? new HashSet<string>(l.OfType<string>()) : new HashSet<string>();
    }
}
