using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FloorTracker : MonoBehaviour
{
    public static FloorTracker Instance { get; private set; }

    [System.Serializable]
    public class FloorInfo
    {
        public int floorNumber;
        public string floorName;
        public float minY;
        public float maxY;
    }

    [Header("Floor Definitions")]
    public List<FloorInfo> floors = new List<FloorInfo>()
    {
        new FloorInfo { floorNumber = 1, floorName = "Floor 1 - Lobby & Reception", minY = -1.0f, maxY = 2.5f },
        new FloorInfo { floorNumber = 2, floorName = "Floor 2 - Dormitory Wing A",   minY = 2.5f,  maxY = 5.8f },
        new FloorInfo { floorNumber = 3, floorName = "Floor 3 - Dormitory Wing B",   minY = 5.8f,  maxY = 9.1f },
        new FloorInfo { floorNumber = 4, floorName = "Floor 4 - Study & Recreation", minY = 9.1f,  maxY = 12.4f },
        new FloorInfo { floorNumber = 5, floorName = "Rooftop - Terrace & Helipad",  minY = 12.4f, maxY = 25.0f }
    };

    [Header("Target Tracking")]
    public Transform targetTransform;
    public int currentFloorNumber = 1;
    public string currentFloorName = "Floor 1 - Lobby & Reception";

    [Header("Events")]
    public UnityEvent<int, string> onFloorChanged;

    private int lastFloorNumber = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        if (targetTransform == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) targetTransform = pc.transform;
            else targetTransform = transform;
        }

        UpdateFloor();
    }

    void Update()
    {
        if (targetTransform != null)
        {
            UpdateFloor();
        }
    }

    private void UpdateFloor()
    {
        float y = targetTransform.position.y;
        FloorInfo detectedFloor = null;

        for (int i = 0; i < floors.Count; i++)
        {
            if (y >= floors[i].minY && y < floors[i].maxY)
            {
                detectedFloor = floors[i];
                break;
            }
        }

        if (detectedFloor == null)
        {
            if (y < floors[0].minY) detectedFloor = floors[0];
            else detectedFloor = floors[floors.Count - 1];
        }

        if (detectedFloor.floorNumber != lastFloorNumber)
        {
            currentFloorNumber = detectedFloor.floorNumber;
            currentFloorName = detectedFloor.floorName;
            lastFloorNumber = currentFloorNumber;
            onFloorChanged?.Invoke(currentFloorNumber, currentFloorName);
            Debug.Log($"[FloorTracker] Player entered {currentFloorName} (Floor {currentFloorNumber})");
        }
    }

    public static int GetFloorForHeight(float y)
    {
        if (Instance != null)
        {
            for (int i = 0; i < Instance.floors.Count; i++)
            {
                if (y >= Instance.floors[i].minY && y < Instance.floors[i].maxY)
                    return Instance.floors[i].floorNumber;
            }
        }
        if (y < 2.5f) return 1;
        if (y < 5.8f) return 2;
        if (y < 9.1f) return 3;
        if (y < 12.4f) return 4;
        return 5;
    }
}

