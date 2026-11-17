using UnityEngine;

/// <summary>
/// Attaches character hands to weapon grip points using Unity's native Mecanim IK solver.
/// All grip positions, rotations, and finger fixes are hardcoded permanently with zero Inspector clutter.
/// </summary>
public class WeaponIKController : MonoBehaviour
{
    private Animator animator;
    private Transform rightHandTarget;
    private Transform leftHandTarget;
    private Transform ring1Bone;
    private Transform ring2Bone;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        AutoWireTargets();
        FindBones();
    }

    private void FindBones()
    {
        Transform root = transform.root;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.EndsWith("LeftHandRing1")) ring1Bone = t;
            else if (n.EndsWith("LeftHandRing2")) ring2Bone = t;
        }
    }

    public void AutoWireTargets()
    {
        Transform root = transform.root;
        Transform gun = null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Equipped_Rifle")
            {
                gun = t;
                break;
            }
        }

        if (gun != null)
        {
            // Right Hand Grip (Hardcoded trigger position)
            Transform rGrip = gun.Find("RightHand_Grip");
            if (rGrip == null)
            {
                GameObject rg = new GameObject("RightHand_Grip");
                rGrip = rg.transform;
                rGrip.SetParent(gun, false);
                rGrip.localPosition = new Vector3(-0.06f, -0.05f, 0f);
                rGrip.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            rightHandTarget = rGrip;

            // Left Hand Grip (Hardcoded handguard position)
            Transform lGrip = gun.Find("LeftHand_Grip");
            if (lGrip == null)
            {
                GameObject lg = new GameObject("LeftHand_Grip");
                lGrip = lg.transform;
                lGrip.SetParent(gun, false);
                lGrip.localPosition = new Vector3(0.050f, -0.020f, 0.020f);
                lGrip.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                lGrip.localPosition = new Vector3(0.050f, -0.020f, 0.020f);
            }
            leftHandTarget = lGrip;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        if (rightHandTarget == null || leftHandTarget == null)
        {
            AutoWireTargets();
        }

        // Right Hand IK (Main Trigger Hand)
        if (rightHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
        }

        // Left Hand IK (Support Hand on Barrel)
        if (leftHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
        }
    }

    void LateUpdate()
    {
        // 4th finger (Ring finger) permanent hardcoded pose
        // Overrides the animation clip every frame so the finger stays naturally gripped
        if (ring1Bone != null)
        {
            ring1Bone.localRotation = Quaternion.Euler(0.89f, 0f, 0f);
        }
        if (ring2Bone != null)
        {
            ring2Bone.localRotation = Quaternion.Euler(64.0f, 0f, 0f);
        }
    }
}
