using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public static class SetupHostelScene
{
    [MenuItem("Hostel/Build Full Hostel Scene")]
    [InitializeOnLoadMethod]
    public static void ExecuteSetup()
    {
        // Avoid running during play mode or repeatedly if already built
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.delayCall += RunBuild;
    }

    private static void RunBuild()
    {
        string scenePath = "Assets/Scenes/Hostal.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Debug.Log("[SetupHostelScene] Validating and configuring colliders on Hostel Scene...");

        // 1. Setup Environment Lighting & Atmosphere
        SetupLighting();

        // 2. Instantiate and setup Hostel Building with Colliders
        GameObject hostelObj = SetupHostelBuilding();
        if (hostelObj == null)
        {
            Debug.LogError("[SetupHostelScene] Failed to load Hostel model!");
            return;
        }

        Bounds b = CalculateTotalBounds(hostelObj);
        Debug.Log($"[SetupHostelScene] Hostel Bounds: Center={b.center}, Size={b.size}, Min={b.min}, Max={b.max}");

        // 3. Create Ground Plane in front of and under Hostel
        if (GameObject.Find("Ground_Courtyard") == null)
        {
            SetupGroundPlane(b);
        }

        // 4. Create Player in front of the Hostel
        GameObject playerObj = GameObject.Find("Player_Survivor");
        if (playerObj == null)
        {
            playerObj = SetupPlayer(b);
        }

        // 5. Setup Gameplay Systems (Minimap, FloorTracker, HUD, Voice, Chat)
        if (GameObject.Find("[GAMEPLAY_SYSTEMS]") == null)
        {
            SetupGameplaySystems(playerObj, b);
        }

        // 6. Spawn Enemies on EACH FLOOR
        if (GameObject.Find("[ENEMIES_ROOT]") == null)
        {
            SetupEnemiesOnFloors(b, playerObj);
        }

        // Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupHostelScene] Hostal.unity successfully validated, updated, and saved!");
    }

    private static void SetupLighting()
    {
        // Directional Light (Sun)
        Light dirLight = Object.FindFirstObjectByType<Light>();
        GameObject lightObj;
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            lightObj = new GameObject("Directional Light (Sun)");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }
        else
        {
            lightObj = dirLight.gameObject;
        }

        lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        dirLight.color = new Color(1f, 0.96f, 0.90f);
        dirLight.intensity = 1.2f;
        dirLight.shadows = LightShadows.Soft;

        // Global Post-Processing Volume with Bloom
        Volume vol = Object.FindFirstObjectByType<Volume>();
        if (vol == null)
        {
            GameObject volObj = new GameObject("Global PostProcess Volume");
            vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile != null)
            {
                vol.profile = profile;
            }
        }
    }

    private static GameObject SetupHostelBuilding()
    {
        GameObject instance = GameObject.Find("[HOSTEL_BUILDING]");
        if (instance == null)
        {
            instance = GameObject.Find("Hostel_Optimized") ?? GameObject.Find("HostelBuilding");
        }

        if (instance == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Hostel_Optimized.fbx");
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/HostelBuilding.fbx");
            }

            if (prefab == null) return null;

            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "[HOSTEL_BUILDING]";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
        }

        // Configure solid BoxColliders for all floors, rooms, and furniture, and convex ramps for stairs
        MeshFilter[] allFilters = instance.GetComponentsInChildren<MeshFilter>(true);
        int floorCount = 0;
        int boxCount = 0;
        int rampCount = 0;
        int meshCount = 0;

        foreach (var mf in allFilters)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            string objName = mf.gameObject.name;
            bool isCol = objName.StartsWith("COL_");
            bool isFloor = objName.Contains("Floor");
            bool isRamp = objName.Contains("Ramp") || (isCol && objName.Contains("Stair"));

            if (isFloor)
            {
                // CRITICAL: Remove thin MeshCollider and add solid 0.85m thick BoxCollider flush with walking surface
                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc != null) Object.DestroyImmediate(mc);

                BoxCollider bc = mf.GetComponent<BoxCollider>();
                if (bc == null) bc = mf.gameObject.AddComponent<BoxCollider>();

                Vector3 meshSize = mf.sharedMesh.bounds.size;
                Vector3 meshCenter = mf.sharedMesh.bounds.center;

                float solidThickness = Mathf.Max(meshSize.y, 0.85f);
                float originalTopY = meshCenter.y + (meshSize.y * 0.5f);

                bc.size = new Vector3(meshSize.x, solidThickness, meshSize.z);
                bc.center = new Vector3(meshCenter.x, originalTopY - (solidThickness * 0.5f), meshCenter.z);
                bc.isTrigger = false;
                floorCount++;
                boxCount++;
            }
            else if (isRamp)
            {
                BoxCollider bc = mf.GetComponent<BoxCollider>();
                if (bc != null) Object.DestroyImmediate(bc);

                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
                mc.isTrigger = false;
                rampCount++;
                meshCount++;
            }
            else if (isCol)
            {
                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc != null) Object.DestroyImmediate(mc);

                BoxCollider bc = mf.GetComponent<BoxCollider>();
                if (bc == null) bc = mf.gameObject.AddComponent<BoxCollider>();
                bc.center = mf.sharedMesh.bounds.center;
                bc.size = mf.sharedMesh.bounds.size;
                bc.isTrigger = false;
                boxCount++;
            }
            else
            {
                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.isTrigger = false;
                meshCount++;
            }
        }
        Debug.Log($"[SetupHostelScene] Configured colliders: {floorCount} solid floor BoxColliders, {boxCount} total BoxColliders, {rampCount} convex ramps, {meshCount} MeshColliders.");
        return instance;
    }

    private static void SetupGroundPlane(Bounds b)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "Ground_Courtyard";
        
        // Size to cover the entire building and a large front courtyard
        float planeW = Mathf.Max(b.size.x * 1.5f, 150f);
        float planeL = Mathf.Max(b.size.z * 2.5f, 150f);
        float groundY = b.min.y;

        plane.transform.position = new Vector3(b.center.x, groundY - 0.25f, b.center.z);
        plane.transform.localScale = new Vector3(planeW, 0.5f, planeL);

        // Asphalt / Pavement Material
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        groundMat.color = new Color(0.25f, 0.27f, 0.30f);
        plane.GetComponent<MeshRenderer>().material = groundMat;
    }

    private static GameObject SetupPlayer(Bounds b)
    {
        // Remove default standalone camera if present
        Camera oldCam = Camera.main;
        if (oldCam != null && oldCam.transform.parent == null)
        {
            Object.DestroyImmediate(oldCam.gameObject);
        }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Survivor";
        player.tag = "Player";

        // Remove default CapsuleCollider primitive to avoid conflict with CharacterController
        CapsuleCollider defaultCol = player.GetComponent<CapsuleCollider>();
        if (defaultCol != null) Object.DestroyImmediate(defaultCol);

        // Position in front of hostel courtyard facing entrance
        float spawnZ = b.min.z - 10f; // 10 meters in front of the building
        float spawnY = b.min.y + 1.1f;
        player.transform.position = new Vector3(b.center.x, spawnY, spawnZ);
        player.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Looking towards building (+Z)

        // CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = Vector3.zero;
        cc.slopeLimit = 60f;
        cc.stepOffset = 0.35f;
        cc.skinWidth = 0.025f;

        // Player Camera
        GameObject camObj = new GameObject("PlayerCamera");
        camObj.transform.parent = player.transform;
        camObj.transform.localPosition = new Vector3(0, 0.7f, 0.2f);
        camObj.transform.localRotation = Quaternion.identity;

        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        // Player Scripts
        var pc = player.AddComponent<PlayerController>();
        pc.cameraHolder = camObj.transform;

        var ft = player.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var ps = player.AddComponent<PlayerShooting>();
        ps.playerCamera = cam;

        // Equip PolyOne Sci-Fi Rifle
        GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyOne/Free Sci Fi Weapon/Prefabs/SM_Sci_FiWeapon_RifleGun.prefab");
        if (gunPrefab != null)
        {
            GameObject gunInstance = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab, camObj.transform);
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

        player.AddComponent<GameHUDAndTasks>();
        return player;
    }

    private static void SetupGameplaySystems(GameObject player, Bounds b)
    {
        GameObject sysRoot = new GameObject("[GAMEPLAY_SYSTEMS]");

        // Floor Tracker
        var tracker = sysRoot.AddComponent<FloorTracker>();
        tracker.targetTransform = player.transform;

        float fH = b.size.y / 4.0f;
        float gY = b.min.y;
        tracker.floors = new System.Collections.Generic.List<FloorTracker.FloorInfo>()
        {
            new FloorTracker.FloorInfo { floorNumber = 1, floorName = "Floor 1 - Lobby & Reception", minY = gY - 5f, maxY = gY + fH },
            new FloorTracker.FloorInfo { floorNumber = 2, floorName = "Floor 2 - Dormitory Wing A", minY = gY + fH, maxY = gY + fH * 2 },
            new FloorTracker.FloorInfo { floorNumber = 3, floorName = "Floor 3 - Dormitory Wing B", minY = gY + fH * 2, maxY = gY + fH * 3 },
            new FloorTracker.FloorInfo { floorNumber = 4, floorName = "Floor 4 - Study & Penthouse", minY = gY + fH * 3, maxY = gY + fH * 4 + 15f }
        };

        // Minimap UI
        var minimap = sysRoot.AddComponent<FloorMinimapUI>();
        minimap.playerTransform = player.transform;
        minimap.floorTracker = tracker;

        // Chat UI
        sysRoot.AddComponent<TeamTextChatUI>();

        // Voice Controller
        var voice = sysRoot.AddComponent<DualChannelVoiceController>();
        voice.listenerTransform = Camera.main != null ? Camera.main.transform : player.transform;

        // Spectator Camera
        sysRoot.AddComponent<SpectatorCameraController>();
    }

    private static void SetupEnemiesOnFloors(Bounds b, GameObject player)
    {
        GameObject enemiesRoot = new GameObject("[ENEMIES_ROOT]");
        float floorHeight = b.size.y / 4.0f;
        float groundY = b.min.y;

        // 4 Floors, 2 enemies per floor = 8 enemies total
        string[] floorNames = new string[] { "Floor_1_Lobby", "Floor_2_LowerRooms", "Floor_3_UpperRooms", "Floor_4_Penthouse" };

        for (int f = 0; f < 4; f++)
        {
            float floorY = groundY + (f * floorHeight) + 1.1f;
            GameObject floorGroup = new GameObject($"Enemies_{floorNames[f]}");
            floorGroup.transform.parent = enemiesRoot.transform;

            // Enemy A: West Wing
            Vector3 posA = new Vector3(b.center.x - (b.size.x * 0.22f), floorY, b.center.z);
            CreateEnemyBot($"Enemy_F{f + 1}_West", posA, floorGroup.transform, f + 1, -1);

            // Enemy B: East Wing
            Vector3 posB = new Vector3(b.center.x + (b.size.x * 0.22f), floorY, b.center.z);
            CreateEnemyBot($"Enemy_F{f + 1}_East", posB, floorGroup.transform, f + 1, 1);
        }
    }

    private static void CreateEnemyBot(string name, Vector3 pos, Transform parent, int floor, int side)
    {
        GameObject botObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        botObj.name = name;
        botObj.transform.parent = parent;
        botObj.transform.position = pos;

        // Distinct Enemy Red Material
        Renderer r = botObj.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.92f, 0.22f, 0.22f); // Hostile Red
            r.material = mat;
        }

        var ft = botObj.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var bot = botObj.AddComponent<TemporaryDummyBot>();
        bot.botName = name;
        bot.isTeammate = false;
        bot.moveSpeed = 2.0f;
        bot.visionRange = 22f;

        // Patrol Waypoints along the hallway of this floor
        float patrolDist = 6.0f;
        bot.waypoints = new Vector3[]
        {
            pos + new Vector3(patrolDist * side, 0f, 0f),
            pos + new Vector3(patrolDist * side, 0f, 2.5f),
            pos + new Vector3(-patrolDist * side, 0f, 2.5f),
            pos + new Vector3(-patrolDist * side, 0f, 0f)
        };
    }

    private static Bounds CalculateTotalBounds(GameObject root)
    {
        Renderer[] rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(root.transform.position, Vector3.one * 10f);

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            b.Encapsulate(rends[i].bounds);
        }
        return b;
    }
}

