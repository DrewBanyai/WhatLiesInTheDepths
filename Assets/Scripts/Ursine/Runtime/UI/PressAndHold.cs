// Ursine — holding a button down to repeat it, faster the longer it is held.
//
// A click is still a click (the UiButton beside it reports it). Held past a short delay, this
// reports repeats instead, each worth more than the last: one at a time at first, then fives,
// then twenty-fives. So a stepper can move one worker exactly, or hundreds in a couple of
// seconds, with nothing to learn but "hold it".
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ursine.UI
{
    public sealed class PressAndHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Seconds held before the first repeat.")]
        public float delay = 0.4f;
        [Tooltip("Seconds between repeats.")]
        public float interval = 0.08f;

        /// <summary>A repeat, and how many steps it is worth.</summary>
        public event Action<int> Repeat;

        /// <summary>True from the first repeat of a press until the next press begins, so the
        /// click that ends a held press can be told apart from a plain click.</summary>
        public bool Repeated { get; private set; }

        bool _down;
        float _heldFor, _next;

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            _down = true;
            Repeated = false;
            _heldFor = 0f;
            _next = delay;
        }

        public void OnPointerUp(PointerEventData e) => _down = false;
        public void OnPointerExit(PointerEventData e) => _down = false;
        void OnDisable() => _down = false;

        void Update()
        {
            if (!_down) return;
            _heldFor += Time.unscaledDeltaTime;
            while (_down && _heldFor >= _next)
            {
                _next += interval;
                Repeated = true;
                // One at a time for the first second of repeating, then fives, then twenty-fives.
                float repeating = _heldFor - delay;
                int step = repeating < 1f ? 1 : repeating < 2.5f ? 5 : 25;
                Repeat?.Invoke(step);
            }
        }
    }
}
