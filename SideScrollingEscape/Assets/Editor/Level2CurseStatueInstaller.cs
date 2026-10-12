#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.EditorTools
{
    /// <summary>Keeps the authored Level 2 curse statues present in edit mode.</summary>
    [InitializeOnLoad]
    public static class Level2CurseStatueInstaller
    {
        private const string ScenePath = "Assets/Scenes/Level2.unity";
        private const string PrefabPath = "Assets/Prefabs/CurseStatue.prefab";

        private static readonly Vector3[] Positions =
        {
            new Vector3(24f, 0.15f, 0f),
            new Vector3(58f, 0.15f, 0f),
            new Vector3(24f, 45.15f, 0f),
            new Vector3(58f, 45.15f, 0f)
        };

        static Level2CurseStatueInstaller()
        {
            EditorApplication.delayCall += EnsureForActiveScene;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (IsLevel2Scene(scene))
                EditorApplication.delayCall += EnsureForActiveScene;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += EnsureForActiveScene;
        }

        [MenuItem("Tools/SideScrollingEscape/Repair Level 2 Curse Statues")]
        private static void EnsureForActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (IsLevel2Scene(scene))
                EnsureSceneContents(scene, saveScene: false);

            EnsureLevel2Asset();
        }

        private static void EnsureLevel2Asset()
        {
            Scene target = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !target.IsValid() || !target.isLoaded;
            if (openedTemporarily)
                target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            bool changed = EnsureSceneContents(target, saveScene: openedTemporarily);
            if (openedTemporarily)
                EditorSceneManager.CloseScene(target, true);

            if (changed)
                AssetDatabase.Refresh();
        }

        private static bool EnsureSceneContents(Scene scene, bool saveScene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[CurseStatueInstaller] CurseStatue prefab could not be loaded.");
                return false;
            }

            bool changed = false;
            for (int i = 0; i < Positions.Length; i++)
            {
                string objectName = $"CurseStatue_{(char)('A' + i)}";
                bool alreadyExists = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == objectName)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                if (alreadyExists) continue;

                GameObject statue = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                statue.name = objectName;
                statue.transform.position = Positions[i];

                SerializedObject serializedStatue = new SerializedObject(statue.GetComponent("CurseStatue"));
                SerializedProperty fakeProperty = serializedStatue.FindProperty("isFake");
                if (fakeProperty != null)
                {
                    fakeProperty.boolValue = i == 0 || i == 3;
                    serializedStatue.ApplyModifiedPropertiesWithoutUndo();
                }

                Undo.RegisterCreatedObjectUndo(statue, "Add Level 2 curse statue");
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[CurseStatueInstaller] Added four persistent curse statues to Level 2. Save the scene to keep them.");
                if (saveScene)
                    EditorSceneManager.SaveScene(scene);
            }

            return changed;
        }

        private static bool IsLevel2Scene(Scene scene)
        {
            if (!scene.IsValid()) return false;
            if (scene.path == ScenePath || scene.name == "Level2") return true;

            // Unity may recover an unsaved Level 2 as Temp/__Backupscenes/0.backup.
            // CurseMirror_ITEM11231 is unique to this level, so it safely identifies that backup.
            return GameObject.Find("CurseMirror_ITEM11231") != null;
        }
    }
}
#endif
