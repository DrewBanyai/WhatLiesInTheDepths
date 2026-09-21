// Ursine — a name-to-sprite lookup.
//
// Content is keyed by string (a resource id, a task id, a place id), and the art for it
// has to be found at runtime. Sprites in a plain folder are not loadable by name, so this
// asset holds the references: it lives in Resources, the sprites do not have to.
using System.Collections.Generic;
using UnityEngine;

namespace Ursine.UI
{
    [CreateAssetMenu(menuName = "Ursine/Sprite Set", fileName = "SpriteSet")]
    public sealed class SpriteSet : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public Sprite sprite;
        }

        [Tooltip("Keys are folder-relative paths, e.g. Resource/silt or Spec/Art_Eye.")]
        public List<Entry> entries = new List<Entry>();

        Dictionary<string, Sprite> _map;

        void BuildMap()
        {
            _map = new Dictionary<string, Sprite>(entries.Count);
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.key)) _map[e.key] = e.sprite;
        }

        /// <summary>Null when there is no art for that key, which is not an error: a game
        /// may legitimately have content the artist has not drawn yet.</summary>
        public Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_map == null || _map.Count != entries.Count) BuildMap();
            return _map.TryGetValue(key, out var s) ? s : null;
        }

        public Sprite Get(string folder, string key)
            => string.IsNullOrEmpty(key) ? null : Get(folder + "/" + key);

        public bool Has(string key) => Get(key) != null;

#if UNITY_EDITOR
        public void Set(string key, Sprite sprite)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].key == key)
                {
                    var e = entries[i]; e.sprite = sprite; entries[i] = e; _map = null; return;
                }
            entries.Add(new Entry { key = key, sprite = sprite });
            _map = null;
        }

        public void Clear() { entries.Clear(); _map = null; }
#endif
    }
}
