// What Lies In The Depths — what a utility in a side bar does when pressed.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using System.Linq;
using UnityEngine;
using Ursine.Combat;
using Ursine.Economy;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>Where the outward links in the left bar go. The bar's items are named
    /// "Utility Discord", "Utility Reddit" and "Utility Twitter"; a link with no address
    /// here stays inert.</summary>
    public static class Links
    {
        public const string Discord = "https://discord.com/channels/1550284623790088302/1552731091490574487";
        public const string Reddit = "";
        public const string Twitter = "";

        public static string For(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (itemName.Contains("Discord")) return Discord;
            if (itemName.Contains("Reddit")) return Reddit;
            if (itemName.Contains("Twitter")) return Twitter;
            return null;
        }
    }

    /// <summary>A utility. Never underlined and never dotted, except Options, which is the
    /// one utility that drives the center and therefore lights.</summary>
    public sealed class UtilityBinder : MonoBehaviour
    {
        public enum Which { Options, Exit, Outward, Achievements }

        public Which utility;
        public BarItem item;

        [Tooltip("Where an outward link goes. Empty falls back to Links.For(the item's name).")]
        public string url;

        void Start()
        {
            if (item == null) item = GetComponent<BarItem>();
            if (item == null) return;
            if (string.IsNullOrEmpty(url)) url = Links.For(gameObject.name);

            switch (utility)
            {
                case Which.Options:
                    item.Configure(() => Router.I != null && Router.I.IsOptionsLit(), () => false,
                                   () => Router.I?.ToggleOptions());
                    break;
                case Which.Achievements:
                    // Absent until the first mark is earned, then here for good, in its slot
                    // beside Options. No dot, ever: no dot appears on a utility.
                    item.Configure(() => Router.I != null && Router.I.IsAchievementsLit(), () => false,
                                   () => Router.I?.ToggleAchievements());
                    if (GameState.I != null) GameState.I.Changed += SyncAchievements;
                    SyncAchievements();
                    break;
                case Which.Exit:
                    // The one control on the screen that does not simply act.
                    item.Configure(() => false, () => false, () => Router.I?.AskExit());
                    break;
                default:
                    // Outward links open in the browser; they never take the underline and
                    // never carry a dot. No dot ever appears on a utility.
                    item.Configure(() => false, () => false,
                                   () => { if (!string.IsNullOrEmpty(url)) Application.OpenURL(url); });
                    break;
            }

            if (Router.I != null) Router.I.Changed += item.Refresh;
        }

        void OnDestroy()
        {
            if (Router.I != null && item != null) Router.I.Changed -= item.Refresh;
            if (GameState.I != null) GameState.I.Changed -= SyncAchievements;
        }

        void SyncAchievements()
        {
            var s = GameState.I;
            if (s == null || item == null) return;
            bool open = s.AchievementsOpen;
            if (item.gameObject.activeSelf != open) item.gameObject.SetActive(open);
        }
    }
}
