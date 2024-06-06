using UnityEngine;

public class MeshData
{
    public Vector3[] vertices { get; private set; }
    public Vector2[] uv { get; private set; }
    public int[] triangles { get; private set; }

    public MeshData(Vector3[] vertices, Vector2[] uv, int[] triangles)
    {
        this.vertices = vertices;
        this.uv = uv;
        this.triangles = triangles;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }
}
