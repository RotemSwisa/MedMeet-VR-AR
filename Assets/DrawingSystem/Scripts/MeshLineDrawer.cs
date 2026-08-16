using UnityEngine;
using System.Collections.Generic;
using Normal.Realtime;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshLineDrawer : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh lineMesh;
    private RealtimeView realtimeView;
    private BoxCollider boxCollider;

    private List<Vector3> points = new List<Vector3>();
    private float lineWidth = 0.01f;
    private Color lineColor = Color.blue;

    private bool isInitialized = false;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        realtimeView = GetComponent<RealtimeView>();
        boxCollider = GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        // Create mesh
        lineMesh = new Mesh();
        lineMesh.name = "LineMesh";
        meshFilter.mesh = lineMesh;

        // Setup material
        if (meshRenderer.sharedMaterial == null)
        {
            meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }
    }

    void Start()
    {
        if (realtimeView != null && !realtimeView.isOwnedLocallySelf)
        {
            realtimeView.RequestOwnership();
        }

        isInitialized = true;

        // Apply color
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = lineColor;
        }
    }

    public void Initialize(Color color, float width)
    {
        lineColor = color;
        lineWidth = width;

        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    public void AddPoint(Vector3 point)
    {
        points.Add(point);
        UpdateMesh();
        UpdateCollider();
    }

    void UpdateMesh()
    {
        if (points.Count < 2)
        {
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Create a quad strip along the line
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];

            // Calculate perpendicular direction
            Vector3 direction = (p2 - p1).normalized;
            Vector3 perpendicular;

            // Choose perpendicular based on camera or world up
            if (Camera.main != null)
            {
                Vector3 toCamera = (Camera.main.transform.position - p1).normalized;
                perpendicular = Vector3.Cross(direction, toCamera).normalized * lineWidth * 0.5f;
            }
            else
            {
                perpendicular = Vector3.Cross(direction, Vector3.up).normalized * lineWidth * 0.5f;
                if (perpendicular.magnitude < 0.001f)
                {
                    perpendicular = Vector3.Cross(direction, Vector3.forward).normalized * lineWidth * 0.5f;
                }
            }

            // Create quad vertices
            int baseIndex = vertices.Count;

            vertices.Add(p1 - perpendicular);
            vertices.Add(p1 + perpendicular);
            vertices.Add(p2 - perpendicular);
            vertices.Add(p2 + perpendicular);

            // Create triangles
            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);

            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);

            // UVs
            float t = (float)i / (points.Count - 1);
            uvs.Add(new Vector2(0, t));
            uvs.Add(new Vector2(1, t));
            uvs.Add(new Vector2(0, t + 0.1f));
            uvs.Add(new Vector2(1, t + 0.1f));
        }

        // Update mesh
        lineMesh.Clear();
        lineMesh.SetVertices(vertices);
        lineMesh.SetTriangles(triangles, 0);
        lineMesh.SetUVs(0, uvs);
        lineMesh.RecalculateNormals();
        lineMesh.RecalculateBounds();
    }

    void UpdateCollider()
    {
        if (boxCollider == null || points.Count < 2) return;

        // Calculate bounds
        Bounds bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 point in points)
        {
            bounds.Encapsulate(point);
        }

        bounds.Expand(lineWidth * 2f + 0.05f);

        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
    }

    public int GetPointCount()
    {
        return points.Count;
    }

    public bool IsOwnedByMe()
    {
        return realtimeView != null && realtimeView.isOwnedLocallySelf;
    }
}