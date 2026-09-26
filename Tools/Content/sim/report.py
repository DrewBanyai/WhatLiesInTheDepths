#!/usr/bin/env python3
"""Plays the dream with the simulated player on several seeds and prints the timing report
as Markdown: chapters, every five veils, when each menu opens, battles lost and the Silt
economy. This is what the "Simulated Playthrough" doc is built from; rerun it after any change
to design.py and paste the tables in.

    python3 Tools/Content/sim/report.py                # good ending, seeds 7 11 23 3 42
    python3 Tools/Content/sim/report.py --seeds 7 11   # fewer seeds, faster
    RUN=path/to/runner.sh python3 ...                  # a different runner than run.sh

Each seed takes well under a minute.
"""
import os, re, subprocess, statistics, sys

HERE = os.path.dirname(os.path.abspath(__file__))
RUN = os.environ.get("RUN", os.path.join(HERE, "run.sh"))

args = sys.argv[1:]
seeds = [7, 11, 23, 3, 42]
if "--seeds" in args:
    i = args.index("--seeds")
    seeds = [int(x) for x in args[i + 1:] if x.isdigit()]


def play(path, seed):
    out = subprocess.run([RUN, path, f"--seed={seed}"], capture_output=True, text=True).stdout
    events, lost, silt = {}, 0, {}
    for line in out.splitlines():
        m = re.match(r"\[\s*(\d+)h(\d+)m\] L(\d+)\s+(\S+)", line)
        if m:
            t = int(m.group(1)) * 60 + int(m.group(2))
            k = m.group(4)
            if k == "lost": lost += 1
            if k in ("STALLED", "TIME"): events["_stalled"] = t
            events.setdefault(k, t)
        m = re.match(r"silt: earned to the bottom (\d+).*cisterns (\d+)", line)
        if m: silt = {"earned": int(m.group(1)), "cisterns": int(m.group(2))}
    events["_lost"] = lost
    events["_silt"] = silt
    return events


def hm(m): return f"{m // 60}h{m % 60:02d}"


def median(runs, k):
    v = [r[k] for r in runs if k in r]
    return int(statistics.median(v)) if v else None


def span(runs, k):
    v = [r[k] for r in runs if k in r]
    if not v: return "—"
    lo, hi = min(v), max(v)
    return hm(lo) if lo == hi else f"{hm(lo)}–{hm(hi)}"


good = [play("good", s) for s in seeds]
bad = [play("bad", s) for s in seeds[:3]]

print(f"Seeds: {', '.join(map(str, seeds))} (good ending); {', '.join(map(str, seeds[:3]))} (nightmare ending)\n")
stalled = [s for s, r in zip(seeds, good + bad) if "_stalled" in r]
print("| Ending | Finished | Range |\n| --- | --- | --- |")
print(f"| Nightmares asleep | {len([r for r in good if 'THE' in r])} of {len(good)} | {span(good, 'THE')} |")
print(f"| Nightmares woken | {len([r for r in bad if 'THE' in r])} of {len(bad)} | {span(bad, 'THE')} |")
if stalled: print(f"\nStalled: seeds {stalled}")

print("\n| Chapter | Veils | Reached at | Took |\n| --- | --- | --- | --- |")
prev = 0
for n, (a, b) in enumerate([(1, 20), (21, 40), (41, 60), (61, 80), (81, 100)], 1):
    t = median(good, f"parted:{b}")
    print(f"| {n} | {a}–{b} | {span(good, f'parted:{b}')} | {hm(t - prev)} |")
    prev = t

print("\n| Veil | " + " | ".join(str(v) for v in range(5, 101, 5)) + " |")
print("| --- |" + " --- |" * 20)
print("| Time | " + " | ".join(hm(median(good, f"parted:{v}")) for v in range(5, 101, 5)) + " |")

print("\n| Opens | Median |\n| --- | --- |")
for k, name in [("menu:revelations", "Revelations"), ("menu:visions", "Visions"), ("menu:assault", "Assault"),
                ("won:gate", "First battle won"), ("built:forge", "The Waking Forge"), ("rev:fear", "The choice (nightmare path)")]:
    runs = bad if k == "rev:fear" else good
    t = median(runs, k)
    if t is not None: print(f"| {name} | {hm(t)} |")

lost = [r["_lost"] for r in good]
silt = good[0]["_silt"]
print(f"\nBattles lost per run: {', '.join(map(str, lost))}.")
if silt: print(f"Silt earned before the bottom: {silt['earned']:,}; Cisterns built: {silt['cisterns']}.")
