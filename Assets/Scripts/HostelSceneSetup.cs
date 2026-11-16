using UnityEngine;
using UnityEngine.Rendering;

public class HostelSceneSetup : MonoBehaviour
{
    [Header("Building Setup")]
    public GameObject hostelPrefab;
    public float groundPlaneSize = 160f;

    [Header("Enemies Setup")]
    public bool spawnEnemies = true;
    public int enemiesPerFloor = 2;
    public float enemyMoveSpeed = 2.2f;

    [Header("Player Setup")]
    public Vector3 playerSpawnOffset = new Vector3(0f, 1.1f, -12f);

    private Bounds buildingBounds;
    private GameObject buildingInstance;
    private GameObject playerInstance;

    void Awake()
    {
        InitializeHostelScene();
    }

    public void InitializeHostelScene()
    {
        // 1. Setup Lighting & Atmosphere
        SetupLightingAndAtmosphere();

        // 2. Setup Hostel Building
        SetupBuilding();

        // 3. Setup Ground Courtyard Plane
        SetupGroundPlane();

        // 4. Setup Player in Front of Hostel
        SetupPlayer();

        // 5. Setup Gameplay Systems
        SetupSystems();

        // 6. Spawn Enemies Across All Floors
        if (spawnEnemies)
        {
            SetupEnemies();
        }

        Debug.Log("[HostelSceneSetup] Hostel scene initialized successfully!");
    }

    private void SetupLightingAndAtmosphere()
    {
        // Directional Light
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light (Sun)");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }
        dirLight.color = new Color(1f, 0.96f, 0.90f);
        dirLight.intensity = 1.25f;
        dirLight.shadows = LightShadows.Soft;

        // Post-Processing Volume with Bloom
        Volume vol = Object.FindFirstObjectByType<Volume>();
        if (vol == null)
        {
            GameObject volObj = new GameObject("Global PostProcess Volume");
            vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;
#if UNITY_EDITOR
            VolumeProfile profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile != null) vol.profile = profile;
#endif
        }
    }

    private void SetupBuilding()
    {
        buildingInstance = GameObject.Find("[HOSTEL_BUILDING]");
        if (buildingInstance == null)
        {
            if (hostelPrefab == null)
            {
#if UNITY_EDITOR
                hostelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Hostel_Optimized.fbx");
                if (hostelPrefab == null)
                {
                    hostelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/HostelBuilding.fbx");
                }
#endif
            }

            if (hostelPrefab != null)
            {
                buildingInstance = Instantiate(hostelPrefab, Vector3.zero, Quaternion.identity);
                buildingInstance.name = "[HOSTEL_BUILDING]";
            }
        }

        if (buildingInstance != null)
        {
            // Ensure all meshes have MeshColliders so player/enemies can walk on floors and stairs
            int colCount = 0;
            MeshRenderer[] renderers = buildingInstance.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = r.GetComponent<MeshCollider>();
                    if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    colCount++;
                }
            }
            Debug.Log($"[HostelSceneSetup] Configured {colCount} MeshColliders on Hostel geometry.");
            buildingBounds = CalculateBounds(buildingInstance);
        }
        else
        {
            buildingBounds = new Bounds(Vector3.zero, new Vector3(74f, 14.5f, 22f));
        }
    }

    private void SetupGroundPlane()
    {
        if (GameObject.Find("Ground_Courtyard") != null) return;

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "Ground_Courtyard";

        float groundY = buildingBounds.min.y;
        float w = Mathf.Max(buildingBounds.size.x * 1.5f, groundPlaneSize);
        float l = Mathf.Max(buildingBounds.size.z * 2.5f, groundPlaneSize);

        plane.transform.position = new Vector3(buildingBounds.center.x, groundY - 0.25f, buildingBounds.center.z);
        plane.transform.localScale = new Vector3(w, 0.5f, l);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.24f, 0.26f, 0.28f);
        plane.GetComponent<MeshRenderer>().material = mat;
    }

    private void SetupPlayer()
    {
        var existingPC = Object.FindFirstObjectByType<PlayerController>();
        if (existingPC != null)
        {
            playerInstance = existingPC.gameObject;
            return;
        }

        // Clean up standalone scene camera
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.parent == null)
        {
            Destroy(mainCam.gameObject);
        }

        playerInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerInstance.name = "Player_Survivor";
        playerInstance.tag = "Player";

        // Position in front of the hostel entrance
        float spawnX = buildingBounds.center.x + playerSpawnOffset.x;
        float spawnY = buildingBounds.min.y + playerSpawnOffset.y;
        float spawnZ = buildingBounds.min.z + playerSpawnOffset.z;
        playerInstance.transform.position = new Vector3(spawnX, spawnY, spawnZ);
        playerInstance.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Looking towards building (+Z)

        // CharacterController
        var cc = playerInstance.GetComponent<CharacterController>();
        if (cc == null) cc = playerInstance.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = Vector3.zero;
        cc.slopeLimit = 55f;
        cc.stepOffset = 0.45f;

        // Camera
        GameObject camObj = new GameObject("PlayerCamera");
        camObj.transform.parent = playerInstance.transform;
        camObj.transform.localPosition = new Vector3(0, 0.7f, 0.2f);
        camObj.transform.localRotation = Quaternion.identity;

        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        // Player scripts
        var pc = playerInstance.AddComponent<PlayerController>();
        pc.cameraHolder = camObj.transform;

        var ft = playerInstance.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var ps = playerInstance.AddComponent<PlayerShooting>();
        ps.playerCamera = cam;

#if UNITY_EDITOR
        // Equip PolyOne Sci-Fi Rifle
        GameObject gunPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyOne/Free Sci Fi Weapon/Prefabs/SM_Sci_FiWeapon_RifleGun.prefab");
        if (gunPrefab != null)
        {
            GameObject gunInstance = Instantiate(gunPrefab, camObj.transform);
            gunInstance.name = "Equipped_Rifle";
            gunInstance.transform.localPosition = new Vector3(0.20f, -0.22f, 0.48f);
            gunInstance.transform.localRotation = Quaternion.Euler(0.5f, -91.5f, 0.8f);

            GameObject muzzle = new GameObject("GunMuzzle");
            muzzle.transform.parent = gunInstance.transform;
            muzzle.transform.localPosition = new Vector3(0.495f, 0.075f, 0f);
            muzzle.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            ps.gunTransform = gunInstance.transform;
            ps.gunMuzzle = muzzle.transform;
        }
#endif

        playerInstance.AddComponent<GameHUDAndTasks>();
    }

    private void SetupSystems()
    {
        GameObject sys = GameObject.Find("[GAMEPLAY_SYSTEMS]");
        if (sys == null)
        {
            sys = new GameObject("[GAMEPLAY_SYSTEMS]");
        }

        // Floor Tracker
        var tracker = sys.GetComponent<FloorTracker>();
        if (tracker == null) tracker = sys.AddComponent<FloorTracker>();
        tracker.targetTransform = playerInstance != null ? playerInstance.transform : transform;
        tracker.groundFloorY = buildingBounds.min.y;
        tracker.floorHeight = buildingBounds.size.y / 4.0f;

        // Minimap UI
        var minimap = sys.GetComponent<FloorMinimapUI>();
        if (minimap == null) minimap = sys.AddComponent<FloorMinimapUI>();
        minimap.playerTransform = playerInstance != null ? playerInstance.transform : transform;
        minimap.floorTracker = tracker;

        // Chat UI
        if (sys.GetComponent<TeamTextChatUI>() == null) sys.AddComponent<TeamTextChatUI>();

        // Voice Controller
        var voice = sys.GetComponent<DualChannelVoiceController>();
        if (voice == null) voice = sys.AddComponent<DualChannelVoiceController>();
        voice.listenerTransform = Camera.main != null ? Camera.main.transform : (playerInstance != null ? playerInstance.transform : transform);

        // Spectator Camera
        if (sys.GetComponent<SpectatorCameraController>() == null) sys.AddComponent<SpectatorCameraController>();
    }

    private void SetupEnemies()
    {
        if (GameObject.Find("[ENEMIES_ROOT]") != null) return;

        GameObject enemiesRoot = new GameObject("[ENEMIES_ROOT]");
        float floorH = buildingBounds.size.y / 4.0f;
        float groundY = buildingBounds.min.y;

        string[] floorTitles = new string[] { "Lobby_Floor1", "LowerRooms_Floor2", "UpperRooms_Floor3", "Penthouse_Floor4" };

        for (int f = 0; f < 4; f++)
        {
            float floorLevelY = groundY + (f * floorH) + 1.1f;
            GameObject fGroup = new GameObject($"Enemies_{floorTitles[f]}");
            fGroup.transform.parent = enemiesRoot.transform;

            // Spawn West Wing Enemy
            Vector3 posW = new Vector3(buildingBounds.center.x - (buildingBounds.size.x * 0.24f), floorLevelY, buildingBounds.center.z);
            CreateEnemy($"Enemy_F{f + 1}_West", posW, fGroup.transform, -1);

            // Spawn East Wing Enemy
            Vector3 posE = new Vector3(buildingBounds.center.x + (buildingBounds.size.x * 0.24f), floorLevelY, buildingBounds.center.z);
            CreateEnemy($"Enemy_F{f + 1}_East", posE, fGroup.transform, 1);
        }
    }

    private void CreateEnemy(string name, Vector3 pos, Transform parent, int direction)
    {
        GameObject botObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        botObj.name = name;
        botObj.transform.parent = parent;
        botObj.transform.position = pos;

        // Distinct Hostile Red Skin
        Renderer r = botObj.GetComponent<Renderer>();
        if (r != null)
        {
            Material redMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            redMat.color = new Color(0.92f, 0.20f, 0.20f);
            r.material = redMat;
        }

        var ft = botObj.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var bot = botObj.AddComponent<TemporaryDummyBot>();
        bot.botName = name;
        bot.isTeammate = false;
        bot.moveSpeed = enemyMoveSpeed;
        bot.visionRange = 22f;

        // Patrol corridor waypoints
        float patrolOffset = 7.0f;
        bot.waypoints = new Vector3[]
        {
            pos + new Vector3(patrolOffset * direction, 0f, 0f),
            pos + new Vector3(patrolOffset * direction, 0f, 2.5f),
            pos + new Vector3(-patrolOffset * direction, 0f, 2.5f),
            pos + new Vector3(-patrolOffset * direction, 0f, 0f)
        };
    }

    private Bounds CalculateBounds(GameObject root)
    {
        Renderer[] rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(root.transform.position, new Vector3(74f, 14.5f, 22f));

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            b.Encapsulate(rends[i].bounds);
        }
        return b;
    }
}
