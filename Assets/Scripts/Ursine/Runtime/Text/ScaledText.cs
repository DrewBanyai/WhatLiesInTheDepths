// Ursine — one text, remembering the size it was authored at.
//
// The same shape as ThemedGraphic, and added in the same place and for the same reason: a
// graphic that remembers its token can be repainted when the palette changes, and a text that
// remembers its size can be reset when the reader asks for larger type. Without it the drawn
// size is the only record there is, and scaling twice would compound.
using TMPro;
using UnityEngine;

namespace Ursine.Text
{
    [DisallowMultipleComponent]
    public sealed class ScaledText : MonoBehaviour
    {
        [Tooltip("The size the design specifies. What is drawn is this, put through TextScale.")]
        public float authored;

        [Tooltip("Whether this text wraps. A wrapped block is budgeted for height and takes a "
               + "gentler rate than a single line, which has slack around it.")]
        public bool wraps;

        [Tooltip("The line height Typeset.Leading was given, or 0 for a text it never touched. "
               + "Leading puts half the leading into the margin in absolute units of the size, "
               + "so a text whose size moves has to be given its leading again.")]
        public float leading;

        TMP_Text _text;
        TMP_Text Text => _text != null ? _text : _text = GetComponent<TMP_Text>();

        /// <summary>Records the authored size and draws it. Called by Typeset, and by a view
        /// that sets a size of its own after the fact.</summary>
        public void Bind(float size)
        {
            authored = size;
            Apply();
        }

        void OnEnable()
        {
            TextScale.Changed += Apply;
            Apply();
        }

        void OnDisable() => TextScale.Changed -= Apply;

        public void Apply()
        {
            var t = Text;
            if (t == null || authored <= 0f) return;

            float size = TextScale.SizeFor(authored, wraps);
            if (Mathf.Approximately(t.fontSize, size)) return;
            t.fontSize = size;

            // Leading's margins are half the leading in units of the size, so they are wrong
            // the moment the size is not what they were worked out from.
            if (leading > 0f) Typeset.Leading(t, leading);
        }
    }
}
