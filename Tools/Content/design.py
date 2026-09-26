import math
# What Lies In The Depths — the whole path, as data.
#
# This is the design source for chapters 1-5. gen.py turns it into
# Assets/Scripts/WhatLiesInTheDepths/Data/OpeningContent.cs and the matching keys in
# Strings.json, and writes each effect's line from the effect itself, so a card never says
# something the numbers do not do.
#
# Every number is a first-pass placeholder, tuned only far enough that a simulated player
# can walk the whole path (see sim/). Conditions use Dream.Holds's grammar:
#   rev:<k>  vision:<k>  built:<g>  won:<place>  parted>=n  max:<res>  got:<res>
#   held:<res>>=n  owned:<g>>=n  done:<focus>>=n  vdone:<vision>>=n  won>=n  lost:any
# "Level L" in the design doc is parted>=L-1.

def L(level):
    """Reaching level L: L-1 veils parted."""
    return f"parted>={level - 1}"

# Silt a dive brings up at each level: 1 through level 30, 2 through 60, 3 through 90, and 4
# from there to the bottom. It is the primary yield of most reaches, so it is also what stops
# the dive when it is full, and every point of it has to be spent or stored. (It was
# ceil(1 + level/15), 2 rising to 8; the simulated player then earned ~223,000 Silt before the
# bottom, used ~90,000 and bought ~220 Cisterns to store the rest. At this rate it earns
# ~96,000, and ~65 Cisterns see it to the end in the same time.)
def silt_per_dive(level):
    return math.ceil(level / 30)

# How fast the fathoms a veil needs grow with depth: 20 + 4·level + NEED_CURVE·level². It was
# 0.06; at 0.045 the deepest veils need about a fifth fewer fathoms (level 90: 750, was 866),
# which is what brings the simulated run from ~14h50 to ~11h30.
NEED_CURVE = 0.045

# Room each Cistern adds for Silt. Twice what it was, so half as many keep the dive moving.
CISTERN_SILT = 20

# ---- resources: key, group (0 gathered, 1 yours, 2 against you), ceiling, shown when, glyph
RESOURCES = [
    ("reverie",    0, 10,  ["done:absorb>=1"], "reverie"),
    ("silt",       0, 15,  ["fathoms>=1"],     "silt"),
    ("moonsilver", 0, 40,  ["rev:cold"],       "moonsilver"),
    ("ember",      0, 20,  ["rev:warmth"],     "ember"),
    ("tallow",     0, 30,  ["rev:fat"],        "tallow"),
    ("salt",       0, 60,  ["rev:salt"],       "salt"),
    ("nacre",      0, 40,  ["vision:garden"],  "nacre"),
    ("echo",       1, 10,  ["done:conjure>=1"], "echo"),
    ("oneiri",     1, 0,   ["owned:hut>=1"],   "oneiri"),
    ("lucidity",   1, 20,  ["rev:quiet"],      "lucidity"),
    ("hush",       1, 30,  ["rev:darkquiet"],  "hush"),
    ("vellum",     1, 40,  ["rev:written"],    "vellum"),
    ("ward",       1, 50,  ["rev:holds"],      "ward"),
    ("chorus",     1, 60,  ["rev:mine"],       "chorus"),
    ("mettle",     1, 60,  ["built:forge"],    "mettle"),
    ("dread",      2, 100, ["vision:door"],    "dread"),
    ("umbra",      2, 100, ["rev:fear"],       "umbra"),
]
# What a resource holds the moment it first appears. Dread arrives with some already in it, so
# the 40th veil (1 Dread a fathom) never waits on the Door's 0.2/s alone.
RES_START = {"dread": 20}

RES_NAMES = {
    "reverie": "Reverie", "silt": "Silt", "moonsilver": "Moonsilver", "ember": "Ember",
    "tallow": "Tallow", "salt": "Salt", "nacre": "Nacre", "echo": "Echo", "oneiri": "Oneiri",
    "lucidity": "Lucidity", "hush": "Hush", "vellum": "Vellum", "ward": "Ward",
    "chorus": "Chorus", "mettle": "Mettle", "dread": "Dread", "umbra": "Umbra",
}
# once Echo becomes Whispers, cards should call it that
LATE_NAMES = {"echo": "Whispers"}

# ---- rules: steps that are not a thing appearing
RULES = [
    ("focus-menu", ["start"], ["menu:focus"]),
    ("ledger", ["done:absorb>=1"], ["col:ledger"]),
    ("constructs-menu", ["max:reverie"], ["menu:constructs"]),
    ("revelations-menu", ["fathoms>=15"], ["menu:revelations"]),
    # The Shallows' dives hold once Silt is full (it is their primary yield), which is 8 dives
    # in: the Revelations open then too, so Room for It is there to be realized. Without this
    # a new dream stops at 8 of 20 fathoms with nothing it can do.
    ("revelations-on-full-silt", ["max:silt"], ["menu:revelations"]),
    ("binding", ["rev:shown"], ["bind:oneiri"]),
    ("visions-menu", ["rev:camefrom"], ["menu:visions"]),
    ("assault-menu", ["rev:mine"], ["menu:assault"]),
    ("chose-dark", ["rev:fear"], ["chose"]),
    ("chose-light", ["rev:shut"], ["chose"]),
]

# ---- Focus: id, section, glyph, cost, gain, seconds, cap, shown when, name, blurb
FOCUS = [
    ("absorb", "stillness", "sit", [], [("reverie", 1)], 6, 12, ["start"], None, None),
    ("conjure", "stillness", "card", [("reverie", 1)], [("echo", 1)], 6, 10, ["rev:sifting"], None, None),
    ("still", "stillness", "still", [("echo", 2), ("reverie", 2)], [("lucidity", 1)], 10, 8, ["rev:quiet"],
     "Still the Mind", "Go back to the quiet you made at the Altar, and stay there a while."),
    ("draw", "labor", "draw", [("silt", 2)], [("moonsilver", 1)], 8, 10, ["rev:cold"],
     "Draw Moonsilver", "Lowered into the cold seam until it fills."),
    ("kindle", "labor", "kindle", [("reverie", 2)], [("ember", 1)], 8, 8, ["rev:warmth"],
     "Kindle", "Something small to warm your hands at. It is allowed."),
    ("render", "labor", "render", [("silt", 1), ("ember", 1)], [("tallow", 2)], 10, 8, ["rev:fat"],
     "Render Tallow", "Slow heat, and a smell that follows you up."),
    ("tend", "labor", "tend", [("tallow", 2)], [("lucidity", 2)], 12, 8, ["rev:lamps"],
     "Keep the Lamps", "None of them stay lit. You light them anyway."),
    ("sit", "labor", "sit", [], [("hush", 2), ("reverie", 1)], 10, 8, ["rev:darkquiet"],
     "Sit with the Hush", "Nothing is asked of you. That is the difficulty."),
    ("weep", "listening", "weep", [("hush", 2)], [("salt", 2)], 10, 10, ["rev:salt"],
     "Let It Out", "It dries faster if you stop trying to keep it."),
    ("sift", "listening", "sift", [("silt", 2), ("salt", 2)], [("nacre", 1)], 12, 10, ["vision:garden"],
     "Sift the Beds", "Something grows around every grain that would not go away."),
    ("page", "listening", "page", [("hush", 2), ("echo", 3)], [("vellum", 1)], 14, 10, ["rev:written"],
     "Copy the Vellum", "The hand remembers what the reading does not."),
    ("watch", "muster", "watch", [("hush", 2), ("salt", 2)], [("ward", 1)], 12, 12, ["rev:stand"],
     "Stand the Watch", "A wall no one watches is only a suggestion."),
    ("rally", "muster", "rally", [("echo", 3)], [("chorus", 1)], 10, 14, ["rev:mine"],
     "Rally", "Say it out loud, and let them answer together."),
    ("temper", "muster", "temper", [("ember", 3), ("moonsilver", 4), ("chorus", 2)], [("mettle", 2)], 16, 16, ["built:forge"],
     "Temper", "Hold what you are good at in the fire until it holds its shape."),
    ("sing", "many", "sing", [("chorus", 3)], [("lucidity", 3), ("ward", 1)], 12, 16, ["rev:everypeople"],
     "Sing Together", "Every people of the dream, one note."),
    ("dredge", "many", "dredge", [("dread", 5)], [("umbra", 2)], 10, 16, ["rev:fear"],
     "Dredge the Nightmares", "Down into your own dark with a hook, for what you can use."),
    ("release", "many", "release", [("dread", 5)], [("lucidity", 3)], 10, 16, ["rev:shut"],
     "Let It Go", "Open your hands. Let what you were holding leave on its own."),
]
FOCUS_SECTIONS = {"stillness": "Stillness", "labor": "Labor", "listening": "Listening",
                  "muster": "Muster", "many": "The Many"}

# ---- effects: (kind, of, n). Kinds match Data/Model.cs Fx.
def rate(r, n): return ("Rate", r, n)
def cap(r, n): return ("Cap", r, n)
def cappct(r, n): return ("CapPct", r, n)
def gain(t, n): return ("Gain", t, n)
def speed(t, n): return ("Speed", t, n)
def divespeed(n): return ("DiveSpeed", None, n)
def divecost(n): return ("DiveCost", None, n)
def power(sec, n): return ("Power", sec, n)
def enemy(n): return ("Enemy", None, n)
def muster(n): return ("Muster", None, n)
def housing(g, n): return ("Housing", g, n)
def exodus(): return ("Exodus", None, 1)
def visioncost(n): return ("VisionCost", None, n)
def revcost(n): return ("RevCost", None, n)

# ---- constructs: g, kind, name, blurb, cost, growth, housing, effects, shown when, pos, once, extra
CONSTRUCTS = [
    dict(g="altar", kind="Works", cost=[("reverie", 10)], once=True, landmark=True, pos=(440, 430),
         requires=["max:reverie"], grants=["col:gauge"]),
    dict(g="hut", kind="Dwellings", cost=[("reverie", 10), ("echo", 5)], housing=1, pos=(736, 206),
         requires=["rev:listening"]),
    dict(g="cistern", kind="Reservoirs", name="Cistern", blurb="It fills whether or not you are watching it.",
         cost=[("silt", 12)], fx=[cap("silt", CISTERN_SILT), cap("reverie", 5), cap("echo", 3)], pos=(212, 716),
         # The one construct that never outgrows the shore: its price climbs only 2% a build and
         # each adds a flat CISTERN_SILT room, so wherever Silt is a reach's primary yield there is a
         # Cistern to spend a full load on. (Price overtakes the room Cisterns alone give at
         # about 315 built; the simulated player finishes with about 65.)
         growth=1.02, requires=["rev:room"]),
    dict(g="spindle", kind="Works", name="Dreamspindle", blurb="Something turns, and turns, and does not stop turning.",
         cost=[("reverie", 12), ("silt", 10)], fx=[rate("reverie", 0.2)], pos=(132, 148), requires=["rev:hums"]),
    dict(g="moonwell", kind="Reservoirs", name="Moonwell", blurb="Still water that keeps every face it has held.",
         cost=[("reverie", 20), ("silt", 10)], fx=[cap("reverie", 25), rate("reverie", 0.1), cap("lucidity", 3)], pos=(138, 566),
         requires=["rev:stillkeeps"]),
    dict(g="winch", kind="Works", name="Seam Winch", blurb="The cold runs in a line. A line can be hauled on.",
         cost=[("moonsilver", 15), ("silt", 12)], fx=[rate("moonsilver", 0.15), cap("moonsilver", 20)],
         pos=(296, 236), requires=["rev:seam"]),
    dict(g="press", kind="Works", name="Tallow Press", blurb="What the shore gives up when it is squeezed.",
         cost=[("silt", 12), ("ember", 8)], fx=[rate("tallow", 0.15), cap("tallow", 15), cap("ember", 5)], pos=(286, 96),
         requires=["rev:fat"]),
    dict(g="lamprow", kind="Works", name="Lamplight Row", blurb="Someone lit these once. They can be lit again.",
         cost=[("tallow", 15), ("moonsilver", 10)], fx=[divecost(0.04), rate("tallow", 0.05)], pos=(118, 330),
         requires=["rev:lamps"]),
    dict(g="longhouse", kind="Dwellings", name="Longhouse", blurb="Room enough that nobody has to be counted.",
         cost=[("moonsilver", 40), ("salt", 20), ("echo", 30)], housing=6, pos=(700, 82),
         requires=["rev:tidefolk"]),
    dict(g="reliquary", kind="Reservoirs", name="Reliquary", blurb="Somewhere to put the things you were told.",
         cost=[("echo", 30), ("moonsilver", 20)], fx=[cap("echo", 40), cap("nacre", 20)], pos=(376, 766),
         requires=["rev:somewhere"]),
    dict(g="scriptorium", kind="Works", name="Scriptorium", blurb="Whatever is written down is harder to swallow.",
         cost=[("hush", 20), ("moonsilver", 25)], fx=[rate("vellum", 0.05), cap("vellum", 20)], pos=(236, 396),
         requires=["rev:written"]),
    dict(g="saltpans", kind="Works", name="Salt Pans", blurb="Shallow water, left alone, gives up what it was carrying.",
         cost=[("silt", 25), ("hush", 15)], fx=[rate("salt", 0.15), cap("salt", 30)], pos=(56, 244),
         requires=["rev:letsea"]),
    dict(g="nightlight", kind="Wards", name="The Nightlight", blurb="The light he asked for, and was told he was too big for.",
         cost=[("tallow", 60), ("ember", 30), ("moonsilver", 40)], fx=[rate("dread", -0.5), cap("ward", 10)],
         once=True, pos=(560, 600), requires=["vision:vigil"]),
    dict(g="rampart", kind="Wards", name="Somnal Rampart", blurb="It holds because you have not stopped believing it holds.",
         cost=[("moonsilver", 40), ("salt", 25)], fx=[rate("dread", -0.1), cap("ward", 8)], pos=(750, 576),
         requires=["rev:holds"]),
    dict(g="chorusstone", kind="Wards", name="Chorus Stone", blurb="A note held by something that does not breathe.",
         cost=[("echo", 50), ("salt", 40)], fx=[cap("ward", 12), cap("chorus", 20)], pos=(636, 734),
         requires=["rev:manyvoices"]),
    dict(g="belltower", kind="Works", name="Bell Tower", blurb="Bellcast's bell, carried home and hung where anyone may ring it.",
         cost=[("moonsilver", 60), ("chorus", 20)], fx=[rate("chorus", 0.1)], pos=(404, 76),
         requires=["won:bell"]),
    dict(g="forge", kind="Works", name="The Waking Forge", blurb="Where what he is good at is made into something that can stand in a line.",
         cost=[("moonsilver", 150), ("ember", 60), ("chorus", 40), ("vellum", 30)], once=True, pos=(340, 300), fx=[cap("mettle", 100)],
         requires=["rev:made"]),
    dict(g="hallmany", kind="Dwellings", name="Hall of the Many", blurb="Every people of the dream under one roof, and room for more.",
         cost=[("moonsilver", 120), ("chorus", 60), ("salt", 60)], housing=12, fx=[speed("*", 0.03)], pos=(560, 250),
         requires=["rev:everypeople"]),
    dict(g="hallstrengths", kind="Works", name="Hall of Strengths", blurb="It was never a short list.",
         cost=[("mettle", 20), ("vellum", 20)], fx=[power("Sworn", 0.05), cap("mettle", 15)], pos=(60, 420),
         requires=["rev:goodat"]),
    dict(g="beacon", kind="Wards", name="The Beacon", blurb="A light left on at the end of the world.",
         cost=[("tallow", 200), ("ember", 100), ("mettle", 40)], fx=[enemy(0.3)], once=True, pos=(800, 700),
         requires=[L(91)]),
    dict(g="kiln", kind="Works", name="The Night Kiln", blurb="Where his nightmares are fired hard enough to fight with.",
         cost=[("mettle", 60), ("ember", 60)], fx=[rate("umbra", 1.0), rate("dread", 0.3), exodus()], once=True,
         pos=(170, 250), requires=["rev:fear"]),
]

# ---- upgrades: id, target, of, when, fields
UPGRADES = [
    dict(id="abode", target="Construct", of="hut", when=["rev:walls"], n="Abode", art="abode",
         b="The walls agree on where they are, and stay agreed.", housing=3,
         cost=[("moonsilver", 12), ("echo", 10)], fx_lines=["Houses <b>3</b> Oneiri"]),
    dict(id="whispers", target="Resource", of="echo", when=["rev:answers"], n="Whispers", art="whispers", ceiling=60),
    dict(id="converse", target="Focus", of="conjure", when=["rev:answers"], n="Converse",
         b="Ask, and wait, and write down what comes back."),
    # the nightmare path: every Sworn unit becomes its nightmare, keeping its count
    dict(id="nm-unflinching", target="Unit", of="unflinching", when=["rev:fear"], n="Hollow Riders", art="hollowriders",
         b="Nobody is in the saddle. Something is, and it rides beautifully.", powerScale=2.5,
         cost=[("mettle", 4), ("umbra", 4)]),
    dict(id="nm-laughing", target="Unit", of="laughing", when=["rev:fear"], n="The Grinning", art="grinning",
         b="The laugh from the other room, when nobody else was home.", powerScale=2.5,
         cost=[("mettle", 8), ("umbra", 6)]),
    dict(id="nm-plainspoken", target="Unit", of="plainspoken", when=["rev:fear"], n="The Voices in the Hall", art="voices",
         b="They only ever say the worst true thing.", powerScale=2.5,
         cost=[("mettle", 15), ("umbra", 8)]),
    dict(id="nm-sentinel", target="Unit", of="sentinel", when=["rev:fear"], n="The Drowned Watch", art="drowned",
         b="They wait at the bottom of the bath, patient as ever.", powerScale=2.5,
         cost=[("mettle", 28), ("umbra", 12)]),
    dict(id="nm-chorus", target="Unit", of="chorus", when=["rev:fear"], n="The Wailing", art="wailing",
         b="Everyone who left, singing.", powerScale=2.5,
         cost=[("mettle", 50), ("umbra", 20)]),
    dict(id="nm-dreamwright", target="Unit", of="dreamwright", when=["rev:fear"], n="The Thing on the Stairs", art="stairs",
         b="Imagination, with the lights off.", powerScale=2.5,
         cost=[("mettle", 90), ("umbra", 30)]),
]

# ---- Realizations: k, sigil, ring slot, cost, shown when, effects, grants, great, withdraws, name, kind, text
REVELATIONS = [
    # Chapter 1 — Arrival
    dict(k="sifting", g="key", cost=[("silt", 15)], requires=["fathoms>=15"]),
    dict(k="listening", g="branch", cost=[("echo", 10)], requires=["max:echo"]),
    dict(k="shown", g="knot", cost=[("echo", 10), ("silt", 10)], requires=["owned:hut>=1"]),
    dict(k="room", g="glass", cost=[("silt", 15)], requires=["max:silt"],
         n="Room for It", kind="unlocks a construct",
         text="Your hands keep filling and the shore keeps giving. Somewhere to set it down would help."),
    dict(k="hums", g="spiral", cost=[("reverie", 10), ("silt", 15)], requires=[L(7)],
         n="The Stone Hums Back", kind="unlocks a construct",
         text="Sit long enough and the Altar hums along with you. Something here wants to turn."),
    dict(k="down", g="stair", cost=[("silt", 25), ("echo", 10)], requires=[L(10), "rev:room"], fx=[divecost(0.08)],
         n="Down Is a Direction", kind="changes the descent",
         text="You stop thinking of the floor as a floor. It was always the next step."),
    dict(k="quiet", g="knot", cost=[("reverie", 20), ("echo", 10), ("silt", 20)], requires=[L(14)],
         n="Quiet Is a Place", kind="unlocks a focus",
         text="The quiet you made at the Altar did not leave when you stood up. You can go back to it."),
    dict(k="stillkeeps", g="glass", cost=[("lucidity", 15), ("silt", 15)], requires=["max:lucidity"],
         n="Still Water Keeps", kind="unlocks a construct",
         text="Water this still holds whatever you pour into it, and gives it back unchanged."),
    dict(k="remember", g="branch", cost=[("lucidity", 20), ("reverie", 30)], requires=["rev:stillkeeps", "owned:hut>=3"],
         fx=[gain("absorb", 1.0)],
         n="They Remember Building", kind="changes generation",
         text="The Oneiri were not made for this clearing. They have built before, somewhere else, and they miss it."),
    dict(k="afraid", g="mirror", great=True, cost=[("lucidity", 20), ("echo", 20), ("silt", 35)], requires=[L(20)],
         fx=[speed("*", 0.10)],
         n="They Are Afraid Too", kind="a greater realization",
         text="They did not come for you. Something has been eating their country, a little every night, and they have watched you walk toward it. They think you might be the one who can make it stop."),

    # Chapter 2 — The Lamps
    dict(k="cold", g="key", cost=[("silt", 35), ("lucidity", 15)], requires=[L(21)],
         n="Something Cold in the Silt", kind="unlocks a focus",
         text="Some of what the dives bring up is heavier than it should be, and cold all the way through."),
    dict(k="walls", g="knot", cost=[("moonsilver", 25), ("echo", 20)], requires=["owned:hut>=5", "rev:cold"],
         n="Walls That Agree", kind="upgrades a construct",
         text="The Warrens keep rearranging themselves. Given better stuff, the walls might agree on where they are."),
    dict(k="warmth", g="lantern", cost=[("moonsilver", 15), ("reverie", 40)], requires=[L(24)],
         n="Warmth Is Allowed", kind="unlocks a focus",
         text="You had not noticed you were cold. Nobody here said you could not be warm."),
    dict(k="fat", g="scales", cost=[("ember", 20), ("silt", 25)], requires=["max:ember"],
         n="Fat of the Shore", kind="unlocks a focus",
         text="Silt, pressed with a little heat, gives up something that burns slowly."),
    dict(k="lamps", g="lantern", cost=[("tallow", 10), ("moonsilver", 20)], requires=["got:tallow"],
         n="The Lamps Were Lit", kind="unlocks a construct",
         text="Someone walked here before you and left light behind. The lamps are set low, at the height of a small hand."),
    dict(k="seam", g="stair", cost=[("moonsilver", 40), ("lucidity", 20)], requires=[L(27)],
         n="The Cold Is a Seam", kind="unlocks a construct",
         text="The cold is not everywhere. It runs in a line, and a line can be followed."),
    dict(k="drifters", g="branch", cost=[("echo", 30), ("moonsilver", 30)], requires=["rev:walls", L(29)],
         fx=[housing("hut", 1), gain("draw", 0.5)],
         n="The Drifters Settle", kind="changes the work",
         text="The half-finished figures from The Drift have started standing in your doorways. Some of them have hands."),
    dict(k="darkquiet", g="glass", cost=[("tallow", 20), ("lucidity", 25)], requires=[L(32)],
         n="The Dark Is Only Quiet", kind="unlocks a focus",
         text="You are holding your breath on the Unlit Stair. The Oneiri are not. To them, dark is only quiet with the lamps put out."),
    dict(k="knot", g="knot", cost=[("hush", 30), ("echo", 30)], requires=["max:hush"],
         fx=[cappct("echo", 1.0), cappct("hush", 1.0), cappct("ember", 1.0)],
         n="The Knot Unties Itself", kind="raises a ceiling",
         text="You were not pulling the right end. You were not, it turns out, meant to pull."),
    dict(k="tasted", g="scales", cost=[("hush", 40), ("tallow", 30), ("lucidity", 30)], requires=[L(36)],
         fx=[gain("dive", 0.25)],
         n="Everything It Ate Tasted of Me", kind="changes the descent",
         text="The Oneiri have been careful not to say it. Every dream the thing has taken, it took from you. They can tell by what is left."),
    dict(k="camefrom", g="mirror", great=True, cost=[("lucidity", 40), ("hush", 50), ("moonsilver", 60)], requires=[L(38)],
         n="It Came From Me", kind="a greater realization · opens the Visions",
         text="It did not find its way here. It was let out, by a boy who was told there was nothing in the dark and chose to believe it rather than be afraid. What he would not feel had to go somewhere. It came here, and it has been eating ever since."),

    # Chapter 3 — The Tide
    dict(k="answers", g="branch", cost=[("echo", 40), ("lucidity", 30)], requires=[L(41)], fx=[gain("conjure", 0.5)],
         n="The Echoes Answer", kind="changes a resource",
         text="What came back was never only your voice. Stop listening for yourself, and listen to the rest of it."),
    dict(k="salt", g="scales", cost=[("hush", 40), ("echo", 40)], requires=[L(47)],
         n="Salt Is What Is Left", kind="unlocks a focus",
         text="A whole sea dried here and left only its salt. You know something about that."),
    dict(k="letsea", g="glass", cost=[("salt", 60), ("silt", 50)], requires=["max:salt"],
         n="Let the Sea Go", kind="unlocks a construct",
         text="It dries faster if you stop trying to keep it."),
    dict(k="breath", g="glass", cost=[("lucidity", 40), ("moonsilver", 60)], requires=[L(50)], fx=[divespeed(0.15)],
         n="Time Is a Held Breath", kind="changes the descent",
         text="Down here time does not pass. It piles up, and you can feel the weight of it."),
    dict(k="tidefolk", g="spiral", cost=[("salt", 60), ("moonsilver", 60), ("echo", 40)], requires=[L(52)],
         n="The Tidefolk Come Ashore", kind="unlocks a construct",
         text="The keepers of the undertow have been watching from the waterline. They come up the beach with their houses on their backs."),
    dict(k="somewhere", g="key", cost=[("nacre", 30), ("echo", 50)], requires=["max:nacre"],
         n="Somewhere to Put It", kind="unlocks a construct",
         text="What they tell you is too much to carry and too important to set down."),
    dict(k="written", g="stair", cost=[("nacre", 20), ("hush", 25), ("moonsilver", 50)], requires=[L(55)],
         n="Written, It Stays", kind="unlocks a focus",
         text="Whatever you only remember, the Nobody can eat. Whatever you write down is harder to swallow."),
    dict(k="holds", g="knot", cost=[("moonsilver", 50), ("salt", 40)], requires=[L(44)],
         n="It Holds Because I Hold It", kind="unlocks a construct",
         text="Walls here are made of belief. You have more of that than you thought."),
    dict(k="stand", g="lantern", cost=[("salt", 30), ("hush", 20)], requires=["owned:rampart>=1"],
         n="Someone Has to Stand There", kind="unlocks a focus",
         text="A wall no one watches is a suggestion."),
    dict(k="owed", g="scales", cost=[("ward", 20), ("lucidity", 40)], requires=["owned:rampart>=3"], fx=[rate("dread", -0.3)],
         n="Nothing Is Owed", kind="changes generation",
         text="You have been paying for this in fear, a little every night, and no one ever asked you to."),
    dict(k="grew", g="spiral", cost=[("nacre", 40), ("vellum", 20)], requires=["vdone:tide>=3"], fx=[visioncost(0.10)],
         n="It Grew When I Did", kind="a realization · changes the work",
         text="It was only the dark, once. Then it was being left, and being laughed at, and being wrong. Every fear you would not look at, it took, and it grew."),
    dict(k="mine", g="mirror", great=True, cost=[("vellum", 30), ("nacre", 40), ("lucidity", 60)],
         requires=[L(58), "vision:well"],
         n="Mine to End", kind="a greater realization · opens the Assault",
         text="I did this. I did not mean to, and I was a child, and none of that has helped the ones it has been eating. It came out of me, and I am the only one it answers to. I will find it, and I will end it."),

    # Chapter 4 — The Road
    dict(k="lost", g="glass", cost=[("ward", 15), ("echo", 40)], requires=["lost:any"], fx=[power("Levied", 0.2)],
         n="Losing Is Also Information", kind="changes the army",
         text="You know now where it is strong. That is more than you knew this morning."),
    dict(k="manyvoices", g="branch", cost=[("chorus", 20), ("salt", 50)], requires=[L(64)], fx=[gain("rally", 0.5)],
         n="Many Voices, One Note", kind="unlocks a construct",
         text="Alone, each of them hums. Together they hold a note that pushes back."),
    dict(k="nothiding", g="key", cost=[("chorus", 20), ("vellum", 20)], requires=["won:deepend"], fx=[enemy(0.05)],
         n="It Is Not Hiding", kind="a realization · changes the road",
         text="The places it holds are not defended. They are kept, like a pantry."),
    dict(k="clocks", g="glass", cost=[("moonsilver", 100), ("lucidity", 50)], requires=[L(66)], fx=[speed("*", 0.10)],
         n="Every Clock Is Late", kind="changes generation",
         text="Every clock in the hall is behind, and every one is sure it is the only one. You were never late. You were worried."),
    dict(k="worry", g="scales", cost=[("ward", 30), ("salt", 60)], requires=[L(71)], fx=[rate("dread", -0.4)],
         n="Worry Is Fear With a Plan", kind="changes the wards",
         text="It learned worry from you, the way you learned it: by rehearsing the worst thing, over and over, as if that were a kind of safety."),
    dict(k="borrowed", g="lantern", cost=[("chorus", 30), ("ward", 30)], requires=["won>=6"], fx=[power("Levied", 0.25)],
         n="Borrowed Courage", kind="changes the army",
         text="They fight for you because you asked, and they are brave, and it is not theirs to spend."),
    dict(k="wants", g="mirror", cost=[("vellum", 30), ("nacre", 40)], requires=["won:mile"],
         n="It Wants the Waking Side", kind="a realization",
         text="It has eaten most of what there is to eat here. There is a whole world on the other side of your eyes, and it knows the way, because it came from you."),
    dict(k="made", g="spiral", great=True, cost=[("chorus", 60), ("vellum", 40), ("lucidity", 60), ("ward", 30)],
         requires=[L(78), "vision:tenant"],
         n="Made, Not Hired", kind="a greater realization · unlocks a construct",
         text="It is made of what I was afraid of. Then what fights it has to be made of what I am not afraid of. Nobody here can lend me that. This place makes whatever is held in mind. I will have to make them."),

    # Chapter 5 — The Far End
    dict(k="wicks", g="lantern", cost=[("tallow", 80), ("chorus", 30)], requires=[L(81)], fx=[gain("render", 1.0), gain("tend", 1.0), gain("kindle", 1.0)],
         n="The Wicks Come Home", kind="changes generation",
         text="The small flames who kept the Lamplit Hollows have followed the light to your Palace. They know every lamp you lit."),
    dict(k="saltwives", g="scales", cost=[("salt", 100), ("ward", 40)], requires=["won:ninth"], fx=[cappct("ward", 0.5)],
         n="The Saltwives Keep Watch", kind="changes the wards",
         text="They have kept the grief of the Salt Flats for longer than there were names. They can keep a wall."),
    dict(k="clockfolk", g="glass", cost=[("moonsilver", 150), ("lucidity", 60)], requires=[L(84)], fx=[divespeed(0.5)],
         n="The Clockfolk Keep Time", kind="changes the descent",
         text="The Clockfolk have set every clock in the hall to the same hour. It is not late."),
    dict(k="hollowed", g="branch", cost=[("ward", 40), ("nacre", 50)], requires=["won:exam"], fx=[enemy(0.10)],
         n="The Hollowed Remember", kind="changes the road",
         text="The trees it ate from the inside are still standing, and they remember what it tasted like."),
    dict(k="everypeople", g="spiral", cost=[("chorus", 80), ("lucidity", 80)],
         requires=["rev:wicks", "rev:saltwives", "rev:clockfolk", "rev:hollowed"],
         n="Every People Has Come", kind="unlocks a construct",
         text="The Oneiri, the Drifters, the Tidefolk, the Wicks, the Saltwives, the Clockfolk, the Hollowed. None of them came for you. All of them came."),
    dict(k="goodat", g="key", cost=[("mettle", 20), ("vellum", 30)], requires=["vision:duck"], fx=[cappct("mettle", 1.0)],
         n="What I Am Good At", kind="unlocks a construct",
         text="It was never a short list. You were only in the habit of reading the other one."),
    dict(k="knows", g="mirror", cost=[("mettle", 30), ("chorus", 60)], requires=[L(86)], fx=[power("Sworn", 0.10), rate("dread", 0.1)],
         n="It Knows I Am Coming", kind="a realization",
         text="The Understair is quiet the way a held breath is quiet. It has stopped eating. It is waiting for you."),
    # the choice
    dict(k="fear", g="spiral", cost=[("nacre", 50), ("vellum", 40)], requires=["vision:wake"], withdraws=["shut"],
         grants=["ending:bad"], fx=[speed("sing", -1.0), gain("rally", -0.5), rate("dread", 3.0), cap("dread", 100)],
         n="Fear Is Also Strength", kind="a choice · the nightmares wake",
         text="It made itself out of my fear. I can make something out of it too, and mine will be bigger."),
    dict(k="shut", g="knot", cost=[("lucidity", 80), ("chorus", 60)], requires=["vision:wake"], withdraws=["fear"],
         grants=["ending:good"], fx=[power("Sworn", 0.25), gain("rally", 1.0)],
         n="Some Doors Stay Shut", kind="a choice · the nightmares sleep",
         text="I have spent my whole life pushing fear somewhere else. I will not do it to them. These stay with me, and stay asleep."),
]

# ---- Visions: k, great, rep, shown when, offers (res, n, percent), effects, name, text
VISIONS = [
    dict(k="door", great=True, requires=["rev:camefrom"], of=[("hush", 10, 4), ("tallow", 10, 4), ("echo", 15, 4)],
         fx=[rate("dread", 0.2)],
         n="The Door Left Ajar",
         text="A bedroom, a door open a hand's width, a voice down the hall saying there is nobody there. You watch the boy decide to believe it. You watch what he decided not to feel slip out through the gap."),
    dict(k="bell", requires=["rev:answers", "max:echo"], of=[("echo", 20, 5), ("hush", 10, 5)], fx=[gain("conjure", 0.18)],
         n="The Bell That Answers", text="You rang it once, badly, and something under the floor rang back."),
    dict(k="vigil", requires=["rev:answers"], of=[("tallow", 20, 5), ("ember", 10, 5)],
         n="The Lamp Left On",
         text="Six years old, asking for the hall light to be left on. Being told he was too big for that now."),
    dict(k="garden", requires=["rev:letsea"], of=[("hush", 20, 5), ("salt", 15, 5)],
         n="The Salt Garden", text="Nothing grows in it. You tend it anyway, and something is growing."),
    dict(k="tide", rep=True, requires=["rev:tidefolk"], of=[("salt", 20, 10), ("silt", 30, 10)], fx=[gain("sift", 0.1)],
         n="The Long Tide",
         text="Each pour shows one fear it took, and how old he was when it took it."),
    dict(k="unblink", rep=True, requires=["rev:grew"], of=[("echo", 30, 10), ("hush", 20, 10)], fx=[revcost(0.04)],
         n="The Unblinking", text="Stop closing your eyes at the part you do not like."),
    dict(k="well", requires=["rev:grew"], of=[("nacre", 20, 10), ("echo", 40, 10)], fx=[gain("conjure", 0.4), housing("longhouse", 1)],
         n="The Well Remembers",
         text="Everything he pushed down is down here, sorted, and the Nobody has been living on it."),
    dict(k="hour", rep=True, requires=["rev:clocks"], of=[("moonsilver", 60, 10), ("echo", 50, 10)], fx=[speed("*", 0.03)],
         n="The Hour That Held", text="One hour of the dream refused to pass. Make it a habit."),
    dict(k="bridge", requires=["rev:worry"], of=[("moonsilver", 80, 5), ("chorus", 15, 5)], fx=[muster(0.10)],
         n="A Way Across", text="The gap was never the problem. Admitting you had stopped at it was."),
    dict(k="tenant", great=True, requires=["rev:wants"], of=[("echo", 60, 4), ("nacre", 20, 4), ("chorus", 20, 4)],
         fx=[cap("dread", 100)],
         n="The Other Tenant",
         text="His own bedroom, in the waking world, before dawn. He is asleep, and his eyes are open, and the thing looking out of them is not him. It has his voice. It is making calls."),
    dict(k="looked", requires=["built:forge"], of=[("mettle", 5, 5), ("ember", 30, 5)],
         n="The Night I Looked", text="Nine years old. He got up on his own, walked to the door, and turned the light on himself."),
    dict(k="duck", requires=["vision:looked"], of=[("mettle", 8, 5), ("chorus", 20, 5)],
         n="The Duck on the Wall", text="His little sister making shadow animals on the wall, until the dark was mostly ducks."),
    dict(k="aloud", requires=["vision:duck"], of=[("mettle", 12, 5), ("vellum", 15, 5)],
         n="Saying It Out Loud", text="The first time he told someone the whole truth about the dreams, and was not laughed at."),
    dict(k="waiting", requires=["vision:aloud"], of=[("mettle", 18, 5), ("salt", 60, 5)],
         n="Waiting Well", text="A long night in a hospital chair, beside someone, not leaving."),
    dict(k="stayed", requires=["vision:waiting"], of=[("mettle", 25, 5), ("chorus", 40, 5)],
         n="Everyone Who Stayed", text="Every person who stayed when he was hard to stay with."),
    dict(k="make", requires=["vision:stayed"], of=[("mettle", 35, 5), ("nacre", 40, 5)],
         n="What I Make", text="A boy drawing castles on the back of his homework. A man building a palace out of attention."),
    dict(k="wake", great=True, requires=[L(96)], of=[("nacre", 40, 4), ("vellum", 30, 4), ("mettle", 30, 4)],
         n="What I Could Wake",
         text="Every nightmare he ever had is still in him, and every one is strong. The rider with nobody in the saddle. The thing on the stairs. The voices in the hall. He could wake them, bind them, and set them on the Nobody. They would win. They would also still be here afterward, in a country that has only just stopped being eaten."),
]

# ---- units: k, section, strength, cost, shown when, name, blurb
UNITS = [
    ("drifter", "Levied", 1, [("echo", 6), ("silt", 2)], ["rev:mine"], "Drifter Levy",
     "Half-finished figures from The Drift, who will stand where they are put."),
    ("guard", "Levied", 3, [("echo", 10), ("ward", 2)], ["won:ford"], "Waking Guard",
     "People who have been awake a long time and have stopped minding."),
    ("bearer", "Levied", 8, [("tallow", 8), ("ward", 3)], ["won:orchard"], "Lantern Bearers",
     "They carry the light and are therefore the first thing anything sees."),
    ("unflinching", "Sworn", 20, [("mettle", 4), ("ward", 4)], ["vision:looked"], "The Unflinching",
     "The night he turned the light on himself, given a spear."),
    ("laughing", "Sworn", 50, [("mettle", 8), ("chorus", 6)], ["vision:duck"], "The Laughing Company",
     "Nothing it has ever made survives being laughed at properly."),
    ("plainspoken", "Sworn", 120, [("mettle", 15), ("vellum", 8)], ["vision:aloud"], "The Plainspoken",
     "They say exactly what is there. It hates that more than anything."),
    ("sentinel", "Sworn", 300, [("mettle", 28), ("salt", 30)], ["vision:waiting"], "The Stillwater Sentinels",
     "They can wait longer than it can. They have before."),
    ("chorus", "Sworn", 700, [("mettle", 50), ("chorus", 30), ("echo", 40)], ["vision:stayed"], "The Chorus",
     "Everyone who stayed, singing. It has never once been able to stand that song."),
    ("dreamwright", "Sworn", 1800, [("mettle", 90), ("vellum", 40), ("nacre", 30)], ["vision:make"], "The Dreamwright",
     "Imagination, given a body big enough to use it."),
]

# ---- the road: k, name, fear, strength, effects, grants, blurb, extra requires
FINAL = ["parted>=100", "chose"]
PLACES = [
    ("gate", "The Lamplit Gate", 3, [], [], [L(59)], "The last of your own ground. The dark begins at the gate, the way it always did."),
    ("thunder", "Thunderhead Hill", 5, [gain("*", 0.05)], [], [L(59)], "A hill under a storm that only ever threatens. He hid under the covers for a whole summer of this."),
    ("aisle", "The Lost Aisle", 8, [power("Levied", 0.25)], [], [L(60)], "Shelves higher than a grown-up, and no one at the end of any of them."),
    ("ford", "Coldwater Ford", 14, [], [], [L(62)], "The water runs the wrong way, away from home. You cross it anyway."),
    ("deepend", "The Deep End", 22, [], [], [L(64)], "A ladder going down into water too deep to stand in, and a voice saying just jump."),
    ("principal", "The Principal's Door", 34, [gain("*", 0.05)], [], [L(66)], "A frosted window, a nameplate, and the long walk up to it."),
    ("orchard", "The Sleeping Orchard", 50, [], [], [L(68)], "Everything here is asleep, including the things that were meant to be guarding it."),
    ("bell", "Bellcast", 80, [], [], [L(71)], "A town built around a bell nobody will ring, in case everyone turns to look."),
    ("mile", "The Sundered Mile", 120, [], [], [L(74)], "A mile of road that goes on for longer than a mile, and knows that it does."),
    ("ninth", "The Ninth Bell", 600, [power("Sworn", 0.25)], [], [L(77)], "You have heard it eight times. Each time you were called on and did not know the answer."),
    ("playground", "The Empty Playground", 1800, [gain("*", 0.10)], [], [L(81)], "One swing still moving. Everyone else went in together."),
    ("exam", "The Unmarked Exam", 2600, [], [], [L(81)], "Every answer written, and not one of them marked, and the bell about to go."),
    ("stage", "The Stage With No Lines", 3500, [muster(0.10)], [], [L(81)], "A spotlight, a full house, and nothing at all to say."),
    ("diary", "The Locked Diary", 6000, [cappct("vellum", 1.0)], [], [L(83)], "Everything he never told anyone, and a lock a child could open."),
    ("phone", "The Unanswered Phone", 9000, [gain("*", 0.10)], [], [L(84)], "It rings and rings. He knows who it is. He knows what they will say."),
    ("waiting", "The Waiting Room", 14000, [housing("longhouse", 1)], [], [L(86)], "Chairs in rows, a door, and behind the door, news."),
    ("interview", "The Interview Room", 19000, [gain("temper", 0.25)], [], [L(87)], "Three faces behind a table, deciding whether he is enough."),
    ("office", "The Office That Never Closes", 28000, [power("Sworn", 0.25)], [], [L(88)], "Every window lit. Every desk full. Nobody ever goes home."),
    ("rising", "The Rising Water", 36000, [rate("dread", -0.2)], [], [L(89)], "The house he lives in now, and the water coming up the stairs."),
    ("chair", "The Empty Chair", 48000, [gain("rally", 0.25)], [], [L(90)], "A place set at the table for someone who is not coming back."),
    ("faces", "The Hall of Faces", 120000, [cappct("chorus", 1.0)], [], [L(92)], "Portraits of everyone he ever wanted to be loved by, all looking away."),
    ("clock", "The Clock With No Hands", 160000, [speed("*", 0.10)], [], [L(94)], "It is late. It has always been late. There is no telling how late."),
    ("mirror", "The Mirror That Lies", 200000, [enemy(0.10)], [], [L(96)], "It shows him exactly as he fears he is, and it is very convincing."),
    ("stairtop", "The Top of the Stair", 260000, [power("Sworn", 0.25)], [], [L(98)], "The top of the stair down to the cupboard, at night, with the light off."),
    ("landing", "The Landing", 370000, [], [], FINAL, "The house at night. The floorboards know where he is."),
    ("hallway", "The Hallway, Light Off", 395000, [], [], FINAL, "The walk to the bathroom, counting the doors so as not to count anything else."),
    ("understair", "The Understair Door", 420000, [], [], FINAL, "The door to the cupboard under the stairs, where the dark goes further than the house does."),
    ("wardrobe", "The Wardrobe", 445000, [], [], FINAL, "The coat that is not a coat. It has been hanging there for thirty years."),
    ("underbed", "Under the Bed", 470000, [], [], FINAL, "Feet kept well away from the edge. It is right there, and it has always been right there."),
    ("nobody", "The Nobody", 490000, [], ["gameover"], FINAL, "Every fear he ever put down, grown into one thing, behind the door he asked to be left open."),
]

# ---- the reaches and each veil's line
REACHES = [
    ("The Shallows", 1, 1), ("The Drift", 2, 6), ("The Silt Shore", 7, 13), ("The Stillwater", 14, 20),
    ("The Lamplit Hollows", 21, 26), ("The Cold Seam", 27, 31), ("The Unlit Stair", 32, 36),
    ("The House With One Window", 37, 40),
    ("The Undertow", 41, 46), ("The Salt Flats", 47, 51), ("The Pearl Beds", 52, 56), ("The Wrack Line", 57, 60),
    ("The Hollow Wood", 61, 65), ("The Hall of Clocks", 66, 70), ("The Waiting Rooms", 71, 75), ("The Mirror Stair", 76, 80),
    ("The Gathering Shore", 81, 85), ("The Understair", 86, 90), ("The Last Lamp", 91, 96), ("The Door Ajar", 97, 100),
]
# What a dive brings up in each reach, beside Silt. Only the FIRST thing a dive brings up can
# hold it (Dream.HoldOfDive): when the primary is full the dive stops until some is spent.
# Silt is primary by default, and the Cistern guarantees it can always be spent. A reach in
# PRIMARY_OWN puts its own resource first instead, which forces the player to spend that
# resource to go on. Only list a reach whose resource some Focus task can always drain by then
# without diving, or the dive can lock itself (The Drift with Echo first deadlocks at level 3:
# Echo fills, and the only things that spend it need Silt, which only diving brings).
REACH_BRING = {
    "The Drift": [("echo", 0.5)], "The Stillwater": [("lucidity", 0.2)], "The Lamplit Hollows": [("tallow", 0.5)],
    "The Cold Seam": [("moonsilver", 0.5)], "The Unlit Stair": [("hush", 0.5)], "The House With One Window": [("ember", 0.5)],
    "The Undertow": [("echo", 1)], "The Salt Flats": [("salt", 1)], "The Pearl Beds": [("nacre", 0.3)],
    "The Wrack Line": [("vellum", 0.2)], "The Hollow Wood": [("ward", 0.2)], "The Hall of Clocks": [("moonsilver", 1)],
    "The Waiting Rooms": [("salt", 1)], "The Mirror Stair": [("nacre", 0.5)], "The Gathering Shore": [("chorus", 0.5)],
    "The Understair": [("echo", 2)], "The Last Lamp": [("tallow", 2), ("ember", 1)], "The Door Ajar": [],
}
# Temper drains Chorus from level 78 and Rally drains Echo from 58. (The Last Lamp is not safe
# with Tallow first: Keep the Lamps stops once Lucidity is full, and the Tallow Press keeps
# filling it, so the simulated player locked there at level 91.)
PRIMARY_OWN = {"The Understair"}
# the price of the dive at the end of each chapter: only its golden beat brings it
GATES = {40: ("dread", 1), 60: ("chorus", 1), 80: ("mettle", 1)}

VEIL_LINES = {
    3: ["More of the almost-dreams. A birthday party with no one at it, the candles still lit. I walked around it rather than through.",
        "A kitchen went by with the kettle just starting to sing. Nobody came to take it off the heat.",
        "None of it is finished. This is where things wait when nobody dreams them the rest of the way."],
    4: ["The pieces thin out here, as if something has been picking through them and taking the good parts.",
        "A picnic with nothing left on the blanket. A letter with the signature torn away. A swing set missing its swing.",
        "Whatever did this was careful. It left the shapes of things behind, the way you leave the rind."],
    5: "I found a fragment with teeth marks in it. A summer, half of one. The rest was gone.",
    6: "The Drift ends in a slope of silt, and the silt is warm, as if something has been lying on it.",
    7: "A shore, finally. The silt here is fine as flour and holds a footprint for exactly as long as you watch it.",
    8: "The tide goes out and never comes back in. What it left behind is waiting to be sorted.",
    9: "The Oneiri follow me down now. They walk the shore behind me picking things up, the way you would after a storm.",
    10: "There are paths worn into the silt that are not mine. Small feet. Going down.",
    11: "I dug a hand's depth and found a marble. Blue, with a white twist. I had one like it.",
    12: "The shore curves away in both directions and never meets anything. The only way is down.",
    13: "Below the silt there is water again, very still. I can see my face in it, and it is paying attention.",
    14: "Water so still it thinks. When I sit at its edge my thoughts slow down to match it.",
    15: "Nothing moves here unless I do. It is the quietest place I have ever been, awake or not.",
    16: "The Oneiri will not go into the water. They wait at the edge and watch me wade.",
    17: "Something far below disturbed the surface once, today. One ring, spreading. Then nothing.",
    18: "I think the stillness is not calm. I think it is holding its breath.",
    19: "The lamps start here. Not lit, just placed. Someone set them along the bottom like a path.",
    20: "At the deepest point there is a hush like a hand over a mouth. The Oneiri are frightened of it, and did not want me to see that they are.",
    21: "Hollows in the rock, each one with a lamp in it, each lamp burnt out. The wicks are short. They burned a long time.",
    22: "The lamps are set low. I have to crouch to reach them. Whoever set them was small.",
    23: "I lit one. It took the flame gratefully, and the hollow around it remembered it had a shape.",
    24: "The Oneiri gathered around the lit lamp like people around a stove. I had not realised they were cold.",
    25: "Every hollow has a lamp and every lamp faces outward, toward the dark, like a line of sentries.",
    26: "Past the last lamp the hollows go on, unlit. I did not like the way they went on.",
    27: "A seam of cold runs through the rock, silver where it surfaces. It hums the way the Altar hums, but lower.",
    28: "The cold here is not weather. It is something being kept.",
    29: "The Oneiri work the seam without being asked. They seem glad to have something to make with.",
    30: "Deep in the seam there is a room cut square, and in it a small bed made of moonsilver, much too cold to sleep in.",
    31: "The seam ends against a stair. The stair has no light at all.",
    32: "The first place down here that is simply dark. Not dim. Dark, the way a room is dark after someone says goodnight.",
    33: "I count the steps. I always counted the steps. I do not know how I know that.",
    34: "Something breathes on the stair below me, in when I breathe out, as if it is trying not to be heard.",
    35: "The Oneiri carry lamps now when they follow me. None of them will say why.",
    36: "At the bottom of the stair there is a house, and one window in it, and a light on behind the window.",
    37: "The house is the house I grew up in. It is wrong in small ways: the door is too tall, the window too high to see into.",
    38: "I walked all the way around it. There is only the one window, and it is my window.",
    39: "The light behind the window is a nightlight. It is the only light in the whole of the dark.",
    40: "I have to go in. I have known that since the Stillwater. I did not want to know it.",
    41: "Under everything, a pull. It moves the silt, the lamps, the Oneiri, me. It has always been moving me.",
    42: "The Oneiri talk more down here. Their voices come back changed off the water, and some of what comes back is advice.",
    43: "The undertow runs toward something. I can feel which way, the way you feel which way is downhill in the dark.",
    44: "Some of the currents are warm. The Oneiri say those are the ones the Nobody has not found yet.",
    45: "A drowned staircase. A drowned kitchen. A drowned swing, still moving.",
    46: "The current slackens and the water goes shallow and bitter. Salt, on the lips.",
    47: "A sea dried here, a long time ago, and left all of its salt behind. White to every horizon.",
    48: "The salt lies in drifts, like snow, and in some of the drifts there are shapes pressed into it. Small ones.",
    49: "The Saltwives watch from far off. They do not come closer. They are not unkind about it.",
    50: "I cried here, and the salt took it up without comment, the way it takes everything.",
    51: "At the edge of the flats the salt gives way to shells, and the shells are full of something that shines.",
    52: "Beds of shell, open, each with a pearl in it the size of a thumb. Each pearl is a memory.",
    53: "A pearl grows around a hurt. Every one of these started as something small that would not go away.",
    54: "I held one up to the light and saw a classroom. Everyone was laughing. I put it back.",
    55: "The Tidefolk tend the beds. They know every pearl by name. They know which ones are mine.",
    56: "The oldest pearl is black. It started as the dark.",
    57: "Where the tide stops it leaves a line of everything it carried. Here it is dreams, broken open and emptied.",
    58: "Thousands of them. Not mine: the Oneiri's, the Tidefolk's, the Drifters'. All with the same bite taken out.",
    59: "I did this. Not with my hands. But it came out of me, and I let it.",
    60: "The wrack line runs down to a road. Someone has to walk it.",
    61: "Trees, eaten from the inside. They still stand. They creak when the wind passes through the holes.",
    62: "Dread comes up through the roots here, a slow cold, and settles in whoever stands still too long.",
    63: "The Hollowed do not speak. They lean together when I pass, as if remembering how to be a forest.",
    64: "I heard my name in the wood, in my mother's voice. It was not her.",
    65: "The wood ends at a wall of clocks.",
    66: "Every clock is late. Every clock knows it is late. The ticking is frantic.",
    67: "The Clockfolk wind them endlessly and never catch up, and do not know that they could stop.",
    68: "One clock is set to the hour I was born. It is running fast.",
    69: "The Nobody learned worry here. I can smell it in the brass.",
    70: "At the end of the hall there is a waiting room, and a name being called that is almost mine.",
    71: "Chairs in rows. A door. Behind the door, news.",
    72: "Everyone here is waiting for something bad, and has been for years, and has gotten good at it.",
    73: "I sat for a while. It felt like sitting in a hospital, holding a hand.",
    74: "The door never opens. That is the worst thing it could do, and it knows it.",
    75: "Past the last room there is a stair, and the stair is a mirror.",
    76: "Every step reflects me going down. The reflections are a little behind, and a little wrong.",
    77: "In the mirrors it wears my face. It is getting better at it.",
    78: "One reflection smiled when I did not.",
    79: "I understand now what it wants. It wants the side of the mirror I am on.",
    80: "The mirror ends. Below it, a shore where the peoples have gathered, waiting to see what I will bring.",
    81: "Everyone is here. Every people of the dream, on one shore, in their own lights.",
    82: "The Wicks came from the Hollows carrying their own lamps. They knew me by the ones I lit.",
    83: "They are not here to watch. They brought tools.",
    84: "I have never been anywhere with so many who wanted the same thing I want.",
    85: "Below the shore, a cupboard door, grown enormous. The Understair.",
    86: "The cupboard under the stairs. Coats, boots, the vacuum cleaner, and the dark at the back that went further than the house did.",
    87: "It went further. It goes on and on.",
    88: "The Nobody has stopped eating. The Understair is quiet the way a held breath is quiet.",
    89: "I found my old torch here, the one with the dead batteries. I put new ones in. It works.",
    90: "At the back of the Understair there is one lamp, still lit.",
    91: "One lamp, burning, in all this dark. It has been burning since I was six.",
    92: "I think I lit it. I think it is the only thing I did not push away.",
    93: "Everything down here leans toward the lamp. Even the dark.",
    94: "The Oneiri set up around it without being asked, as if it were an altar.",
    95: "Past the lamp's reach there is a landing, and a hallway, and a door.",
    96: "Something showed me, here, what I could wake if I wanted to. I will have to decide.",
    97: "My bedroom door, open a hand's width, the way I asked for it to be left every night.",
    98: "The light from the hall falls across the carpet in a stripe. Nothing crosses it.",
    99: "I can hear it breathing on the other side of the door. It has been waiting a very long time.",
    100: "The bottom. Nothing further down. Only the door, and what is behind it.",
}
BOTTOM = ("What Lies In The Depths", "There is nothing under this. Whatever happens now happens on the road.")

# ---- the Journal: id, day, chapter, source, great, shown when, name, paragraphs (None keeps the existing strings)
JOURNAL = [
    ("doorway", 1, 1, "veil", False, ["start"], None, None),
    ("altar", 3, 1, "veil", False, ["built:altar"], None, None),
    ("listening", 14, 1, "revelation", False, ["rev:listening"], None, None),
    ("drift", 9, 1, "veil", False, ["parted:1"], None, None),
    ("afraid", 80, 1, "revelation", True, ["rev:afraid"], "They Are Afraid Too", [
        "One of the Oneiri showed me today, without being asked. It took me to the edge of the Stillwater and pointed down, and then it pointed at the clearing, and then it would not look at me.",
        "There is something in their country that eats. It does not hunt, exactly. It grazes. Every night a little more of what they built is gone in the morning, and what is left has a bite taken out of it.",
        "They did not come to the clearing because it was new. They came because I was walking toward the thing, and nobody else ever had.",
    ]),
    ("lamps", 92, 2, "revelation", False, ["rev:lamps"], "The Lamps Were Lit", [
        "Somebody walked here before me and left light behind. The lamps are burnt out, every one, but they were lit once by somebody who thought it was worth the tallow.",
        "They are set low. Knee height. I keep crouching to reach them, and I keep thinking about how small you would have to be for that to be the right height.",
    ]),
    ("tasted", 150, 2, "revelation", False, ["rev:tasted"], "Everything It Ate Tasted of Me", [
        "The Oneiri have known for a while. They could tell from what was left behind: every dream it has taken has my fingerprints on what remains.",
        "I asked them why they helped me, if they knew. One of them put its hand flat on my chest, where you would feel for a heartbeat, and held it there.",
    ]),
    ("door", 160, 2, "vision", True, ["vision:door"], "The Door Left Ajar", [
        "I saw it. A bedroom, mine. The door open a hand's width, because that is how I asked for it to be left. A voice down the hall, tired and kind: there is nobody there, go to sleep.",
        "I watched myself decide to believe it. I watched the fear go out of the boy's face, not because it was gone but because he had put it somewhere. And I watched something small and dark slip out through the gap in the door.",
        "He called it the Nobody. Of course he did. That was what he had been told was there.",
    ]),
    ("lamp", 170, 3, "vision", False, ["vision:vigil"], "The Lamp Left On", [
        "I asked for the hall light to be left on, the year I turned six. I was told I was too big for that now. I agreed, because I wanted to be.",
        "The Oneiri helped me build it anyway: a small lamp at the edge of the Palace, the kind a child could reach. The dark around the Palace backed off a step. I felt it go.",
    ]),
    ("tide1", 190, 3, "vision", False, ["vdone:tide>=1"], "The Long Tide", ["Five years old. The dark. That was the first thing it was, and for a while the only thing."]),
    ("tide2", 196, 3, "vision", False, ["vdone:tide>=2"], "The Long Tide", ["Eight. Being left: a car pulling away from a school gate, and the certainty it would not come back. It took that too."]),
    ("tide3", 202, 3, "vision", False, ["vdone:tide>=3"], "The Long Tide", ["Twelve. A classroom, laughing. I laughed along. It ate the part of me that wanted to cry."]),
    ("tide4", 208, 3, "vision", False, ["vdone:tide>=4"], "The Long Tide", ["Sixteen. Being wrong, out loud, in front of everyone. I decided never to be sure of anything again. It grew fat on that."]),
    ("tide5", 214, 3, "vision", False, ["vdone:tide>=5"], "The Long Tide", ["Twenty-four. Being ordinary. A long year of it. It took the year whole."]),
    ("tide6", 220, 3, "vision", False, ["vdone:tide>=6"], "The Long Tide", ["Thirty-one. Losing people. I did not grieve properly. I gave it to the Nobody instead, and it has been wearing that grief ever since."]),
    ("mine", 238, 3, "revelation", True, ["rev:mine"], "Mine to End", [
        "I went down to the wrack line and looked at all of it. Everything that thing has eaten, lying on the shore with the same bite taken out.",
        "I did this. I did not mean to, and I was a child, and none of that has helped the ones it has been eating. It came out of me. It answers to me, or it answers to no one.",
        "The Drifters say they will stand where I put them. It is not much of an army. It is a start.",
    ]),
    ("gate", 244, 4, "assault", False, ["won:gate"], "The Lamplit Gate", [
        "We went out through the gate at first light, a handful of Drifters and me. The dark on the other side was only dark. We took it.",
        "I looked back and the lamps at the gate were still going.",
    ]),
    ("wants", 290, 4, "revelation", False, ["rev:wants"], "It Wants the Waking Side", [
        "On the Sundered Mile I understood it. It has eaten nearly everything here. It is not trying to finish the dream world. It is trying to get out of it.",
        "It knows the way out, because it came in through me.",
    ]),
    ("tenant", 300, 4, "vision", True, ["vision:tenant"], "The Other Tenant", [
        "I saw my own bedroom in the waking world, before dawn. I was asleep, and my eyes were open.",
        "The thing looking out of them was not me. It got up. It used my voice on the phone. It was very convincing, and it was so pleased to be warm.",
    ]),
    ("made", 318, 4, "revelation", True, ["rev:made"], "Made, Not Hired", [
        "The Drifters are brave and they are not mine to spend. Everything I have thrown at it so far has been borrowed.",
        "It is made of what I was afraid of. Then what fights it has to be made of what I am not afraid of. This place makes whatever is held in mind. I will build a forge, and I will hold my strengths in it until they hold their shape.",
    ]),
    ("everypeople", 336, 5, "revelation", False, ["rev:everypeople"], "Every People Has Come", [
        "The Oneiri, the Drifters, the Tidefolk, the Wicks, the Saltwives, the Clockfolk, the Hollowed. They came up to the Palace all day in their own lights.",
        "None of them came for me. All of them came.",
    ]),
    ("knows", 350, 5, "revelation", False, ["rev:knows"], "It Knows I Am Coming", [
        "It has stopped eating. The whole of the Understair has gone quiet, the way a house goes quiet when someone is standing very still in it.",
    ]),
    ("wake", 380, 5, "vision", True, ["vision:wake"], "What I Could Wake", [
        "Something showed me, at the Last Lamp, what I still have. Every nightmare I ever had is still in me. The rider with nobody in the saddle. The thing on the stairs. The voices in the hall.",
        "I could wake them. They would win. And then they would be here, afterward, in a country that has only just stopped being eaten.",
    ]),
    ("fear", 384, 5, "revelation", True, ["rev:fear"], "Fear Is Also Strength", [
        "I woke them. They came up out of me the way the Nobody did, once, and they stood in the lines where my strengths had stood, and they were so much bigger.",
        "The Oneiri did not say anything. Most of them left that night. The ones at work stayed at their work, and did not look at me.",
    ]),
    ("shut", 384, 5, "revelation", True, ["rev:shut"], "Some Doors Stay Shut", [
        "I have spent my whole life putting fear somewhere else. I will not put it here. These stay with me, and they stay asleep.",
        "The Oneiri sang all night. I do not think it was for the battle.",
    ]),
    ("landing", 390, 5, "assault", False, ["won:landing"], "The Landing", ["The house at night. Every floorboard knew where I was. We went up anyway."]),
    ("hallway", 394, 5, "assault", False, ["won:hallway"], "The Hallway, Light Off", ["I counted the doors, the way I used to, so as not to count anything else. There were more doors than there used to be. We opened all of them."]),
    ("understair", 398, 5, "assault", False, ["won:understair"], "The Understair Door", ["The cupboard under the stairs. The dark at the back went further than the house. We went further."]),
    ("wardrobe", 402, 5, "assault", False, ["won:wardrobe"], "The Wardrobe", ["The coat that is not a coat. It has been hanging there for thirty years, waiting for me to open the door at night. I opened it."]),
    ("underbed", 406, 5, "assault", False, ["won:underbed"], "Under the Bed", ["It was right there. It had always been right there. I put my feet on the floor."]),
    ("nobody", 410, 5, "assault", True, ["won:nobody"], "The Nobody", [
        "It was smaller than I thought. It always is. It was every fear I ever put down, and when we had taken the last of them away there was only a boy's worth of dark left, and then not even that.",
        "I opened the door the rest of the way.",
    ]),
]

ENDINGS = {
    "good": ("The Door Opened", [
        "The Nobody is gone. Nothing took its place. Across the whole of the dream the peoples came out of their houses in the morning and found that nothing had been eaten in the night.",
        "The Oneiri still build. The Drifters have settled into shapes. The Wicks keep the lamps, although none of them strictly need keeping any more.",
        "He sleeps now, all the way through, with the door shut. Some nights he dreams of a clearing with a humming stone in it, and somebody is always there to meet him.",
    ]),
    "bad": ("The Door Held", [
        "The Nobody is gone. What he woke to destroy it did not go back to sleep.",
        "The rider still rides the roads with nobody in the saddle. The thing on the stairs has found other stairs. The peoples of the dream keep their doors shut at night now, and their lamps lit, and they do not come to the Palace.",
        "He sleeps soundly. He is free of it. Down in the country beyond his dreams, something else is grazing, and it has his face.",
    ]),
}
