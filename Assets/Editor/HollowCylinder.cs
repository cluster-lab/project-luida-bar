using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HollowCylinder : MonoBehaviour
{
    [Range(0.01f, 100f)] public float outerRadius = 1f;
    [Range(0.001f, 99f)] public float thickness = 0.2f;
    [Range(0.01f, 100f)] public float height = 2f;
    [Range(3, 256)] public int segments = 24;

    private Mesh mesh;

    void Start() => GenerateMesh();

    void OnValidate() => GenerateMesh();

    void GenerateMesh()
    {
        float innerRadius = Mathf.Max(outerRadius - thickness, 0.01f);

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Half-circle: segments quads need segments+1 vertex columns
        int cols = segments + 1;
        int wallVerts = cols * 8;
        int endCapVerts = 8; // 4 per end cap x 2 end caps
        int totalVerts = wallVerts + endCapVerts;

        Vector3[] vertices = new Vector3[totalVerts];
        Vector3[] normals = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];

        // segments quads x 4 surfaces x 2 tris x 3 indices + 2 end caps x 2 tris x 3 indices
        int[] triangles = new int[segments * 4 * 2 * 3 + 2 * 2 * 3];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI; // Half circle: 0 to PI
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float u = (float)i / segments;

            Vector3 outerBottom = new Vector3(cos * outerRadius, 0, sin * outerRadius);
            Vector3 outerTop = new Vector3(cos * outerRadius, height, sin * outerRadius);
            Vector3 innerBottom = new Vector3(cos * innerRadius, 0, sin * innerRadius);
            Vector3 innerTop = new Vector3(cos * innerRadius, height, sin * innerRadius);

            int idx = i * 8;

            // Outer wall
            vertices[idx + 0] = outerBottom;
            vertices[idx + 1] = outerTop;
            normals[idx + 0] = new Vector3(cos, 0, sin);
            normals[idx + 1] = new Vector3(cos, 0, sin);

            // Inner wall
            vertices[idx + 2] = innerBottom;
            vertices[idx + 3] = innerTop;
            normals[idx + 2] = new Vector3(-cos, 0, -sin);
            normals[idx + 3] = new Vector3(-cos, 0, -sin);

            // Top cap
            vertices[idx + 4] = outerTop;
            vertices[idx + 5] = innerTop;
            normals[idx + 4] = Vector3.up;
            normals[idx + 5] = Vector3.up;

            // Bottom cap
            vertices[idx + 6] = outerBottom;
            vertices[idx + 7] = innerBottom;
            normals[idx + 6] = Vector3.down;
            normals[idx + 7] = Vector3.down;

            for (int j = 0; j < 8; j++)
                uvs[idx + j] = new Vector2(u, j % 2 == 0 ? 0 : 1);
        }

        // End cap vertices (flat faces closing the two cut ends)
        int endIdx = wallVerts;
        Vector3 endNormal = new Vector3(0, 0, -1);

        // Right end cap (angle=0): rectangle at z=0, positive x
        vertices[endIdx + 0] = new Vector3(outerRadius, 0, 0);
        vertices[endIdx + 1] = new Vector3(outerRadius, height, 0);
        vertices[endIdx + 2] = new Vector3(innerRadius, height, 0);
        vertices[endIdx + 3] = new Vector3(innerRadius, 0, 0);

        // Left end cap (angle=PI): rectangle at z=0, negative x
        vertices[endIdx + 4] = new Vector3(-outerRadius, 0, 0);
        vertices[endIdx + 5] = new Vector3(-outerRadius, height, 0);
        vertices[endIdx + 6] = new Vector3(-innerRadius, height, 0);
        vertices[endIdx + 7] = new Vector3(-innerRadius, 0, 0);

        for (int j = 0; j < 8; j++)
            normals[endIdx + j] = endNormal;

        uvs[endIdx + 0] = new Vector2(0, 0);
        uvs[endIdx + 1] = new Vector2(0, 1);
        uvs[endIdx + 2] = new Vector2(1, 1);
        uvs[endIdx + 3] = new Vector2(1, 0);
        uvs[endIdx + 4] = new Vector2(0, 0);
        uvs[endIdx + 5] = new Vector2(0, 1);
        uvs[endIdx + 6] = new Vector2(1, 1);
        uvs[endIdx + 7] = new Vector2(1, 0);

        // Generate triangles for wall quads
        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            int curr = i * 8;
            int next = (i + 1) * 8;

            // Outer wall
            triangles[tri++] = curr + 0; triangles[tri++] = curr + 1; triangles[tri++] = next + 1;
            triangles[tri++] = curr + 0; triangles[tri++] = next + 1; triangles[tri++] = next + 0;

            // Inner wall (reversed winding)
            triangles[tri++] = curr + 2; triangles[tri++] = next + 3; triangles[tri++] = curr + 3;
            triangles[tri++] = curr + 2; triangles[tri++] = next + 2; triangles[tri++] = next + 3;

            // Top cap
            triangles[tri++] = curr + 4; triangles[tri++] = curr + 5; triangles[tri++] = next + 4;
            triangles[tri++] = curr + 5; triangles[tri++] = next + 5; triangles[tri++] = next + 4;

            // Bottom cap
            triangles[tri++] = curr + 6; triangles[tri++] = next + 6; triangles[tri++] = curr + 7;
            triangles[tri++] = curr + 7; triangles[tri++] = next + 6; triangles[tri++] = next + 7;
        }

        // Right end cap (angle=0, normal -Z)
        triangles[tri++] = endIdx + 0; triangles[tri++] = endIdx + 2; triangles[tri++] = endIdx + 1;
        triangles[tri++] = endIdx + 0; triangles[tri++] = endIdx + 3; triangles[tri++] = endIdx + 2;

        // Left end cap (angle=PI, normal -Z)
        triangles[tri++] = endIdx + 4; triangles[tri++] = endIdx + 5; triangles[tri++] = endIdx + 6;
        triangles[tri++] = endIdx + 4; triangles[tri++] = endIdx + 6; triangles[tri++] = endIdx + 7;

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
