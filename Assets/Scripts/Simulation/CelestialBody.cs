using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class CelestialBody : MonoBehaviour
{
    [Header("Simulation")]
    public float radius;
    public float surfaceGravity;

    [SerializeField]
    public Vector3 initialVelocity;

    [Header("Debug Orbits")]
    public Color color;

    [HideInNormalInspector]
    public Vector3 apoapsis;
    [HideInNormalInspector]
    public Vector3 periapsis;

    public Vector3 currentVelocity { get; private set; }
    public float mass { get; private set; }

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        currentVelocity = initialVelocity;
        CalculateMass();
    }

    public void UpdateVelocity(CelestialBody[] allBodies, float timeStep)
    {
        foreach (var otherBody in allBodies)
        {
            if (otherBody == this) continue;

            float sqrDist = (otherBody.rb.position - rb.position).sqrMagnitude;
            Vector3 direction = (otherBody.rb.position - rb.position).normalized;

            Vector3 force = direction * Universe.gravitationalConstant * mass * otherBody.mass / sqrDist;
            Vector3 acceleration = force / mass;
            currentVelocity += acceleration * timeStep;
        }
    }

    public void UpdatePosition(float timeStep)
    {
        rb.position += currentVelocity * timeStep;
    }

    public void CalculateMass()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        rigidBody.mass = mass; // Update rigidbody mass
    }

    public Rigidbody rigidBody { get { return GetComponent<Rigidbody>(); } }

    #region Editor
    public void OnValidate()
    {
        CalculateMass();
    }
    #endregion
}
