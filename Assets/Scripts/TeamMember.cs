using System;
using UnityEngine;

public enum TeamType
{
    TeamA_Blue,
    TeamB_Red
}

public class TeamMember : MonoBehaviour
{
    [Header("Team Configuration")]
    [SerializeField] private TeamType _team = TeamType.TeamA_Blue;
    public TeamType team
    {
        get => _team;
        set
        {
            _team = value;
            ApplyTeamVisuals();
        }
    }

    [Header("Team Materials (Assigned or Auto-Loaded)")]
    public Material teamBlueMaterial;
    public Material teamRedMaterial;

    [Header("Renderers to Tint")]
    public Renderer[] characterRenderers;

    private void Awake()
    {
        LoadMaterialsIfNeeded();
        if (characterRenderers == null || characterRenderers.Length == 0)
        {
            characterRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void Start()
    {
        ApplyTeamVisuals();
    }

    private void LoadMaterialsIfNeeded()
    {
        if (teamBlueMaterial == null)
        {
            teamBlueMaterial = Resources.Load<Material>("Materials/Ch38_Team_Blue");
            if (teamBlueMaterial == null)
            {
#if UNITY_EDITOR
                teamBlueMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Characters/Materials/Ch38_Team_Blue.mat");
#endif
            }
        }

        if (teamRedMaterial == null)
        {
            teamRedMaterial = Resources.Load<Material>("Materials/Ch38_Team_Red");
            if (teamRedMaterial == null)
            {
#if UNITY_EDITOR
                teamRedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Characters/Materials/Ch38_Team_Red.mat");
#endif
            }
        }
    }

    public void ApplyTeamVisuals()
    {
        LoadMaterialsIfNeeded();
        Material targetMat = (_team == TeamType.TeamA_Blue) ? teamBlueMaterial : teamRedMaterial;

        if (targetMat == null) return;

        if (characterRenderers == null || characterRenderers.Length == 0)
        {
            characterRenderers = GetComponentsInChildren<Renderer>(true);
        }

        foreach (var rend in characterRenderers)
        {
            if (rend == null) continue;
            // Don't recolor the gun or weapons attached to the character
            if (rend.gameObject.name.Contains("Gun") || rend.gameObject.name.Contains("Weapon") || rend.gameObject.name.Contains("Rifle"))
            {
                continue;
            }

            rend.material = targetMat;
        }
    }

    public void SetTeam(TeamType newTeam)
    {
        team = newTeam;
    }
}

