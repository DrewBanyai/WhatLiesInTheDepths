// Ursine — binds a Graphic's color to a palette token so a swap reaches it.
// Any color that is not bound this way will not follow a palette change.
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

        // The color this component last put on the graphic. Serialized, so an instance made
        // from a prefab knows what its builder set even before it has ever been switched on.
        [SerializeField, HideInInspector] Color _applied;
        [SerializeField, HideInInspector] bool _hasApplied;

        Graphic _g;
        Graphic G => _g != null ? _g : (_g = GetComponent<Graphic>());

        void OnEnable()
        {
            Theme.Changed += Apply;
            // Switching on must not undo a view. A view that tints a bound graphic on purpose
            // (a cost gone rose, a hover) did so while it was switched off, or before the page
            // it sits on was shown; reapplying the token here painted over it every time a
            // page was opened. So only a graphic still wearing this component's own color
            // is brought up to date.
            if (!_hasApplied || G == null || G.color == _applied) Apply();
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
            _applied = G.color;
            _hasApplied = true;
        }
    }
}
