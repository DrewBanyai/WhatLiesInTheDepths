// Ursine — the tactile part of a button: a light wash under the pointer, and a shrink about its
// center for exactly as long as it is held down.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Ursine.Theming;

namespace Ursine.UI
{
    /// <summary>Gives a <see cref="UiButton"/> some feel. While the pointer rests on it (and only
    /// while it is interactable, which is when UiButton reports hover at all) a wash fades in over
    /// its ground; its size never changes on hover. Pressing an interactable button shrinks it at
    /// once, in from every side about its own center whatever its pivot, and it stays shrunk only
    /// while the mouse button is down; letting go restores it the same frame. No easing either
    /// way: the press is the pointer, not an animation of it. Only scale, a position offset held
    /// for the length of a press, and one overlay are touched, so a view that paints the button's
    /// own colors keeps doing so undisturbed.</summary>
    public sealed class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("The button whose hover and press drive this. Empty: the one on this object.")]
        public UiButton button;
        [Tooltip("What grows and shrinks. Empty: this object.")]
        public RectTransform target;
        [Tooltip("The ground the wash copies its shape from. Empty: the button's own image.")]
        public Image washShape;
        [Tooltip("The wash laid over the ground on hover; alpha is its full strength. Clear for none.")]
        public Color washColor = new Color(1f, 1f, 1f, 0.14f);
        [Tooltip("A palette token to wash with instead, so the wash follows a theme change; -1 uses washColor. Its strength is washColor's alpha.")]
        public int washToken = -1;

        public float pressScale = 0.93f;
        public float hoverSeconds = 0.10f;

        Image _wash;
        bool _hover, _held;
        float _lift;            // 0..1, eased toward hover
        Vector3 _rest;          // the target's position at rest, held while a press plays
        bool _resting = true;

        void Awake()
        {
            if (button == null) button = GetComponent<UiButton>();
            if (target == null) target = (RectTransform)transform;
            if (button != null) button.Hovered += h => _hover = h;
            MakeWash();
        }

        void MakeWash()
        {
            if (washColor.a <= 0f) return;
            var shape = washShape != null ? washShape : button != null ? button.GetComponent<Image>() : null;
            if (shape == null) return;
            var go = new GameObject("FeelWash", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(shape.transform, false);
            rt.SetAsFirstSibling();                     // over the ground, under its label and border
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var le = go.AddComponent<LayoutElement>(); le.ignoreLayout = true;
            _wash = go.GetComponent<Image>();
            _wash.sprite = shape.sprite;
            _wash.type = shape.type;
            _wash.pixelsPerUnitMultiplier = shape.pixelsPerUnitMultiplier;
            _wash.raycastTarget = false;
            _wash.color = new Color(washColor.r, washColor.g, washColor.b, 0f);
        }

        bool Live => button == null || (button.interactable && button.isActiveAndEnabled);

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !Live) return;
            _held = true;
            Squeeze(pressScale);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            _held = false;
            Squeeze(1f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool hover = _hover && Live;
            _lift = Mathf.MoveTowards(_lift, hover ? 1f : 0f, dt / Mathf.Max(0.01f, hoverSeconds));

            // A button that stops being able to act while it is held down (the price moved out of
            // reach under the pointer) springs back at once rather than waiting for the release.
            if (_held && !Live) { _held = false; Squeeze(1f); }

            float lift = Mathf.SmoothStep(0f, 1f, _lift);
            if (_wash != null)
            {
                var c = washToken >= 0 ? Theme.Get(washToken) : washColor; c.a = washColor.a * lift;
                if (!Mathf.Approximately(_wash.color.a, c.a)) _wash.color = c;
            }
        }

        /// <summary>Scales the target by <paramref name="s"/> about its center. The pivot may sit
        /// anywhere (these buttons are placed from their top-left), so the position is offset by
        /// the part of the size that the scale removes: the center stays where it was.</summary>
        void Squeeze(float s)
        {
            if (target == null) return;
            bool atRest = Mathf.Approximately(s, 1f);
            if (atRest)
            {
                if (!_resting) { target.localScale = Vector3.one; target.localPosition = _rest; _resting = true; }
                return;
            }
            if (_resting) { _rest = target.localPosition; _resting = false; }
            var r = target.rect;
            var toCenter = new Vector2((0.5f - target.pivot.x) * r.width, (0.5f - target.pivot.y) * r.height);
            target.localScale = new Vector3(s, s, 1f);
            target.localPosition = _rest + (Vector3)(toCenter * (1f - s));
        }

        void OnDisable()
        {
            _hover = _held = false; _lift = 0f;
            Squeeze(1f);
            if (_wash != null) { var c = washColor; c.a = 0f; _wash.color = c; }
        }
    }
}
