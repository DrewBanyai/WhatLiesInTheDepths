// What Lies In The Depths — the Exit and Hard reset questions. Spec section 10.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Palette = Ursine.Theming.Palette;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>The Exit question and the Hard reset question. Both appear in the center
    /// column like any other panel — neither is a window.</summary>
    public sealed class QuestionView : MonoBehaviour
    {
        public enum Kind { Exit, HardReset }

        public Kind kind = Kind.Exit;
        public Image frame;
        public Image mark;
        public TMP_Text title;
        public TMP_Text body;
        public TMP_Text saveNote;

        [Header("Answers — No sits first, because it is the safe answer and the cheap one")]
        public UiButton no;
        public Image noGround;
        public TMP_Text noLabel;
        public UiButton yes;
        public Image yesGround;
        public Image yesBorder;
        public TMP_Text yesLabel;

        void Start()
        {
            bool reset = kind == Kind.HardReset;

            string q = reset ? "ui.question.reset." : "ui.question.exit.";
            if (title != null) title.text = Strings.T(q + "title");
            if (body != null) body.text = Strings.T(q + "body");

            if (noLabel != null) noLabel.text = Strings.T(q + "no");
            if (yesLabel != null) yesLabel.text = Strings.T(q + "yes");

            if (noGround != null) noGround.color = Theme.Get(Tok.Iris);
            if (noLabel != null) noLabel.color = Theme.Get(Tok.Veil);

            // Yes, leave is outlined in rose; Yes, clear it all is filled rose — the only
            // filled rose anywhere in the interface, reserved so that seeing it is the warning.
            if (yesGround != null) yesGround.color = reset ? Theme.Get(Tok.Rose) : Theme.Get(Tok.Rose, 0f);
            if (yesBorder != null) yesBorder.color = Theme.Get(Tok.RoseB);
            if (yesLabel != null) yesLabel.color = reset ? Theme.Get(Tok.Veil) : Theme.Get(Tok.RoseD);
            if (frame != null) frame.color = Theme.Get(reset ? Tok.RoseB : Tok.Haze);
            if (mark != null) mark.color = Theme.Get(reset ? Tok.RoseD : Tok.IrisD);

            if (no != null)
                no.Clicked += () =>
                {
                    // No is a navigation, not a dismissal.
                    if (reset) Router.I?.BackToOptions();
                    else Router.I?.BackToGame();
                };

            if (yes != null)
                yes.Clicked += () =>
                {
                    // What happens on Yes is undecided in the spec set — section 9 of the
                    // brief lists "leaving and resetting" as an open question, so this is
                    // deliberately left as the one hook and nothing more.
                    Debug.Log(reset
                        ? "[What Lies In The Depths] Hard reset confirmed. Behavior is not specified — see brief section 9."
                        : "[What Lies In The Depths] Exit confirmed. Behavior is not specified — see brief section 9.");
                };
        }

        void Update()
        {
            if (kind != Kind.Exit || saveNote == null || SaveClock.I == null) return;
            // The Exit question does not offer to save. The useful thing to say is how much
            // has accumulated since the dream last saved itself.
            saveNote.text = Strings.T("ui.question.saveNote", Fmt.Seconds(SaveClock.I.Staleness));
        }
    }
}
