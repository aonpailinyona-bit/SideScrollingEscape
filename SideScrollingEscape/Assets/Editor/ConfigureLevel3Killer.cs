using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SideScrollingEscape.Enemy;

public class ConfigureLevel3Killer
{
    public static void Configure()
    {
        string scenePath = "Assets/Scenes/Level3.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var killerAI = root.GetComponentInChildren<KillerAI>(true);
            if (killerAI != null)
            {
                // Set isBlind via SerializedObject to avoid modifying the prefab definition
                SerializedObject so = new SerializedObject(killerAI);
                var isBlindProp = so.FindProperty("isBlind");
                if (isBlindProp != null)
                {
                    isBlindProp.boolValue = true;
                    so.ApplyModifiedProperties();
                    Debug.Log("Set isBlind to true on KillerAI in Level3.");
                }

                var animator = killerAI.GetComponent<Animator>();
                if (animator != null)
                {
                    var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>("Assets/Animations/Killer/Enemy3OverrideController.overrideController");
                    if (overrideController != null)
                    {
                        // To set it on the prefab instance, we need to use SerializedObject
                        SerializedObject animSo = new SerializedObject(animator);
                        var controllerProp = animSo.FindProperty("m_Controller");
                        if (controllerProp != null)
                        {
                            controllerProp.objectReferenceValue = overrideController;
                            animSo.ApplyModifiedProperties();
                            Debug.Log("Set Animator Controller to Enemy3OverrideController in Level3.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Could not load Enemy3OverrideController.");
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Successfully configured Level3 Killer.");
                return;
            }
        }
        
        Debug.LogError("Could not find KillerAI in Level3.");
    }
}
