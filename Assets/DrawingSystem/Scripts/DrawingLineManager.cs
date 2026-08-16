using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DrawingLineManager : MonoBehaviour
{
    private Dictionary<int, StrokeLineRenderer> activeStrokes = new Dictionary<int, StrokeLineRenderer>();
    private float updateInterval = 0.1f;
    private float lastUpdateTime = 0f;

    void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateAllStrokes();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateAllStrokes()
    {
        // Find all dots in the scene
        DrawingDot[] allDots = FindObjectsOfType<DrawingDot>();

        //Debug.Log($"DrawingLineManager: Found {allDots.Length} dots in scene");

        // Group by stroke ID
        var strokeGroups = allDots.GroupBy(d => d.GetStrokeID()).Where(g => g.Key != 0);

        int groupCount = 0;
        foreach (var group in strokeGroups)
        {
            groupCount++;
            int strokeID = group.Key;
            int dotCount = group.Count();

            Debug.Log($"DrawingLineManager: Stroke {strokeID} has {dotCount} dots");

            // Create line renderer if doesn't exist
            if (!activeStrokes.ContainsKey(strokeID))
            {
                CreateLineForStroke(strokeID, group.ToList());
            }
        }

        if (groupCount == 0 && allDots.Length > 0)
        {
            Debug.LogWarning($"DrawingLineManager: Found {allDots.Length} dots but no valid stroke groups (all strokeID = 0)");

            // Debug first dot
            if (allDots.Length > 0)
            {
                DrawingDot firstDot = allDots[0];
                Debug.Log($"First dot name: {firstDot.gameObject.name}, strokeID: {firstDot.GetStrokeID()}");
            }
        }

        // Clean up old strokes
        List<int> toRemove = new List<int>();
        foreach (var kvp in activeStrokes)
        {
            if (kvp.Value == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            activeStrokes.Remove(id);
        }
    }

    void CreateLineForStroke(int strokeID, List<DrawingDot> dots)
    {
        if (dots.Count == 0) return;

        // Create container
        GameObject container = new GameObject($"Stroke_{strokeID}");
        container.transform.SetParent(transform);

        // Add line renderer
        StrokeLineRenderer lineRenderer = container.AddComponent<StrokeLineRenderer>();

        // Get color and width from first dot
        DrawingDot firstDot = dots[0];
        Color color = Color.blue;
        float width = 0.01f;

        if (firstDot != null)
        {
            MeshRenderer mr = firstDot.GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                color = mr.material.color;
            }
            width = firstDot.transform.localScale.x;
        }

        lineRenderer.Initialize(strokeID, color, width);

        // Register all dots
        foreach (DrawingDot dot in dots)
        {
            lineRenderer.RegisterDot(dot);
        }

        activeStrokes[strokeID] = lineRenderer;

        Debug.Log($"Created line renderer for stroke {strokeID} with {dots.Count} dots");
    }
}