// Ursine — one mark, one meaning: there is something here you have not seen.
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
    /// <summary>One mark, one meaning: there is something here you have not seen. Never a
    /// count — one thing waiting and nine things waiting draw the same mark, because the
    /// question it answers is whether to look, not how much is there.</summary>
    public sealed class AttentionDot : MonoBehaviour
    {
        public GameObject root;

        public void Set(bool unseen)
        {
            if (root != null && root.activeSelf != unseen) root.SetActive(unseen);
        }
    }
}
