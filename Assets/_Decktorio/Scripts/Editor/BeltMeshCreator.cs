using UnityEngine;
using UnityEditor;
using System.IO;

public class BeltMeshCreator : EditorWindow
{
    [MenuItem("Decktorio/Generate Belt Meshes")]
    public static void GenerateMeshes()
    {
        string path = "Assets/_Decktorio/Art/Models";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        // 1. Generate Straight Mesh
        Mesh straight = CreateStraightMesh();
        SaveMesh(straight, path + "/Mesh_Belt_Straight.asset");

        // 2. Generate Curved Mesh (Left Turn)
        Mesh curve = CreateCurvedMesh();
        SaveMesh(curve, path + "/Mesh_Belt_Corner.asset");

        AssetDatabase.Refresh();
        Debug.Log("Belt Meshes Generated in " + path);
    }

    static void SaveMesh(Mesh mesh, string path)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, path);
        }
    }

    static Mesh CreateStraightMesh()
    {
        Mesh m = new Mesh();
        m.name = "Straight Belt";

        float w = 0.8f; // Belt Width (0 to 1)
        float l = 1.0f; // Length
        float h = 0.1f; // Height

        Vector3[] verts = new Vector3[]
        {
            // Top Face
            new Vector3(-w/2, h, -l/2), new Vector3(w/2, h, -l/2),
            new Vector3(-w/2, h,  l/2), new Vector3(w/2, h,  l/2),
        };

        int[] tris = new int[] { 0, 2, 1, 2, 3, 1 };
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1),
        };

        m.vertices = verts;
        m.triangles = tris;
        m.uv = uvs;
        m.RecalculateNormals();
        return m;
    }

    static Mesh CreateCurvedMesh()
    {
        Mesh m = new Mesh();
        m.name = "Corner Belt";

        // Logic: Create a 90-degree arc turning LEFT (South to West)
        // Center of rotation is relative to the tile
        int segments = 10;
        float width = 0.8f;
        float radius = 0.5f; // Centerline radius
        float h = 0.1f;

        Vector3[] verts = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];
        int[] tris = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-Mathf.PI / 2, 0, t); // -90 to 0 degrees

            // We want to curve from Bottom (0, -0.5) to Left (-0.5, 0)
            // Pivot for this arc is at (-0.5, -0.5)
            float pivotX = -0.5f;
            float pivotZ = -0.5f;

            // Inner and Outer radii relative to pivot
            float rInner = radius - (width / 2); // 0.1
            float rOuter = radius + (width / 2); // 0.9

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Vertices
            verts[i * 2] = new Vector3(pivotX + rOuter * cos, h, pivotZ + rOuter * sin); // Outer
            verts[i * 2 + 1] = new Vector3(pivotX + rInner * cos, h, pivotZ + rInner * sin); // Inner

            // UVs (stretched along the curve)
            uvs[i * 2] = new Vector2(1, t);
            uvs[i * 2 + 1] = new Vector2(0, t);

            if (i < segments)
            {
                int start = i * 2;
                tris[i * 6] = start;
                tris[i * 6 + 1] = start + 1;
                tris[i * 6 + 2] = start + 2;
                tris[i * 6 + 3] = start + 2;
                tris[i * 6 + 4] = start + 1;
                tris[i * 6 + 5] = start + 3;
            }
        }

        m.vertices = verts;
        m.triangles = tris;
        m.uv = uvs;
        m.RecalculateNormals();
        return m;
    }
}