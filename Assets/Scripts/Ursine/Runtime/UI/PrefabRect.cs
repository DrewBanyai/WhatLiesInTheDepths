// Ursine — the design size of a prefab's root, for laying a grid of cards out.
//
// A grid cell and the card that goes in it are the same measurement, so they should not
// be two numbers. Read the size off the prefab and there is nothing to keep in agreement:
// a card whose content grew taller than its cell would otherwise hang out past its own
// border, and the overflow looks like a styling mistake rather than a layout one.
using UnityEngine;

namespace Ursine.UI
{
    public static class PrefabRect
    {
        public static Vector2 Size(RectTransform rt)
        {
            if (rt == null) return Vector2.zero;
            var r = rt.rect.size;
            // A prefab asset's rect is computed from its serialized values; if anchors make
            // that meaningless, sizeDelta is what the builder actually set.
            if (r.x <= 0f) r.x = rt.sizeDelta.x;
            if (r.y <= 0f) r.y = rt.sizeDelta.y;
            return r;
        }

        public static Vector2 Size(Component prefab)
            => prefab == null ? Vector2.zero : Size(prefab.transform as RectTransform);

        public static Vector2 Size(GameObject prefab)
            => prefab == null ? Vector2.zero : Size(prefab.transform as RectTransform);
    }
}
