using UnityEngine;

[CreateAssetMenu()]
public class SystemSettings : ScriptableObject
{
    public Planet centralBody;
    public Vector3 centralBodyVelocity;
    public Planet[] planets;

    [System.Serializable]
    public struct Planet
    {
        public string name;
        public Color color;

        [Header("Size and gravity")]
        public float surfaceGravity;
        public ShapeSettings shape;

        public OrbitSettings orbit;
    }

    [System.Serializable]
    public struct ShapeSettings
    {
        public float radius;
        [Range(2, 104)] public int resolution;
        public int elevation;
        public Texture2D heightMap;
        public Texture2D colorMap;
    }

    [System.Serializable]
    public struct OrbitSettings
    {
        public bool clockwiseOrbit;
        public float apoapsis;
        public float periapsis;
        public float inclination;
        public float axis;
        public float rotationSpeed;
    }
}
