// Ursine — a label whose words come from the strings file.
//
// Put on any TMP text that says the same thing every time (a caption, a button, a heading).
// It fills itself in when it is switched on and again whenever the language changes, so a
// label built into a prefab follows the strings file rather than whatever the builder typed.
using TMPro;
using UnityEngine;
using Ursine.Text;

namespace Ursine.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        public string key;
        [Tooltip("Set the text in upper case, for small-caps labels.")]
        public bool upper;
        [Tooltip("The words the builder typed. A view that writes its own text over them is left "
               + "alone: this only fills text that still says what it was built saying, or what "
               + "this component last put there.")]
        public string built;

        TMP_Text _text;
        string _applied;

        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        void OnDisable() => Loc.Changed -= Apply;

        public void Apply()
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text == null) return;
            string now = _text.text;
            bool ours = string.IsNullOrEmpty(built) || now == built || now == _applied;
            if (!ours) return;
            string s = Loc.T(key);
            _applied = upper ? s.ToUpper(System.Globalization.CultureInfo.CurrentCulture) : s;
            _text.text = _applied;
        }
    }
}
