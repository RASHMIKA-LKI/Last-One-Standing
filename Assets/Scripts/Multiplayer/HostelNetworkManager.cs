using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HostelNetworkManager : MonoBehaviour
{
    public static HostelNetworkManager Instance { get; private set; }

    [System.Serializable]
    public class NetworkPlayerData
    {
        public string playerId;
        public string playerName;
        public int teamId;
        public Vector3 position;
        public float rotationY;
        public bool isFrozen;
        public int lives;
        public int currentFloor;
        public GameObject avatarInstance;
    }

    [Header("Network Configuration")]
    public string serverUrl = "wss://relay.example.com/hostel";
    public string roomId = "hostel-room-1";
    public string localPlayerId = "player_local";
    public string localPlayerName = "HostelSurvivor";
    public int localTeamId = 1;
    public bool isConnected = false;

    [Header("Sync Settings")]
    public float sendRate = 20f;
    public bool enableBotSimulation = true;

    [Header("Prefabs & References")]
    public GameObject remotePlayerPrefab;
    public GameObject localPlayerObject;

    public Dictionary<string, NetworkPlayerData> players = new Dictionary<string, NetworkPlayerData>();

    private float sendTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (string.IsNullOrEmpty(localPlayerId) || localPlayerId == "player_local")
        {
            localPlayerId = "player_" + UnityEngine.Random.Range(1000, 9999);
        }
    }

    void Start()
    {
        if (localPlayerObject == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) localPlayerObject = pc.gameObject;
        }

        players[localPlayerId] = new NetworkPlayerData
        {
            playerId = localPlayerId,
            playerName = localPlayerName,
            teamId = localTeamId,
            position = localPlayerObject != null ? localPlayerObject.transform.position : Vector3.zero,
            rotationY = localPlayerObject != null ? localPlayerObject.transform.eulerAngles.y : 0,
            isFrozen = false,
            lives = 3,
            currentFloor = 1,
            avatarInstance = localPlayerObject
        };
    }

    void Update()
    {
        sendTimer += Time.deltaTime;
        if (sendTimer >= (1f / sendRate))
        {
            sendTimer = 0f;
            SendLocalPlayerUpdate();
        }
    }

    private void SendLocalPlayerUpdate()
    {
        if (localPlayerObject == null) return;

        var ft = localPlayerObject.GetComponent<FreezeTarget>();
        bool frozen = ft != null ? ft.isFrozen : false;
        int lives = ft != null ? ft.currentLives : 3;
        int floor = FloorTracker.GetFloorForHeight(localPlayerObject.transform.position.y);

        var data = players[localPlayerId];
        data.position = localPlayerObject.transform.position;
        data.rotationY = localPlayerObject.transform.eulerAngles.y;
        data.isFrozen = frozen;
        data.lives = lives;
        data.currentFloor = floor;
    }
}

