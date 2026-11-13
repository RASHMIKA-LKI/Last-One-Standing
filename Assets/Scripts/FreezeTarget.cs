using UnityEngine;
using UnityEngine.Events;

public class FreezeTarget : MonoBehaviour
{
    [Header("Life Settings")]
    public int maxLives = 3;
    public int currentLives = 3;

    [Header("Freeze State")]
    public bool isFrozen = false;
    public float freezeDuration = 8f; // Auto-unfreeze if task takes too long, or stays until task done
    private float freezeTimer = 0f;

    [Header("Components")]
    private PlayerController playerController;

    [Header("Events")]
    public UnityEvent<int> onLivesChanged;
    public UnityEvent onFrozen;
    public UnityEvent onUnfrozen;
    public UnityEvent onEliminated;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        currentLives = maxLives;
    }

    void Update()
    {
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                Unfreeze();
            }
        }
    }

    public void TakeFreezeHit()
    {
        if (currentLives <= 0) return;

        if (!isFrozen)
        {
            // First hit freezes the player
            Freeze();
        }
        else
        {
            // If already frozen and hit again, lose 1 life!
            LoseLife();
        }
    }

    public void Freeze()
    {
        isFrozen = true;
        freezeTimer = freezeDuration;

        if (playerController != null)
        {
            playerController.SetFrozen(true);
        }

        onFrozen?.Invoke();
        Debug.Log($"{gameObject.name} is FROZEN! Complete the task to unfreeze.");
    }

    public void Unfreeze()
    {
        if (!isFrozen) return;

        isFrozen = false;

        if (playerController != null)
        {
            playerController.SetFrozen(false);
        }

        onUnfrozen?.Invoke();
        Debug.Log($"{gameObject.name} has UNFROZEN!");
    }

    public void LoseLife()
    {
        currentLives--;
        onLivesChanged?.Invoke(currentLives);
        Debug.Log($"{gameObject.name} lost a life! Lives remaining: {currentLives}");

        if (currentLives <= 0)
        {
            Eliminate();
        }
    }

    private void Eliminate()
    {
        Debug.Log($"{gameObject.name} has been ELIMINATED! Transitioning to Spectator Mode.");
        onEliminated?.Invoke();
    }
}
