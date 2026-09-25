// Ursine — a minus/count/cap/plus control for assigning workers to a task.
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
            if (cap != null) cap.text = "/" + Fmt.Count(_cap);
            Refresh();
        }

        void Awake()
        {
            if (minus != null) minus.Clicked += () => Step(-1);
            if (plus != null) plus.Clicked += () => Step(+1);
        }

        void Step(int d)
        {
            int next = Mathf.Clamp(_value + d, 0, _cap);
            if (d > 0 && _free != null && _free() <= 0) return;
            if (next == _value) return;
            _value = next;
            if (count != null) count.text = Fmt.Count(_value);
            Refresh();
            Changed?.Invoke(_value);
        }

        void Refresh()
        {
            bool canMinus = _value > 0;
            bool canPlus = _value < _cap && (_free == null || _free() > 0);
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
            if (_value >= _cap) return Say(fullMessageKey, fullMessage);
            if (_free != null && _free() <= 0) return Say(emptyPoolMessageKey, emptyPoolMessage);
            return null;
        }
    }
}
