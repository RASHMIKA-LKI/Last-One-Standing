#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterAnimationSetup
{
    private const string AnimDir = "Assets/Models/Characters/Animation";
    private const string ControllerPath = "Assets/Models/Characters/Animation/Player_Animator.controller";

    [MenuItem("Tools/Last One Standing/Build Player Animator Controller", false, 11)]
    public static void BuildAnimatorController()
    {
        Debug.Log("[CharacterAnimationSetup] === Building Player Animator Controller ===");

        // 1. Ensure all animation FBXs are configured as Humanoid with correct loop settings
        ConfigureFbxAsHumanoid($"{AnimDir}/Rifle Aiming Idle.fbx", true);
        ConfigureFbxAsHumanoid($"{AnimDir}/Idle.fbx", true);
        ConfigureFbxAsHumanoid($"{AnimDir}/Walk With Rifle.fbx", true);
        ConfigureFbxAsHumanoid($"{AnimDir}/Run Forward.fbx", true);
        ConfigureFbxAsHumanoid($"{AnimDir}/Rifle Jump.fbx", false);

        AssetDatabase.Refresh();

        // 2. Load the animation clips
        AnimationClip idleClip = LoadAnimationClip($"{AnimDir}/Rifle Aiming Idle.fbx");
        if (idleClip == null) idleClip = LoadAnimationClip($"{AnimDir}/Idle.fbx");
        AnimationClip walkClip = LoadAnimationClip($"{AnimDir}/Walk With Rifle.fbx");
        AnimationClip runClip = LoadAnimationClip($"{AnimDir}/Run Forward.fbx");
        AnimationClip jumpClip = LoadAnimationClip($"{AnimDir}/Rifle Jump.fbx");

        if (idleClip == null || walkClip == null || runClip == null)
        {
            Debug.LogWarning("[CharacterAnimationSetup] Some animation clips could not be loaded yet. Make sure FBXs are imported.");
        }

        // 3. Create or reset the AnimatorController
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        // 4. Create Locomotion BlendTree (Idle, Walk, Run)
        BlendTree locomotionBlendTree;
        AnimatorState locomotionState = controller.CreateBlendTreeInController("Locomotion", out locomotionBlendTree, 0);
        locomotionBlendTree.blendType = BlendTreeType.Simple1D;
        locomotionBlendTree.blendParameter = "Speed";
        locomotionBlendTree.useAutomaticThresholds = false;

        if (idleClip != null) locomotionBlendTree.AddChild(idleClip, 0.0f);
        if (walkClip != null) locomotionBlendTree.AddChild(walkClip, 4.5f);
        if (runClip != null) locomotionBlendTree.AddChild(runClip, 7.0f);

        rootStateMachine.defaultState = locomotionState;

        // 5. Create Jump State
        if (jumpClip != null)
        {
            AnimatorState jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;

            // Transition: Locomotion -> Jump (on Jump trigger)
            AnimatorStateTransition toJump = locomotionState.AddTransition(jumpState);
            toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            toJump.hasExitTime = false;
            toJump.duration = 0.1f;

            // Transition: Jump -> Locomotion (when grounded and exit time reached)
            AnimatorStateTransition backToLoco = jumpState.AddTransition(locomotionState);
            backToLoco.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            backToLoco.hasExitTime = true;
            backToLoco.exitTime = 0.7f;
            backToLoco.duration = 0.2f;
        }

        var layers = controller.layers;
        if (layers.Length > 0)
        {
            layers[0].defaultWeight = 1.0f;
            layers[0].iKPass = true;
            controller.layers = layers;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CharacterAnimationSetup] Successfully generated Animator Controller at: {ControllerPath}");
    }

    private static void ConfigureFbxAsHumanoid(string path, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        bool dirty = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            dirty = true;
        }

        // Configure to Copy From Ch38 avatar so all bone retargeting matches 100%
        Avatar characterAvatar = null;
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/Ch38_nonPBR.fbx");
        foreach (var obj in assets)
        {
            if (obj is Avatar av) { characterAvatar = av; break; }
        }

        if (characterAvatar != null)
        {
            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther || importer.sourceAvatar != characterAvatar)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = characterAvatar;
                dirty = true;
            }
        }
        else if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            dirty = true;
        }

        // Set loop time on default clip
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips != null && clips.Length > 0)
        {
            foreach (var clip in clips)
            {
                if (clip.loopTime != loop)
                {
                    clip.loopTime = loop;
                    dirty = true;
                }
            }
            importer.clipAnimations = clips;
        }

        if (dirty)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[CharacterAnimationSetup] Configured {path} as Humanoid (Loop={loop}).");
        }
    }

    private static AnimationClip LoadAnimationClip(string fbxPath)
    {
        UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var obj in objects)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
#endif

