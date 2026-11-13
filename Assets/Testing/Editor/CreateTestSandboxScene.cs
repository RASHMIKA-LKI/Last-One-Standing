#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateTestSandboxScene
{
    [MenuItem("Last One Standing/🚀 Open or Create Test Sandbox Scene", false, 1)]
    public static void OpenOrCreateScene()
    {
        string scenePath = "Assets/Scenes/TestingSandbox.unity";

        // If scene already exists, just open it
        var existingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (existingScene != null)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"[Sandbox] Opened existing test scene: {scenePath}");
            }
            return;
        }

        // Otherwise create new test scene
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Test Sandbox Setup Conductor
            GameObject testManager = new GameObject("TemporaryTestSandbox");
            testManager.AddComponent<TemporarySandboxSetup>();

            // Save Scene
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[Sandbox] Successfully created and opened new isolated test scene at: {scenePath}");
            EditorUtility.DisplayDialog("Test Sandbox Ready", 
                "Test Sandbox Scene created!\n\nJust press PLAY in Unity to test movement, 4-floor climbing, freeze guns, bots, chat, and spectator mode.\n\nYour main scene and hostel models remain 100% untouched.", "Awesome!");
        }
    }
}
#endif

