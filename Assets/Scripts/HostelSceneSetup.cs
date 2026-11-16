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
            buildingInstance = GameObject.Find("Hostel_Optimized") ?? GameObject.Find("HostelBuilding");
        }

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
            // Remove any legacy slabs that block stairwells
            GameObject oldSlabs = GameObject.Find("Hostel_Floor_Slabs");
            if (oldSlabs != null) Destroy(oldSlabs);

            // 1. Remove ALL COL_ helper objects (COL_Room_*_Floor, etc.)
            // The user requested: "remove all COL_Room_... just let's keep the original floor"
            Transform[] allChildren = buildingInstance.GetComponentsInChildren<Transform>(true);
            int removedColCount = 0;
            foreach (var child in allChildren)
            {
                if (child != null && child.gameObject.name.StartsWith("COL_"))
                {
                    child.gameObject.SetActive(false); // Disable immediately
                    Destroy(child.gameObject);
                    removedColCount++;
                }
            }

            // 2. Ensure ALL remaining original building meshes (floors, rooms, architecture, stairs) have solid MeshColliders
            int originalMeshCount = 0;
            MeshFilter[] allFilters = buildingInstance.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in allFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.gameObject.name.StartsWith("COL_")) continue; // Skip any COL_ objects awaiting destruction

                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.isTrigger = false;
                originalMeshCount++;
            }

            Debug.Log($"[HostelSceneSetup] Removed {removedColCount} COL_ helper objects. Configured {originalMeshCount} solid MeshColliders on original building floors, rooms, and architecture.");
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

        float w = Mathf.Max(buildingBounds.size.x * 2.0f, 200f);
        float l = Mathf.Max(buildingBounds.size.z * 3.0f, 200f);
        float thickness = 4.0f;

        // Position ground box so top surface is exactly at Y = 0.0f
        plane.transform.position = new Vector3(buildingBounds.center.x, -thickness / 2.0f, buildingBounds.center.z);
        plane.transform.localScale = new Vector3(w, thickness, l);

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

        // CRITICAL: Remove default CapsuleCollider so it doesn't conflict with CharacterController
        CapsuleCollider defaultCol = playerInstance.GetComponent<CapsuleCollider>();
        if (defaultCol != null) Destroy(defaultCol);

        // Position directly on the ground courtyard (Y = 1.0m puts CharacterController center above Y = 0)
        float spawnX = buildingBounds.center.x;
        float spawnY = 1.0f;
        float spawnZ = buildingBounds.min.z - 6.0f;
        playerInstance.transform.position = new Vector3(spawnX, spawnY, spawnZ);
        playerInstance.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Looking towards building (+Z)

        // CharacterController tuned for smooth stair and slope traversal
        var cc = playerInstance.GetComponent<CharacterController>();
        if (cc == null) cc = playerInstance.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f; // Slim radius for narrow doors and stairways
        cc.center = Vector3.zero;
        cc.slopeLimit = 60f; // Climbs 45-60 degree stairs smoothly
        cc.stepOffset = 0.35f; // Steps over curbs and stair steps cleanly
        cc.skinWidth = 0.025f; // Optimal skin width prevents sinking into floors
        cc.minMoveDistance = 0f;

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

        float fH = buildingBounds.size.y / 4.0f;
        float gY = buildingBounds.min.y;
        tracker.floors = new System.Collections.Generic.List<FloorTracker.FloorInfo>()
        {
            new FloorTracker.FloorInfo { floorNumber = 1, floorName = "Floor 1 - Lobby & Reception", minY = gY - 5f, maxY = gY + fH },
            new FloorTracker.FloorInfo { floorNumber = 2, floorName = "Floor 2 - Dormitory Wing A", minY = gY + fH, maxY = gY + fH * 2 },
            new FloorTracker.FloorInfo { floorNumber = 3, floorName = "Floor 3 - Dormitory Wing B", minY = gY + fH * 2, maxY = gY + fH * 3 },
            new FloorTracker.FloorInfo { floorNumber = 4, floorName = "Floor 4 - Study & Penthouse", minY = gY + fH * 3, maxY = gY + fH * 4 + 15f }
        };

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

