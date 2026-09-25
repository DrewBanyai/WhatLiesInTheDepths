// What Lies In The Depths — the one-click build.
// Tools > What Lies In The Depths > Build All UI. Everything downstream is generated, so this is
// re-runnable: change a builder, run it again, and the prefabs are rewritten in place.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class WhatLiesInTheDepthsMenu
    {
        const string Root = "Tools/What Lies In The Depths/";

        [MenuItem(Root + "Build All UI", priority = 0)]
        public static void BuildAll()
        {
            try
            {
                // The brief's own build order: the stage and the tokens first, because
                // everything downstream reads from them.
                // Read the strings file fresh: the build adds its labels to it as it goes.
                Ursine.Text.Loc.Reload();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Palettes and type", 0.05f);
                AssetFactory.BuildPalettes();
                AssetFactory.BuildTypeKit();

                // The converted spec art has to be imported and indexed before anything
                // that shows a glyph or a plate is built.
                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Spec art", 0.12f);
                ArtFactory.Build();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Placeholder sprites", 0.20f);
                SpriteFactory.BuildAll();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Widgets", 0.35f);
                BuildWidgets.All();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Bars, ledger and gauge", 0.50f);
                BuildColumns.All();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Journal and Focus", 0.65f);
                BuildCenterA.All();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Revelations to Assault", 0.80f);
                BuildCenterB.All();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "Options and the ending", 0.90f);
                BuildUtility.All();

                EditorUtility.DisplayProgressBar("What Lies In The Depths", "The composite", 0.97f);
                BuildScreen.Build();

                UiFactory.FlushStrings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                int reverted = RefreshInstances();
                Debug.Log("[What Lies In The Depths] Build complete. UI_Screen.prefab is the composite — drop it into a scene."
                          + (reverted > 0 ? $" {reverted} UI_Screen instance(s) in the open scene took the rebuild." : string.Empty));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>A UI_Screen in the open scene is an instance of a prefab this build has
        /// just rewritten, and an instance keeps its own overrides — TextMeshPro records a few
        /// on its own, and any value the prefab used to have stays pinned in the scene. Those
        /// stale values win over the rebuild, which is how a scene ends up showing a layout
        /// nothing in the project still describes. Everything here is generated, so the prefab
        /// is the only truth: each instance is reverted whole and its scene left dirty to save.</summary>
        static int RefreshInstances()
        {
            var prefab = UiFactory.Load("UI_Screen");
            if (prefab == null) return 0;

            int n = 0;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(root)) continue;
                    if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(root) != prefab) continue;

                    PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);
                    EditorSceneManager.MarkSceneDirty(scene);
                    n++;
                }
            }
            return n;
        }

        [MenuItem(Root + "Rebuild Placeholder Sprites", priority = 20)]
        public static void Sprites() => SpriteFactory.BuildAll();

        [MenuItem(Root + "Rebuild Palettes and Type Kit", priority = 21)]
        public static void Assets()
        {
            AssetFactory.BuildPalettes();
            AssetFactory.BuildTypeKit();
        }

        [MenuItem(Root + "Drop UI_Screen into the open scene", priority = 40)]
        public static void DropIntoScene()
        {
            var prefab = UiFactory.Load("UI_Screen");
            if (prefab == null)
            {
                Debug.LogError("[What Lies In The Depths] UI_Screen has not been built yet. Run Build All UI first.");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Add What Lies In The Depths UI");

            // Mouse only, so no EventSystem navigation setup and no explicit navigation
            // order — but a raycaster still needs an EventSystem to feed it.
            //
            // This project is set to the Input System package (activeInputHandler: 1), where
            // StandaloneInputModule throws as soon as it reads UnityEngine.Input. The module
            // is picked to match whichever handler the project is actually on.
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(es, "Add EventSystem");
            }

            Selection.activeGameObject = go;
        }
    }
}
