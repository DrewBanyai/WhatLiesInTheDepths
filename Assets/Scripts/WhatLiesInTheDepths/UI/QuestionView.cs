// What Lies In The Depths — the Exit and Hard reset questions. Options spec, sections 3 and 4.
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>The Exit question and the Hard reset question. Both appear in the center
    /// column like any other panel — neither is a window. They are one shape: the Hard reset
    /// question is that shape made grave, in rose.</summary>
    public sealed class QuestionView : MonoBehaviour
    {
        public enum Kind { Exit, HardReset }

        public Kind kind = Kind.Exit;
        public RectTransform card;
        public Image frame;
        public Image shadow;
        public Image markGround;
        public Image mark;
        public TMP_Text kicker;
        public TMP_Text title;
        public TMP_Text body;
        public RectTransform noteRoot;
        public TMP_Text saveNote;
        public RectTransform answers;

        [Header("Answers — No sits first, because it is the safe answer and the cheap one")]
        public UiButton no;
        public Image noGround;
        public TMP_Text noLabel;
        public UiButton yes;
        public Image yesGround;
        public Image yesBorder;
        public TMP_Text yesLabel;

        // The spec's hover values, which are not tokens: iris and rose-d a step darker.
        static readonly Color StayHover = new Color32(0x8E, 0x76, 0xC2, 0xFF);
        static readonly Color WipeHover = new Color32(0x8E, 0x40, 0x58, 0xFF);

        const float PadTop = 26f, PadSide = 28f, PadBottom = 24f, Gap = 13f;

        bool Reset => kind == Kind.HardReset;
        string Q => Reset ? "ui.question.reset." : "ui.question.exit.";

        bool _noHover, _yesHover, _repaint, _relayout;
        string _lastNote;

        void Start()
        {
            if (kicker != null) kicker.text = Strings.T(Q + "kicker").ToUpperInvariant();
            if (title != null) title.text = Strings.T(Q + "title");
            if (body != null) body.text = Strings.T(Q + "body");
            if (noLabel != null) noLabel.text = Strings.T(Q + "no");
            if (yesLabel != null) yesLabel.text = Strings.T(Q + "yes");
            WriteNote();

            if (no != null)
            {
                no.Hovered += h => { _noHover = h; Paint(); };
                no.Clicked += () =>
                {
                    // No is a navigation, not a dismissal.
                    if (Reset) Router.I?.BackToOptions();
                    else Router.I?.BackToGame();
                };
            }

            if (yes != null)
            {
                yes.Hovered += h => { _yesHover = h; Paint(); };
                yes.Clicked += () =>
                {
                    // A hard reset forgets the save and starts the dream over; Exit saves and leaves.
                    if (Reset) Data.GameState.I?.HardReset();
                    else Data.GameState.I?.SaveAndQuit();
                };
            }

            Paint();
            Layout();
        }

        // A palette or contrast change sends every bound graphic back to its token; the
        // question's own colors are drawn again after it, once everything else has taken it.
        void OnEnable() { Theme.Changed += AskRepaint; _repaint = true; _relayout = true; }
        void OnDisable() => Theme.Changed -= AskRepaint;
        void AskRepaint() => _repaint = true;

        void Update()
        {
            // The Exit question does not offer to save. The useful thing to say is how much has
            // accumulated since the dream last saved.
            if (!Reset && WriteNote()) _relayout = true;
        }

        void LateUpdate()
        {
            if (_repaint) { _repaint = false; Paint(); }
            if (_relayout) { _relayout = false; Layout(); }
        }

        /// <summary>Writes the save line. Returns true when it changed.</summary>
        bool WriteNote()
        {
            if (saveNote == null) return false;
            string s;
            if (Reset) s = Strings.T("ui.question.reset.save");
            else
            {
                double since = SaveClock.I != null ? SaveClock.I.Staleness : 0;
                double every = SaveClock.I != null ? SaveClock.I.intervalSeconds : 120;
                s = Strings.T("ui.question.exit.save", Fmt.Seconds(since), Fmt.Seconds(every));
            }
            if (s == _lastNote) return false;
            _lastNote = s;
            saveNote.text = s;
            return true;
        }

        void Paint()
        {
            bool grave = Reset;
            if (frame != null) frame.color = Theme.Get(grave ? Tok.RoseB : Tok.Haze);
            if (shadow != null) shadow.color = grave ? Theme.Get(Tok.RoseD, 0.22f) : Theme.Get(Tok.Ink, 0.16f);
            if (markGround != null) markGround.color = Theme.Get(grave ? Tok.RoseL : Tok.IrisL);
            if (mark != null) mark.color = Theme.Get(grave ? Tok.RoseD : Tok.IrisD);
            if (kicker != null) kicker.color = Theme.Get(grave ? Tok.RoseD : Tok.IrisD);
            if (title != null) title.color = Theme.Get(Tok.Ink);
            if (body != null) body.color = Theme.Get(Tok.Prose);
            if (saveNote != null) saveNote.color = Theme.Get(Tok.Ink2);

            // Stay / go back: filled iris, first.
            if (noGround != null) noGround.color = _noHover ? StayHover : Theme.Get(Tok.Iris);
            if (noLabel != null) noLabel.color = Color.white;

            if (grave)
            {
                // Clear it all: filled rose-d on white — the only filled rose in the game.
                if (yesGround != null) yesGround.color = _yesHover ? WipeHover : Theme.Get(Tok.RoseD);
                if (yesBorder != null) yesBorder.color = Theme.Get(Tok.RoseB, 0f);
                if (yesLabel != null) yesLabel.color = Color.white;
            }
            else
            {
                // Leave: outlined in rose, the color Exit already hovers to in the right bar.
                if (yesGround != null) yesGround.color = _yesHover ? Theme.Get(Tok.RoseL) : Theme.Get(Tok.RoseL, 0f);
                if (yesBorder != null) yesBorder.color = Theme.Get(Tok.RoseB);
                if (yesLabel != null) yesLabel.color = Theme.Get(Tok.RoseD);
            }
        }

        /// <summary>Stacks the rows top to bottom at their real heights, then fits the card to
        /// them, so the answers always sit 13 under the last line of text.</summary>
        void Layout()
        {
            if (card == null) return;
            float w = card.rect.width - PadSide * 2f;
            float y = PadTop;

            y = Place(markGround != null ? (RectTransform)markGround.transform : null, y, 44f) + Gap;
            y = Place(kicker != null ? kicker.rectTransform : null, y, 10f) + 6f;
            y = Place(title != null ? title.rectTransform : null, y, Height(title, w, 30f)) + Gap;
            y = Place(body != null ? body.rectTransform : null, y, Height(body, w, 23f)) + Gap;
            if (noteRoot != null && saveNote != null)
            {
                float lh = Height(saveNote, w - 24f, 20f);
                saveNote.rectTransform.sizeDelta = new Vector2(w - 24f, lh);
                saveNote.rectTransform.anchoredPosition = new Vector2(12f, -10f);
                y = Place(noteRoot, y, lh + 20f) + Gap;
            }
            y = Place(answers, y + 4f, 40f) + PadBottom;

            card.sizeDelta = new Vector2(card.sizeDelta.x, Mathf.Ceil(y));
        }

        float Place(RectTransform rt, float y, float h)
        {
            if (rt == null) return y;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            return y + h;
        }

        static float Height(TMP_Text t, float width, float min)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) return min;
            return Mathf.Max(min, Mathf.Ceil(t.GetPreferredValues(t.text, width, 0f).y));
        }
    }
}
