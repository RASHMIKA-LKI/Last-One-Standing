using System.Runtime.InteropServices;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DualChannelVoiceController : MonoBehaviour
{
    public static DualChannelVoiceController Instance { get; private set; }

    [Header("Voice Settings")]
    public string localPlayerId = "player_local";
    public int teamId = 1;
    public bool isMuted = false;
    public bool isRadioTransmitting = false;

    [Header("Proximity Audio Range")]
    public float maxProximityDistance = 25f;
    public Transform listenerTransform;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebAudioVoice_Init(string localId, int teamId);

    [DllImport("__Internal")]
    private static extern void WebAudioVoice_SetRadioActive(bool isActive);

    [DllImport("__Internal")]
    private static extern void WebAudioVoice_UpdateListener(float x, float y, float z, float fwdX, float fwdY, float fwdZ, float upX, float upY, float upZ);

    [DllImport("__Internal")]
    private static extern void WebAudioVoice_UpdatePeerPosition(string peerId, float x, float y, float z);
#endif

    private GUIStyle voiceBadgeStyle;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        if (listenerTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) listenerTransform = cam.transform;
            else listenerTransform = transform;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        WebAudioVoice_Init(localPlayerId, teamId);
#endif
    }

    void Update()
    {
        HandleInput();
        UpdateListenerSpatialOrientation();
    }

    private void HandleInput()
    {
        bool radioHeld = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            radioHeld = Keyboard.current.vKey.isPressed;
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                isMuted = !isMuted;
            }
        }
#else
        radioHeld = Input.GetKey(KeyCode.V);
        if (Input.GetKeyDown(KeyCode.B))
        {
            isMuted = !isMuted;
        }
#endif

        if (radioHeld != isRadioTransmitting)
        {
            isRadioTransmitting = radioHeld;
            SetRadioActive(isRadioTransmitting);
        }
    }

    private void SetRadioActive(bool active)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebAudioVoice_SetRadioActive(active);
#endif
    }

    private void UpdateListenerSpatialOrientation()
    {
        if (listenerTransform == null) return;

        Vector3 pos = listenerTransform.position;
        Vector3 fwd = listenerTransform.forward;
        Vector3 up = listenerTransform.up;

#if UNITY_WEBGL && !UNITY_EDITOR
        WebAudioVoice_UpdateListener(pos.x, pos.y, pos.z, fwd.x, fwd.y, fwd.z, up.x, up.y, up.z);
#endif
    }

    void OnGUI()
    {
        if (voiceBadgeStyle == null)
        {
            voiceBadgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
        }

        float badgeW = 230f;
        float badgeH = 28f;
        float x = (Screen.width - badgeW) / 2f;
        float y = Screen.height - 40f;

        Color originalColor = GUI.backgroundColor;

        if (isMuted)
        {
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.85f);
            GUI.Box(new Rect(x, y, badgeW, badgeH), " 🔇 MIC MUTED  [B to Unmute]", voiceBadgeStyle);
        }
        else if (isRadioTransmitting)
        {
            GUI.backgroundColor = new Color(0.9f, 0.6f, 0.1f, 0.95f);
            GUI.Box(new Rect(x, y, badgeW, badgeH), " 📻 TEAM RADIO  [V: TRANSMITTING]", voiceBadgeStyle);
        }
        else
        {
            GUI.backgroundColor = new Color(0.15f, 0.22f, 0.3f, 0.75f);
            GUI.Box(new Rect(x, y, badgeW, badgeH), " 🎙 3D PROXIMITY ON  [Hold V: Radio]", voiceBadgeStyle);
        }

        GUI.backgroundColor = originalColor;
    }
}

