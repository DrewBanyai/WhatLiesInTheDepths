// Ursine — binds a Graphic's colour to a palette token so a swap reaches it.
// Any colour that is not bound this way will not follow a palette change.
using UnityEngine;
using UnityEngine.UI;

namespace Ursine.Theming
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class ThemedGraphic : MonoBehaviour
    {
        [Tooltip("An index into the game's own token enum.")]
        public int token;

        [Range(0f, 1f)]
        [Tooltip("Applied on top of the token. For scrims and anything that yields.")]
        public float alpha = 1f;

        Graphic _g;
        Graphic G => _g != null ? _g : (_g = GetComponent<Graphic>());

        void OnEnable()
        {
            Theme.Changed += Apply;
            Apply();
        }

        void OnDisable() => Theme.Changed -= Apply;

#if UNITY_EDITOR
        void OnValidate() { if (isActiveAndEnabled) Apply(); }
#endif

        public void Bind(int t, float a = 1f)
        {
            token = t;
            alpha = a;
            Apply();
        }

        public void Apply()
        {
            if (G == null) return;
            G.color = Theme.Get(token, alpha);
        }
    }
}
