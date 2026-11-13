using UnityEngine;

public class TemporarySandboxSetup : MonoBehaviour
{
    [Header("Testing Automation")]
    public bool autoSpawnPlayerIfMissing = true;
    public bool autoSpawnDummyBots = false;
    public bool spawnEnemyBots = false;
    public bool spawnTeammateBot = false;

    private GameObject testRoot;

    void Start()
    {
        testRoot = new GameObject("[TEMPORARY_TEST_SYSTEMS]");

        // 1. Build Arena
        var arenaBuilder = testRoot.AddComponent<TemporaryArenaBuilder>();

        // 2. Setup Player
        GameObject playerObj = SetupPlayer();

        // 3. Setup Core Systems
        SetupSystems(playerObj);

        // 4. Setup Test Bots
        if (autoSpawnDummyBots)
        {
            SetupTestBots();
        }
    }

    private GameObject SetupPlayer()
    {
        var existingController = FindFirstObjectByType<PlayerController>();
        if (existingController != null)
        {
            return existingController.gameObject;
        }

        // Create player capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "TestPlayer";
        player.transform.position = new Vector3(0, 1.1f, -4f);

        // CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0, 0);
        cc.slopeLimit = 55f;
        cc.stepOffset = 0.4f;

        // Camera
        GameObject camObj = new GameObject("PlayerCamera");
        camObj.transform.parent = player.transform;
        camObj.transform.localPosition = new Vector3(0, 0.7f, 0.2f);
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        // Player scripts
        var pc = player.AddComponent<PlayerController>();
        pc.cameraHolder = camObj.transform;

        var ft = player.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var ps = player.AddComponent<PlayerShooting>();
        ps.playerCamera = cam;

#if UNITY_EDITOR
        // Equip PolyOne Sci-Fi Rifle to player camera
        GameObject gunPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyOne/Free Sci Fi Weapon/Prefabs/SM_Sci_FiWeapon_RifleGun.prefab");
        if (gunPrefab != null)
        {
            GameObject gunInstance = Instantiate(gunPrefab, camObj.transform);
            gunInstance.name = "Equipped_Rifle";
            
            // Model mesh is exported along +X. Rotate -91.5 deg on Y to face forward (+Z) and converge with crosshair
            gunInstance.transform.localPosition = new Vector3(0.20f, -0.22f, 0.48f);
            gunInstance.transform.localRotation = Quaternion.Euler(0.5f, -91.5f, 0.8f);

            // Muzzle is at the exact physical tip of the rifle barrel (+X = 0.495m, +Y = 0.075m in mesh space)
            GameObject muzzle = new GameObject("GunMuzzle");
            muzzle.transform.parent = gunInstance.transform;
            muzzle.transform.localPosition = new Vector3(0.495f, 0.075f, 0.0f);
            muzzle.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            ps.gunTransform = gunInstance.transform;
            ps.gunMuzzle = muzzle.transform;
        }
#endif

        player.AddComponent<GameHUDAndTasks>();

        return player;
    }

    private void SetupSystems(GameObject player)
    {
        // Floor Tracker
        var tracker = testRoot.AddComponent<FloorTracker>();
        tracker.targetTransform = player.transform;

        // Minimap UI
        var minimap = testRoot.AddComponent<FloorMinimapUI>();
        minimap.playerTransform = player.transform;
        minimap.floorTracker = tracker;

        // Chat UI
        testRoot.AddComponent<TeamTextChatUI>();

        // Voice Controller
        var voice = testRoot.AddComponent<DualChannelVoiceController>();
        voice.listenerTransform = Camera.main != null ? Camera.main.transform : player.transform;

        // Spectator Camera
        testRoot.AddComponent<SpectatorCameraController>();
    }

    private void SetupTestBots()
    {
        // 1. Friendly Teammate on Floor 1
        if (spawnTeammateBot)
        {
            SpawnBot("Alex (Teammate)", new Vector3(-5f, 1f, 2f), true, 1);
        }

        // 2. Enemy Bots (Disabled for beam testing)
        if (spawnEnemyBots)
        {
            SpawnBot("Hunter_Alpha", new Vector3(5f, 1f, 3f), false, 1);
            SpawnBot("Shadow_Floor2", new Vector3(-2f, 4.3f, 0f), false, 2);
        }
    }

    private void SpawnBot(string name, Vector3 pos, bool isTeammate, int floor)
    {
        GameObject botObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        botObj.name = name;
        botObj.transform.parent = testRoot.transform;
        botObj.transform.position = pos;

        var ft = botObj.AddComponent<FreezeTarget>();
        ft.maxLives = 3;

        var bot = botObj.AddComponent<TemporaryDummyBot>();
        bot.botName = name;
        bot.isTeammate = isTeammate;

        // Waypoints based on spawn
        bot.waypoints = new Vector3[]
        {
            pos + new Vector3(3f, 0, 0),
            pos + new Vector3(3f, 0, 3f),
            pos + new Vector3(-3f, 0, 3f),
            pos + new Vector3(-3f, 0, -2f)
        };
    }

    void OnGUI()
    {
        // Sandbox Info Watermark in Top-Left
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft
        };
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.Box(new Rect(16, 16, 320, 85), 
            "<b>[SANDBOX TEST MODE - ZERO GAME IMPACT]</b>\n" +
            "• WASD: Move | Shift: Sprint | Space: Jump\n" +
            "• Left Click: Freeze Laser | Space (when hit): Tap to Unfreeze\n" +
            "• M: Toggle Minimap | Enter: Chat | Hold V: Team Radio\n" +
            "• Blue = Teammate | Red = Enemy", style);
    }
}

