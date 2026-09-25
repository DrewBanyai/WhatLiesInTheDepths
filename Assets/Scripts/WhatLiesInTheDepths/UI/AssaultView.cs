// What Lies In The Depths — center destination. Spec section 9.
// One scroll track: the band, the map, the reading band, then the roster. The band becomes
// the battle rather than opening above it, so nothing below moves, and this menu overlays
// nothing.
using System.Collections.Generic;
using System.Linq;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine.Combat;
using Ursine.Economy;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    public sealed class AssaultView : MonoBehaviour
    {
        [Header("Band — at rest, fighting, or telling what happened")]
        public Image bandGround;
        public Image bandBorder;
        public GameObject restRow;
        public TMP_Text yourStrength;
        public TMP_Text fieldsLine;
        public TMP_Text theirStrength;
        public Image verdictGround, verdictBorder;
        public TMP_Text verdictLabel;
        public GameObject fightRow;
        public TMP_Text fightName;
        public ProgressTrack fightBar;
        public TMP_Text fightNumbers;
        public GameObject toldRow;
        public Image toldRing, toldGlyph;
        public TMP_Text toldCaption, toldName, toldTell;

        [Header("Map — 880 x 430")]
        public PathLine roadAhead;
        public PathLine roadWalked;
        public RectTransform home;
        public RectTransform pinLayer;
        public RoadPinView pinPrefab;

        [Header("Reading band — 880 x 152, and it never empties")]
        public Image placeArt;
        public TMP_Text kindCaption, placeName, placeBlurb, benefitCaption, benefitValue;
        public TMP_Text fieldCaption, fieldNumber, yours;
        public GameObject chanceRow;
        public Image chanceGround, chanceBorder;
        public TMP_Text chanceLabel;
        public UiButton giveBattle;
        public Image giveBattleGround;
        public TMP_Text giveBattleLabel;
        public GameObject wonRow;

        [Header("Roster")]
        public RectTransform roster;
        public GameObject rosterHeadPrefab;
        public UnitCardView unitCardPrefab;
        public ScrollRect scroll;

        // The spec's raw values where it does not use a token.
        static readonly Color GoodEdge    = new Color32(0xBF, 0xDF, 0xDA, 0xFF);
        static readonly Color TakenEdge   = new Color32(0xA8, 0xD2, 0xCC, 0xFF);
        static readonly Color FightGround = new Color32(0xFB, 0xF8, 0xFE, 0xFF);
        static readonly Color DisabledInk = new Color32(0xB6, 0xAE, 0xCB, 0xFF);

        readonly List<UnitCardView> _units = new List<UnitCardView>();
        readonly List<RoadPinView> _pins = new List<RoadPinView>();
        readonly System.Random _rng = new System.Random();

        RoadLocation _reading;
        Battle _battle;
        Told _told;
        float _toldUntil;

        sealed class Battle { public RoadLocation place; public double mine; public bool win; public float t0; }
        sealed class Told { public bool win; public RoadLocation place; public string ben; public double before, after; public int lost; }

        const float BattleSeconds = Layout.BattleMs / 1000f;
        const float ToldSeconds = Layout.BattleResultHoldMs / 1000f;

        void Start()
        {
            if (roadAhead != null) roadAhead.SetPath(Road.Path, Road.Space);
            if (roadWalked != null) roadWalked.SetPath(Road.Path, Road.Space);
            if (home != null) home.anchoredPosition = Road.Anchored(0f);
            if (giveBattle != null) giveBattle.Clicked += () => Fight(_reading);

            DrawMap();
            DrawRoster();
            if (GameState.I != null) GameState.I.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= Refresh;
        }

        static double Army() => GameState.I == null ? 0 : GameState.I.Dream.Army;

        /// <summary>Places are taken in order; only the next one on the road can be entered.</summary>
        static RoadLocation Next() => GameState.I?.road.FirstOrDefault(l => !l.won);

        static int Pct(double x) => (int)System.Math.Round(x * 100.0);

        // good above 55, bad below 45, even between, none at zero.
        enum Verdict { None, Bad, Even, Good }
        static Verdict Judge(double c) => c <= 0 ? Verdict.None : c > 0.55 ? Verdict.Good : c < 0.45 ? Verdict.Bad : Verdict.Even;

        static void PaintVerdict(Verdict v, Image ground, Image border, TMP_Text label)
        {
            Color g, b; Tok ink;
            switch (v)
            {
                case Verdict.Good: g = Theme.Get(Tok.TealL); b = GoodEdge; ink = Tok.TealD; break;
                case Verdict.Even: g = Theme.Get(Tok.Wait); b = Theme.Get(Tok.Haze); ink = Tok.Ink2; break;
                case Verdict.Bad: g = Theme.Get(Tok.RoseT); b = Theme.Get(Tok.RoseB); ink = Tok.RoseD; break;
                default: g = Theme.Get(Tok.RoseL); b = Theme.Get(Tok.RoseB); ink = Tok.RoseD; break;
            }
            if (ground != null) ground.color = g;
            if (border != null) border.color = b;
            if (label != null)
            {
                label.color = Theme.Get(ink);
                label.fontStyle = v == Verdict.None ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // ---- the band ----------------------------------------------------------

        void DrawBand()
        {
            bool fighting = _battle != null, telling = !fighting && _told != null;
            if (restRow != null) restRow.SetActive(!fighting && !telling);
            if (fightRow != null) fightRow.SetActive(fighting);
            if (toldRow != null) toldRow.SetActive(telling);

            if (fighting)
            {
                if (bandGround != null) bandGround.color = FightGround;
                if (bandBorder != null) bandBorder.color = Theme.Get(Tok.IrisB);
                if (fightName != null) fightName.text = _battle.place.n;
                if (fightNumbers != null)
                    fightNumbers.text = Strings.T("ui.assault.against", Strong(Fmt.Count(_battle.mine), Tok.Prose), Strong(Fmt.Count(_battle.place.en), Tok.Prose));
                return;
            }
            if (telling)
            {
                bool w = _told.win;
                if (bandGround != null) bandGround.color = Theme.Get(w ? Tok.TealL : Tok.RoseT);
                if (bandBorder != null) bandBorder.color = w ? GoodEdge : Theme.Get(Tok.RoseB);
                if (toldRing != null) toldRing.color = w ? TakenEdge : Theme.Get(Tok.RoseB);
                if (toldGlyph != null) { Art.Apply(toldGlyph, Art.Ui(w ? "check" : "cross")); toldGlyph.color = Theme.Get(w ? Tok.TealD : Tok.RoseD); }
                if (toldCaption != null) { toldCaption.text = Strings.T(w ? "ui.assault.taken" : "ui.assault.drivenBack").ToUpperInvariant(); toldCaption.color = Theme.Get(w ? Tok.TealD : Tok.RoseD); }
                if (toldName != null) toldName.text = _told.place.n;
                if (toldTell != null)
                    toldTell.text = w
                        ? TealBold(_told.ben)
                        : Strings.T("ui.assault.lost", Strong(_told.lost + "%", Tok.RoseD),
                                    Strong(Fmt.Count(_told.before), Tok.RoseD), Strong(Fmt.Count(_told.after), Tok.RoseD));
                return;
            }

            if (bandGround != null) bandGround.color = Theme.Get(Tok.Block);
            if (bandBorder != null) bandBorder.color = Theme.Get(Tok.Haze2);
            double a = Army();
            var next = Next();
            if (yourStrength != null) yourStrength.text = Fmt.Count(a);
            bool any = next != null;
            if (fieldsLine != null) { fieldsLine.gameObject.SetActive(any); if (any) fieldsLine.text = Strings.T("ui.assault.fields", next.n); }
            if (theirStrength != null) { theirStrength.gameObject.SetActive(any); if (any) theirStrength.text = Fmt.Count(next.en); }
            if (verdictLabel != null)
            {
                verdictLabel.transform.parent.gameObject.SetActive(any);
                if (any)
                {
                    double c = Odds.Chance(a, next.en);
                    var v = Judge(c);
                    verdictLabel.text = v == Verdict.None ? Strings.T("ui.assault.noChance") : Strings.T("ui.assault.toTake", "<b>" + Pct(c) + "%</b> ");
                    PaintVerdict(v, verdictGround, verdictBorder, verdictLabel);
                }
            }
        }

        static string Strong(string s, Tok t) => "<b><color=#" + ColorUtility.ToHtmlStringRGB(Theme.Get(t)) + ">" + s + "</color></b>";
        static string TealBold(string s)
        {
            string teal = "#" + ColorUtility.ToHtmlStringRGB(Theme.Get(Tok.TealD));
            return (s ?? "").Replace("<b>", "<b><color=" + teal + ">").Replace("</b>", "</color></b>");
        }

        // ---- the map -----------------------------------------------------------

        void DrawMap()
        {
            var s = GameState.I;
            if (s == null) return;
            foreach (var p in _pins) if (p != null) Destroy(p.gameObject);
            _pins.Clear();

            // Taken places, plus the one being faced. The rest is road and weather.
            int shown = 0;
            for (int i = 0; i < s.road.Count; i++) if (s.road[i].won) shown = i + 1;
            shown = Mathf.Min(s.road.Count, shown + 1);

            var next = Next();
            float cut = next != null ? next.t : 1f;
            if (roadWalked != null) roadWalked.SetRange(0f, cut);

            if (pinLayer != null && pinPrefab != null)
                for (int i = 0; i < shown; i++)
                {
                    var pin = Instantiate(pinPrefab, pinLayer);
                    pin.name = "Pin " + s.road[i].k;
                    pin.Bind(s.road[i]);
                    pin.Rested += p => Read(p.Place);
                    _pins.Add(pin);
                }
            MarkPins();
        }

        void MarkPins()
        {
            foreach (var p in _pins)
            {
                p.SetOn(p.Place == _reading);
                p.SetFighting(_battle != null && p.Place == _battle.place);
            }
        }

        void Read(RoadLocation place)
        {
            _reading = place;
            DrawSlot();
            MarkPins();
        }

        // ---- the reading band --------------------------------------------------

        void DrawSlot()
        {
            var s = GameState.I;
            if (s == null || s.road.Count == 0) return;
            var place = _reading ?? Next() ?? s.road[s.road.Count - 1];
            _reading = place;
            double a = Army(), c = Odds.Chance(a, place.en);
            bool isNext = place == Next(), won = place.won;

            Art.Apply(placeArt, Art.PlaceArt(place.k));
            if (kindCaption != null) { kindCaption.text = Strings.T(won ? "ui.assault.taken" : "ui.assault.next").ToUpperInvariant(); kindCaption.color = Theme.Get(won ? Tok.TealD : Tok.IrisD); }
            if (placeName != null) placeName.text = place.n;
            if (placeBlurb != null) placeBlurb.text = place.bl;
            if (benefitCaption != null) benefitCaption.text = Strings.T(won ? "ui.assault.gave" : "ui.assault.whenWon").ToUpperInvariant();
            if (benefitValue != null) benefitValue.text = TealBold(place.ben);

            if (fieldCaption != null) fieldCaption.text = Strings.T(won ? "ui.assault.fielded" : "ui.assault.field").ToUpperInvariant();
            if (fieldNumber != null) fieldNumber.text = Fmt.Count(place.en);
            string mine = "<b><color=#" + ColorUtility.ToHtmlStringRGB(Theme.Get(Tok.Ink)) + ">";
            if (yours != null)
                yours.text = won
                    ? Strings.T("ui.assault.takenWith", mine + Fmt.Count(place.tookWith > 0 ? place.tookWith : place.en) + "</color></b>")
                    : Strings.T("ui.assault.youBring", mine + Fmt.Count(a) + "</color></b>");

            if (chanceRow != null) chanceRow.SetActive(!won);
            if (!won && chanceLabel != null)
            {
                var v = Judge(c);
                chanceLabel.text = v == Verdict.None ? Strings.T("ui.assault.noChanceHere") : Strings.T("ui.assault.toTake", "<b>" + Pct(c) + "%</b>");
                PaintVerdict(v, chanceGround, chanceBorder, chanceLabel);
            }

            if (wonRow != null) wonRow.SetActive(won);
            if (giveBattle != null)
            {
                giveBattle.gameObject.SetActive(!won);
                bool live = isNext && c > 0 && _battle == null && s.Open(place);
                giveBattle.SetInteractable(live);
                if (giveBattleGround != null) giveBattleGround.color = Theme.Get(live ? Tok.Iris : Tok.Track);
                if (giveBattleLabel != null)
                {
                    giveBattleLabel.text = Strings.T(_battle != null && _battle.place == place ? "ui.assault.underWay" : "ui.assault.giveBattle");
                    giveBattleLabel.color = live ? Theme.Get(Tok.Veil) : DisabledInk;
                }
            }
        }

        // ---- battle ------------------------------------------------------------

        void Fight(RoadLocation place)
        {
            if (_battle != null || place == null || place.won || place != Next()) return;
            if (GameState.I == null || !GameState.I.Open(place)) return;
            double a = Army(), c = Odds.Chance(a, place.en);
            if (c <= 0) return;
            // The roll is taken here, at the press. The ten seconds are the march, not the dice.
            _battle = new Battle { place = place, mine = a, win = _rng.NextDouble() < c, t0 = Time.unscaledTime };
            _told = null;
            Refresh();
        }

        void Update()
        {
            if (_battle != null)
            {
                float u = Mathf.Clamp01((Time.unscaledTime - _battle.t0) / BattleSeconds);
                if (fightBar != null)
                {
                    fightBar.width = ((RectTransform)fightBar.transform).rect.width;
                    fightBar.Set(u);
                }
                if (u >= 1f) Resolve();
            }
            else if (_told != null && Time.unscaledTime >= _toldUntil)
            {
                _told = null;
                DrawBand();
            }
        }

        void Resolve()
        {
            var b = _battle;
            _battle = null;
            var s = GameState.I;
            if (b.win)
            {
                // Everything winning does — the discount, the unit it hands over, its grants —
                // is the dream's to do, so the debugger's "Take it" does exactly the same.
                s.TakePlace(b.place, b.mine);
                _told = new Told { win = true, place = b.place, ben = b.place.ben };
                _reading = Next() ?? b.place;
            }
            else
            {
                // A loss costs a quarter to nearly half the army; the dream takes it.
                var loss = s.Dream.LoseBattle(b.place, _rng);
                _told = new Told { win = false, place = b.place, before = loss.before, after = loss.after, lost = loss.pct };
                _reading = b.place;
            }
            _toldUntil = Time.unscaledTime + ToldSeconds;
            DrawMap();
            DrawRoster();
            s.Dirty();
        }

        // ---- the roster --------------------------------------------------------

        void DrawRoster()
        {
            var s = GameState.I;
            if (s == null || roster == null || unitCardPrefab == null) return;
            _roster = RosterSignature();
            foreach (Transform c in roster) Destroy(c.gameObject);
            _units.Clear();

            var width = PrefabRect.Size(unitCardPrefab).x;
            foreach (var section in s.units.Where(s.Shown).Select(u => u.sec ?? Strings.T("ui.assault.levied")).Distinct())
            {
                if (rosterHeadPrefab != null)
                {
                    var head = Instantiate(rosterHeadPrefab, roster);
                    var t = head.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = section.ToUpperInvariant();
                }
                // Two columns, 16 gutter, each row as tall as the taller card.
                var grid = (RectTransform)new GameObject(section + " grid", typeof(RectTransform)).transform;
                grid.SetParent(roster, false);
                var rows = grid.gameObject.AddComponent<VerticalLayoutGroup>();
                rows.spacing = 16f;
                rows.childControlWidth = rows.childControlHeight = true;
                rows.childForceExpandWidth = rows.childForceExpandHeight = false;
                RectTransform row = null;
                int inRow = 0;
                foreach (var u in s.units.Where(x => s.Shown(x) && (x.sec ?? Strings.T("ui.assault.levied")) == section))
                {
                    if (row == null || inRow == 2)
                    {
                        row = (RectTransform)new GameObject("Row", typeof(RectTransform)).transform;
                        row.SetParent(grid, false);
                        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                        h.spacing = 16f;
                        h.childControlWidth = h.childControlHeight = true;
                        h.childForceExpandWidth = false;
                        h.childForceExpandHeight = true;
                        inRow = 0;
                    }
                    var card = Instantiate(unitCardPrefab, row);
                    var le = card.GetComponent<LayoutElement>();
                    if (le == null) le = card.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = width;
                    card.Bind(u);
                    _units.Add(card);
                    inRow++;
                }
            }
        }

        /// <summary>The roster is built once and rebuilt only when what it holds changes: a unit
        /// appearing, or an upgrade renaming or redrawing one.</summary>
        string _roster;

        static string RosterSignature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var u in s.units) if (s.Shown(u)) b.Append(u.k).Append(',');
            b.Append('|').Append(s.Version);
            return b.ToString();
        }

        void Refresh()
        {
            if (RosterSignature() != _roster) DrawRoster();
            DrawBand();
            DrawSlot();
            MarkPins();
            foreach (var u in _units) u.Refresh();
        }
    }
}
