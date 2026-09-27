// Ursine — does a freshly built hierarchy say anything its saved prefab does not?
//
// SaveAsPrefabAsset gives every object in a new hierarchy a fresh local file id, and a
// prefab's YAML is ordered by those ids. A builder that runs twice over identical input
// therefore writes a different file each time: the same objects, renumbered and reshuffled.
// Version control sees a changed file, and so does every scene or prefab that points into it.
//
// This compares what the two hierarchies MEAN rather than how they were written down: the
// same objects in the same places, carrying the same components with the same serialized
// values. A reference to another object in the same prefab is compared by where that object
// sits in the hierarchy, not by its file id; a reference to anything outside is compared by
// asset GUID and local id. Ui.Save uses it to leave an unchanged prefab alone.
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ursine.EditorTools
{
    public static class PrefabDiff
    {
        /// <summary>Logs the first difference found for each prefab that is rewritten, so a
        /// rewrite nobody expected can be traced to the property that caused it.</summary>
        public static bool Verbose = true;

        /// <summary>Bookkeeping Unity writes for its own purposes; none of it is content.</summary>
        static readonly HashSet<string> Ignored = new HashSet<string>
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_EditorHideFlags",
        };

        /// <summary>True when <paramref name="built"/> (an unsaved root) and
        /// <paramref name="saved"/> (a prefab asset root) serialize to the same content.</summary>
        public static bool Same(GameObject built, GameObject saved)
        {
            if (built == null || saved == null) return false;
            var why = new StringBuilder();
            bool same = SameObject(built, built.transform, saved, saved.transform, "", why);
            if (!same && Verbose) Debug.Log($"[Ursine] {saved.name} changed: {why}");
            return same;
        }

        static bool SameObject(GameObject a, Transform aRoot, GameObject b, Transform bRoot,
                               string path, StringBuilder why)
        {
            string here = path.Length == 0 ? a.name : path + "/" + a.name;

            // The root takes the file's name when saved, whatever it was called when built.
            if (path.Length > 0 && a.name != b.name) return Fail(why, here, "name");
            if (a.activeSelf != b.activeSelf) return Fail(why, here, "active");
            if (a.layer != b.layer) return Fail(why, here, "layer");
            if (a.tag != b.tag) return Fail(why, here, "tag");
            if (a.isStatic != b.isStatic) return Fail(why, here, "static");

            // A nested prefab has to come from the same prefab on both sides; its content is
            // compared below like anything else.
            if (SourcePath(a) != SourcePath(b)) return Fail(why, here, "nested prefab source");

            var ca = Saved(a);
            var cb = Saved(b);
            if (ca.Length != cb.Length) return Fail(why, here, "component count");
            for (int i = 0; i < ca.Length; i++)
            {
                // A missing script is never "the same" as anything: rewrite to be safe.
                if (ca[i] == null || cb[i] == null) return Fail(why, here, "missing script");
                if (ca[i].GetType() != cb[i].GetType()) return Fail(why, here, $"component {i} type");
                if (!SameComponent(ca[i], aRoot, cb[i], bRoot, out string prop))
                    return Fail(why, here, $"{ca[i].GetType().Name}.{prop}");
            }

            var ka = Saved(a.transform);
            var kb = Saved(b.transform);
            if (ka.Count != kb.Count) return Fail(why, here, "child count");
            for (int i = 0; i < ka.Count; i++)
                if (!SameObject(ka[i].gameObject, aRoot, kb[i].gameObject, bRoot, here, why))
                    return false;
            return true;
        }

        /// <summary>Whether an object would be written to the file at all. TextMeshPro, for
        /// one, hangs objects it never saves off text it has laid out.</summary>
        static bool IsSaved(Object o) => (o.hideFlags & HideFlags.DontSaveInEditor) == 0;

        static List<Transform> Saved(Transform t)
        {
            var list = new List<Transform>(t.childCount);
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (IsSaved(c.gameObject)) list.Add(c);
            }
            return list;
        }

        static Component[] Saved(GameObject go)
        {
            var all = go.GetComponents<Component>();
            // A missing script reads as null; keep it, so it is caught and forces a rewrite.
            return System.Array.FindAll(all, c => c == null || IsSaved(c));
        }

        static bool Fail(StringBuilder why, string where, string what)
        {
            why.Append(where).Append(" (").Append(what).Append(')');
            return false;
        }

        static string SourcePath(GameObject go)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) return "";
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            return src == null ? "" : AssetDatabase.GetAssetPath(src);
        }

        static bool SameComponent(Component a, Transform aRoot, Component b, Transform bRoot, out string prop)
        {
            prop = "";
            using (var sa = new SerializedObject(a))
            using (var sb = new SerializedObject(b))
            {
                var pa = sa.GetIterator();
                var pb = sb.GetIterator();
                bool enter = true;
                while (true)
                {
                    bool na = pa.Next(enter);
                    bool nb = pb.Next(enter);
                    if (na != nb) { prop = na ? pa.propertyPath : pb.propertyPath; return false; }
                    if (!na) return true;

                    prop = pa.propertyPath;
                    if (pa.propertyPath != pb.propertyPath || pa.propertyType != pb.propertyType) return false;

                    // At the top level, skip Unity's own bookkeeping, children and all.
                    if (pa.depth == 0 && Ignored.Contains(pa.name)) { enter = false; continue; }

                    switch (pa.propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                        case SerializedPropertyType.ExposedReference:
                            var ra = pa.propertyType == SerializedPropertyType.ObjectReference
                                ? pa.objectReferenceValue : pa.exposedReferenceValue;
                            var rb = pb.propertyType == SerializedPropertyType.ObjectReference
                                ? pb.objectReferenceValue : pb.exposedReferenceValue;
                            if (Ref(ra, aRoot) != Ref(rb, bRoot)) return false;
                            enter = false;
                            break;

                        case SerializedPropertyType.Generic:
                        case SerializedPropertyType.ManagedReference:
                            // Structs, lists and [SerializeReference] fields: walk into them, so
                            // any object reference inside is compared by what it points at.
                            enter = true;
                            break;

                        default:
                            // A value with no object references in it: strings, numbers,
                            // vectors, colors, rects, enums, curves, gradients.
                            if (!SerializedProperty.DataEquals(pa, pb)) return false;
                            enter = false;
                            break;
                    }
                }
            }
        }

        /// <summary>What a reference means, independent of how the file numbered it.</summary>
        static string Ref(Object o, Transform root)
        {
            if (o == null) return "null";

            Transform t = null;
            if (o is GameObject go) t = go.transform;
            else if (o is Component c) t = c.transform;

            if (t != null && t.root == root)
            {
                var sb = new StringBuilder("local:");
                var chain = new List<int>();
                for (var p = t; p != root; p = p.parent) chain.Add(Saved(p.parent).IndexOf(p));
                for (int i = chain.Count - 1; i >= 0; i--) sb.Append(chain[i]).Append('/');
                if (o is Component comp)
                {
                    sb.Append('#').Append(System.Array.IndexOf(Saved(t.gameObject), comp));
                }
                else sb.Append("#go");
                return sb.ToString();
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long local))
                return $"asset:{guid}:{local}";

            // Something that exists only in memory (a material made on the fly, say) is not
            // written into a prefab: the saved file holds nothing there, so neither do we.
            return "null";
        }
    }
}
