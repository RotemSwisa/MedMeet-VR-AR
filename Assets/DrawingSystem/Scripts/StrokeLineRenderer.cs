using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class StrokeLineRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private int strokeID;
    private Color lineColor;
    private float lineWidth;

    private List<DrawingDot> dots = new List<DrawingDot>();
    private float updateInterval = 0.05f;
    private float lastUpdateTime = 0f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // Configure line renderer
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }

    void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateLine();
            lastUpdateTime = Time.time;
        }
    }

    public void Initialize(int id, Color color, float width)
    {
        strokeID = id;
        lineColor = color;
        lineWidth = width;

        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.material.color = color;
        }
    }

    public void RegisterDot(DrawingDot dot)
    {
        if (!dots.Contains(dot))
        {
            dots.Add(dot);
            UpdateLine();
        }
    }

    void UpdateLine()
    {
        // Find all dots with matching stroke ID in the scene
        DrawingDot[] allDots = FindObjectsOfType<DrawingDot>();
        dots.Clear();

        foreach (DrawingDot dot in allDots)
        {
            if (dot.GetStrokeID() == strokeID)
            {
                dots.Add(dot);
            }
        }

        // Sort by point index
        dots = dots.OrderBy(d => d.GetPointIndex()).ToList();

        // Update line renderer positions
        if (lineRenderer != null && dots.Count > 0)
        {
            lineRenderer.positionCount = dots.Count;

            for (int i = 0; i < dots.Count; i++)
            {
                if (dots[i] != null)
                {
                    lineRenderer.SetPosition(i, dots[i].transform.position);
                }
            }
        }
    }

    void OnDestroy()
    {
        dots.Clear();
    }
}