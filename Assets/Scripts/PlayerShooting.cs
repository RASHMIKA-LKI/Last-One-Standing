using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerShooting : MonoBehaviour
{
    [Header("Gun & Aim Settings")]
    public float range = 80f;
    public float fireRate = 0.28f;
    public Transform gunMuzzle;
    public Camera playerCamera;
    public Transform gunTransform;

    [Header("Realistic Laser Optics")]
    [ColorUsage(true, true)]
    public Color beamGlowColor = new Color(3.8f, 0.12f, 0.12f, 1f); // Intense HDR Crimson

    [ColorUsage(true, true)]
    public Color beamCoreColor = new Color(5.5f, 5.0f, 5.0f, 1f); // Blinding White Ion Core

    public float beamDuration = 0.075f;
    public float outerWidth = 0.038f;
    public float coreWidth = 0.014f;

    [Header("Impact & Sparks")]
    public float impactFlareSize = 0.28f;
    public float muzzleFlareSize = 0.18f;
    public float impactLightRange = 4.0f;
    public float impactLightIntensity = 3.0f;
    public int sparkCount = 14;

    [Header("Weapon Feedback")]
    public float gunKickDistance = 0.045f;
    public float cameraKickAngle = 0.35f;

    // Internal components
    private LineRenderer outerBeam;
    private LineRenderer coreBeam;
    private GameObject impactQuad;
    private GameObject muzzleQuad;
    private Light impactLight;
    private Light muzzleLight;
    private ParticleSystem impactSparks;
    private Material outerMat;
    private Material coreMat;
    private Material flareMat;

    private float nextFireTime = 0f;
    private PlayerController playerController;
    private Coroutine activeLaserRoutine;
    private Vector3 originalGunLocalPos;
    private bool hasOriginalGunPos = false;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // Load procedural FX textures
        Texture2D beamTex = LoadTextureFromAssets("Textures/LaserFX/Laser_Beam.png");
        Texture2D flareTex = LoadTextureFromAssets("Textures/LaserFX/Laser_Impact_Flare.png");

        // 1. Outer Glow Beam (Coherent collimated laser cylinder)
        outerMat = CreateAdditiveMaterial(beamTex, beamGlowColor);
        outerBeam = GetComponent<LineRenderer>();
        if (outerBeam == null) outerBeam = gameObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(outerBeam, outerMat, outerWidth);

        // 2. Inner White-Hot Core Beam
        GameObject coreObj = new GameObject("Laser_CoreBeam");
        coreObj.transform.SetParent(transform);
        coreMat = CreateAdditiveMaterial(beamTex, beamCoreColor);
        coreBeam = coreObj.AddComponent<LineRenderer>();
        ConfigureLineRenderer(coreBeam, coreMat, coreWidth);

        // 3. Impact Flare & Dynamic Point Light
        flareMat = CreateAdditiveMaterial(flareTex, beamGlowColor);

        impactQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        impactQuad.name = "Laser_ImpactFlare";
        impactQuad.transform.SetParent(transform);
        Destroy(impactQuad.GetComponent<Collider>());
        impactQuad.GetComponent<MeshRenderer>().material = flareMat;
        impactLight = impactQuad.AddComponent<Light>();
        impactLight.type = LightType.Point;
        impactLight.color = new Color(1f, 0.2f, 0.2f);
        impactLight.intensity = impactLightIntensity;
        impactLight.range = impactLightRange;
        impactLight.shadows = LightShadows.None;
        impactQuad.SetActive(false);

        // 4. Muzzle Flash Flare & Barrel Light
        muzzleQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        muzzleQuad.name = "Laser_MuzzleFlare";
        muzzleQuad.transform.SetParent(transform);
        Destroy(muzzleQuad.GetComponent<Collider>());
        muzzleQuad.GetComponent<MeshRenderer>().material = flareMat;
        muzzleLight = muzzleQuad.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.color = new Color(1f, 0.25f, 0.25f);
        muzzleLight.intensity = impactLightIntensity * 0.8f;
        muzzleLight.range = 2.5f;
        muzzleQuad.SetActive(false);

        // 5. Dynamic Ricochet Sparks System
        SetupSparks();
    }

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
        }

        if (gunTransform != null)
        {
            originalGunLocalPos = gunTransform.localPosition;
            hasOriginalGunPos = true;
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

    public Vector3 GetMuzzleOrigin()
    {
        if (gunMuzzle != null)
        {
            return gunMuzzle.position;
        }

        // Realistic First-Person weapon offset (lower-right, 0.45m forward from camera view)
        if (playerCamera != null)
        {
            Transform cam = playerCamera.transform;
            return cam.position + cam.right * 0.24f - cam.up * 0.18f + cam.forward * 0.45f;
        }

        return transform.position + transform.forward * 0.5f;
    }

    void Shoot()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        Transform cam = playerCamera != null ? playerCamera.transform : transform;

        // 1. Raycast originates from true aim center (camera crosshair)
        Ray aimRay = new Ray(cam.position, cam.forward);
        RaycastHit hit;
        Vector3 targetPoint = cam.position + cam.forward * range;
        Vector3 hitNormal = -cam.forward;
        bool hasHit = false;

        if (Physics.Raycast(aimRay, out hit, range))
        {
            targetPoint = hit.point;
            hitNormal = hit.normal;
            hasHit = true;

            // Damage / Freeze target check
            FreezeTarget target = hit.collider.GetComponentInParent<FreezeTarget>();
            if (target != null && target.gameObject != gameObject)
            {
                target.TakeFreezeHit();
            }
        }

        // 2. Visual beam origin ALWAYS starts at the weapon muzzle (never inside camera)
        Vector3 origin = GetMuzzleOrigin();

        if (activeLaserRoutine != null) StopCoroutine(activeLaserRoutine);
        activeLaserRoutine = StartCoroutine(RenderLaserPulse(origin, targetPoint, hitNormal, hasHit));

        // 3. Gun kick & camera kick
        if (gunTransform != null)
        {
            if (!hasOriginalGunPos)
            {
                originalGunLocalPos = gunTransform.localPosition;
                hasOriginalGunPos = true;
            }
            StartCoroutine(KickGun());
        }

        if (playerCamera != null)
        {
            StartCoroutine(KickCamera());
        }
    }

    private IEnumerator RenderLaserPulse(Vector3 start, Vector3 end, Vector3 hitNormal, bool hasHit)
    {
        // 1. Set beam positions (from weapon muzzle pointing directly towards hit point)
        outerBeam.SetPosition(0, start);
        outerBeam.SetPosition(1, end);
        coreBeam.SetPosition(0, start);
        coreBeam.SetPosition(1, end);

        outerBeam.enabled = true;
        coreBeam.enabled = true;

        // 2. Muzzle Flash (at barrel tip, oriented towards shot direction)
        muzzleQuad.transform.position = start;
        muzzleQuad.transform.rotation = Quaternion.LookRotation(end - start);
        muzzleQuad.transform.localScale = Vector3.one * muzzleFlareSize;
        muzzleLight.intensity = impactLightIntensity * 0.8f;
        muzzleQuad.SetActive(true);

        // 3. Impact FX & Ricochet Sparks
        if (hasHit)
        {
            impactQuad.transform.position = end + hitNormal * 0.02f;
            impactQuad.transform.rotation = Quaternion.LookRotation(hitNormal);
            impactQuad.transform.localScale = Vector3.one * impactFlareSize;
            impactLight.intensity = impactLightIntensity;
            impactQuad.SetActive(true);

            if (impactSparks != null)
            {
                impactSparks.transform.position = end + hitNormal * 0.02f;
                impactSparks.transform.rotation = Quaternion.LookRotation(hitNormal);
                impactSparks.Emit(sparkCount);
            }
        }
        else
        {
            impactQuad.SetActive(false);
        }

        // 4. Ultra-snappy plasma dissipation curve
        float elapsed = 0f;
        while (elapsed < beamDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / beamDuration;
            float falloff = 1f - (t * t); // Snappy high-energy dissipation

            outerBeam.startWidth = outerWidth * falloff;
            outerBeam.endWidth = (outerWidth * 0.85f) * falloff;
            coreBeam.startWidth = coreWidth * falloff;
            coreBeam.endWidth = (coreWidth * 0.85f) * falloff;

            if (hasHit && impactLight != null) impactLight.intensity = impactLightIntensity * falloff;
            if (muzzleLight != null) muzzleLight.intensity = (impactLightIntensity * 0.8f) * falloff;

            yield return null;
        }

        // Turn off
        outerBeam.enabled = false;
        coreBeam.enabled = false;
        impactQuad.SetActive(false);
        muzzleQuad.SetActive(false);
        activeLaserRoutine = null;
    }

    private IEnumerator KickGun()
    {
        float kickTime = 0.03f;
        float returnTime = 0.11f;
        Vector3 kickPos = originalGunLocalPos - Vector3.forward * gunKickDistance;

        float t = 0f;
        while (t < kickTime)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(originalGunLocalPos, kickPos, t / kickTime);
            yield return null;
        }

        t = 0f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(kickPos, originalGunLocalPos, t / returnTime);
            yield return null;
        }
        gunTransform.localPosition = originalGunLocalPos;
    }

    private IEnumerator KickCamera()
    {
        Transform cam = playerCamera.transform;
        Quaternion startRot = cam.localRotation;
        Quaternion kickRot = startRot * Quaternion.Euler(-cameraKickAngle, 0f, 0f);

        float t = 0f;
        float dur = 0.07f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cam.localRotation = Quaternion.Slerp(kickRot, startRot, t / dur);
            yield return null;
        }
        cam.localRotation = startRot;
    }

    private void ConfigureLineRenderer(LineRenderer line, Material mat, float width)
    {
        line.material = mat;
        line.startWidth = width;
        line.endWidth = width * 0.85f;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View; // Always face camera properly without distortion
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.textureMode = LineTextureMode.Stretch;
        line.enabled = false;
    }

    private void SetupSparks()
    {
        GameObject sparksObj = new GameObject("Laser_ImpactSparks");
        sparksObj.transform.SetParent(transform);
        impactSparks = sparksObj.AddComponent<ParticleSystem>();

        var main = impactSparks.main;
        main.duration = 0.15f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.3f, 0.3f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = impactSparks.emission;
        emission.rateOverTime = 0;

        var shape = impactSparks.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.02f;

        var renderer = sparksObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = outerMat;
    }

    private Material CreateAdditiveMaterial(Texture2D tex, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 1);   // Additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetColor("_EmissionColor", color);
        mat.EnableKeyword("_EMISSION");
        return mat;
    }

    private Texture2D LoadTextureFromAssets(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath);
        if (System.IO.File.Exists(fullPath))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        return null;
    }
}
