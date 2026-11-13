using UnityEngine;

public class TemporaryArenaBuilder : MonoBehaviour
{
    [Header("Sandbox Arena Settings")]
    public float floorWidth = 28f;  // X: -14 to +14
    public float floorLength = 18f; // Z: -9 to +9
    public float floorHeight = 3.3f;
    public int numberOfFloors = 4;

    private GameObject arenaRoot;

    void Awake()
    {
        BuildTemporaryArena();
    }

    public void BuildTemporaryArena()
    {
        if (arenaRoot != null) Destroy(arenaRoot);

        arenaRoot = new GameObject("[TEMPORARY_TEST_ARENA]");
        arenaRoot.transform.position = Vector3.zero;

        Color[] floorColors = new Color[]
        {
            new Color(0.32f, 0.36f, 0.42f), // Floor 1: Grey
            new Color(0.18f, 0.32f, 0.48f), // Floor 2: Blue
            new Color(0.18f, 0.44f, 0.32f), // Floor 3: Green
            new Color(0.48f, 0.34f, 0.18f)  // Floor 4: Amber
        };

        float rampW = 3.0f;
        float rampL = 16.0f; // Long 16-meter super-gentle ramp (~11 degree slope)
        float rampAngle = Mathf.Atan2(floorHeight, rampL) * Mathf.Rad2Deg;

        for (int f = 0; f < numberOfFloors; f++)
        {
            float y = f * floorHeight;
            GameObject floorObj = new GameObject($"Floor_{f + 1}_Platform");
            floorObj.transform.parent = arenaRoot.transform;
            floorObj.transform.position = new Vector3(0, y, 0);

            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMat.color = floorColors[f % floorColors.Length];

            // 1. FLOOR SLABS (100% open airspace above all ramps - impossible to hit head)
            if (f == 0)
            {
                // Ground Floor: Full solid floor slab
                CreateCube("Ground_Slab", floorObj.transform, new Vector3(0, -0.15f, 0), new Vector3(floorWidth, 0.3f, floorLength), floorMat);
            }
            else
            {
                // Main Central Platform (Z: -4 to +4)
                CreateCube($"Main_Floor_F{f + 1}", floorObj.transform, new Vector3(0, -0.15f, 0), new Vector3(floorWidth, 0.3f, 8f), floorMat);

                if (f % 2 == 1) // Floors 2 & 4: North ramp arrives on East side (X = +8)
                {
                    // Landing pad ONLY at the very top of the ramp (X: +8 to +14).
                    // The entire rest of North side (X: -14 to +8) is 100% OPEN SKY so you never hit your head!
                    CreateCube($"Landing_East_F{f + 1}", floorObj.transform, new Vector3(11f, -0.15f, 6.5f), new Vector3(6f, 0.3f, 5f), floorMat);

                    // Full South walkway so you can walk over to the South ramp
                    CreateCube($"Walkway_South_F{f + 1}", floorObj.transform, new Vector3(0, -0.15f, -6.5f), new Vector3(floorWidth, 0.3f, 5f), floorMat);
                }
                else // Floor 3: South ramp arrives on West side (X = -8)
                {
                    // Landing pad ONLY at the very top of the ramp (X: -14 to -8).
                    // The entire rest of South side (X: -8 to +14) is 100% OPEN SKY!
                    CreateCube($"Landing_West_F{f + 1}", floorObj.transform, new Vector3(-11f, -0.15f, -6.5f), new Vector3(6f, 0.3f, 5f), floorMat);

                    // Full North walkway so you can walk over to the North ramp
                    CreateCube($"Walkway_North_F{f + 1}", floorObj.transform, new Vector3(0, -0.15f, 6.5f), new Vector3(floorWidth, 0.3f, 5f), floorMat);
                }
            }

            // 2. OUTER PERIMETER SAFETY WALLS (Zero obstructions to walkways)
            Material railMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            railMat.color = new Color(0.15f, 0.18f, 0.22f);

            CreateCube($"Wall_West_F{f + 1}", floorObj.transform, new Vector3(-floorWidth / 2f, 0.6f, 0), new Vector3(0.2f, 1.2f, floorLength), railMat);
            CreateCube($"Wall_East_F{f + 1}", floorObj.transform, new Vector3(floorWidth / 2f, 0.6f, 0), new Vector3(0.2f, 1.2f, floorLength), railMat);

            // 3. COVER PROPS (In the central area, leaving all stairs 100% open)
            Material boxMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            boxMat.color = new Color(0.6f, 0.52f, 0.42f);

            CreateCube($"Cover_Crate_F{f + 1}", floorObj.transform, new Vector3(-3f, 0.75f, 0f), new Vector3(1.8f, 1.5f, 1.8f), boxMat);
            CreateCube($"Cover_Wall_F{f + 1}", floorObj.transform, new Vector3(3f, 1.0f, 0f), new Vector3(3.5f, 2.0f, 0.3f), boxMat);

            // 4. GENTLE 16-METER RAMPS (Floor to Floor)
            if (f < numberOfFloors - 1)
            {
                Material rampMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rampMat.color = new Color(0.95f, 0.75f, 0.20f); // Bright yellow

                GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.transform.parent = arenaRoot.transform;

                float rampLengthHypot = Mathf.Sqrt(rampL * rampL + floorHeight * floorHeight);

                if (f % 2 == 0)
                {
                    // North Ramp (Starts at X = -8, climbs to X = +8 at Z = +6.5)
                    ramp.name = $"Ramp_North_F{f + 1}_to_F{f + 2}";
                    ramp.transform.position = new Vector3(0, y + (floorHeight / 2f), 6.5f);
                    ramp.transform.rotation = Quaternion.Euler(0, 0, -rampAngle);
                    ramp.transform.localScale = new Vector3(rampLengthHypot, 0.2f, rampW);
                }
                else
                {
                    // South Ramp (Starts at X = +8, climbs to X = -8 at Z = -6.5)
                    ramp.name = $"Ramp_South_F{f + 1}_to_F{f + 2}";
                    ramp.transform.position = new Vector3(0, y + (floorHeight / 2f), -6.5f);
                    ramp.transform.rotation = Quaternion.Euler(0, 0, rampAngle);
                    ramp.transform.localScale = new Vector3(rampLengthHypot, 0.2f, rampW);
                }

                var rend = ramp.GetComponent<Renderer>();
                if (rend != null) rend.material = rampMat;
            }
        }

        Debug.Log("[TemporaryArenaBuilder] Ultra-gentle 16m ramps with 100% open airspace built!");
    }

    private void CreateCube(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.parent = parent;
        cube.transform.localPosition = localPos;
        cube.transform.localScale = scale;

        var r = cube.GetComponent<Renderer>();
        if (r != null) r.material = mat;
    }
}
