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

    [Header("References")]
    public GameObject centralBarrier;
    public GameObject hostelGateBarrier;
    public GameObject rivalBotInstance;
    public Transform blueSpawnPoint;
    public Transform redSpawnPoint;

    private List<GameObject> bluePads = new List<GameObject>();
    private List<GameObject> redPads = new List<GameObject>();

    private Material forcefieldMaterial;
    private Material bluePadMaterial;
    private Material redPadMaterial;

    private bool rivalReady = false;

    void Awake()
    {
        Instance = this;
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
        // Clean up any existing lobby
        GameObject existing = GameObject.Find("[PRE_GAME_LOBBY_ARENA]");
        if (existing != null) Destroy(existing);

        GameObject lobbyRoot = new GameObject("[PRE_GAME_LOBBY_ARENA]");

        float centerX = buildingCenter.x;
        float centerZ = buildingMinZ - 18.0f;
        Vector3 lobbyCenter = new Vector3(centerX, 0.0f, centerZ);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // 1. Materials
        // Architectural Stone / Concrete
        Material stoneMat = new Material(litShader);
        stoneMat.color = new Color(0.88f, 0.90f, 0.93f);
        stoneMat.SetFloat("_Smoothness", 0.35f);

        // Dark Platform & Canopy
        Material darkPlatMat = new Material(litShader);
        darkPlatMat.color = new Color(0.12f, 0.14f, 0.18f);
        darkPlatMat.SetFloat("_Smoothness", 0.5f);

        // Glowing Blue Team Material
        bluePadMaterial = new Material(litShader);
        bluePadMaterial.color = new Color(0.05f, 0.55f, 0.95f);
        bluePadMaterial.EnableKeyword("_EMISSION");
        bluePadMaterial.SetColor("_EmissionColor", new Color(0.0f, 0.6f, 1.0f) * 1.5f);

        // Glowing Red Team Material
        redPadMaterial = new Material(litShader);
        redPadMaterial.color = new Color(0.92f, 0.18f, 0.25f);
        redPadMaterial.EnableKeyword("_EMISSION");
        redPadMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.2f) * 1.5f);

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

        // 2. Base Terrace Platform
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

        // 3. Four Massive Architectural Pillars (Matching Reference Image)
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

        // 4. Overhead Canopy Roof (Floating Architectural Roof from Reference)
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.name = "Lobby_Canopy_Roof";
        canopy.transform.parent = lobbyRoot.transform;
        canopy.transform.position = lobbyCenter + new Vector3(0f, 8.2f, 0f);
        canopy.transform.localScale = new Vector3(22f, 0.5f, 17f);
        canopy.GetComponent<Renderer>().material = darkPlatMat;

        // 5. Sci-Fi Geometric Pedestals / Shapes (As seen in Reference)
        CreateGeometricProp(lobbyRoot.transform, lobbyCenter + new Vector3(-8.5f, 0.7f, 2f), PrimitiveType.Cube, new Vector3(1.4f, 1.0f, 1.4f), stoneMat);
        CreateGeometricProp(lobbyRoot.transform, lobbyCenter + new Vector3(-7f, 0.9f, 2f), PrimitiveType.Cylinder, new Vector3(0.9f, 0.8f, 0.9f), stoneMat);
        CreateGeometricProp(lobbyRoot.transform, lobbyCenter + new Vector3(8.5f, 0.7f, 2f), PrimitiveType.Cube, new Vector3(1.4f, 1.0f, 1.4f), stoneMat);
        CreateGeometricProp(lobbyRoot.transform, lobbyCenter + new Vector3(7f, 0.9f, 2f), PrimitiveType.Cylinder, new Vector3(0.9f, 0.8f, 0.9f), stoneMat);

        // 6. TEAM BLUE ZONE (Left / West - Alpha Team)
        GameObject blueLabel = new GameObject("BlueTeam_Zone_Sign");
        blueLabel.transform.parent = lobbyRoot.transform;
        blueLabel.transform.position = lobbyCenter + new Vector3(-5.5f, 3.2f, -pz);
        TextMesh btm = blueLabel.AddComponent<TextMesh>();
        btm.text = "ALPHA TEAM [BLUE]";
        btm.fontSize = 28;
        btm.alignment = TextAlignment.Center;
        btm.anchor = TextAnchor.MiddleCenter;
        btm.color = new Color(0.2f, 0.75f, 1.0f);
        blueLabel.transform.localScale = Vector3.one * 0.15f;

        // Team Blue Pads
        Vector3[] bluePadCoords = new Vector3[]
        {
            lobbyCenter + new Vector3(-4.5f, 0.32f, -3.5f), // Primary Player Pad
            lobbyCenter + new Vector3(-4.5f, 0.32f, 3.5f),
            lobbyCenter + new Vector3(-7.5f, 0.32f, -0.5f),
            lobbyCenter + new Vector3(-7.5f, 0.32f, 4.5f)
        };

        for (int i = 0; i < bluePadCoords.Length; i++)
        {
            GameObject pad = CreateTeamPad($"Blue_Pad_{i + 1}", bluePadCoords[i], lobbyRoot.transform, bluePadMaterial, new Color(0f, 0.6f, 1.0f));
            bluePads.Add(pad);
            if (i == 0)
            {
                blueSpawnPoint = pad.transform;
            }
        }

        // 7. TEAM RED ZONE (Right / East - Bravo Team)
        GameObject redLabel = new GameObject("RedTeam_Zone_Sign");
        redLabel.transform.parent = lobbyRoot.transform;
        redLabel.transform.position = lobbyCenter + new Vector3(5.5f, 3.2f, -pz);
        TextMesh rtm = redLabel.AddComponent<TextMesh>();
        rtm.text = "BRAVO TEAM [RED]";
        rtm.fontSize = 28;
        rtm.alignment = TextAlignment.Center;
        rtm.anchor = TextAnchor.MiddleCenter;
        rtm.color = new Color(1.0f, 0.25f, 0.35f);
        redLabel.transform.localScale = Vector3.one * 0.15f;

        // Team Red Pads
        Vector3[] redPadCoords = new Vector3[]
        {
            lobbyCenter + new Vector3(4.5f, 0.32f, -3.5f), // Primary Rival Pad
            lobbyCenter + new Vector3(4.5f, 0.32f, 3.5f),
            lobbyCenter + new Vector3(7.5f, 0.32f, -0.5f),
            lobbyCenter + new Vector3(7.5f, 0.32f, 4.5f)
        };

        for (int i = 0; i < redPadCoords.Length; i++)
        {
            GameObject pad = CreateTeamPad($"Red_Pad_{i + 1}", redPadCoords[i], lobbyRoot.transform, redPadMaterial, new Color(1.0f, 0.2f, 0.3f));
            redPads.Add(pad);
            if (i == 0)
            {
                redSpawnPoint = pad.transform;
            }
        }

        // 8. THE CENTER FORCEFIELD BARRIER (See-through border between teams)
        centralBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centralBarrier.name = "Central_Team_Forcefield_Barrier";
        centralBarrier.transform.parent = lobbyRoot.transform;
        centralBarrier.transform.position = lobbyCenter + new Vector3(0f, 2.7f, 0f);
        centralBarrier.transform.localScale = new Vector3(0.12f, 5.2f, 20.2f);
        centralBarrier.GetComponent<Renderer>().material = forcefieldMaterial;
        
        // Solid physical collider preventing either team from crossing
        BoxCollider cbCol = centralBarrier.GetComponent<BoxCollider>();
        cbCol.isTrigger = false;

        // Decorative Laser Frame Poles along the center divider
        for (float zOff = -9f; zOff <= 9f; zOff += 6f)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Forcefield_Emitter_Pole";
            pole.transform.parent = centralBarrier.transform;
            pole.transform.position = lobbyCenter + new Vector3(0f, 2.7f, zOff);
            pole.transform.localScale = new Vector3(0.25f, 2.6f, 0.25f);
            pole.GetComponent<Renderer>().material = darkPlatMat;
        }

        // 9. THE FRONT HOSTEL GATE BARRIER (Blocks entering building until match starts)
        hostelGateBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hostelGateBarrier.name = "Hostel_Entrance_Forcefield_Gate";
        hostelGateBarrier.transform.parent = lobbyRoot.transform;
        hostelGateBarrier.transform.position = lobbyCenter + new Vector3(0f, 2.7f, 10.1f);
        hostelGateBarrier.transform.localScale = new Vector3(28.2f, 5.2f, 0.15f);
        hostelGateBarrier.GetComponent<Renderer>().material = forcefieldMaterial;
        hostelGateBarrier.GetComponent<BoxCollider>().isTrigger = false;

        // 10. SPAWN OPPONENT PREVIEW CHARACTER ON RED PAD (Matching Reference Screenshot)
        SpawnOpponentPreview(redSpawnPoint.position + Vector3.up * 0.95f, lobbyRoot.transform);

        Debug.Log("[PreGameLobbyManager] Pre-game Team Lobby Arena created successfully with see-through central forcefield!");
    }

    private GameObject CreateTeamPad(string name, Vector3 pos, Transform parent, Material glowMat, Color lightColor)
    {
        GameObject padRoot = new GameObject(name);
        padRoot.transform.parent = parent;
        padRoot.transform.position = pos;

        // Base riser
        GameObject riser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        riser.name = "Pad_Riser";
        riser.transform.parent = padRoot.transform;
        riser.transform.position = pos;
        riser.transform.localScale = new Vector3(2.4f, 0.18f, 2.4f);

        // Glowing Core Surface
        GameObject glowCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowCore.name = "Pad_GlowCore";
        glowCore.transform.parent = padRoot.transform;
        glowCore.transform.position = pos + new Vector3(0f, 0.08f, 0f);
        glowCore.transform.localScale = new Vector3(2.0f, 0.06f, 2.0f);
        glowCore.GetComponent<Renderer>().material = glowMat;

        // Soft Point Light
        GameObject lightObj = new GameObject("Pad_GlowLight");
        lightObj.transform.parent = padRoot.transform;
        lightObj.transform.position = pos + new Vector3(0f, 0.6f, 0f);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.range = 3.5f;
        l.intensity = 1.6f;

        return padRoot;
    }

    private void CreateGeometricProp(Transform parent, Vector3 pos, PrimitiveType type, Vector3 scale, Material mat)
    {
        GameObject prop = GameObject.CreatePrimitive(type);
        prop.name = "Lobby_Deco_Prop";
        prop.transform.parent = parent;
        prop.transform.position = pos;
        prop.transform.localScale = scale;
        prop.GetComponent<Renderer>().material = mat;
    }

    private void SpawnOpponentPreview(Vector3 pos, Transform parent)
    {
        rivalBotInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rivalBotInstance.name = "Rival_Player_Preview";
        rivalBotInstance.transform.parent = parent;
        rivalBotInstance.transform.position = pos;
        rivalBotInstance.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Facing across center barrier towards Blue team!

        // Give Mannequin Red/Metallic Skin
        Renderer r = rivalBotInstance.GetComponent<Renderer>();
        if (r != null)
        {
            Material rivalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rivalMat.color = new Color(0.85f, 0.22f, 0.25f);
            rivalMat.SetFloat("_Metallic", 0.7f);
            rivalMat.SetFloat("_Smoothness", 0.6f);
            r.material = rivalMat;
        }

        // 3D Billboard Overhead Name Badge (Matching Reference: "DR Dreamrise / Not Ready")
        GameObject badgeObj = new GameObject("Rival_NameTag_Badge");
        badgeObj.transform.parent = rivalBotInstance.transform;
        badgeObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        // Icon + Name
        GameObject nameTextObj = new GameObject("Badge_Name");
        nameTextObj.transform.parent = badgeObj.transform;
        nameTextObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        TextMesh ntm = nameTextObj.AddComponent<TextMesh>();
        ntm.text = "[DR] Dreamrise";
        ntm.fontSize = 24;
        ntm.alignment = TextAlignment.Center;
        ntm.anchor = TextAnchor.MiddleCenter;
        ntm.color = Color.white;
        nameTextObj.transform.localScale = Vector3.one * 0.12f;

        // Status Plate ("Not Ready")
        GameObject statusTextObj = new GameObject("Badge_Status");
        statusTextObj.transform.parent = badgeObj.transform;
        statusTextObj.transform.localPosition = Vector3.zero;
        TextMesh stm = statusTextObj.AddComponent<TextMesh>();
        stm.text = "NOT READY";
        stm.fontSize = 20;
        stm.alignment = TextAlignment.Center;
        stm.anchor = TextAnchor.MiddleCenter;
        stm.color = new Color(1.0f, 0.3f, 0.3f);
        statusTextObj.transform.localScale = Vector3.one * 0.12f;

        badgeObj.AddComponent<LobbyBillboardLook>();
    }

    public void StartGameCountdown()
    {
        if (isCountingDown || isMatchStarted) return;
        isCountingDown = true;
        currentCountdown = countdownDuration;
        rivalReady = true;

        // Update Rival status to READY
        if (rivalBotInstance != null)
        {
            Transform statusT = rivalBotInstance.transform.Find("Rival_NameTag_Badge/Badge_Status");
            if (statusT != null)
            {
                TextMesh tm = statusT.GetComponent<TextMesh>();
                if (tm != null)
                {
                    tm.text = "READY!";
                    tm.color = new Color(0.2f, 1.0f, 0.4f);
                }
            }
        }

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
            if (rend != null)
            {
                Color c = rend.material.color;
                c.a = Mathf.Lerp(0.35f, 0.0f, t);
                rend.material.color = c;
            }
            yield return null;
        }
        barrier.SetActive(false);
    }

    void OnGUI()
    {
        // 1. Top-Left Advanced Lobby System Info (Exactly matching Reference Image)
        if (!isMatchStarted)
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 20;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(1.0f, 0.82f, 0.4f); // Golden amber like screenshot

            GUIStyle bulletStyle = new GUIStyle(GUI.skin.label);
            bulletStyle.fontSize = 13;
            bulletStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);

            GUI.Label(new Rect(25, 25, 300, 30), "Advanced Lobby System", headerStyle);
            GUI.Label(new Rect(25, 55, 300, 20), "• Replicated Lobby System", bulletStyle);
            GUI.Label(new Rect(25, 75, 300, 20), "• Waiting for players ready", bulletStyle);
            GUI.Label(new Rect(25, 95, 300, 20), "• Chat System Active", bulletStyle);

            // Center Forcefield Warning
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label);
            bannerStyle.fontSize = 14;
            bannerStyle.alignment = TextAnchor.MiddleCenter;
            bannerStyle.fontStyle = FontStyle.Bold;
            bannerStyle.normal.textColor = new Color(0.4f, 0.85f, 1.0f);

            GUI.Label(new Rect(Screen.width * 0.5f - 200, 25, 400, 30), "◄ ALPHA TEAM [BLUE]  |  FORCEFIELD BARRIER  |  BRAVO TEAM [RED] ►", bannerStyle);
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

            // Small prompt under button
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

            GUI.Label(new Rect(Screen.width * 0.5f - 250, 45, 500, 35), "BARRIERS DOWN! ENTER THE HOSTEL AND ELIMINATE ENEMIES!", goStyle);
        }
    }
}

// Billboard component so nametags always face the active camera
public class LobbyBillboardLook : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
