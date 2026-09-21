// Ursine — a segmented pill, for choosing between views of one thing.
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
    /// <summary>A segmented pill. Deliberately not the same mark as a tab underline, so a
    /// view of one thing never reads as a different thing.</summary>
    public sealed class SegmentedToggle : MonoBehaviour
    {
        public List<UiButton> segments = new List<UiButton>();
        public List<Image> grounds = new List<Image>();
        public List<TMP_Text> labels = new List<TMP_Text>();

        [Header("Tokens")]
        public int selectedGroundToken;
        public int selectedInkToken;
        public int restInkToken;

        public event Action<int> Selected;

        int _index;
        public int Index => _index;

        void Awake()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                int captured = i;
                if (segments[i] != null) segments[i].Clicked += () => Select(captured);
            }
        }

        void OnEnable() { Theme.Changed += Paint; Paint(); }
        void OnDisable() => Theme.Changed -= Paint;

        public void Select(int i)
        {
            if (i == _index) return;
            _index = i;
            Paint();
            Selected?.Invoke(i);
        }

        public void SetSilently(int i) { _index = i; Paint(); }

        void Paint()
        {
            for (int i = 0; i < grounds.Count; i++)
            {
                bool on = i == _index;
                if (grounds[i] != null)
                    grounds[i].color = Theme.Get(selectedGroundToken, on ? 1f : 0f);
                if (i < labels.Count && labels[i] != null)
                    labels[i].color = Theme.Get(on ? selectedInkToken : restInkToken);
            }
        }
    }
}
