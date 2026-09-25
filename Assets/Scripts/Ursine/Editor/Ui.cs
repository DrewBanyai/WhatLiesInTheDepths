// Ursine — prefab construction plumbing.
//
// Geometry and hierarchy only: no tokens, no typefaces, no opinions about what anything
// looks like. A project layers its own factory on top of this, one that knows its palette
// and its type roles.
//
// Everything is anchored top-left with a top-left pivot, so a design coordinate goes in
// unchanged: x runs right, y runs DOWN from the parent's top-left corner, the way CSS and
// most design tools measure.
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ursine.EditorTools
{
    public static class Ui
    {
        // ---- nodes -------------------------------------------------------------

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        /// <summary>Places a node at a design coordinate: x from the left, y DOWN from the top.</summary>
        public static RectTransform At(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Node(string name, Transform parent, float x, float y, float w, float h)
            => At(Node(name, parent), x, y, w, h);

        /// <summary>A node that fills its parent. Negative insets expand it beyond the
        /// parent, which is how a shadow sits outside the thing casting it.</summary>
        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Centers a node on its parent, for anything that orbits or is dead center.</summary>
        public static RectTransform Center(RectTransform rt, float w = 0f, float h = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        // ---- layout ------------------------------------------------------------

        /// <summary>A vertical stack that grows to fit. Idempotent, so calling it twice on
        /// the same node reconfigures rather than stacking two layout groups on one object.</summary>
        public static VerticalLayoutGroup Stack(RectTransform rt, float spacing = 0f, RectOffset padding = null)
        {
            var v = rt.gameObject.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset();
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fit = rt.gameObject.GetComponent<ContentSizeFitter>();
            if (fit == null) fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return v;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, float spacing = 0f,
                                                TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (h == null) h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return h;
        }

        public static GridLayoutGroup Grid(RectTransform rt, Vector2 cell, Vector2 spacing, int columns)
        {
            var old = rt.gameObject.GetComponent<VerticalLayoutGroup>();
            if (old != null) Object.DestroyImmediate(old);

            var g = rt.gameObject.GetComponent<GridLayoutGroup>();
            if (g == null) g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            return g;
        }

        /// <summary>A vertical scroll view with a stacked, self-sizing content node.</summary>
        public static (ScrollRect scroll, RectTransform content) Scroll(
            string name, Transform parent, float x, float y, float w, float h,
            float spacing = 0f, RectOffset padding = null)
        {
            var root = Node(name, parent, x, y, w, h);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = Stretch(Node("Viewport", root));
            viewport.gameObject.AddComponent<RectMask2D>();
            // The wheel is answered over the whole view, not only where a card happens to be.
            // uGUI sends a scroll to the graphic under the pointer, so a viewport with nothing
            // drawn on it hands the gaps between cards to whatever is behind the menu.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;
            scroll.viewport = viewport;

            var content = Node("Content", viewport, 0, 0, w, 0);
            Stack(content, spacing, padding);
            scroll.content = content;

            return (scroll, content);
        }

        // ---- prefabs -----------------------------------------------------------

        public static GameObject Save(GameObject go, string dir, string name)
        {
            System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
            if (!ok) Debug.LogError($"[Ursine] Failed to save {path}.");
            Object.DestroyImmediate(go);
            return asset;
        }

        public static GameObject Load(string dir, string name)
            => AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/{name}.prefab");

        /// <summary>Nests a saved prefab inside the one being built, so a composite is a real
        /// composite rather than a copy. Worth insisting on: a component that looks right on
        /// its own is not evidence that it is right in place.</summary>
        public static GameObject Nest(string dir, string prefabName, Transform parent, float x, float y)
        {
            var asset = Load(dir, prefabName);
            if (asset == null)
            {
                Debug.LogError($"[Ursine] Missing prefab {dir}/{prefabName}.prefab.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            return go;
        }
    }
}
