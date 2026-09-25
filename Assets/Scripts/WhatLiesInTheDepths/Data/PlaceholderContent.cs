// What Lies In The Depths — the demo content lifted from the spec set.
//
// ============================== READ THIS ==============================
// EVERY NUMBER AND EVERY WORD IN THIS FILE IS A PLACEHOLDER. Costs, rates, ceilings,
// enemy strengths, unit powers, Oneiri counts, dream-day counts and percentages were all
// chosen to make the interface legible at a representative mid-game state. None of it is
// balanced and none of it was ever meant to be. Treat each number as an example of the
// shape a number takes in that slot and nothing more. Names, blurbs, veil entries and
// journal prose are first-draft placeholder copy in the game's voice; rewrite freely,
// nothing depends on the words except their length.
//
// If a rebuild treats these as design intent, the game will be wrong in a way that is
// very hard to see.
// =======================================================================
using System.Collections.Generic;
using UnityEngine;
using Ursine.Economy;

namespace WhatLiesInTheDepths.Data
{
    public static class PlaceholderContent
    {
        static Amount A(string k, double n) => new Amount { k = k, n = n };
        static List<Amount> Cost(params Amount[] a) => new List<Amount>(a);
        static List<string> Fx(params string[] s) => new List<string>(s);

        public static readonly string[] Groups = { "Gathered", "Yours", "Against you" };

        public static readonly Dictionary<int, string> Chapters = new Dictionary<int, string>
        {
            { 1, "Arrival" }, { 2, "The Lamps" }, { 3, "The Tide" },
            { 4, "The Road" }, { 5, "The Far End" }
        };

        public static void Fill(Dream s)
        {
            FillResources(s);
            FillTasks(s);
            FillConstructs(s);
            FillRevelations(s);
            FillVisions(s);
            FillAssault(s);
            FillJournal(s);
            FillVeil(s);
        }

        // ---- fifteen resources -------------------------------------------------
        static void FillResources(Dream s)
        {
            // Ursine's Res groups by index; the names for those indices are this game's.
            void R(string k, string n, double c, double m, double r, ResGroup g)
                => s.resources.Add(new Res
                {
                    k = k, n = n, c = c, m = m, r = r, passive = r,
                    g = (int)g,
                    glyph = k,
                    // Dread and anything like it: the sign convention inverts, so a
                    // positive rate is the bad one.
                    hostile = g == ResGroup.AgainstYou
                });

            R("reverie", "Reverie", 1284, 2000, 12.4, ResGroup.Gathered);
            R("moonsilver", "Moonsilver", 318, 500, -1.2, ResGroup.Gathered);
            R("silt", "Silt", 4602, 6000, 31.5, ResGroup.Gathered);
            R("tallow", "Tallow", 77, 400, 1.8, ResGroup.Gathered);
            R("nacre", "Nacre", 0, 1645, 0, ResGroup.Gathered);
            R("ember", "Ember", 15, 25, -0.4, ResGroup.Gathered);
            R("salt", "Salt", 208, 208, 3.3, ResGroup.Gathered);      // at the ceiling
            R("vellum", "Vellum", 36, 90, 0.2, ResGroup.Gathered);
            R("lucidity", "Lucidity", 60, 60, 2.1, ResGroup.Yours);   // at the ceiling
            R("whispers", "Whispers", 892, 1645, 5.7, ResGroup.Yours);
            R("chorus", "Chorus", 14, 120, 0.6, ResGroup.Yours);
            R("hush", "Hush", 431, 900, -2.8, ResGroup.Yours);
            R("oneiri", "Oneiri", 24, 30, 0.5, ResGroup.Yours);
            R("ward", "Ward", 8, 64, 0, ResGroup.Yours);
            R("dread", "Dread", 61, 100, 0.8, ResGroup.AgainstYou);   // hostile
        }

        // ---- Focus -------------------------------------------------------------
        static void FillTasks(Dream s)
        {
            void T(string sec, string id, string g, string n, string b, List<Amount> cost,
                   List<Amount> gain, double baseSeconds, int w, int cap, float p, bool seen = true)
                => s.tasks.Add(new FocusTask
                {
                    section = sec, id = id, g = g, n = n, b = b, cost = cost, gain = gain,
                    baseSeconds = baseSeconds, w = w, cap = cap, p = p, seen = seen
                });

            T("Labor", "sift", "sift", "Sift the silt",
              "Oneiri comb the floor for what the tide left.",
              null, Cost(A("silt", 6)), 26, 3, 4, 34f);
            T("Labor", "draw", "draw", "Draw moonsilver",
              "Lowered into the cold seam until it fills.",
              Cost(A("silt", 12)), Cost(A("moonsilver", 3)), 19, 5, 6, 71f);
            T("Labor", "render", "render", "Render tallow",
              "Slow heat, and a smell that follows you up.",
              Cost(A("silt", 8), A("ember", 2)), Cost(A("tallow", 5)), 14, 2, 3, 100f);

            T("Tending", "card", "card", "Card the whispers",
              "Tangled sound, combed until it lies flat.",
              Cost(A("tallow", 4)), Cost(A("whispers", 9)), 18.4, 4, 5, 22f);
            T("Tending", "tend", "tend", "Keep the lamps",
              "None of them stay lit. You light them anyway.",
              Cost(A("tallow", 3)), Cost(A("lucidity", 2)), 18, 2, 2, 58f);

            T("Listening", "sit", "sit", "Sit with the hush",
              "Nothing is asked of you. That is the difficulty.",
              null, Cost(A("hush", 4), A("reverie", 1)), 22, 0, 8, 0f);
            T("Listening", "page", "page", "Copy the vellum",
              "The hand remembers what the reading does not.",
              Cost(A("hush", 6)), Cost(A("vellum", 2)), 14.8, 2, 4, 46f, seen: false);

            // The spec's demo mind is attending to the silt.
            s.attendedTaskId = "sift";
        }

        // ---- Constructs --------------------------------------------------------
        static void FillConstructs(Dream s)
        {
            void C(string g, string n, ConstructKind kind, int owned, string b, List<string> fx,
                   List<Amount> cost, Vector2 pos, bool seen = true, bool built = true)
                => s.constructs.Add(new ConstructDef
                {
                    g = g, n = n, kind = kind, owned = owned, b = b, fx = fx, cost = cost,
                    palacePos = pos, seen = seen, built = built
                });

            C("spindle", "Dreamspindle", ConstructKind.Works, 1,
              "Something turns, and turns, and does not stop turning.",
              Fx("<b>+1.2</b> Reverie /s", "Oneiri yield <b>+5%</b> each"),
              Cost(A("reverie", 1420), A("moonsilver", 380)), new Vector2(132, 148));
            C("press", "Tallow Press", ConstructKind.Works, 0,
              "What the shore gives up when it is squeezed.",
              Fx("<b>+0.8</b> Tallow /s"),
              Cost(A("silt", 820), A("tallow", 15)), new Vector2(286, 96));
            C("lamprow", "Lamplight Row", ConstructKind.Works, 0,
              "Someone lit these once. They can be lit again.",
              Fx("dives cost <b>4%</b> less Reverie", "<b>+0.2</b> Tallow /s"),
              Cost(A("tallow", 60), A("reverie", 640)), new Vector2(118, 330));

            C("abode", "Abode", ConstructKind.Dwellings, 2,
              "The walls agree on where they are, and stay agreed.",
              Fx("<b>+4</b> Oneiri", "<b>+1</b> Oneiri ceiling"),
              Cost(A("moonsilver", 480), A("whispers", 90)), new Vector2(592, 118));
            C("hut", "Warren", ConstructKind.Dwellings, 3,
              "Rooms that lead to rooms, none of them the first one.",
              Fx("<b>+2</b> Oneiri"),
              Cost(A("reverie", 980), A("silt", 210)), new Vector2(736, 206));
            C("longhouse", "Longhouse", ConstructKind.Dwellings, 0,
              "Room enough that nobody has to be counted.",
              Fx("<b>+9</b> Oneiri", "<b>+3</b> Oneiri ceiling"),
              Cost(A("reverie", 900), A("moonsilver", 220), A("silt", 9000)), // above a ceiling
              new Vector2(700, 82), built: false);

            C("moonwell", "Moonwell", ConstructKind.Reservoirs, 2,
              "Still water that keeps every face it has held.",
              Fx("<b>+250</b> max Reverie", "<b>+0.4</b> Reverie /s"),
              Cost(A("reverie", 320), A("moonsilver", 64)), new Vector2(138, 566));
            C("cistern", "Cistern", ConstructKind.Reservoirs, 1,
              "It fills whether or not you are watching it.",
              Fx("<b>+600</b> max Silt"),
              Cost(A("silt", 1450), A("moonsilver", 95)), new Vector2(212, 716));
            C("reliquary", "Reliquary", ConstructKind.Reservoirs, 0,
              "Somewhere to put the things you were told.",
              Fx("<b>+40</b> max Whispers"),
              Cost(A("whispers", 120), A("moonsilver", 140)), new Vector2(376, 766),
              seen: false, built: false);

            C("rampart", "Somnal Rampart", ConstructKind.Wards, 0,
              "It holds because you have not stopped believing it holds.",
              Fx("<b>−0.2</b> Dread /s", "<b>+8</b> Ward"),
              Cost(A("reverie", 760), A("moonsilver", 290)), new Vector2(750, 576), built: false);
            C("chorusstone", "Chorus Stone", ConstructKind.Wards, 0,
              "A note held by something that does not breathe.",
              Fx("<b>+12</b> max Ward"),
              Cost(A("whispers", 340), A("silt", 520)), new Vector2(636, 734), built: false);
        }

        // ---- Revelations -------------------------------------------------------
        // Starting angles are the spec's: the inner five at fifths of a turn, the outer four
        // at quarters offset by 0.45 rad (25.78 degrees).
        static void FillRevelations(Dream s)
        {
            void V(string k, string n, string kind, string bl, List<string> fx, List<Amount> cost,
                   bool great, float ring, float deg)
                => s.revelations.Add(new RevelationDef
                {
                    k = k, n = n, kind = kind, bl = bl, fx = fx, cost = cost, great = great,
                    ring = ring, angle = deg * Mathf.Deg2Rad
                });

            V("lantern", "The Lamps Were Lit", "unlocks a construct",
              "Someone walked here before you and left light behind. That they bothered is the part that changes you.",
              Fx("unlocks <b>Lamplight Row</b>", "<b>+0.2</b> Tallow /s once built"),
              Cost(A("reverie", 420), A("whispers", 80)), false, 306f, 0f);
            V("key", "Nothing Is Locked", "unlocks a focus",
              "You had been turning handles. It had not occurred to you to push.",
              Fx("unlocks the <b>Comb the Silt Shore</b> focus"),
              Cost(A("reverie", 640), A("moonsilver", 120)), false, 306f, 72f);
            V("spiral", "Recursion", "a greater realization · changes generation",
              "The thought about the thought is also a thought, and it is turning at the same speed.",
              Fx("Reverie generation <b>×1.25</b>", "Oneiri yield <b>+5%</b> each", "the Journal gains an entry"),
              Cost(A("reverie", 4200), A("moonsilver", 900), A("lucidity", 120)), true, 306f, 144f);
            V("stair", "Down Is a Direction", "changes the descent",
              "You stop thinking of the floor as a floor. It has always been the next step.",
              Fx("dives cost <b>−8%</b> Reverie"),
              Cost(A("reverie", 1420), A("lucidity", 95)), false, 306f, 216f);
            V("knot", "The Knot Unties Itself", "raises a ceiling",
              "You were not pulling the right end. You were not, it turns out, meant to pull.",
              Fx("<b>+600</b> max Whispers", "<b>+250</b> max Hush"),
              Cost(A("whispers", 300), A("moonsilver", 180)), false, 306f, 288f);

            V("scales", "Nothing Is Owed", "changes generation",
              "The ledger you have been keeping was never asked for by anyone.",
              Fx("Dread accrues <b>−0.3</b> /s"),
              Cost(A("silt", 9000), A("ward", 140)), false, 388f, 25.78f);   // above a ceiling
            V("glass", "Time Is a Held Breath", "changes the descent",
              "Down here it does not pass. It accumulates, and you can feel the weight of it on your chest.",
              Fx("a dive every <b>5.2s</b> instead of 6.0s"),
              Cost(A("reverie", 1600), A("moonsilver", 320)), false, 388f, 115.78f);
            V("branch", "It Branches", "unlocks a focus",
              "One question, asked honestly, turns out to have been three questions in a coat.",
              Fx("unlocks the <b>Listen</b> focus", "<b>+1</b> Oneiri ceiling"),
              Cost(A("whispers", 520), A("lucidity", 60)), false, 388f, 205.78f);
            V("mirror", "You Are the One Dreaming", "a greater realization · unlocks a construct",
              "It is the oldest realization there is, and it has never once survived waking up.",
              Fx("unlocks <b>Somnal Rampart</b>", "<b>+8</b> Ward", "the Journal gains an entry"),
              Cost(A("reverie", 5600), A("moonsilver", 1400), A("whispers", 240)), true, 388f, 295.78f);

            // The spec's demo mind has already kept two.
            s.absorbed.Add("lantern");
            s.absorbed.Add("glass");
        }

        // ---- Visions -----------------------------------------------------------
        static void FillVisions(Dream s)
        {
            void W(string k, string n, string bl, List<string> fx, List<Offer> of, int sel,
                   float p, bool rep, int done, bool great, bool channelling, float deg)
                => s.visions.Add(new VisionDef
                {
                    k = k, n = n, bl = bl, fx = fx, of = of, sel = sel, p = p, rep = rep,
                    done = done, great = great, a = channelling, angle = deg * Mathf.Deg2Rad
                });

            List<Offer> O(params Offer[] o) => new List<Offer>(o);
            Offer F(string r, double n, double g) => new Offer { r = r, n = n, g = g };

            W("vigil", "The Lantern Vigil",
              "Keep one lamp burning where the silt is deepest, and keep it burning.",
              Fx("Tallow gain <b>+8%</b>", "Ember ceiling <b>+40</b>"),
              O(F("silt", 240, 1), F("tallow", 18, 1)), 0, 62f, true, 2, false, false, 0f);
            W("tide", "The Long Tide",
              "Learn the floor well enough that the pull of it stops being weather.",
              Fx("Silt ceiling <b>×1.5</b>", "a dive brings up <b>+2</b> Silt", "the Journal gains an entry"),
              O(F("silt", 150, 1), F("moonsilver", 24, 1), F("nacre", 8, 4)), 0, 28f, false, 0, true, false, 60f);
            W("unblink", "The Unblinking",
              "Stop closing your eyes at the part you do not like.",
              Fx("Revelations cost <b>−4%</b>"),
              O(F("whispers", 40, 1), F("hush", 12, 1)), 1, 5f, true, 1, false, true, 120f);
            W("door", "A Door in the Floor",
              "There is a way further down that is not falling. Find where it was put.",
              Fx("the next veil opens a fathom shallower", "<b>+1</b> Oneiri ceiling", "the Journal gains an entry"),
              O(F("silt", 300, 1), F("moonsilver", 120, 2), F("ember", 20, 5)), 0, 91f, false, 0, true, false, 180f);
            W("garden", "The Salt Garden",
              "Nothing grows in it. You tend it anyway, and something is growing.",
              Fx("Nacre gain <b>+25%</b>", "unlocks <b>Sift the beds</b>"),
              O(F("hush", 90, 1), F("nacre", 6, 3)), 0, 0f, false, 0, false, false, 240f);
            W("hour", "The Hour That Held",
              "One hour of the dream refused to pass. Make it a habit.",
              Fx("all Focus rates <b>+3%</b>"),
              O(F("reverie", 900, 1), F("whispers", 70, 1)), 1, 37f, true, 4, false, false, 300f);

            // Golden Visions the spec's demo mind has already finished.
            void Kept(string k, string n, string bl, List<string> fx)
                => s.visionsAbsorbed.Add(new VisionDef { k = k, n = n, bl = bl, fx = fx, great = true, p = 100f });
            Kept("bell", "The Bell That Answers",
                 "You rang it once, badly, and something under the floor rang back.",
                 Fx("Whispers gain <b>+18%</b>", "Chorus ceiling <b>+240</b>", "the Journal gained an entry"));
            Kept("bridge", "A Way Across",
                 "The gap was never the problem. Admitting you had stopped at it was.",
                 Fx("dives cost <b>\u221212%</b> Reverie", "unlocked <b>Cross the Span</b>", "the Journal gained an entry"));
            Kept("well", "The Well Remembers",
                 "Everything you have let go of down here is still down here, and it is sorted.",
                 Fx("Moonsilver gain <b>\u00D71.4</b>", "<b>+3</b> Oneiri ceiling", "the Journal gained an entry"));
        }

        // ---- Assault -----------------------------------------------------------
        static void FillAssault(Dream s)
        {
            void L(string k, string n, float t, bool won, double en, string bl, string ben,
                   double costScale = 1.0)
                => s.road.Add(new RoadLocation
                {
                    k = k, n = n, t = t, won = won, en = en, bl = bl, ben = ben, costScale = costScale
                });

            L("gate", "The Lamplit Gate", .135f, true, 240,
              "The last of your own ground. Somebody had kept the lamps burning here, and it was you.",
              "unlocked <b>Waking Guard</b>");
            L("ford", "Coldwater Ford", .295f, true, 520,
              "The water runs the wrong way and is colder than water. You cross it anyway.",
              "unlocked <b>Lantern Bearers</b>");
            L("orchard", "The Sleeping Orchard", .455f, true, 980,
              "Everything here is asleep, including the things that were meant to be guarding it.",
              "<b>+240</b> max Ward");
            L("bell", "Bellcast", .61f, true, 1450,
              "A town built around a bell nobody will ring. You understand why by the time you leave.",
              "unlocked <b>The Chorus</b>");
            L("mile", "The Sundered Mile", .765f, false, 1800,
              "A mile of road that goes on for longer than a mile, and knows that it does.",
              "unlocks <b>Hollow Riders</b>");
            L("ninth", "The Ninth Bell", .90f, false, 3400,
              "You have heard it eight times. Each time you were somewhere you should not have been.",
              "Assault costs <b>−15%</b>", costScale: 0.85);

            string sec = "Levied";
            void U(string k, string n, double p, int c, string bl, List<Amount> cost)
                => s.units.Add(new UnitDef { k = k, n = n, p = p, c = c, bl = bl, cost = cost, sec = sec });

            U("guard", "Waking Guard", 12, 48,
              "People who have been awake a long time and have stopped minding.",
              Cost(A("whispers", 60), A("silt", 30)));
            U("bearer", "Lantern Bearers", 34, 16,
              "They carry the light and are therefore the first thing anything sees.",
              Cost(A("silt", 420), A("tallow", 24)));
            sec = "Sworn";
            U("chorus", "The Chorus", 95, 6,
              "One voice would be a man singing. Six is something else entirely.",
              Cost(A("chorus", 6), A("lucidity", 40)));
            U("sentinel", "Salt Sentinels", 240, 0,
              "Set down at the edge of a thing to mark that the thing has an edge.",
              Cost(A("salt", 260), A("ward", 6)));

            // What the next battle hands over.
            s.unitsUnlockedBy["mile"] = new UnitDef
            {
                k = "rider", n = "Hollow Riders", p = 520, c = 0, sec = "Sworn", isNew = true,
                bl = "Nobody is in the saddle. Something is, and it rides beautifully.",
                cost = Cost(A("silt", 900), A("vellum", 40))
            };
        }

        // ---- Journal -----------------------------------------------------------
        static void FillJournal(Dream s)
        {
            void J(int d, int ch, string src, bool great, string n, bool seen, params string[] p)
                => s.journal.Add(new JournalEntry
                {
                    d = d, ch = ch, src = src, great = great, n = n, seen = seen,
                    p = new List<string>(p)
                });

            J(412, 5, "assault", true, "The Sundered Mile", false,
              "The road went on for longer than a road should, and it knew that it did.",
              "We walked it anyway, because the alternative was to stand at the near end of it and admit what standing there would mean.");
            J(366, 4, "vision", true, "A Door in the Floor", false,
              "There is a way further down that is not falling, and today I found where it had been put.",
              "It had been put there by somebody. I keep arriving at that sentence from different directions.");
            J(231, 2, "revelation", true, "The Lamps Were Lit", true,
              "Somebody walked here before me. That is the whole of it, and it has changed everything.",
              "The lamps are burnt out, every one. But they were lit once, by somebody who thought it was worth the tallow, and who set each one down carefully before going on.",
              "I had been assuming I was the first. I have been assuming a great many things.");
            J(198, 2, "assault", false, "The Lamplit Gate", true,
              "The last of my own ground. Somebody had kept the lamps burning at the gate, and it took me most of the morning to understand that the somebody was me.",
              "We went out through it at first light. I looked back and the lamps were still going.");
            J(96, 1, "veil", false, "The Silt Shore", false,
              "The floor of the Shallows gave out today. Not dramatically — it thinned, the way ice thins, and then it was not there.",
              "I went down. I am writing this from the bottom of somewhere that has its own bottom, which I am told is how it goes the entire way.");
            J(1, 1, "veil", false, "The Shallows", true,
              "First night. Nothing here but shallow gray water and the sense that it is shallow because something underneath is holding it up.",
              "I have started keeping this because I do not trust myself to remember, and because writing it down is the only thing here that feels like doing rather than being done to.");
        }

        // ---- the veil ----------------------------------------------------------
        static void FillVeil(Dream s)
        {
            s.veil = new VeilDef
            {
                n = "The Silt Shore",
                ord = 2,
                sunk = 412,
                need = 1000,
                spend = Cost(A("reverie", 240), A("lucidity", 3)),
                bring = Cost(A("moonsilver", 18), A("whispers", 4)),
                w = 4,
                cap = 9,
                baseSeconds = 24.0,           // a dive every 6.0s at four Oneiri
                p = 0.58f,
                revealed = 2,
                entry = new List<string>
                {
                    "The floor here is not a floor. It is what the floor has been putting off being for a long while, and it gives under the hand the way a held breath gives.",
                    "There is silt, and under the silt there is more silt, and somewhere under that there is the thing the silt has been keeping covered.",
                    "I have stopped counting the fathoms out loud. It was not helping and it was not true.",
                    "Something down here was lit once. I can see where the light stopped.",
                    "The shore ends. I had not expected a shore this deep to end at all."
                }
            };
        }
    }
}
