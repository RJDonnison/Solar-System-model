using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class BodySimulation : MonoBehaviour
{
    public SystemSettings settings;

    [SerializeField] private bool displayOrbitExtremes = false;

    [SerializeField] private bool displayOrbits = false;
    [SerializeField] private bool useReferenceBody = false;
    [SerializeField] private CelestialBody referenceBody;

    [Range(1, 99999), SerializeField] private int numSteps = 1000;
    [SerializeField] private bool usePhysicsTimeStep = false;
    [SerializeField] private float timeStep = 0.01f;

    CelestialBody[] bodies;

    #region Body Simulation
    private void Awake()
    {
        if (!Application.isPlaying) return;
        bodies = FindObjectsOfType<CelestialBody>();
        Time.fixedDeltaTime = Universe.physicsTimeStep;
        UpdateCelestialBodies();
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying) return;
        foreach (var body in bodies) body.UpdateVelocity(bodies, Universe.physicsTimeStep);
        foreach (var body in bodies) body.UpdatePosition(Universe.physicsTimeStep);
    }
    #endregion

    #region Planet Init
    private void CreateCelestialBody(SystemSettings.Planet planet)
    {
        GameObject planetObject = new GameObject(planet.name);
        planetObject.transform.SetParent(transform);

        CelestialBody celestialBody = planetObject.AddComponent<CelestialBody>();

        GameObject planetMesh = new GameObject("mesh");
        planetMesh.transform.SetParent(planetObject.transform);

        planetMesh.AddComponent<PlanetMesh>().Init(planet.shape);

        UpdateCelstialBody(celestialBody);
    }

    // Call on settings changed
    public void UpdateCelestialBodies()
    {
        if (settings == null) return;

        bodies = FindObjectsOfType<CelestialBody>();

        // Create central body
        if (!bodies.Any(b => b.name == settings.centralBody.name))
            CreateCelestialBody(settings.centralBody);

        // Always crating planet
        foreach (var planet in settings.planets)
        {
            // TODO: fix warning on create planet
            if (!bodies.Any(b => b.name == planet.name))
                CreateCelestialBody(planet);
        }

        // Remove bodies not in planets 
        foreach (var body in bodies)
        {
            if (!settings.planets.Any(p => p.name == body.name) && body.name != settings.centralBody.name)
                DestroyImmediate(body.gameObject);
        }

        foreach (var body in bodies) UpdateCelstialBody(body);
        foreach (var body in bodies) UpdateMesh(body);
    }

    private void UpdateCelstialBody(CelestialBody body)
    {
        // Get planet info
        var planet = settings.planets.FirstOrDefault(p => p.name == body.name);

        // Is central body
        bool isCentralBody = false;
        if (body.name == settings.centralBody.name) isCentralBody = true;

        // Use central body planet
        if (isCentralBody) planet = settings.centralBody;

        if (planet.name == null) return;

        body.rigidBody.useGravity = false;

        body.radius = planet.shape.radius;
        body.surfaceGravity = planet.surfaceGravity;
        body.CalculateMass();
        body.color = planet.color;

        // Set transform
        if (!isCentralBody) body.transform.position = CalculateInitialPosition(body, planet);
        else body.rigidBody.transform.localPosition = Vector3.zero; // Set to 0,0,0 in local transform

        // Set rotation
        if (!isCentralBody) body.transform.rotation = CalculateInitialAngle(planet);

        // Set initial velocity
        var centralBody = transform.Find(settings.centralBody.name).GetComponent<CelestialBody>(); // Temp
        if (!isCentralBody) body.initialVelocity = CalculateInitialVelocity(planet, body.mass, centralBody) + settings.centralBodyVelocity; // Different for central body  
        else body.initialVelocity = settings.centralBodyVelocity;


    }

    private void UpdateMesh(CelestialBody body)
    {
        // Get planet info
        var planet = settings.planets.FirstOrDefault(p => p.name == body.name);

        // Is central body
        if (body.name == settings.centralBody.name) planet = settings.centralBody;

        //Get mesh
        PlanetMesh bodyMesh = body.gameObject.GetComponentInChildren<PlanetMesh>();

        bodyMesh.Init(planet.shape);
    }

    private Vector3 CalculateInitialPosition(CelestialBody body, SystemSettings.Planet planet)
    {
        // Convert the angle to radians
        float inclinationRad = planet.orbit.inclination * Mathf.Deg2Rad;

        // Calculate apoapsis
        float ay = transform.position.y + Mathf.Sin(inclinationRad) * planet.orbit.apoapsis;
        float az = transform.position.z + Mathf.Cos(inclinationRad) * planet.orbit.apoapsis;

        Vector3 apoapsis = new Vector3(transform.position.x, ay, az);

        body.apoapsis = apoapsis;

        // Calculate periapsis
        float py = transform.position.y - Mathf.Sin(inclinationRad) * planet.orbit.periapsis;
        float pz = transform.position.z - Mathf.Cos(inclinationRad) * planet.orbit.periapsis;

        Vector3 periapsis = new Vector3(transform.position.x, py, pz);

        body.periapsis = periapsis;

        return apoapsis;
    }

    // TODO: set axis angle
    private Quaternion CalculateInitialAngle(SystemSettings.Planet planet)
    {
        Quaternion tiltRotation = Quaternion.Euler(planet.orbit.axis, 0, 0);
        return tiltRotation;
    }

    // TODO: Update to account for all bodies??
    private Vector3 CalculateInitialVelocity(SystemSettings.Planet body, float bodyMass, CelestialBody centralBody)
    {
        // Constant to get correct value
        float k = 1 + (bodyMass / centralBody.mass);
        // Semi-major axis
        float semiMajorAxis = (body.orbit.apoapsis + body.orbit.periapsis) / 2.0f;

        // Gravitational parameter (mu) = G * M
        float gravitationalParameter = Universe.gravitationalConstant * centralBody.mass;

        // Velocity at apoapsis using the vis-viva equation: v^2 = G * M * (2 / r - 1 / a)
        float apoapsisVelocity = Mathf.Sqrt(gravitationalParameter * k * (2.0f / body.orbit.apoapsis - 1.0f / semiMajorAxis));
        if (!body.orbit.clockwiseOrbit) apoapsisVelocity *= -1.0f; // Invert orbit direction if necessary

        return new Vector3(apoapsisVelocity, 0, 0);
    }
    #endregion

    #region Orbit Display
    private void Update()
    {
        if (!Application.isPlaying)
        {
            timeStep = usePhysicsTimeStep ? Universe.physicsTimeStep : timeStep;
            bodies = FindObjectsOfType<CelestialBody>();
            if (displayOrbits) DrawOrbits();
            if (displayOrbitExtremes) DrawOrbitExtremes();
        }
    }

    private void DrawOrbits()
    {
        if (bodies.Length <= 0) { Debug.LogError("No bodies found"); return; }

        var virtualBodies = new VirtualBody[bodies.Length];
        var drawPoints = new Vector3[bodies.Length][];

        int referenceFrameIndex = 0;
        Vector3 referenceBodyInitialPosition = Vector3.zero;

        // Initialize virtual bodies (don't want to move the actual bodies)
        for (int i = 0; i < virtualBodies.Length; i++)
        {
            virtualBodies[i] = new VirtualBody(bodies[i]);
            drawPoints[i] = new Vector3[numSteps];

            if (bodies[i] == referenceBody && useReferenceBody && referenceBody != null)
            {
                referenceFrameIndex = i;
                referenceBodyInitialPosition = virtualBodies[i].position;
            }
        }

        // Simulate
        for (int step = 0; step < numSteps; step++)
        {
            Vector3 referenceBodyPosition = (useReferenceBody && referenceBody != null) ? virtualBodies[referenceFrameIndex].position : Vector3.zero;
            // Update velocities
            for (int i = 0; i < virtualBodies.Length; i++)
                virtualBodies[i].velocity += CalculateAcceleration(i, virtualBodies) * timeStep;

            // Update positions
            for (int i = 0; i < virtualBodies.Length; i++)
            {
                Vector3 newPos = virtualBodies[i].position + virtualBodies[i].velocity * timeStep;
                virtualBodies[i].position = newPos;

                if (useReferenceBody && referenceBody != null)
                {
                    var referenceFrameOffset = referenceBodyPosition - referenceBodyInitialPosition;
                    newPos -= referenceFrameOffset;
                }
                if (useReferenceBody && referenceBody != null && i == referenceFrameIndex)
                {
                    newPos = referenceBodyInitialPosition;
                }

                drawPoints[i][step] = newPos;
            }
        }

        // Draw paths
        for (int bodyIndex = 0; bodyIndex < virtualBodies.Length; bodyIndex++)
        {
            var pathColour = bodies[bodyIndex].color;

            for (int i = 0; i < drawPoints[bodyIndex].Length - 1; i++)
            {
                Debug.DrawLine(drawPoints[bodyIndex][i], drawPoints[bodyIndex][i + 1], pathColour);
            }
        }
    }

    private void DrawOrbitExtremes()
    {
        if (bodies.Length <= 0) { Debug.LogError("No bodies found"); return; }

        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i].name == settings.centralBody.name) continue;

            var pathColour = bodies[i].color;
            Debug.DrawLine(transform.position, bodies[i].apoapsis, pathColour);
            Debug.DrawLine(transform.position, bodies[i].periapsis, pathColour);
        }
    }

    private Vector3 CalculateAcceleration(int i, VirtualBody[] virtualBodies)
    {
        Vector3 acceleration = Vector3.zero;
        for (int j = 0; j < virtualBodies.Length; j++)
        {
            if (i == j)
            {
                continue;
            }
            Vector3 forceDir = (virtualBodies[j].position - virtualBodies[i].position).normalized;
            float sqrDst = (virtualBodies[j].position - virtualBodies[i].position).sqrMagnitude;
            acceleration += forceDir * Universe.gravitationalConstant * virtualBodies[j].mass / sqrDst;
        }
        return acceleration;
    }

    private class VirtualBody
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;

        public VirtualBody(CelestialBody body)
        {
            position = body.transform.position;
            velocity = body.initialVelocity;
            mass = body.mass;
        }
    }
    #endregion
}
