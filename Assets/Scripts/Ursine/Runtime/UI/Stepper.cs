// Ursine — a minus/count/cap/plus control for assigning workers to a task.
//
// A capacity of 0 means no cap: the plus is limited only by the free pool, and the "/cap"
// figure is left blank. Holding either button repeats it, faster the longer it is held
// (PressAndHold), so a large pool can be moved without a click per worker.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Ursine.Economy;
using Ursine.Theming;

namespace Ursine.UI
{
    /// <summary>A minus/count/cap/plus control for assigning workers. The plus can die for
    /// two different reasons — this one is full, or the pool is empty — and BindingLimit
    /// names which, so a card can say it in its own space rather than in a tooltip.</summary>
    public sealed class Stepper : MonoBehaviour
    {
        public UiButton minus;
        public UiButton plus;
        public Image minusGround;
        public Image plusGround;
        public TMP_Text count;
        public TMP_Text cap;

        [Header("Tokens")]
        public int liveGroundToken;
        public int deadGroundToken;

        [Header("Copy")]
        public string fullMessage = "at capacity";
        public string emptyPoolMessage = "none free";
        [Tooltip("Strings-file keys for the two messages. Used when set; the plain text above otherwise.")]
        public string fullMessageKey = "ursine.stepper.full";
        public string emptyPoolMessageKey = "ursine.stepper.empty";

        public event Action<int> Changed;

        int _value, _cap;
        Func<int> _free;

        /// <summary>Supplies how many workers are unbound, so the plus can know.</summary>
        public void Configure(Func<int> freeWorkers) => _free = freeWorkers;

        public void Set(int value, int capacity)
        {
            _value = value;
            _cap = capacity;
            if (count != null) count.text = Fmt.Count(_value);
            if (cap != null) cap.text = Capped ? "/" + Fmt.Count(_cap) : string.Empty;
            Refresh();
        }

        bool Capped => _cap > 0;

        void Awake()
        {
            Hook(minus, -1);
            Hook(plus, +1);
        }

        // A click steps once; a hold repeats. The click that ends a held press is not counted
        // again on top of its repeats.
        void Hook(UiButton b, int sign)
        {
            if (b == null) return;
            var hold = b.GetComponent<PressAndHold>() ?? b.gameObject.AddComponent<PressAndHold>();
            b.Clicked += () => { if (!hold.Repeated) Step(sign); };
            hold.Repeat += n => { if (b.interactable) Step(sign * n); };
        }

        void Step(int d)
        {
            if (d > 0 && _free != null)
            {
                int free = _free();
                if (free <= 0) return;
                d = Mathf.Min(d, free);
            }
            int next = Mathf.Max(0, _value + d);
            if (Capped) next = Mathf.Min(next, _cap);
            if (next == _value) return;
            _value = next;
            if (count != null) count.text = Fmt.Count(_value);
            Refresh();
            Changed?.Invoke(_value);
        }

        void Refresh()
        {
            bool canMinus = _value > 0;
            bool canPlus = (!Capped || _value < _cap) && (_free == null || _free() > 0);
            if (minus != null) minus.SetInteractable(canMinus);
            if (plus != null) plus.SetInteractable(canPlus);
            if (minusGround != null) minusGround.color = Theme.Get(canMinus ? liveGroundToken : deadGroundToken);
            if (plusGround != null) plusGround.color = Theme.Get(canPlus ? liveGroundToken : deadGroundToken);
        }

        static string Say(string key, string plain)
            => !string.IsNullOrEmpty(key) && Ursine.Text.Loc.Has(key) ? Ursine.Text.Loc.T(key) : plain;

        /// <summary>Which limit binds first, or null when neither does.</summary>
        public string BindingLimit()
        {
            if (Capped && _value >= _cap) return Say(fullMessageKey, fullMessage);
            if (_free != null && _free() <= 0) return Say(emptyPoolMessageKey, emptyPoolMessage);
            return null;
        }
    }
}
