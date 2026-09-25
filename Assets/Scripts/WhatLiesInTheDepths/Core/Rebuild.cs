// What Lies In The Depths — clearing a container so it can be drawn again.
using UnityEngine;

namespace WhatLiesInTheDepths.Core
{
    public static class Rebuild
    {
        /// <summary>Removes every child. Each is switched off first, because Destroy only lands
        /// at the end of the frame and a layout group would lay the old ones out beside the new
        /// ones until then.</summary>
        public static void Clear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Object.Destroy(c);
            }
        }

        /// <summary>Finds a descendant by name, at any depth.</summary>
        public static Transform Deep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var hit = Deep(c, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
