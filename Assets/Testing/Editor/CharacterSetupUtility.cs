#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CharacterSetupUtility
{
    private const string FbxPath = "Assets/Models/Characters/Ch38_nonPBR.fbx";
    private const string TexturesDir = "Assets/Models/Characters/Textures";
    private const string MaterialsDir = "Assets/Models/Characters/Materials";

    [MenuItem("Tools/Last One Standing/Setup Player Directly In Scene", false, 1)]
    public static void ManualSetupPlayerInScene()
    {
        EnsurePlayerInScene();
    }

    public static void EnsurePlayerInScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Hostal" && scene.name != "TestingSandbox")
        {
            // If Hostal is not active scene, open it
            string hostalPath = "Assets/Scenes/Hostal.unity";
            if (File.Exists(hostalPath))
            {
                scene = EditorSceneManager.OpenScene(hostalPath, OpenSceneMode.Single);
            }
        }

        Debug.Log("[CharacterSetupUtility] === Setting up 3D Humanoid Player directly in Scene Hierarchy ===");

        // 1. Locate or create Player_Survivor
        GameObject playerObj = GameObject.Find("Player_Survivor");
        if (playerObj == null)
        {
            playerObj = new GameObject("Player_Survivor");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(-0.3f, 1.0f, -21.8f);
            playerObj.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
        }

        // 2. Remove any old primitive capsule renderer/filter/collider
        var mf = playerObj.GetComponent<MeshFilter>();
        if (mf != null) UnityEngine.Object.DestroyImmediate(mf);
        var mr = playerObj.GetComponent<MeshRenderer>();
        if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
        var capCol = playerObj.GetComponent<CapsuleCollider>();
        if (capCol != null) UnityEngine.Object.DestroyImmediate(capCol);

        // 3. Ensure CharacterController
        var cc = playerObj.GetComponent<CharacterController>();
        if (cc == null) cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 60f;
        cc.stepOffset = 0.35f;
        cc.skinWidth = 0.025f;

        // 4. Instantiate or find Character_Visual (Ch38 humanoid model)
        Transform visualT = playerObj.transform.Find("Character_Visual");
        GameObject visualModel = visualT != null ? visualT.gameObject : null;
        if (visualModel == null)
        {
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxAsset != null)
            {
                visualModel = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, playerObj.transform);
                visualModel.name = "Character_Visual";
                visualModel.transform.localPosition = Vector3.zero;
                visualModel.transform.localRotation = Quaternion.identity;
            }
        }

        // Clean up any missing MonoBehaviours left behind by deleted scripts
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(playerObj);
        if (visualModel != null)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(visualModel);
        }

        if (visualModel != null)
        {
            // Apply Team Blue material
            Material blueMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/Ch38_Team_Blue.mat");
            if (blueMat != null)
            {
                foreach (var rend in visualModel.GetComponentsInChildren<Renderer>())
                {
                    rend.sharedMaterial = blueMat;
                }
            }

            // Assign Animator Controller & Avatar
            Animator anim = visualModel.GetComponent<Animator>();
            if (anim != null)
            {
                // Auto-wire Avatar from Ch38_nonPBR model
                if (anim.avatar == null)
                {
                    UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
                    foreach (var obj in subAssets)
                    {
                        if (obj is Avatar av)
                        {
                            anim.avatar = av;
                            break;
                        }
                    }
                }

                RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Models/Characters/Animation/Player_Animator.controller");
                if (animCtrl != null)
                {
                    anim.runtimeAnimatorController = animCtrl;
                }
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // Ensure WeaponIKController is on Character_Visual
                if (visualModel.GetComponent<WeaponIKController>() == null)
                {
                    visualModel.AddComponent<WeaponIKController>();
                }
            }
        }

        // 5. Camera (First-Person Eye View - Solution 3)
        Transform camT = playerObj.transform.Find("PlayerCamera");
        GameObject camObj = camT != null ? camT.gameObject : null;
        if (camObj == null)
        {
            camObj = new GameObject("PlayerCamera");
            camObj.transform.parent = playerObj.transform;
        }
        camObj.transform.localPosition = new Vector3(0f, 1.62f, 0.12f);
        camObj.transform.localRotation = Quaternion.identity;

        var cam = camObj.GetComponent<Camera>();
        if (cam == null) cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.03f;
        if (camObj.GetComponent<AudioListener>() == null) camObj.AddComponent<AudioListener>();

        // 6. Scripts on Player
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc == null) pc = playerObj.AddComponent<PlayerController>();
        pc.cameraHolder = camObj.transform;
        if (visualModel != null) pc.animator = visualModel.GetComponent<Animator>();
        pc.SyncComponents();

        var ft = playerObj.GetComponent<FreezeTarget>();
        if (ft == null) ft = playerObj.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var ps = playerObj.GetComponent<PlayerShooting>();
        if (ps == null) ps = playerObj.AddComponent<PlayerShooting>();
        ps.playerCamera = cam;

        // 7. Setup Gun directly on PlayerCamera (Clean FPS Standard)
        Transform existingCamGun = camObj.transform.Find("Equipped_Rifle");
        Transform[] allTransforms = playerObj.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t == null) continue;
            if ((t.name.Contains("Equipped_Rifle") || t.name.Contains("SM_Sci_FiWeapon")) && t != existingCamGun)
            {
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyOne/Free Sci Fi Weapon/Prefabs/SM_Sci_FiWeapon_RifleGun.prefab");
        if (gunPrefab != null)
        {
            GameObject gunInstance;
            if (existingCamGun != null)
            {
                gunInstance = existingCamGun.gameObject;
            }
            else
            {
                gunInstance = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab, camObj.transform);
                gunInstance.name = "Equipped_Rifle";
            }

            gunInstance.transform.parent = camObj.transform;
            gunInstance.transform.localPosition = new Vector3(0.20f, -0.22f, 0.48f);
            gunInstance.transform.localRotation = Quaternion.Euler(0.5f, 88.5f, 0.8f);

            Transform muzzle = gunInstance.transform.Find("GunMuzzle");
            if (muzzle == null)
            {
                GameObject muzzleObj = new GameObject("GunMuzzle");
                muzzle = muzzleObj.transform;
                muzzle.parent = gunInstance.transform;
                muzzle.localPosition = new Vector3(-0.495f, 0.075f, 0f);
                muzzle.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else
            {
                muzzle.localPosition = new Vector3(-0.495f, 0.075f, 0f);
                muzzle.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            pc.gunTransform = gunInstance.transform;
            ps.gunTransform = gunInstance.transform;
            ps.gunMuzzle = muzzle;
        }

        // Renderers ON for character visual
        if (visualModel != null)
        {
            foreach (var rend in visualModel.GetComponentsInChildren<Renderer>(true))
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        var tm = playerObj.GetComponent<TeamMember>();
        if (tm == null) tm = playerObj.AddComponent<TeamMember>();
        tm.team = TeamType.TeamA_Blue;

        if (playerObj.GetComponent<GameHUDAndTasks>() == null)
        {
            playerObj.AddComponent<GameHUDAndTasks>();
        }

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CharacterSetupUtility] Successfully placed Player_Survivor with Character_Visual and Equipped_Rifle in Scene Hierarchy!");
    }
}
#endif
