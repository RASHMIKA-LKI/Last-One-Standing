using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SpectatorCameraController : MonoBehaviour
{
    public static SpectatorCameraController Instance { get; private set; }

    [Header("Spectator Status")]
    public bool isSpectating = false;
    public int currentTargetIndex = 0;

    [Header("Camera Settings")]
    public Camera spectatorCamera;
    public Vector3 followOffset = new Vector3(0, 2.2f, -3.8f);
    public float smoothSpeed = 8f;
    public float rotationSpeed = 2.5f;

    [Header("Tracked Teammates")]
    public List<FreezeTarget> livingTeammates = new List<FreezeTarget>();

    private float pitch = 15f;
    private float yaw = 0f;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        if (spectatorCamera == null)
        {
            spectatorCamera = Camera.main;
        }

        var localTarget = FindFirstObjectByType<FreezeTarget>();
        if (localTarget != null)
        {
            if (localTarget.onEliminated == null) localTarget.onEliminated = new UnityEngine.Events.UnityEvent();
            localTarget.onEliminated.AddListener(EnterSpectatorMode);
        }
    }

    public void EnterSpectatorMode()
    {
        isSpectating = true;
        Debug.Log("[Spectator] Local player eliminated! Entering Spectator Mode...");

        var localController = FindFirstObjectByType<PlayerController>();
        if (localController != null) localController.enabled = false;

        var localShooting = FindFirstObjectByType<PlayerShooting>();
        if (localShooting != null) localShooting.enabled = false;

        RefreshTeammateList();
    }

    private void RefreshTeammateList()
    {
        livingTeammates.Clear();
        var allTargets = FindObjectsByType<FreezeTarget>(FindObjectsSortMode.None);

        foreach (var t in allTargets)
        {
            if (t.currentLives > 0 && t.gameObject != gameObject)
            {
                livingTeammates.Add(t);
            }
        }

        if (currentTargetIndex >= livingTeammates.Count)
        {
            currentTargetIndex = 0;
        }
    }

    void Update()
    {
        if (!isSpectating) return;

        if (livingTeammates.Count > 0 && (livingTeammates[currentTargetIndex] == null || livingTeammates[currentTargetIndex].currentLives <= 0))
        {
            RefreshTeammateList();
        }

        if (livingTeammates.Count == 0) return;

        HandleCycleInput();
        HandleCameraOrbit();
    }

    private void HandleCycleInput()
    {
        bool cycleNext = false;
        bool cyclePrev = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) cycleNext = true;
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) cyclePrev = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) cycleNext = true;
#else
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetMouseButtonDown(0)) cycleNext = true;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) cyclePrev = true;
#endif

        if (cycleNext)
        {
            currentTargetIndex = (currentTargetIndex + 1) % livingTeammates.Count;
        }
        else if (cyclePrev)
        {
            currentTargetIndex--;
            if (currentTargetIndex < 0) currentTargetIndex = livingTeammates.Count - 1;
        }
    }

    private void HandleCameraOrbit()
    {
        if (livingTeammates.Count == 0 || spectatorCamera == null) return;

        Transform target = livingTeammates[currentTargetIndex].transform;

        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            var delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * 0.1f * rotationSpeed;
            mouseY = delta.y * 0.1f * rotationSpeed;
        }
#else
        mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;
#endif

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, -20f, 60f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * followOffset;

        spectatorCamera.transform.position = Vector3.Lerp(spectatorCamera.transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        spectatorCamera.transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    void OnGUI()
    {
        if (!isSpectating) return;

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.yellow;

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            subHeaderStyle.normal.textColor = Color.white;
        }

        float bannerW = 420f;
        float bannerH = 65f;
        float x = (Screen.width - bannerW) / 2f;
        float y = 20f;

        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.9f);
        GUI.Box(new Rect(x, y, bannerW, bannerH), "");
        GUI.backgroundColor = oldBg;

        if (livingTeammates.Count > 0 && currentTargetIndex < livingTeammates.Count)
        {
            var target = livingTeammates[currentTargetIndex];
            string targetName = target.gameObject.name;
            int floor = FloorTracker.GetFloorForHeight(target.transform.position.y);
            string hearts = new string('♥', target.currentLives);
            string status = target.isFrozen ? "<color=#55ddff>[FROZEN!]</color>" : "<color=#55ff55>[ACTIVE]</color>";

            GUI.Label(new Rect(x, y + 6, bannerW, 24), $"SPECTATING: {targetName} {status}", headerStyle);
            GUI.Label(new Rect(x, y + 32, bannerW, 18), $"Lives: {hearts}  |  Floor: {floor}  |  [A / D or Click to Switch]", subHeaderStyle);
        }
        else
        {
            GUI.Label(new Rect(x, y + 12, bannerW, 24), "ALL TEAMMATES ELIMINATED", headerStyle);
            GUI.Label(new Rect(x, y + 36, bannerW, 18), "MATCH OVER", subHeaderStyle);
        }
    }
}

