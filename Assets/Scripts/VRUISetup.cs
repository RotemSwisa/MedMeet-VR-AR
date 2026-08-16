using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// סקריפט עזר - מוודא שכל מה שצריך לאינטראקציה עם UI ב-VR קיים
/// </summary>
public class VRUISetup : MonoBehaviour
{
    [Header("Auto-Setup on Start")]
    [Tooltip("האם להוסיף אוטומטית Graphic Raycaster לכל Canvas")]
    public bool autoAddGraphicRaycaster = true;

    [Tooltip("האם לוודא שיש Event System מתאים")]
    public bool ensureEventSystem = true;

    void Start()
    {
        SetupVRUI();
    }

    void SetupVRUI()
    {
        Debug.Log("🔧 Starting VR UI Setup...");

        // 1. בדיקת Event System
        if (ensureEventSystem)
        {
            CheckEventSystem();
        }

        // 2. הוספת Graphic Raycaster לכל Canvas
        if (autoAddGraphicRaycaster)
        {
            AddGraphicRaycastersToCanvases();
        }

        Debug.Log("✅ VR UI Setup Complete!");
    }

    void CheckEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogWarning("⚠️ No EventSystem found! Creating one...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        // בדיקה אם יש XR UI Input Module
        XRUIInputModule xrInput = eventSystem.GetComponent<XRUIInputModule>();

        if (xrInput == null)
        {
            Debug.Log("📝 Adding XR UI Input Module...");

            // הסרת StandaloneInputModule אם קיים
            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                DestroyImmediate(standalone);
            }

            // הוספת XR UI Input Module
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("✅ XR UI Input Module added!");
        }
        else
        {
            Debug.Log("✅ XR UI Input Module already exists");
        }
    }

    void AddGraphicRaycastersToCanvases()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();

        int added = 0;
        foreach (Canvas canvas in allCanvases)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();

            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"✅ Added Graphic Raycaster to: {canvas.name}");
                added++;
            }
        }

        if (added > 0)
        {
            Debug.Log($"✅ Added Graphic Raycaster to {added} Canvas(es)");
        }
        else
        {
            Debug.Log("ℹ️ All Canvases already have Graphic Raycaster");
        }
    }

    // פונקציה לבדיקה ידנית מה-Inspector
    [ContextMenu("Check VR UI Setup")]
    void CheckSetup()
    {
        Debug.Log("=== VR UI Setup Check ===");

        // בדיקת Event System
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            XRUIInputModule xrInput = eventSystem.GetComponent<XRUIInputModule>();
            if (xrInput != null)
            {
                Debug.Log("✅ Event System with XR UI Input Module - OK");
            }
            else
            {
                Debug.LogWarning("⚠️ Event System missing XR UI Input Module!");
            }
        }
        else
        {
            Debug.LogError("❌ No Event System found!");
        }

        // בדיקת Canvases
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"✅ {canvas.name} - Graphic Raycaster OK");
            }
            else
            {
                Debug.LogWarning($"⚠️ {canvas.name} - Missing Graphic Raycaster!");
            }
        }

        // בדיקת Ray Interactors
        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>();
        if (rayInteractors.Length > 0)
        {
            Debug.Log($"✅ Found {rayInteractors.Length} Ray Interactor(s)");
        }
        else
        {
            Debug.LogWarning("⚠️ No Ray Interactors found on controllers!");
        }

        Debug.Log("=== Check Complete ===");
    }
}