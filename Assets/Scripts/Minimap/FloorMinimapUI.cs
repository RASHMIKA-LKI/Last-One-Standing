using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FloorMinimapUI : MonoBehaviour
{
    [Header("Floor References")]
    public FloorTracker floorTracker;
    public Transform playerTransform;

    [Header("Minimap Display Settings")]
    public bool showMinimap = true;
    public bool isExpanded = false;
    public float mapWorldWidth = 36f;
    public float mapWorldHeight = 20f;

    private Texture2D bgTexture;
    private Texture2D roomTexture;
    private Texture2D hallTexture;
    private Texture2D playerTexture;
    private Texture2D teammateTexture;
    private Texture2D frozenTexture;
    private Texture2D stairTexture;

    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;

    void Start()
    {
        if (floorTracker == null) floorTracker = FindFirstObjectByType<FloorTracker>();
        if (playerTransform == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
        }

        GenerateTextures();
    }

    private void GenerateTextures()
    {
        bgTexture = MakeTex(2, 2, new Color(0.08f, 0.10f, 0.14f, 0.88f));
        roomTexture = MakeTex(2, 2, new Color(0.18f, 0.22f, 0.28f, 0.85f));
        hallTexture = MakeTex(2, 2, new Color(0.24f, 0.30f, 0.38f, 0.90f));
        playerTexture = MakeTex(2, 2, new Color(0.2f, 0.9f, 0.4f, 1f));
        teammateTexture = MakeTex(2, 2, new Color(0.2f, 0.6f, 1f, 1f));
        frozenTexture = MakeTex(2, 2, new Color(0.3f, 0.85f, 1f, 1f));
        stairTexture = MakeTex(2, 2, new Color(0.9f, 0.7f, 0.2f, 0.9f));
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            isExpanded = !isExpanded;
        }
#else
        if (Input.GetKeyDown(KeyCode.M))
        {
            isExpanded = !isExpanded;
        }
#endif
    }

    void OnGUI()
    {
        if (!showMinimap) return;

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.white;

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            subHeaderStyle.normal.textColor = new Color(0.7f, 0.85f, 1f, 0.9f);
        }

        int currentFloor = floorTracker != null ? floorTracker.currentFloorNumber : 1;
        string floorName = floorTracker != null ? floorTracker.currentFloorName : "Floor 1";

        float mapW = isExpanded ? 460f : 240f;
        float mapH = isExpanded ? 260f : 140f;
        float screenPadding = 16f;
        float mapX = Screen.width - mapW - screenPadding;
        float mapY = screenPadding;

        Rect mapRect = new Rect(mapX, mapY, mapW, mapH);
        GUI.DrawTexture(mapRect, bgTexture);
        DrawBorder(mapRect, 2, new Color(0.3f, 0.6f, 0.9f, 0.8f));

        float headerH = 24f;
        Rect headerRect = new Rect(mapX, mapY, mapW, headerH);
        GUI.DrawTexture(headerRect, MakeTex(1, 1, new Color(0.12f, 0.16f, 0.22f, 0.95f)));
        GUI.Label(new Rect(mapX, mapY + 2, mapW, 14), $"[MAP] FLOOR {currentFloor} / 4", headerStyle);
        GUI.Label(new Rect(mapX, mapY + 14, mapW, 10), floorName.ToUpper(), subHeaderStyle);

        float innerX = mapX + 8;
        float innerY = mapY + headerH + 6;
        float innerW = mapW - 16;
        float innerH = mapH - headerH - 12;

        DrawFloorLayout(innerX, innerY, innerW, innerH, currentFloor);
        DrawEntitiesOnFloor(innerX, innerY, innerW, innerH, currentFloor);

        GUI.Label(new Rect(mapX, mapY + mapH - 14, mapW, 12), "[M] TOGGLE MAP SIZE", subHeaderStyle);
    }

    private void DrawFloorLayout(float x, float y, float w, float h, int floor)
    {
        float scaleX = w / mapWorldWidth;
        float scaleY = h / mapWorldHeight;

        float centerX = x + (w / 2f);
        float centerY = y + (h / 2f);

        float hallW = 32f * scaleX;
        float hallH = 3.0f * scaleY;
        GUI.DrawTexture(new Rect(centerX - hallW / 2f, centerY - hallH / 2f, hallW, hallH), hallTexture);

        float roomW = 7.5f * scaleX;
        float roomH = 5.5f * scaleY;
        float roomY_N = centerY - hallH / 2f - roomH;

        for (int i = 0; i < 4; i++)
        {
            float rx = centerX - (16f * scaleX) + (i * 8f * scaleX) + (0.25f * scaleX);
            GUI.DrawTexture(new Rect(rx, roomY_N, roomW, roomH), roomTexture);
        }

        float roomY_S = centerY + hallH / 2f;
        for (int i = 0; i < 4; i++)
        {
            float rx = centerX - (16f * scaleX) + (i * 8f * scaleX) + (0.25f * scaleX);
            GUI.DrawTexture(new Rect(rx, roomY_S, roomW, roomH), roomTexture);
        }

        float stairW = 5.0f * scaleX;
        float stairH = 4.0f * scaleY;
        GUI.DrawTexture(new Rect(centerX - stairW / 2f, centerY - stairH / 2f, stairW, stairH), stairTexture);
    }

    private void DrawEntitiesOnFloor(float x, float y, float w, float h, int currentFloor)
    {
        float scaleX = w / mapWorldWidth;
        float scaleY = h / mapWorldHeight;
        float centerX = x + (w / 2f);
        float centerY = y + (h / 2f);

        if (playerTransform != null)
        {
            int playerFloor = FloorTracker.GetFloorForHeight(playerTransform.position.y);
            if (playerFloor == currentFloor)
            {
                float px = centerX + (playerTransform.position.x * scaleX);
                float py = centerY - (playerTransform.position.z * scaleY);

                float dotSize = isExpanded ? 10f : 8f;
                GUI.DrawTexture(new Rect(px - dotSize / 2f, py - dotSize / 2f, dotSize, dotSize), playerTexture);

                Vector3 fwd = playerTransform.forward;
                Vector2 dir2D = new Vector2(fwd.x, -fwd.z).normalized;
                float arrowLen = isExpanded ? 12f : 8f;
                DrawLine(new Vector2(px, py), new Vector2(px + dir2D.x * arrowLen, py + dir2D.y * arrowLen), Color.green, 2);
            }
        }
    }

    private void DrawBorder(Rect rect, int thickness, Color color)
    {
        Texture2D tex = MakeTex(1, 1, color);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), tex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), tex);
    }

    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color savedColor = GUI.color;
        GUI.color = color;

        float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y, (pointB - pointA).magnitude, width), Texture2D.whiteTexture);

        GUI.matrix = matrix;
        GUI.color = savedColor;
    }
}

