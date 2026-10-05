// Ursine — drawing the small type larger than it was authored.
//
// A design document gives every text a size, and on a fixed stage that number is the number
// drawn. That is the right default and it is also why the smallest labels — an 8px caption, a
// 9px tracked head, a line of 13.5 italic under a name — are a squint on a big monitor, where
// the whole stage is already being drawn at 133%. The reader's eye is not 133% bigger.
//
// Two things make this safe to do at all.
//
// The first is that only the SMALL type moves. Below Small it takes the whole boost, above
// Large none of it, and between the two it tapers — so a 27px title is never touched and
// there is no step at the boundary for the eye to catch. The sizes in this game cluster
// either side of that gap, which is what makes the taper cheap: about half the design is at
// 14.5 or below and the next cluster does not start until 16.
//
// The second is that a line and a block are not the same risk. A single line sits in a rect
// with slack around it and overflows harmlessly; a wrapped block is budgeted for height, and
// the Focus card's blurb — two lines of 13.5 at 1.5 leading — already fills its 40 exactly,
// with the ledger block 10 below it. So prose gets a gentler rate than captions do. It is a
// smaller lift, and it is the lift that does not push the flavour text into the ledger.
using System;
using UnityEngine;

namespace Ursine.Text
{
    public static class TextScale
    {
        /// <summary>What a caption or a figure may reach, and what a wrapped block may. The
        /// gap between them is the whole point — see the note above.</summary>
        public const float LineMax = 1.40f;
        public const float WrapMax = 1.15f;

        /// <summary>At or below Small, the whole boost; at or above Large, none of it.</summary>
        public const float Small = 14.5f, Large = 19f;

        /// <summary>Raised after the amount changes, so every text can take its size again.</summary>
        public static event Action Changed;

        /// <summary>How far along, 0 to 1. Kept as the amount rather than as a multiplier
        /// because there are two multipliers and only one of them is a number the player is
        /// shown; a fraction is the thing they are actually choosing.</summary>
        public static float Amount { get; private set; }

        /// <summary>What the player is shown: the rate the captions move at, as a percentage.
        /// Prose moves at its own rate, which is quieter and does not need a second number.</summary>
        public static float Percent => Mathf.Lerp(1f, LineMax, Amount) * 100f;

        public static void Set(float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Approximately(amount, Amount)) return;
            Amount = amount;
            Changed?.Invoke();
        }

        /// <summary>The size to draw a text that was authored at this size. Everything that
        /// draws type goes through here, so the rule lives in exactly one place.</summary>
        public static float SizeFor(float authored, bool wraps)
        {
            if (authored <= 0f) return authored;
            float reach = Mathf.Lerp(1f, wraps ? WrapMax : LineMax, Amount);
            // 1 at Small and below, 0 at Large and above.
            float near = Mathf.InverseLerp(Large, Small, authored);
            return authored * Mathf.Lerp(1f, reach, near);
        }
    }
}
