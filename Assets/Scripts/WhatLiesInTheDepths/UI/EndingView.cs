// What Lies In The Depths — the ending. Spec section 12.
// The one surface that is not a column, and the only place anything ignores the
// three-column budget. Nothing is counted: no time played, no veils parted, no battles won.
//
// It comes in two parts. First the art and the story alone, with a single down arrow where the
// answers will be. Pressing it takes the page down to the credits and the thanks, and only then
// do Hard reset, Continue and Exit arrive (Exit not on the web), fading in together over a second. The page cannot be
// scrolled by hand until it has been taken down once; after that it can, to read the story again.
using System.Collections;
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

        [Header("The two parts")]
        public ScrollRect scroll;
        public UiButton more;
        public Image moreBorder;
        public Image moreArrow;
        [Tooltip("The three answers, faded in as one once the page has been taken down.")]
        public CanvasGroup choices;
        public float scrollSeconds = 1.4f;
        public float choicesSeconds = 1f;

        [Header("Thanks")]
        public LayoutElement thanks;
        public RectTransform thanksPanel;
        public TMP_Text thanksTitle;
        public TMP_Text thanksBody;

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
            PartOne();
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
            if (credits != null)
                foreach (var t in credits.GetComponentsInChildren<TMP_Text>(true))
                    if (t.name.StartsWith("Name")) t.text = Strings.T("ui.ending.creditName");
            FillThanks();

            if (more != null)
            {
                more.Clicked += TakeDown;
                more.Hovered += h => { if (moreBorder != null) moreBorder.color = Theme.Get(h ? Tok.Iris : Tok.IrisB); };
            }
            if (title != null && string.IsNullOrEmpty(title.text)) title.text = Strings.T("ui.ending.title");

            if (hardResetLabel != null) hardResetLabel.text = Strings.T("ui.ending.hardReset");
            if (contLabel != null) contLabel.text = Strings.T("ui.ending.continue");
            if (exitLabel != null) exitLabel.text = Strings.T("ui.ending.exit");

            if (contGround != null) contGround.color = Theme.Get(Tok.Iris);
            if (contLabel != null) contLabel.color = Theme.Get(Tok.OnIris);
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

            PartOne();
            if (Application.platform == RuntimePlatform.WebGLPlayer) DropExit();

            if (exit != null)
                exit.Hovered += h =>
                {
                    // Turns rose under the pointer, exactly as it does in the right bar.
                    if (exitLabel != null) exitLabel.color = Theme.Get(h ? Tok.RoseD : Tok.Ink2);
                    if (exitBorder != null) exitBorder.color = Theme.Get(h ? Tok.RoseB : Tok.Haze);
                };
        }

        /// <summary>A web page cannot close itself, so on the web the ending offers only Hard
        /// reset and Continue. The pair is recentred: the row loses Exit's 200 and the 14 gap
        /// before it, so everything left of it moves right by half of that.</summary>
        void DropExit()
        {
            if (exit == null || !exit.gameObject.activeSelf) return;
            var gone = (RectTransform)exit.transform;
            float shift = 0f;
            var cr = cont != null ? (RectTransform)cont.transform : null;
            if (cr != null)
                shift = (gone.anchoredPosition.x + gone.sizeDelta.x - (cr.anchoredPosition.x + cr.sizeDelta.x)) * 0.5f;
            exit.gameObject.SetActive(false);
            if (cr != null) cr.anchoredPosition += new Vector2(shift, 0f);
            if (hardReset != null) ((RectTransform)hardReset.transform).anchoredPosition += new Vector2(shift, 0f);
        }

        Coroutine _going;

        /// <summary>The first part: the page at the top and held there, the arrow up, the
        /// answers away.</summary>
        void PartOne()
        {
            if (_going != null) { StopCoroutine(_going); _going = null; }
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.vertical = false;
                scroll.verticalNormalizedPosition = 1f;
            }
            if (more != null) more.gameObject.SetActive(true);
            SetMore(1f);
            SetChoices(0f);
        }

        void TakeDown()
        {
            if (_going != null) return;
            _going = StartCoroutine(TakeDownRoutine());
        }

        /// <summary>The arrow goes, the page glides to the foot, then the answers fade in.</summary>
        IEnumerator TakeDownRoutine()
        {
            if (more != null) more.SetInteractable(false);
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                SetMore(1f - t / 0.25f);
                yield return null;
            }
            SetMore(0f);
            if (more != null) more.gameObject.SetActive(false);

            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                float from = scroll.verticalNormalizedPosition;
                for (float t = 0f; t < scrollSeconds; t += Time.unscaledDeltaTime)
                {
                    scroll.verticalNormalizedPosition = Mathf.Lerp(from, 0f, Mathf.SmoothStep(0f, 1f, t / scrollSeconds));
                    yield return null;
                }
                scroll.verticalNormalizedPosition = 0f;
                scroll.vertical = true;          // from here the story can be scrolled back to
            }

            for (float t = 0f; t < choicesSeconds; t += Time.unscaledDeltaTime)
            {
                SetChoices(Mathf.SmoothStep(0f, 1f, t / choicesSeconds));
                yield return null;
            }
            SetChoices(1f);
            _going = null;
        }

        void SetMore(float a)
        {
            if (moreBorder != null) { var c = moreBorder.color; c.a = a; moreBorder.color = c; }
            if (moreArrow != null) { var c = moreArrow.color; c.a = a; moreArrow.color = c; }
            if (a >= 1f && more != null) more.SetInteractable(true);
        }

        void SetChoices(float a)
        {
            if (choices == null) return;
            choices.alpha = a;
            // Pressable only once they have fully arrived.
            choices.interactable = a >= 1f;
            choices.blocksRaycasts = a >= 1f;
        }

        /// <summary>The thanks panel: its title and body from Strings.json, and the panel as
        /// tall as the body needs.</summary>
        void FillThanks()
        {
            if (thanksTitle != null) thanksTitle.text = Strings.T("ui.ending.thanks.title");
            if (thanksBody == null) return;
            thanksBody.text = Strings.T("ui.ending.thanks.body");
            var rt = thanksBody.rectTransform;
            float h = Mathf.Ceil(thanksBody.GetPreferredValues(thanksBody.text, rt.rect.width, 0f).y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            float panelH = -rt.anchoredPosition.y + h + 24f;
            if (thanksPanel != null) thanksPanel.sizeDelta = new Vector2(thanksPanel.sizeDelta.x, panelH);
            if (thanks != null) thanks.preferredHeight = panelH + 20f;
        }
    }
}
