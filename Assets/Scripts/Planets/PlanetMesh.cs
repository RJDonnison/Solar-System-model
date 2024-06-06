using System;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlanetMesh : MonoBehaviour
{
    // TODO: collisions
    // TODO: fix max resolution to be approx 256 per face
    public SystemSettings.ShapeSettings shape;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private bool settingsChanged = true;

    public void Init(SystemSettings.ShapeSettings shape)
    {
        this.shape = shape;

        settingsChanged = true;
    }

    void OnValidate()
    {
        settingsChanged = true;
    }

    void Update()
    {
        if (meshFilter == null) meshFilter = this.GetOrAddComponent<MeshFilter>();

        if (meshRenderer == null) meshRenderer = this.GetOrAddComponent<MeshRenderer>();

        if (settingsChanged)
        {
            settingsChanged = false;

            // Create a new material or use the existing one
            Material material = meshRenderer.sharedMaterial;

            if (shape.colorMap != null) material.SetTexture("_MainTex", shape.colorMap);
            else if (shape.heightMap != null) material.SetTexture("_MainTex", shape.heightMap);

            GenerateMesh();
        }
    }

    //TODO: set texture

    // TODO: Optimize so renders only part of the mesh
    private void GenerateMesh()
    {
        MeshData[] allMeshData = GenerateFaces(shape.resolution);

        CombineInstance[] combine = new CombineInstance[allMeshData.Length];
        for (int i = 0; i < allMeshData.Length; i++)
        {
            combine[i].mesh = allMeshData[i].CreateMesh();
            combine[i].transform = Matrix4x4.identity;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, true, false);

        meshFilter.sharedMesh = mesh;
    }

    MeshData[] GenerateFaces(int resolution)
    {
        MeshData[] allMeshData = new MeshData[6];
        Vector3[] faceNormals =
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        for (int i = 0; i < faceNormals.Length; i++)
        {
            allMeshData[i] = CreateFace(faceNormals[i], resolution);
        }

        return allMeshData;
    }

    MeshData CreateFace(Vector3 normal, int resolution)
    {
        Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
        Vector3 axisB = Vector3.Cross(normal, axisA);

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uv = new Vector2[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int vertexIndex = x + y * resolution;

                // Generate vertices
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = normal + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = PointOnCubeToPointOnSphere(pointOnUnitCube);

                //TODO: fix uvs
                uv[vertexIndex] = PointToCoordinate(pointOnUnitSphere).ToUV();

                vertices[vertexIndex] = HightScaledPointOnSphere(pointOnUnitSphere);

                // Generate triangles
                if (x != resolution - 1 && y != resolution - 1)
                {
                    // First triangle
                    triangles[triIndex] = vertexIndex;
                    triangles[triIndex + 1] = vertexIndex + resolution + 1;
                    triangles[triIndex + 2] = vertexIndex + resolution;

                    // Second triangle
                    triangles[triIndex + 3] = vertexIndex;
                    triangles[triIndex + 4] = vertexIndex + 1;
                    triangles[triIndex + 5] = vertexIndex + resolution + 1;

                    triIndex += 6;
                }
            }
        }

        return new MeshData(vertices, uv, triangles);
    }

    public Vector3 PointOnCubeToPointOnSphere(Vector3 p)
    {
        float x2 = p.x * p.x;
        float y2 = p.y * p.y;
        float z2 = p.z * p.z;

        float x = p.x * MathF.Sqrt(1 - (y2 + z2) / 2 + (y2 * z2) / 3);
        float y = p.y * MathF.Sqrt(1 - (z2 + x2) / 2 + (z2 * x2) / 3);
        float z = p.z * MathF.Sqrt(1 - (x2 + y2) / 2 + (x2 * y2) / 3);

        return new Vector3(x, y, z);
    }

    public static Coordinate PointToCoordinate(Vector3 pointOnUnitSphere)
    {
        float latitude = Mathf.Asin(pointOnUnitSphere.y);
        float a = pointOnUnitSphere.x;
        float b = -pointOnUnitSphere.z;

        float longitude = Mathf.Atan2(a, b);
        return new Coordinate(longitude, latitude);
    }

    public Vector3 HightScaledPointOnSphere(Vector3 p)
    {
        float height;
        if (shape.heightMap != null)
        {
            Vector2 coord = PointToCoordinate(p).ToUV();
            int x = Mathf.FloorToInt(coord.x * shape.heightMap.width);
            int y = Mathf.FloorToInt(coord.y * shape.heightMap.height);
            Color heightColor = shape.heightMap.GetPixel(x, y);

            height = (heightColor.grayscale / 256) * shape.elevation;
        }
        else
        {
            height = 0;
        }

        return p * shape.radius * (1 + height);
    }
}
