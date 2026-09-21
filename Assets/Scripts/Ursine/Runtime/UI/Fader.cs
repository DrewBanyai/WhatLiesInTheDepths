// Ursine — fades a canvas group and stops it taking the pointer on the way out.
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
    /// <summary>Fades a canvas group and stops it taking the pointer on the way out.</summary>
    public sealed class Fader : MonoBehaviour
    {
        public CanvasGroup group;

        float _target = 1f, _speed = 1f;

        public void FadeTo(float alpha, float seconds)
        {
            _target = Mathf.Clamp01(alpha);
            _speed = seconds <= 0f ? 1000f : 1f / seconds;
            if (group != null && seconds <= 0f) group.alpha = _target;
        }

        void Update()
        {
            if (group == null || Mathf.Approximately(group.alpha, _target)) return;
            group.alpha = Mathf.MoveTowards(group.alpha, _target, _speed * UnityEngine.Time.deltaTime);
            group.blocksRaycasts = group.alpha > 0.01f;
            group.interactable = group.blocksRaycasts;
        }
    }
}
