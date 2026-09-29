// Ursine — "this is the resource I am pointing at", from anywhere a cost is written.
//
// A cost pill, a ledger line or an offer row carries a ResourceHover naming its resource. While
// the pointer rests on it, ResourceHint.Current is that resource, and whatever lists resources
// (a ledger) can light the matching row, so a price points at the stock that pays it.
//
// Leaving does not clear at once: several surfaces rebuild their pills every second under a
// pointer that has not moved, and the new pill's enter arrives a frame after the old one's exit.
// The hint holds for a moment in case the same resource is entered again.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ursine.UI
{
    public static class ResourceHint
    {
        const float Grace = 0.12f;
        static string _key;
        static float _leftAt = -1f;

        /// <summary>The resource under the pointer, or null.</summary>
        public static string Current
            => _key != null && (_leftAt < 0f || Time.unscaledTime - _leftAt < Grace) ? _key : null;

        public static void Enter(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _key = key;
            _leftAt = -1f;
        }

        public static void Exit(string key)
        {
            if (_key == key && _leftAt < 0f) _leftAt = Time.unscaledTime;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ResourceHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string key;
        bool _in;

        public void OnPointerEnter(PointerEventData e)
        {
            if (string.IsNullOrEmpty(key)) return;
            _in = true;
            ResourceHint.Enter(key);
        }

        public void OnPointerExit(PointerEventData e) => Leave();
        void OnDisable() => Leave();

        void Leave()
        {
            if (!_in) return;
            _in = false;
            ResourceHint.Exit(key);
        }

        /// <summary>Makes <paramref name="go"/> report <paramref name="resourceKey"/> under the
        /// pointer. Its images are made to catch the pointer (an image with nothing drawn still
        /// does); if it has none, a clear one is added behind it, outside any layout.</summary>
        public static ResourceHover On(GameObject go, string resourceKey)
        {
            if (go == null) return null;
            var h = go.GetComponent<ResourceHover>();
            if (h == null) h = go.AddComponent<ResourceHover>();
            if (h._in && h.key != resourceKey) { ResourceHint.Exit(h.key); h._in = false; }
            h.key = resourceKey;

            bool any = false;
            foreach (var img in go.GetComponentsInChildren<Image>(true))
            {
                img.raycastTarget = true;
                any = true;
            }
            if (!any)
            {
                var catcher = new GameObject("HoverArea", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                var rt = (RectTransform)catcher.transform;
                rt.SetParent(go.transform, false);
                rt.SetAsFirstSibling();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                catcher.GetComponent<Image>().color = Color.clear;
                catcher.GetComponent<LayoutElement>().ignoreLayout = true;
            }
            return h;
        }
    }
}
