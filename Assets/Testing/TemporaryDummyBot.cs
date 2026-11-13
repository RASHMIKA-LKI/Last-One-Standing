using System.Collections;
using UnityEngine;

[RequireComponent(typeof(FreezeTarget))]
public class TemporaryDummyBot : MonoBehaviour
{
    [Header("Bot Identity")]
    public string botName = "Bot_Enemy";
    public bool isTeammate = false;
    public float moveSpeed = 2.5f;

    [Header("Combat")]
    public float visionRange = 18f;
    public float fireCooldown = 3f;
    private float nextFireTime = 0f;

    [Header("Patrol Waypoints")]
    public Vector3[] waypoints;
    private int currentWaypointIndex = 0;

    private FreezeTarget freezeTarget;
    private LineRenderer laserLine;
    private GameObject iceBlockInstance;
    private Renderer botRenderer;

    void Awake()
    {
        freezeTarget = GetComponent<FreezeTarget>();
        botRenderer = GetComponent<Renderer>();

        // Laser beam visual
        laserLine = gameObject.AddComponent<LineRenderer>();
        laserLine.startWidth = 0.05f;
        laserLine.endWidth = 0.05f;
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.startColor = new Color(0.2f, 0.7f, 1f, 0.8f);
        laserLine.endColor = new Color(0.2f, 0.7f, 1f, 0.8f);
        laserLine.enabled = false;

        // Hook into Freeze events
        freezeTarget.onFrozen.AddListener(OnBotFrozen);
        freezeTarget.onUnfrozen.AddListener(OnBotUnfrozen);
        freezeTarget.onEliminated.AddListener(OnBotEliminated);
    }

    void Start()
    {
        // Visual color distinction
        if (botRenderer != null)
        {
            botRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            botRenderer.material.color = isTeammate ? new Color(0.2f, 0.6f, 1f) : new Color(0.9f, 0.25f, 0.25f);
        }

        // Generate default waypoints around spawn position if none set
        if (waypoints == null || waypoints.Length == 0)
        {
            Vector3 origin = transform.position;
            waypoints = new Vector3[]
            {
                origin + new Vector3(4f, 0, 0),
                origin + new Vector3(4f, 0, 4f),
                origin + new Vector3(-4f, 0, 4f),
                origin + new Vector3(-4f, 0, -2f)
            };
        }
    }

    void Update()
    {
        if (freezeTarget.isFrozen || freezeTarget.currentLives <= 0) return;

        Patrol();

        if (!isTeammate)
        {
            CheckForPlayerTarget();
        }
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 targetPoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetPoint - transform.position);
        direction.y = 0; // stay on floor

        if (direction.magnitude < 0.8f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }

    private void CheckForPlayerTarget()
    {
        if (Time.time < nextFireTime) return;

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= visionRange)
        {
            // Aim at player
            Vector3 aimDir = (player.transform.position + Vector3.up * 1.0f) - (transform.position + Vector3.up * 1.0f);
            if (Vector3.Angle(transform.forward, aimDir) < 60f)
            {
                RaycastHit hit;
                Vector3 origin = transform.position + Vector3.up * 1.0f;
                if (Physics.Raycast(origin, aimDir.normalized, out hit, visionRange))
                {
                    if (hit.collider.GetComponentInParent<PlayerController>() != null)
                    {
                        ShootFreezeLaser(origin, hit.point, hit.collider.GetComponentInParent<FreezeTarget>());
                        nextFireTime = Time.time + fireCooldown;
                    }
                }
            }
        }
    }

    private void ShootFreezeLaser(Vector3 origin, Vector3 hitPoint, FreezeTarget target)
    {
        StartCoroutine(RenderLaser(origin, hitPoint));
        if (target != null)
        {
            target.TakeFreezeHit();
        }
    }

    private IEnumerator RenderLaser(Vector3 from, Vector3 to)
    {
        laserLine.enabled = true;
        laserLine.SetPosition(0, from);
        laserLine.SetPosition(1, to);
        yield return new WaitForSeconds(0.12f);
        laserLine.enabled = false;
    }

    private void OnBotFrozen()
    {
        // Spawn temporary translucent ice cube over bot
        if (iceBlockInstance == null)
        {
            iceBlockInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            iceBlockInstance.name = "Bot_IceBlock";
            iceBlockInstance.transform.parent = transform;
            iceBlockInstance.transform.localPosition = new Vector3(0, 0, 0);
            iceBlockInstance.transform.localScale = new Vector3(1.3f, 2.2f, 1.3f);

            var col = iceBlockInstance.GetComponent<Collider>();
            if (col != null) Destroy(col); // don't block raycasts to bot

            var r = iceBlockInstance.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                r.material.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            }
        }
        iceBlockInstance.SetActive(true);
    }

    private void OnBotUnfrozen()
    {
        if (iceBlockInstance != null)
        {
            iceBlockInstance.SetActive(false);
        }
    }

    private void OnBotEliminated()
    {
        Debug.Log($"[TemporaryDummyBot] {botName} eliminated!");
        if (iceBlockInstance != null) Destroy(iceBlockInstance);
        gameObject.SetActive(false);
    }
}

