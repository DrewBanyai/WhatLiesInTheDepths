# Turns design.py into OpeningContent.cs and Strings.json keys.
#   python3 gen.py <Assets dir>
import json, math, sys, os, re
from collections import OrderedDict
import design as D
import veil_more as VM

ASSETS = sys.argv[1]
CS = os.path.join(ASSETS, "Scripts/WhatLiesInTheDepths/Data/OpeningContent.cs")
STRINGS = os.path.join(ASSETS, "Resources/WhatLiesInTheDepths/Strings.json")

S = OrderedDict()          # strings to write

def fmtn(n):
    if abs(n - round(n)) < 1e-9: return str(int(round(n)))
    return ("%.2f" % n).rstrip("0").rstrip(".")

def pct(n): return fmtn(n * 100) + "%"

def sign(n): return "+" if n >= 0 else "−"

FOCUS_NAME = {}
for f in D.FOCUS:
    FOCUS_NAME[f[0]] = f[8] or {"absorb": "Absorb", "conjure": "Conjure"}[f[0]]
CONSTRUCT_NAME = {c["g"]: c.get("name") for c in D.CONSTRUCTS}
CONSTRUCT_NAME.update({"altar": "The Silent Altar", "hut": "Warren"})
UNIT_NAME = {u[0]: u[5] for u in D.UNITS}
VISION_NAME = {v["k"]: v["n"] for v in D.VISIONS}
REV_NAME = {r["k"]: r.get("n") for r in D.REVELATIONS}
REV_NAME.update({"sifting": "Sifting the Silt", "listening": "Someone Is Listening", "shown": "Shown, Not Told"})
PLACE_NAME = {p[0]: p[1] for p in D.PLACES}

def rn(k): return D.RES_NAMES.get(k, k)

def fx_line(e):
    kind, of, n = e
    if kind == "Rate": return f"<b>{sign(n)}{fmtn(abs(n))}</b> {rn(of)} /s"
    if kind == "Cap": return f"<b>+{fmtn(n)}</b> max {rn(of)}"
    if kind == "CapPct": return f"max {rn(of)} <b>+{pct(n)}</b>"
    if kind == "Gain":
        if of == "*": return f"all Focus gains <b>{sign(n)}{pct(abs(n))}</b>"
        if of == "dive": return f"dives bring up <b>{sign(n)}{pct(abs(n))}</b>"
        return f"<b>{FOCUS_NAME[of]}</b> gives <b>{sign(n)}{pct(abs(n))}</b>"
    if kind == "Speed":
        if n <= -1: return f"<b>{FOCUS_NAME[of]}</b> falls silent"
        if of == "*": return f"all Focus <b>{sign(n)}{pct(abs(n))}</b> faster"
        return f"<b>{FOCUS_NAME[of]}</b> <b>{sign(n)}{pct(abs(n))}</b> faster"
    if kind == "DiveSpeed": return f"dives <b>+{pct(n)}</b> faster"
    if kind == "DiveCost": return f"dives cost <b>−{pct(n)}</b>"
    if kind == "Power":
        who = "every unit" if of == "*" else f"{of} units"
        return f"{who} <b>+{pct(n)}</b> strength"
    if kind == "Enemy": return f"the road fields <b>−{pct(n)}</b>"
    if kind == "Muster": return f"mustering costs <b>−{pct(n)}</b>"
    if kind == "Housing": return f"each {CONSTRUCT_NAME.get(of, of)} houses <b>+{fmtn(n)}</b> Oneiri"
    if kind == "Exodus": return "no new Oneiri come, and the unbound ones leave"
    if kind == "VisionCost": return f"Visions cost <b>−{pct(n)}</b>"
    if kind == "RevCost": return f"Realizations cost <b>−{pct(n)}</b>"
    raise ValueError(kind)

def unlock_lines(cond):
    """Everything that appears the moment cond holds, as card lines."""
    out = []
    for f in D.FOCUS:
        if cond in f[7]: out.append(f"unlocks the <b>{FOCUS_NAME[f[0]]}</b> focus")
    for c in D.CONSTRUCTS:
        if cond in c.get("requires", []): out.append(f"unlocks <b>{CONSTRUCT_NAME[c['g']]}</b>")
    for r in D.RESOURCES:
        if cond in r[3] and r[0] not in ("oneiri",):
            if not any(cond in f[7] for f in D.FOCUS if any(g[0] == r[0] for g in f[4])):
                out.append(f"brings <b>{rn(r[0])}</b>")
    for u in D.UNITS:
        if cond in u[4]: out.append(f"unlocks <b>{u[5]}</b>")
    for v in D.VISIONS:
        if cond in v["requires"]: out.append(f"shows the Vision <b>{v['n']}</b>")
    for r in D.REVELATIONS:
        if cond in r["requires"] and r.get("n"): out.append(f"a realization: <b>{r['n']}</b>")
    for u in D.UPGRADES:
        if cond in u["when"]:
            if u["target"] == "Construct": out.append(f"every <b>{CONSTRUCT_NAME[u['of']]}</b> becomes <b>{u['n']}</b>")
            elif u["target"] == "Resource": out.append(f"<b>{rn(u['of'])}</b> becomes <b>{u['n']}</b>")
            elif u["target"] == "Focus": out.append(f"<b>{FOCUS_NAME[u['of']]}</b> becomes <b>{u['n']}</b>")
            elif u["target"] == "Unit": pass
    if any(cond in u["when"] and u["target"] == "Unit" for u in D.UPGRADES):
        out.append("every Sworn unit becomes its nightmare, <b>×2.5</b>")
    for rule in D.RULES:
        if rule[1] == [cond]:
            for g in rule[2]:
                if g.startswith("menu:"): out.append(f"opens <b>{g[5:].title()}</b>")
    if any(cond in j[5] for j in D.JOURNAL): out.append("the Journal gains an entry")
    return out

# ---------------------------------------------------------------------------------------
cs = []
def w(line=""): cs.append(line)

def amounts(lst):
    if not lst: return "null"
    return "C(" + ", ".join(f'"{k}", {fmtn(n)}' for k, n in lst) + ")"

def strs(lst):
    if not lst: return "null"
    return "L(" + ", ".join(json.dumps(x) for x in lst) + ")"

def effs(lst):
    if not lst: return "null"
    parts = []
    for kind, of, n in lst:
        parts.append(f'E(Fx.{kind}, {json.dumps(of) if of is not None else "null"}, {fmtn(n)})')
    return "Fx_(" + ", ".join(parts) + ")"

# ---- the veils
def need(level):
    if level == 1: return 20
    if level == 2: return 60
    if level in D.GATES: return 60
    return int(round(20 + 4 * level + 0.06 * level * level))

def veil_rows():
    rows = []
    for (name, a, b) in D.REACHES:
        for lv in range(a, b + 1):
            spend = [("reverie", max(1, math.ceil(lv / 12)))]
            if lv in D.GATES: spend.append(D.GATES[lv])
            silt = [("silt", math.ceil(1 + lv / 15))]
            own = [(k, round(n * (1 + lv / 40), 2)) for k, n in D.REACH_BRING.get(name, [])]
            # the primary (first) yield is the only one that can hold the dive
            bring = own + silt if name in D.PRIMARY_OWN and own else silt + own
            if name == "The Door Ajar": bring = []
            rows.append(dict(ord=lv, reach=name, need=need(lv), spend=spend, bring=bring,
                             cap=3 + lv // 4, secs=round(6 + lv * 0.2, 1)))
    # The bottom never finishes: it is sunk for ever, and it is where silt comes from once
    # there is nowhere further to go.
    rows.append(dict(ord=101, reach=D.BOTTOM[0], need=999999, spend=[("reverie", 9)],
                     bring=[("silt", 8), ("moonsilver", 2), ("echo", 4)], cap=30, secs=24, bottom=True))
    return rows

def slug(s): return re.sub(r"[^a-z]+", "", s.lower().replace("the ", ""))

# ---------------------------------------------------------------------------------------
w("// What Lies In The Depths — the whole dream, from the first journal entry to the last battle.")
w("//")
w("// GENERATED by Tools/Content/gen.py from Tools/Content/design.py. Edit the design and run")
w("// the generator; a hand edit here is overwritten. No word lives here: every name, blurb,")
w("// paragraph and effect line is read from Strings.json by key, and the effect lines are")
w("// written from the effects themselves, so a card never says what its numbers do not do.")
w("//")
w("// Every number is a first-pass placeholder, tuned only far enough that a simulated player")
w("// walks the whole path (Tools/Content/sim). The five chapters are twenty veils each; the")
w("// last veil of chapters two to four is sunk with a resource only that chapter's golden beat")
w("// brings (Dread, Chorus, Mettle), which is the roadblock. The road has thirty places; the")
w("// last six open only at the bottom, and only once the choice is made.")
w("using System.Collections.Generic;")
w("using UnityEngine;")
w("using Ursine.Economy;")
w("using Ursine.Text;")
w("")
w("namespace WhatLiesInTheDepths.Data")
w("{")
w("    public static class OpeningContent")
w("    {")
w("        static List<Amount> C(params object[] kv)")
w("        {")
w("            var list = new List<Amount>();")
w("            for (int i = 0; i + 1 < kv.Length; i += 2)")
w("                list.Add(new Amount((string)kv[i], System.Convert.ToDouble(kv[i + 1])));")
w("            return list;")
w("        }")
w("        static List<string> L(params string[] s) => new List<string>(s);")
w("        static List<Effect> Fx_(params Effect[] e) => new List<Effect>(e);")
w("        static Effect E(Fx kind, string of, double n) => new Effect(kind, of, n);")
w("        static string T(string key) => Loc.T(key);")
w("        static List<string> Lines(string key) => Loc.Lines(key);")
w("")
w("        public static void Fill(Dream s)")
w("        {")
w("            Resources(s);")
w("            Rules(s);")
w("            Focus(s);")
w("            Constructs(s);")
w("            Upgrades(s);")
w("            Revelations(s);")
w("            Visions(s);")
w("            Veils(s);")
w("            Road(s);")
w("            Journal(s);")
w("            Achievements(s);")
w("        }")
w("")

# resources
w("        static void Res_(Dream s, string k, double m, ResGroup g, string glyph, List<string> requires)")
w("        {")
w("            s.resources.Add(new Res { k = k, n = T(\"res.\" + k), c = 0, m = m, g = (int)g, glyph = glyph,")
w("                                      hostile = g == ResGroup.AgainstYou, requires = requires });")
w("        }")
w("")
w("        static void Resources(Dream s)")
w("        {")
grp = {0: "ResGroup.Gathered", 1: "ResGroup.Yours", 2: "ResGroup.AgainstYou"}
for k, g, m, req, glyph in D.RESOURCES:
    w(f'            Res_(s, "{k}", {m}, {grp[g]}, "{glyph}", {strs(req)});')
    S[f"res.{k}"] = D.RES_NAMES[k]
for k, n in getattr(D, "RES_START", {}).items():
    w(f'            s.startOnShow["{k}"] = {n};')
w("        }")
w("")

# rules
w("        static void Rules(Dream s)")
w("        {")
for rid, when, grants in D.RULES:
    w(f'            s.rules.Add(new Rule {{ id = "{rid}", when = {strs(when)}, grants = {strs(grants)} }});')
w("        }")
w("")

# focus
w("        static void Task(Dream s, string id, string section, string g, List<Amount> cost, List<Amount> gain,")
w("                         double seconds, int cap, List<string> requires)")
w("        {")
w("            s.tasks.Add(new FocusTask")
w("            {")
w("                section = T(\"focus.section.\" + section), id = id, g = g,")
w("                n = T(\"focus.\" + id + \".name\"), b = T(\"focus.\" + id + \".blurb\"),")
w("                cost = cost, gain = gain, baseSeconds = seconds, w = 0, cap = cap, p = 0, seen = true,")
w("                requires = requires")
w("            });")
w("        }")
w("")
w("        static void Focus(Dream s)")
w("        {")
for fid, sec, g, cost, gain, secs, capn, req, n, b in D.FOCUS:
    w(f'            Task(s, "{fid}", "{sec}", "{g}", {amounts(cost)}, {amounts(gain)}, {secs}, {capn}, {strs(req)});')
    if n: S[f"focus.{fid}.name"] = n
    if b: S[f"focus.{fid}.blurb"] = b
for k, v in D.FOCUS_SECTIONS.items(): S[f"focus.section.{k}"] = v
w("        }")
w("")

# constructs
w("        static ConstructDef Build(Dream s, string g, ConstructKind kind, List<Amount> cost, int housing,")
w("                                  List<Effect> effects, List<string> requires, float x, float y)")
w("        {")
w("            var c = new ConstructDef")
w("            {")
w("                g = g, n = T(\"construct.\" + g + \".name\"), kind = kind, b = T(\"construct.\" + g + \".blurb\"),")
w("                fx = Lines(\"construct.\" + g + \".fx\"), baseCost = cost, cost = cost, growth = 1.15,")
w("                housing = housing, effects = effects, owned = 0, built = false, seen = true,")
w("                palacePos = new Vector2(x, y), requires = requires")
w("            };")
w("            s.constructs.Add(c);")
w("            return c;")
w("        }")
w("")
w("        static void Constructs(Dream s)")
w("        {")
w("            ConstructDef c;")
for c in D.CONSTRUCTS:
    g = c["g"]
    w(f'            c = Build(s, "{g}", ConstructKind.{c["kind"]}, {amounts(c["cost"])}, {c.get("housing", 0)}, '
      f'{effs(c.get("fx"))}, {strs(c.get("requires"))}, {c["pos"][0]}, {c["pos"][1]});')
    if c.get("once"): w("            c.once = true;")
    if c.get("growth"): w(f"            c.growth = {c['growth']};")
    if c.get("landmark"):
        w('            c.landmark = true; c.kindLabel = T("construct.altar.kind"); c.line = T("construct.altar.line");')
    if c.get("grants"): w(f'            c.grants = {strs(c["grants"])};')
    if c.get("name"): S[f"construct.{g}.name"] = c["name"]
    if c.get("blurb"): S[f"construct.{g}.blurb"] = c["blurb"]
    lines = []
    if c.get("housing"): lines.append(f"Houses <b>{c['housing']}</b> Oneiri")
    lines += [fx_line(e) for e in c.get("fx", [])]
    S[f"construct.{g}.fx"] = lines   # the Altar has none: an empty list, not a missing key
w("        }")
w("")

# upgrades
w("        static void Upgrades(Dream s)")
w("        {")
for u in D.UPGRADES:
    uid = u["id"]
    parts = [f'id = "{uid}"', f'target = UpgradeTarget.{u["target"]}', f'of = "{u["of"]}"', f'when = {strs(u["when"])}']
    if u.get("n"): parts.append(f'n = T("upgrade.{uid}.name")'); S[f"upgrade.{uid}.name"] = u["n"]
    if u.get("b"): parts.append(f'b = T("upgrade.{uid}.blurb")'); S[f"upgrade.{uid}.blurb"] = u["b"]
    if u.get("art"): parts.append(f'art = "{u["art"]}"')
    if u.get("fx_lines"): parts.append(f'fx = Lines("upgrade.{uid}.fx")'); S[f"upgrade.{uid}.fx"] = u["fx_lines"]
    if "housing" in u: parts.append(f'housing = {u["housing"]}')
    if u.get("cost"): parts.append(f'cost = {amounts(u["cost"])}')
    if "powerScale" in u: parts.append(f'powerScale = {fmtn(u["powerScale"])}')
    if "ceiling" in u: parts.append(f'ceiling = {u["ceiling"]}')
    w("            s.upgrades.Add(new UpgradeDef { " + ", ".join(parts) + " });")
w("        }")
w("")

# revelations
w("        static RevelationDef Rev(Dream s, string k, string g, List<Amount> cost, bool great, float ring, float deg,")
w("                                 List<Effect> effects, List<string> requires)")
w("        {")
w("            var r = new RevelationDef")
w("            {")
w("                k = k, g = g, n = T(\"rev.\" + k + \".name\"), kind = T(\"rev.\" + k + \".kind\"),")
w("                bl = T(\"rev.\" + k + \".text\"), fx = Lines(\"rev.\" + k + \".fx\"), cost = cost, great = great,")
w("                ring = ring, angle = deg * Mathf.Deg2Rad, effects = effects, requires = requires")
w("            };")
w("            s.revelations.Add(r);")
w("            return r;")
w("        }")
w("")
w("        static void Revelations(Dream s)")
w("        {")
w("            RevelationDef r;")
for i, r in enumerate(D.REVELATIONS):
    k = r["k"]
    deg = round((i * 137.5) % 360, 1)
    ring = 306 if i % 2 == 0 else 388
    w(f'            r = Rev(s, "{k}", "{r["g"]}", {amounts(r["cost"])}, {"true" if r.get("great") else "false"}, '
      f'{ring}f, {deg}f, {effs(r.get("fx"))}, {strs(r["requires"])});')
    if r.get("withdraws"): w(f'            r.withdraws = {strs(r["withdraws"])};')
    if r.get("grants"): w(f'            r.grants = {strs(r["grants"])};')
    if r.get("n"):
        S[f"rev.{k}.name"] = r["n"]; S[f"rev.{k}.kind"] = r["kind"]; S[f"rev.{k}.text"] = r["text"]
    lines = [fx_line(e) for e in r.get("fx", [])] + unlock_lines(f"rev:{k}")
    if r.get("withdraws"): lines.append("the other choice is gone for good")
    S[f"rev.{k}.fx"] = lines
w("        }")
w("")

# visions
w("        static VisionDef Vision(Dream s, string k, bool great, bool rep, float deg, List<Offer> of,")
w("                                List<Effect> effects, List<string> requires)")
w("        {")
w("            var v = new VisionDef")
w("            {")
w("                k = k, n = T(\"vision.\" + k + \".name\"), bl = T(\"vision.\" + k + \".text\"),")
w("                fx = Lines(\"vision.\" + k + \".fx\"), of = of, sel = 0, p = 0f, rep = rep, done = 0,")
w("                great = great, a = false, angle = deg * Mathf.Deg2Rad, effects = effects, requires = requires")
w("            };")
w("            s.visions.Add(v);")
w("            return v;")
w("        }")
w("")
w("        static Offer O(string r, double n, double g) => new Offer { r = r, n = n, g = g };")
w("")
w("        static void Visions(Dream s)")
w("        {")
for i, v in enumerate(D.VISIONS):
    k = v["k"]
    deg = round((i * 97.0) % 360, 1)
    offers = "new List<Offer> { " + ", ".join(f'O("{r}", {fmtn(n)}, {fmtn(g)})' for r, n, g in v["of"]) + " }"
    w(f'            Vision(s, "{k}", {"true" if v.get("great") else "false"}, {"true" if v.get("rep") else "false"}, '
      f'{deg}f, {offers}, {effs(v.get("fx"))}, {strs(v["requires"])});')
    S[f"vision.{k}.name"] = v["n"]; S[f"vision.{k}.text"] = v["text"]
    lines = [fx_line(e) + (" each time" if v.get("rep") else "") for e in v.get("fx", [])] + unlock_lines(f"vision:{k}")
    if k == "tide": lines.append("the first six each write a Journal entry")
    S[f"vision.{k}.fx"] = lines
w("        }")
w("")

# veils
w("        static void Veil(Dream s, int ord, string name, double need, List<Amount> spend, List<Amount> bring,")
w("                         int cap, double seconds, string entry)")
w("        {")
w("            var lines = Lines(entry);")
w("            var reveal = new List<double>();")
w("            for (int i = 0; i < lines.Count; i++) reveal.Add(System.Math.Floor(need * i / System.Math.Max(1, lines.Count)));")
w("            s.veils.Add(new VeilDef")
w("            {")
w("                n = T(name), ord = ord, sunk = 0, need = need, spend = spend, bring = bring, w = 0, cap = cap,")
w("                baseSeconds = seconds, entry = lines, reveal = reveal")
w("            });")
w("        }")
w("")
w("        static void Veils(Dream s)")
w("        {")
for row in veil_rows():
    lv = row["ord"]
    if row.get("bottom"):
        name_key, entry_key = "veil.bottom.name", "veil.bottom.entry"
        S[name_key] = D.BOTTOM[0]; S[entry_key] = list(VM.BOTTOM)
    else:
        name_key = f"veil.{slug(row['reach'])}.name"
        S.setdefault(name_key, row["reach"])
        if lv == 1: entry_key = "veil.shallows.entry"; S[entry_key] = list(VM.SHALLOWS)
        elif lv == 2: entry_key = "veil.drift.entry"
        else:
            line = D.VEIL_LINES[lv]
            paras = list(line) if isinstance(line, list) else [line] + list(VM.MORE.get(lv, ()))
            entry_key = f"veil.{lv}.entry"; S[entry_key] = paras
    if lv == 1: name_key = "veil.shallows.name"
    w(f'            Veil(s, {lv}, "{name_key}", {row["need"]}, {amounts(row["spend"]) if row["spend"] else "C()"}, '
      f'{amounts(row["bring"]) if row["bring"] else "C()"}, {row["cap"]}, {row["secs"]}, "{entry_key}");')
w("        }")
w("")

# road + units
w("        static RoadLocation Place(Dream s, string k, float t, double en, List<Effect> effects, List<string> grants,")
w("                                  List<string> requires)")
w("        {")
w("            var l = new RoadLocation")
w("            {")
w("                k = k, n = T(\"place.\" + k + \".name\"), t = t, won = false, en = en,")
w("                bl = T(\"place.\" + k + \".blurb\"), ben = T(\"place.\" + k + \".ben\"),")
w("                effects = effects, grants = grants, requires = requires")
w("            };")
w("            s.road.Add(l);")
w("            return l;")
w("        }")
w("")
w("        static void Unit(Dream s, string k, string sec, double p, double growth, List<Amount> cost, List<string> requires)")
w("        {")
w("            s.units.Add(new UnitDef")
w("            {")
w("                k = k, n = T(\"unit.\" + k + \".name\"), bl = T(\"unit.\" + k + \".blurb\"), p = p, c = 0,")
w("                sec = sec, growth = growth, cost = cost, requires = requires")
w("            });")
w("        }")
w("")
w("        static void Road(Dream s)")
w("        {")
n = len(D.PLACES)
for i, (k, name, en, fx, grants, req, blurb) in enumerate(D.PLACES):
    t = round(0.03 + i * (0.94 / (n - 1)), 3)
    w(f'            Place(s, "{k}", {t}f, {en}, {effs(fx)}, {strs(grants)}, {strs(req)});')
    S[f"place.{k}.name"] = name; S[f"place.{k}.blurb"] = blurb
    lines = [fx_line(e) for e in fx]
    lines += [x.replace("a realization: ", "the realization ") for x in unlock_lines(f"won:{k}")]
    if any(g.startswith("journal:") for g in grants) and "the Journal gains an entry" not in lines:
        lines.append("the Journal gains an entry")
    if "gameover" in grants: lines.append("the end of it")
    S[f"place.{k}.ben"] = "; ".join(lines) if lines else "the road goes on"
for k, sec, p, cost, req, name, blurb in D.UNITS:
    w(f'            Unit(s, "{k}", "{sec}", {p}, {1.12 if sec == "Levied" else 1.06}, {amounts(cost)}, {strs(req)});')
    S[f"unit.{k}.name"] = name; S[f"unit.{k}.blurb"] = blurb
S["unit.section.levied"] = "Levied"; S["unit.section.sworn"] = "Sworn"
w("        }")
w("")

# journal
w("        static void J(Dream s, string id, int d, int ch, string src, bool great, List<string> requires)")
w("        {")
w("            s.journalDefs.Add(new JournalEntry")
w("            {")
w("                id = id, d = d, ch = ch, src = src, great = great, n = T(\"journal.\" + id + \".name\"), seen = false,")
w("                requires = requires, p = Lines(\"journal.\" + id + \".p\")")
w("            });")
w("        }")
w("")
w("        static void Journal(Dream s)")
w("        {")
for jid, d, ch, src, great, req, name, paras in D.JOURNAL:
    w(f'            J(s, "{jid}", {d}, {ch}, "{src}", {"true" if great else "false"}, {strs(req)});')
    if name: S[f"journal.{jid}.name"] = name
    if paras: S[f"journal.{jid}.p"] = paras
w("        }")
w("")

# achievements: keep the one that exists, plus one mark per chapter and one per ending
w("        static void Ach(Dream s, string k, string group, string glyph, List<string> when)")
w("        {")
w("            s.achievements.Add(new AchievementDef")
w("            {")
w("                k = k, group = T(\"ach.group.\" + group), glyph = glyph,")
w("                n = T(\"ach.\" + k + \".name\"), hint = T(\"ach.\" + k + \".hint\"), req = T(\"ach.\" + k + \".req\"),")
w("                when = when")
w("            });")
w("        }")
w("")
w("        static void Achievements(Dream s)")
w("        {")
ACH = [("drift", ["parted:1"], None, None, None),
       ("arrival", ["parted>=20"], "Arrival", "Go down twenty veils.", "Parted the twentieth veil."),
       ("lamps", ["parted>=40"], "The Lamps", "See what came through the door.", "Parted the fortieth veil."),
       ("tide", ["parted>=60"], "The Tide", "Promise to end it.", "Parted the sixtieth veil."),
       ("road", ["parted>=80"], "The Road", "Make something that can fight.", "Parted the eightieth veil."),
       ("bottom", ["parted>=100"], "The Far End", "Reach the bottom.", "Reached the bottom of the dream."),
       ("good", ["won:nobody", "ending:good"], "Some Doors Stay Shut", "End it without waking anything.", "Ended it with the nightmares asleep."),
       ("bad", ["won:nobody", "ending:bad"], "Fear Is Also Strength", "End it any way you can.", "Ended it with the nightmares awake.")]
for k, when, n, hint, req in ACH:
    w(f'            Ach(s, "{k}", "story", "drift", {strs(when)});')
    if n: S[f"ach.{k}.name"] = n; S[f"ach.{k}.hint"] = hint; S[f"ach.{k}.req"] = req
w("        }")
w("    }")
w("}")

for key, (title, paras) in D.ENDINGS.items():
    S[f"ending.{key}.title"] = title; S[f"ending.{key}.p"] = paras

# Every veil's story must be three paragraphs and short enough to sit above the gauge's
# "Part the veil" button (VEIL_MAX characters, measured against the Depth Gauge).
_existing = json.load(open(STRINGS, encoding="utf-8"))["en"]
for key in [k for k in list(S) + ["veil.drift.entry"] if k.startswith("veil.") and k.endswith(".entry")]:
    paras = S.get(key, _existing.get(key))
    total = sum(len(p) for p in paras)
    if len(paras) != 3 or total > VM.VEIL_MAX:
        raise SystemExit(f"{key}: {len(paras)} paragraphs, {total} characters (need 3, at most {VM.VEIL_MAX})")

open(CS, "w", encoding="utf-8", newline="\n").write("\n".join(cs) + "\n")

# merge strings: existing keys kept in place and overwritten, new keys appended
data = json.load(open(STRINGS, encoding="utf-8"), object_pairs_hook=OrderedDict)
en = data["en"]
for k, v in S.items(): en[k] = v
json.dump(data, open(STRINGS, "w", encoding="utf-8", newline="\n"), ensure_ascii=False, indent=2)
open(STRINGS, "a", encoding="utf-8").write("\n")
print(f"wrote {CS} ({len(cs)} lines); {len(S)} strings")
