using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TeamTextChatUI : MonoBehaviour
{
    [System.Serializable]
    public class ChatMessage
    {
        public string sender;
        public string message;
        public Color color;
        public float time;
    }

    [Header("Chat Settings")]
    public int maxMessages = 15;
    public float messageFadeDuration = 12f;
    public string localPlayerName = "HostelSurvivor";
    public Color teamColor = new Color(0.3f, 0.75f, 1f);

    private List<ChatMessage> messages = new List<ChatMessage>();
    private bool isChatOpen = false;
    private string currentInput = "";
    private Vector2 scrollPos = Vector2.zero;

    private GUIStyle chatBoxStyle;
    private GUIStyle chatTextStyle;
    private GUIStyle inputFieldStyle;

    void Start()
    {
        AddMessage("SYSTEM", "Welcome to Hostel Survival! Use [WASD] to move, [L-Click] to Freeze.", Color.yellow);
        AddMessage("SYSTEM", "Press [Enter] to type in Team Chat. Hold [V] for Team Radio.", Color.cyan);
        AddMessage("Alex (Teammate)", "I'm checking Floor 2 bedrooms. Watch out for hunters!", new Color(0.4f, 0.8f, 1f));
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                if (!isChatOpen) OpenChat();
                else SendChatMessage();
            }
            else if (isChatOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseChat();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!isChatOpen) OpenChat();
            else SendChatMessage();
        }
        else if (isChatOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
#endif
    }

    private void OpenChat()
    {
        isChatOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseChat()
    {
        isChatOpen = false;
        currentInput = "";
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SendChatMessage()
    {
        if (!string.IsNullOrWhiteSpace(currentInput))
        {
            AddMessage(localPlayerName, currentInput, teamColor);
            currentInput = "";
        }
        CloseChat();
    }

    public void AddMessage(string sender, string text, Color color)
    {
        messages.Add(new ChatMessage
        {
            sender = sender,
            message = text,
            color = color,
            time = Time.time
        });

        if (messages.Count > maxMessages)
        {
            messages.RemoveAt(0);
        }

        scrollPos.y = Mathf.Infinity;
    }

    void OnGUI()
    {
        if (chatBoxStyle == null)
        {
            chatBoxStyle = new GUIStyle(GUI.skin.box);
            chatTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true
            };
            inputFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12
            };
        }

        float chatW = 320f;
        float chatH = isChatOpen ? 220f : 160f;
        float chatX = 16f;
        float chatY = Screen.height - chatH - 50f;

        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = isChatOpen ? new Color(0.08f, 0.12f, 0.16f, 0.85f) : new Color(0.05f, 0.08f, 0.12f, 0.45f);
        GUI.Box(new Rect(chatX, chatY, chatW, chatH), "");
        GUI.backgroundColor = oldBg;

        GUI.Label(new Rect(chatX + 8, chatY + 4, chatW, 16), "<b>[TEAM TEXT CHAT]</b>", chatTextStyle);

        float listH = isChatOpen ? chatH - 54f : chatH - 24f;
        GUILayout.BeginArea(new Rect(chatX + 6, chatY + 22, chatW - 12, listH));
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, isChatOpen);

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            if (!isChatOpen && (Time.time - msg.time > messageFadeDuration)) continue;

            string hexCol = ColorUtility.ToHtmlStringRGBA(msg.color);
            GUILayout.Label($"<color=#{hexCol}>[{msg.sender}]</color>: {msg.message}", chatTextStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (isChatOpen)
        {
            GUI.SetNextControlName("ChatInputField");
            currentInput = GUI.TextField(new Rect(chatX + 6, chatY + chatH - 26, chatW - 60, 22), currentInput, 120, inputFieldStyle);
            GUI.FocusControl("ChatInputField");

            if (GUI.Button(new Rect(chatX + chatW - 52, chatY + chatH - 26, 46, 22), "Send"))
            {
                SendChatMessage();
            }
        }
        else
        {
            GUI.Label(new Rect(chatX + 8, chatY + chatH - 18, chatW, 16), "<size=10><color=#888888>[Enter] to chat</color></size>", chatTextStyle);
        }
    }
}

