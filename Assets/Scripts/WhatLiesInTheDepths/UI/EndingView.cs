// What Lies In The Depths — the ending. Spec section 12.
// The one surface that is not a column, and the only place anything ignores the
// three-column budget. Nothing is counted: no time played, no veils parted, no battles won.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using Ursine.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public sealed class EndingView : MonoBehaviour
    {
        public Image art;
        public TMP_Text smallCapsLine;
        public TMP_Text title;
        public RectTransform message;
        public RectTransform credits;

        [Header("Answers")]
        public UiButton hardReset;
        public Image hardResetBorder;
        public TMP_Text hardResetLabel;
        public UiButton cont;
        public Image contGround;
        public TMP_Text contLabel;
        public UiButton exit;
        public Image exitBorder;
        public TMP_Text exitLabel;

        /// <summary>Which ending is written is read from how the choice went: taking the
        /// nightmares sets ending:bad, leaving them asleep sets ending:good.</summary>
        void OnEnable()
        {
            var s = GameState.I;
            if (s == null) return;
            string key = s.unlocks.Has("ending:bad") ? "ending.bad" : "ending.good";
            if (title != null) title.text = Strings.T(key + ".title");
            if (message == null) return;
            var lines = Loc.Lines(key + ".p");
            var blocks = message.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < blocks.Length; i++)
            {
                bool on = lines != null && i < lines.Count;
                blocks[i].gameObject.SetActive(on);
                if (on) blocks[i].text = lines[i];
            }
        }

        void Start()
        {
            if (smallCapsLine != null) smallCapsLine.text = Strings.T("ui.ending.caps").ToUpperInvariant();
            if (title != null && string.IsNullOrEmpty(title.text)) title.text = Strings.T("ui.ending.title");

            if (hardResetLabel != null) hardResetLabel.text = Strings.T("ui.ending.hardReset");
            if (contLabel != null) contLabel.text = Strings.T("ui.ending.continue");
            if (exitLabel != null) exitLabel.text = Strings.T("ui.ending.exit");

            if (contGround != null) contGround.color = Theme.Get(Tok.Iris);
            if (contLabel != null) contLabel.color = Theme.Get(Tok.Veil);
            if (hardResetBorder != null) hardResetBorder.color = Theme.Get(Tok.RoseB);
            if (hardResetLabel != null) hardResetLabel.color = Theme.Get(Tok.RoseD);
            if (exitBorder != null) exitBorder.color = Theme.Get(Tok.Haze);

            // Continue puts the game back exactly as it was. Beating the game ends the
            // story, not the dream — a Vision left channelling has been channelling the
            // whole time the player was reading.
            if (cont != null) cont.Clicked += () => Router.I?.ShowEnding(false);

            // Exit and Hard reset dismiss the ending first and are then asked in the center
            // column by the two questions section 10 already draws. The spec calls this the
            // weakest decision on the page; it is implemented as specified.
            if (exit != null) exit.Clicked += () => { Router.I?.ShowEnding(false); Router.I?.AskExit(); };
            if (hardReset != null) hardReset.Clicked += () => { Router.I?.ShowEnding(false); Router.I?.AskHardReset(); };

            if (exit != null)
                exit.Hovered += h =>
                {
                    // Turns rose under the pointer, exactly as it does in the right bar.
                    if (exitLabel != null) exitLabel.color = Theme.Get(h ? Tok.RoseD : Tok.Ink2);
                    if (exitBorder != null) exitBorder.color = Theme.Get(h ? Tok.RoseB : Tok.Haze);
                };
        }
    }
}
