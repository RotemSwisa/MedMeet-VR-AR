using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Owns the three showcase canvases (Setup → Loading → Dashboard) and routes
/// between them. Also ensures every canvas has a TrackedDeviceGraphicRaycaster
/// so XR laser pointers can click in VR.
///
/// Quick-test toggles let you jump straight to a screen during development.
/// </summary>
public class DashboardSceneSetup : MonoBehaviour
{
    [Header("Canvases")]
    [Tooltip("Screen 1 — participants pick names + cities")]
    public GameObject setupCanvas;

    [Tooltip("Screen 2 — globe + airplane orbit while metrics arrive")]
    public GameObject loadingCanvas;

    [Tooltip("Screen 3 — full impact report")]
    public GameObject dashboardCanvas;

    [Header("Quick Testing")]
    [Tooltip("Skip Setup and go straight to a screen (uses testCities + testDemos below)")]
    public bool skipSetupForTesting = false;

    [Tooltip("Screen to open when skipSetupForTesting is on")]
    public DashboardSync.ScreenState testStartScreen = DashboardSync.ScreenState.Dashboard;

    public string[] testCities  = { "Tel Aviv", "London", "New York" };
    public string[] testNames   = { "Dr. Maya Levi", "Dr. James Carter", "Dr. Sarah Kim" };
    public int      testDemos   = 3;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Awake()
    {
        EnsureXRRaycaster(setupCanvas);
        EnsureXRRaycaster(loadingCanvas);
        EnsureXRRaycaster(dashboardCanvas);
    }

    void Start() => StartCoroutine(InitWhenReady());

    private IEnumerator InitWhenReady()
    {
        SetActive(setupCanvas,     false);
        SetActive(loadingCanvas,   false);
        SetActive(dashboardCanvas, false);

        // Wait briefly for DashboardDataManager — it may live in another scene
        float timeout = 5f;
        while (DashboardDataManager.Instance == null && timeout > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (skipSetupForTesting && DashboardDataManager.Instance != null)
        {
            DashboardDataManager.Instance.ApplyShowcaseSession(
                new List<string>(testCities),
                new List<string>(testNames),
                testDemos);
            ShowScreen(testStartScreen);
            Debug.Log($"[DashboardSceneSetup] Skip-Setup → {testStartScreen}");
        }
        else
        {
            ShowScreen(DashboardSync.ScreenState.Setup);
        }
    }

    // ── Public API — called by DashboardSync (synced across clients) ───────
    public void ShowScreen(DashboardSync.ScreenState s)
    {
        SetActive(setupCanvas,     s == DashboardSync.ScreenState.Setup);
        SetActive(loadingCanvas,   s == DashboardSync.ScreenState.Loading);
        SetActive(dashboardCanvas, s == DashboardSync.ScreenState.Dashboard);
    }

    public void ShowSetup()     => ShowScreen(DashboardSync.ScreenState.Setup);
    public void ShowLoading()   => ShowScreen(DashboardSync.ScreenState.Loading);
    public void ShowDashboard() => ShowScreen(DashboardSync.ScreenState.Dashboard);

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static void SetActive(GameObject go, bool v)
    {
        if (go != null && go.activeSelf != v) go.SetActive(v);
    }

    private static void EnsureXRRaycaster(GameObject canvasGO)
    {
        if (canvasGO == null) return;
        var standard = canvasGO.GetComponent<GraphicRaycaster>();
        if (standard != null) Destroy(standard);
        if (canvasGO.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    // ── Debug helpers ──────────────────────────────────────────────────────
    [ContextMenu("Debug → Show Setup")]    public void DbgSetup()     => ShowSetup();
    [ContextMenu("Debug → Show Loading")]  public void DbgLoading()   => ShowLoading();
    [ContextMenu("Debug → Show Dashboard")]public void DbgDashboard() => ShowDashboard();
}
