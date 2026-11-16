using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreGameLobbyManager : MonoBehaviour
{
    public static PreGameLobbyManager Instance { get; private set; }

    [Header("Lobby Settings")]
    public bool isMatchStarted = false;
    public float countdownDuration = 5f;
    private float currentCountdown = 0f;
    private bool isCountingDown = false;

    [Header("Barriers")]
    public GameObject centralBarrier;
    public GameObject hostelGateBarrier;

    [Header("Spawn Reference")]
    public Vector3 playerLobbySpawnPos;

    private Material forcefieldMaterial;
    private Material stoneMat;
    private Material darkPlatMat;

    void Awake()
    {
        Instance = this;
        // Connect directly to existing scene objects without modifying, destroying, or recreating anything
        if (centralBarrier == null) centralBarrier = GameObject.Find("Central_Team_Forcefield_Barrier");
        if (hostelGateBarrier == null) hostelGateBarrier = GameObject.Find("Hostel_Entrance_Forcefield_Gate");

        GameObject lobbyFloor = GameObject.Find("Lobby_Terrace_Floor");
        if (lobbyFloor != null)
        {
            playerLobbySpawnPos = lobbyFloor.transform.position + new Vector3(-4.5f, 0.9f, 0f);
        }
    }

    void Update()
    {
        if (!isMatchStarted && !isCountingDown)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame))
            {
                StartGameCountdown();
            }
#else
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.F))
            {
                StartGameCountdown();
            }
#endif
        }
    }

    public void BuildLobbyEnvironment(Vector3 buildingCenter, float buildingMinZ)
    {
        // If the lobby arena already exists in the scene, DO NOT touch or overwrite it!
        // Preserves the exact visual look and any manual edits you make in Unity.
        if (GameObject.Find("[PRE_GAME_LOBBY_ARENA]") != null)
        {
            if (centralBarrier == null) centralBarrier = GameObject.Find("Central_Team_Forcefield_Barrier");
            if (hostelGateBarrier == null) hostelGateBarrier = GameObject.Find("Hostel_Entrance_Forcefield_Gate");
            GameObject floor = GameObject.Find("Lobby_Terrace_Floor");
            if (floor != null) playerLobbySpawnPos = floor.transform.position + new Vector3(-4.5f, 0.9f, 0f);
            return;
        }

        GameObject lobbyRoot = new GameObject("[PRE_GAME_LOBBY_ARENA]");

        float centerX = buildingCenter.x;
        float centerZ = buildingMinZ - 18.0f;
        Vector3 lobbyCenter = new Vector3(centerX, 0.0f, centerZ);

        playerLobbySpawnPos = lobbyCenter + new Vector3(-4.5f, 1.0f, 0f);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // 2. Clean Materials
        // Architectural Stone / Marble Concrete
        stoneMat = new Material(litShader);
        stoneMat.color = new Color(0.88f, 0.90f, 0.93f);
        stoneMat.SetFloat("_Smoothness", 0.35f);

        // Dark Platform & Canopy
        darkPlatMat = new Material(litShader);
        darkPlatMat.color = new Color(0.12f, 0.14f, 0.18f);
        darkPlatMat.SetFloat("_Smoothness", 0.5f);

        // Transparent Glowing Cyan Forcefield Material
        forcefieldMaterial = new Material(litShader);
        forcefieldMaterial.SetFloat("_Surface", 1); // Transparent
        forcefieldMaterial.SetFloat("_Blend", 0);   // Alpha
        forcefieldMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        forcefieldMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        forcefieldMaterial.SetInt("_ZWrite", 0);
        forcefieldMaterial.renderQueue = 3000;
        forcefieldMaterial.color = new Color(0.15f, 0.75f, 1.0f, 0.32f);
        forcefieldMaterial.EnableKeyword("_EMISSION");
        forcefieldMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 1.0f) * 1.8f);

        // 3. Base Terrace Platform
        GameObject terrace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terrace.name = "Lobby_Terrace_Floor";
        terrace.transform.parent = lobbyRoot.transform;
        terrace.transform.position = lobbyCenter + new Vector3(0f, 0.125f, 0f);
        terrace.transform.localScale = new Vector3(28f, 0.25f, 20f);
        terrace.GetComponent<Renderer>().material = darkPlatMat;

        // Terrace Border Curb
        GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        curb.name = "Lobby_Terrace_Curb";
        curb.transform.parent = lobbyRoot.transform;
        curb.transform.position = lobbyCenter + new Vector3(0f, 0.28f, 0f);
        curb.transform.localScale = new Vector3(28.4f, 0.15f, 20.4f);
        curb.GetComponent<Renderer>().material = stoneMat;

        // 4. Four Massive Architectural Pillars (Matching Reference Image)
        float px = 9.5f;
        float pz = 7.5f;
        Vector3[] pillarOffsets = new Vector3[]
        {
            new Vector3(-px, 0f, -pz),
            new Vector3(px, 0f, -pz),
            new Vector3(-px, 0f, pz),
            new Vector3(px, 0f, pz)
        };

        foreach (var poff in pillarOffsets)
        {
            // Base plinth
            GameObject pBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pBase.name = "Pillar_Base";
            pBase.transform.parent = lobbyRoot.transform;
            pBase.transform.position = lobbyCenter + poff + new Vector3(0f, 0.5f, 0f);
            pBase.transform.localScale = new Vector3(2.0f, 0.6f, 2.0f);
            pBase.GetComponent<Renderer>().material = stoneMat;

            // Pillar column shaft
            GameObject pShaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pShaft.name = "Pillar_Shaft";
            pShaft.transform.parent = lobbyRoot.transform;
            pShaft.transform.position = lobbyCenter + poff + new Vector3(0f, 4.2f, 0f);
            pShaft.transform.localScale = new Vector3(1.3f, 7.0f, 1.3f);
            pShaft.GetComponent<Renderer>().material = stoneMat;

            // Top capital
            GameObject pCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pCap.name = "Pillar_Capital";
            pCap.transform.parent = lobbyRoot.transform;
            pCap.transform.position = lobbyCenter + poff + new Vector3(0f, 7.85f, 0f);
            pCap.transform.localScale = new Vector3(1.9f, 0.4f, 1.9f);
            pCap.GetComponent<Renderer>().material = stoneMat;
        }

        // 5. Overhead Canopy Roof (Floating Architectural Roof from Reference)
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.name = "Lobby_Canopy_Roof";
        canopy.transform.parent = lobbyRoot.transform;
        canopy.transform.position = lobbyCenter + new Vector3(0f, 8.2f, 0f);
        canopy.transform.localScale = new Vector3(22f, 0.5f, 17f);
        canopy.GetComponent<Renderer>().material = darkPlatMat;

        // 6. THE CENTER FORCEFIELD BARRIER (See-through border between the two sides)
        centralBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centralBarrier.name = "Central_Team_Forcefield_Barrier";
        centralBarrier.transform.parent = lobbyRoot.transform;
        centralBarrier.transform.position = lobbyCenter + new Vector3(0f, 2.7f, 0f);
        centralBarrier.transform.localScale = new Vector3(0.12f, 5.2f, 20.2f);
        centralBarrier.GetComponent<Renderer>().material = forcefieldMaterial;
        
        // Solid physical collider preventing either side from crossing
        BoxCollider cbCol = centralBarrier.GetComponent<BoxCollider>();
        cbCol.isTrigger = false;

        // 7. THE FRONT HOSTEL GATE BARRIER (Blocks entering building until match starts)
        hostelGateBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hostelGateBarrier.name = "Hostel_Entrance_Forcefield_Gate";
        hostelGateBarrier.transform.parent = lobbyRoot.transform;
        hostelGateBarrier.transform.position = lobbyCenter + new Vector3(0f, 2.7f, 10.1f);
        hostelGateBarrier.transform.localScale = new Vector3(28.2f, 5.2f, 0.15f);
        hostelGateBarrier.GetComponent<Renderer>().material = forcefieldMaterial;
        hostelGateBarrier.GetComponent<BoxCollider>().isTrigger = false;

        Debug.Log("[PreGameLobbyManager] Clean Pre-game Staging Arena created with transparent central barrier.");
    }

    private void CleanupResidualLobbyObjects()
    {
        // Clean old arena root if present
        GameObject oldRoot = GameObject.Find("[PRE_GAME_LOBBY_ARENA]");
        if (oldRoot != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(oldRoot);
            else Destroy(oldRoot);
#else
            Destroy(oldRoot);
#endif
        }

        // Clean any standalone props, poles, pad/sign/rival objects
        string[] residualNames = new string[]
        {
            "Forcefield_Emitter_Pole", "Lobby_Deco_Prop",
            "Blue_Pad_1", "Blue_Pad_2", "Blue_Pad_3", "Blue_Pad_4",
            "Red_Pad_1", "Red_Pad_2", "Red_Pad_3", "Red_Pad_4",
            "Pad_Riser", "Pad_GlowCore", "Pad_GlowLight",
            "BlueTeam_Zone_Sign", "RedTeam_Zone_Sign",
            "Rival_Player_Preview", "Rival_NameTag_Badge", "Badge_Name", "Badge_Status"
        };

        foreach (var rName in residualNames)
        {
            GameObject[] found = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in found)
            {
                if (go != null && (go.name == rName || go.name.StartsWith("Blue_Pad_") || go.name.StartsWith("Red_Pad_") || go.name.StartsWith("Forcefield_Emitter_Pole") || go.name.StartsWith("Lobby_Deco_Prop")))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(go);
                    else Destroy(go);
#else
                    Destroy(go);
#endif
                }
            }
        }
    }

    public void StartGameCountdown()
    {
        if (isCountingDown || isMatchStarted) return;
        isCountingDown = true;
        currentCountdown = countdownDuration;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        while (currentCountdown > 0f)
        {
            yield return new WaitForSeconds(1.0f);
            currentCountdown -= 1.0f;
        }

        BeginMatch();
    }

    public void BeginMatch()
    {
        isMatchStarted = true;
        isCountingDown = false;

        // Dissolve / Lower Forcefields
        if (centralBarrier != null)
        {
            StartCoroutine(DissolveBarrier(centralBarrier));
        }
        if (hostelGateBarrier != null)
        {
            StartCoroutine(DissolveBarrier(hostelGateBarrier));
        }

        Debug.Log("[PreGameLobbyManager] Forcefields dropped! Match started!");
    }

    private IEnumerator DissolveBarrier(GameObject barrier)
    {
        if (barrier == null) yield break;

        Collider col = barrier.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer rend = barrier.GetComponent<Renderer>();
        Vector3 startScale = barrier.transform.localScale;

        float elapsed = 0f;
        float dur = 0.8f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            barrier.transform.localScale = new Vector3(startScale.x, Mathf.Lerp(startScale.y, 0.01f, t), startScale.z);
            if (rend != null && rend.material != null)
            {
                Color c = rend.material.color;
                c.a = Mathf.Lerp(0.32f, 0.0f, t);
                rend.material.color = c;
            }
            yield return null;
        }
        barrier.SetActive(false);
    }

    void OnGUI()
    {
        // 1. Top-Left Advanced Lobby System Info (Matching Reference Image)
        if (!isMatchStarted)
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 20;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(1.0f, 0.82f, 0.4f); // Golden amber

            GUIStyle bulletStyle = new GUIStyle(GUI.skin.label);
            bulletStyle.fontSize = 13;
            bulletStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);

            GUI.Label(new Rect(25, 25, 300, 30), "Advanced Lobby System", headerStyle);
            GUI.Label(new Rect(25, 55, 300, 20), "• Replicated Lobby System", bulletStyle);
            GUI.Label(new Rect(25, 75, 300, 20), "• Waiting for players ready", bulletStyle);
            GUI.Label(new Rect(25, 95, 300, 20), "• Chat System Active", bulletStyle);

            // Center Forcefield Info
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label);
            bannerStyle.fontSize = 14;
            bannerStyle.alignment = TextAnchor.MiddleCenter;
            bannerStyle.fontStyle = FontStyle.Bold;
            bannerStyle.normal.textColor = new Color(0.4f, 0.85f, 1.0f);

            GUI.Label(new Rect(Screen.width * 0.5f - 200, 25, 400, 30), "◄ TEAM 1  |  TRANSPARENT BARRIER  |  TEAM 2 ►", bannerStyle);
        }

        // 2. Countdown Display
        if (isCountingDown)
        {
            GUIStyle countStyle = new GUIStyle(GUI.skin.label);
            countStyle.fontSize = 42;
            countStyle.fontStyle = FontStyle.Bold;
            countStyle.alignment = TextAnchor.MiddleCenter;
            countStyle.normal.textColor = new Color(1.0f, 0.9f, 0.2f);

            GUI.Label(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.22f, 400, 60), $"MATCH STARTS IN: {Mathf.CeilToInt(currentCountdown)}", countStyle);
        }

        // 3. Green "START GAME" Button (Matching Reference Image bottom center)
        if (!isMatchStarted && !isCountingDown)
        {
            float btnW = 240;
            float btnH = 46;
            Rect btnRect = new Rect((Screen.width - btnW) * 0.5f, Screen.height - 85, btnW, btnH);

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.18f, 0.78f, 0.28f); // Bright vibrant green

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 17;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.normal.textColor = Color.white;

            if (GUI.Button(btnRect, "START GAME  [Enter]", btnStyle))
            {
                StartGameCountdown();
            }

            GUI.backgroundColor = oldBg;

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 11;
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.85f);
            GUI.Label(new Rect((Screen.width - 300) * 0.5f, Screen.height - 35, 300, 20), "Press [Enter] or [Esc] to toggle cursor and click", hintStyle);
        }

        // 4. Match Started Notification
        if (isMatchStarted && currentCountdown <= 0f && Time.timeSinceLevelLoad < 25f)
        {
            GUIStyle goStyle = new GUIStyle(GUI.skin.label);
            goStyle.fontSize = 24;
            goStyle.fontStyle = FontStyle.Bold;
            goStyle.alignment = TextAnchor.MiddleCenter;
            goStyle.normal.textColor = new Color(0.2f, 1.0f, 0.4f);

            GUI.Label(new Rect(Screen.width * 0.5f - 250, 45, 500, 35), "BARRIERS DOWN! ENTER THE HOSTEL AND PLAY!", goStyle);
        }
    }
}
