using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerShooting : MonoBehaviour
{
    [Header("Gun Settings")]
    public float range = 50f;
    public float fireRate = 0.5f;
    public Transform gunMuzzle;
    public Camera playerCamera;

    [Header("Visual Effects")]
    public Color freezeBeamColor = new Color(0.2f, 0.7f, 1f, 0.8f);
    public float beamDuration = 0.08f;

    private float nextFireTime = 0f;
    private LineRenderer beamLine;
    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // Create or get LineRenderer for the freeze laser beam
        beamLine = GetComponent<LineRenderer>();
        if (beamLine == null)
        {
            beamLine = gameObject.AddComponent<LineRenderer>();
            beamLine.startWidth = 0.05f;
            beamLine.endWidth = 0.05f;
            beamLine.material = new Material(Shader.Find("Sprites/Default"));
            beamLine.startColor = freezeBeamColor;
            beamLine.endColor = freezeBeamColor;
            beamLine.enabled = false;
        }
    }

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
    }

    void Update()
    {
        // Cannot shoot if frozen or cursor is unlocked
        if (playerController != null && playerController.isFrozen) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        bool shootPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            shootPressed = true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            shootPressed = true;
        }
#endif

        if (shootPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 origin = gunMuzzle != null ? gunMuzzle.position : (playerCamera != null ? playerCamera.transform.position : transform.position);
        Vector3 shootDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;

        Ray ray = new Ray(playerCamera != null ? playerCamera.transform.position : origin, shootDir);
        RaycastHit hit;
        Vector3 targetPoint = origin + shootDir * range;

        if (Physics.Raycast(ray, out hit, range))
        {
            targetPoint = hit.point;

            // Check if we hit a player or target
            FreezeTarget target = hit.collider.GetComponentInParent<FreezeTarget>();
            if (target != null && target.gameObject != gameObject)
            {
                target.TakeFreezeHit();
            }
        }

        StartCoroutine(ShowBeam(origin, targetPoint));
    }

    private System.Collections.IEnumerator ShowBeam(Vector3 start, Vector3 end)
    {
        if (beamLine != null)
        {
            beamLine.SetPosition(0, start);
            beamLine.SetPosition(1, end);
            beamLine.enabled = true;
            yield return new WaitForSeconds(beamDuration);
            beamLine.enabled = false;
        }
    }
}
