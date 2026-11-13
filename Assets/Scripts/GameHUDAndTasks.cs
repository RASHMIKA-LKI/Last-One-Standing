using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameHUDAndTasks : MonoBehaviour
{
    [Header("References")]
    public FreezeTarget freezeTarget;

    [Header("Task Settings")]
    public int tapsRequired = 10;
    private int currentTaps = 0;

    void Start()
    {
        if (freezeTarget == null)
        {
            freezeTarget = GetComponent<FreezeTarget>();
            if (freezeTarget == null)
            {
                freezeTarget = FindFirstObjectByType<FreezeTarget>();
            }
        }
    }

    void Update()
    {
        if (freezeTarget != null && freezeTarget.isFrozen)
        {
            bool tapPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                tapPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                tapPressed = true;
            }
#endif
            if (tapPressed)
            {
                currentTaps++;
                if (currentTaps >= tapsRequired)
                {
                    currentTaps = 0;
                    freezeTarget.Unfreeze();
                }
            }
        }
        else
        {
            currentTaps = 0;
        }
    }

    void OnGUI()
    {
        // 1. Draw Crosshair in screen center
        float crosshairSize = 14f;
        float cx = Screen.width / 2f - crosshairSize / 2f;
        float cy = Screen.height / 2f - crosshairSize / 2f;
        GUI.color = Color.white;
        GUI.Box(new Rect(cx, cy, crosshairSize, crosshairSize), "+");

        // 2. Draw Lives in Top-Left
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.red;

        string hearts = "";
        int lives = freezeTarget != null ? freezeTarget.currentLives : 3;
        for (int i = 0; i < lives; i++)
        {
            hearts += "♥ ";
        }

        GUI.Label(new Rect(25, 20, 300, 40), $"LIVES: {hearts}", style);

        // 3. Draw Frozen Alert & Task in Center if frozen
        if (freezeTarget != null && freezeTarget.isFrozen)
        {
            // Dark overlay box
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height / 2f - 110, 440, 160), "");

            GUIStyle alertStyle = new GUIStyle(GUI.skin.label);
            alertStyle.fontSize = 28;
            alertStyle.fontStyle = FontStyle.Bold;
            alertStyle.alignment = TextAnchor.MiddleCenter;
            alertStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);

            GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 95, 400, 40), "❄ YOU ARE FROZEN! ❄", alertStyle);

            GUIStyle taskStyle = new GUIStyle(GUI.skin.label);
            taskStyle.fontSize = 18;
            taskStyle.alignment = TextAnchor.MiddleCenter;
            taskStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 50, 400, 30), "TASK: Rapidly tap [SPACE] to break free!", taskStyle);

            // Progress bar
            float progress = (float)currentTaps / tapsRequired;
            GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 10, 300, 25), $"Breaking Ice: {currentTaps}/{tapsRequired}");
            GUI.color = new Color(0f, 0.7f, 1f, 0.6f);
            GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 10, 300 * progress, 25), "");
        }

        // 4. Draw Game Info (Floor & Hints)
        GUI.color = Color.white;
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 14;
        hintStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(25, 60, 400, 30), "Controls: [WASD] Move | [Mouse] Look | [Left Click] Freeze Gun | [Esc] Unlock Mouse", hintStyle);
    }
}
