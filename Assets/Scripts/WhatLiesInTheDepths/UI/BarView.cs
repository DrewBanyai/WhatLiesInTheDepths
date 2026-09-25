// What Lies In The Depths — the three bars. Spec section 3.
//   The serif is a place, the small caps is an act.
//   The underline lives where the button is, not where the content is.
//   Exactly one underline exists across all three bars at any moment.
using System;
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public enum BarItemKind { Destination, Utility }


    /// <summary>A bar: full column width, 31 tall, a 1px rule along the bottom drawn
    /// whether there is one item or seven. No bar scrolls and no bar wraps.</summary>
    public sealed class BarView : MonoBehaviour
    {
        public RectTransform itemsRow;
        public Image rule;
        public List<BarItem> items = new List<BarItem>();

        public void Refresh()
        {
            foreach (var i in items)
                if (i != null) i.Refresh();
        }
    }
}
