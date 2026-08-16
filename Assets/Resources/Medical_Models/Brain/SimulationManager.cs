using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    [Header("UI Screens")]
    public GameObject startScreen;
    public GameObject liveMonitorScreen;
    public GameObject summaryScreen;

    [Header("Live Monitor UI")]
    public TMP_Text liveStatusText;

    [Header("Summary UI (Sliders & Text)")]
    public TMP_Text heartDetailsText;
    public Slider heartScoreSlider;
    public TMP_Text brainDetailsText;
    public Slider brainScoreSlider;

    // --- ה-UI של הריאות ---
    public TMP_Text lungsDetailsText;
    public Slider lungsScoreSlider;

    public TMP_Text finalScoreText;

    [Header("Anatomy Roots")]
    public GameObject heartRoot;
    public GameObject brainRoot;
    public GameObject lungsRoot; // השורש של הריאות

    private SurgeryManager heartManager;
    private BrainSurgeryManager brainManager;
    private LungsSurgeryManager lungsManager;

    private int heartAttempts = 0;
    private int brainStep1Attempts = 0;
    private int brainStep2Attempts = 0;
    private int lungsAttempts = 0;

    private float sumHeartScores = 0f;
    private float sumBrainScores = 0f;
    private float sumLungsScores = 0f;

    void Awake() { Instance = this; }

    void Start()
    {
        // Defensive: any of these Inspector references can legitimately be unassigned
        // when the simulation isn't part of the current scene's flow. Null-check them
        // instead of throwing a NullReferenceException that blocks the whole scene.
        if (heartRoot != null) heartManager = heartRoot.GetComponentInChildren<SurgeryManager>(true);
        if (brainRoot != null) brainManager = brainRoot.GetComponentInChildren<BrainSurgeryManager>(true);
        if (lungsRoot != null) lungsManager = lungsRoot.GetComponentInChildren<LungsSurgeryManager>(true);
        InitializeGame();
    }

    private void InitializeGame()
    {
        heartAttempts = 0; brainStep1Attempts = 0; brainStep2Attempts = 0; lungsAttempts = 0;
        sumHeartScores = 0f; sumBrainScores = 0f; sumLungsScores = 0f;

        SetIfNotNull(startScreen,       true);
        SetIfNotNull(liveMonitorScreen, false);
        SetIfNotNull(summaryScreen,     false);
        SetIfNotNull(heartRoot,         false);
        SetIfNotNull(brainRoot,         false);
        SetIfNotNull(lungsRoot,         false);
    }

    public void StartSimulation()
    {
        SetIfNotNull(startScreen,       false);
        SetIfNotNull(liveMonitorScreen, true);
        SetIfNotNull(heartRoot,         true);
        UpdateLiveMonitor("Heart Surgery", heartAttempts, -1f);
    }

    /// <summary>
    /// Safely toggle a GameObject. If the Inspector reference is null (because
    /// the simulation isn't wired up in this scene) the call is silently
    /// skipped instead of throwing a NullReferenceException.
    /// </summary>
    private static void SetIfNotNull(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    public void AddAttempt(string part, float cutScore = 0f)
    {
        if (part == "Heart") { heartAttempts++; sumHeartScores += cutScore; UpdateLiveMonitor("Heart Surgery", heartAttempts, cutScore); }
        else if (part.StartsWith("Brain")) { if (part == "Brain1") brainStep1Attempts++; else brainStep2Attempts++; sumBrainScores += cutScore; UpdateLiveMonitor("Brain Surgery", brainStep1Attempts + brainStep2Attempts, cutScore); }
        else if (part == "Lungs") { lungsAttempts++; sumLungsScores += cutScore; UpdateLiveMonitor("Lungs Surgery", lungsAttempts, cutScore); }
    }

    // --- סדר הניתוחים המעודכן: לב -> מוח -> ריאות ---
    public void CompleteHeart(float finalScore = 0f) { StartCoroutine(TransitionToBrain()); }
    public void CompleteBrain(float finalScore = 0f) { StartCoroutine(TransitionToLungs()); }
    public void CompleteLungs(float finalScore = 0f) { StartCoroutine(ShowSummaryScreen()); }

    private void UpdateLiveMonitor(string procedure, int attempts, float lastScore)
    {
        if (liveStatusText == null) return;
        string status = $"<color=#E74C3C><b>● LIVE PATIENT MONITOR</b></color>\n\n<b>Procedure:</b> {procedure}\n<b>Incisions:</b> {attempts}\n\n";
        if (attempts > 0 && lastScore >= 0f)
        {
            string scoreColor = lastScore >= 90f ? "#2ECC71" : "#E74C3C";
            status += $"<b>Last Cut:</b> <color={scoreColor}>{lastScore:F1}%</color>\n\n";
        }
        status += $"<size=80%><color=#F1C40F>* Requirement: 90%+</color></size>";
        liveStatusText.text = status;
    }

    private IEnumerator TransitionToBrain()
    {
        yield return new WaitForSeconds(4f);
        SetIfNotNull(heartRoot, false);
        SetIfNotNull(brainRoot, true);
        UpdateLiveMonitor("Brain Surgery", 0, -1f);
    }

    private IEnumerator TransitionToLungs()
    {
        yield return new WaitForSeconds(4f);
        SetIfNotNull(brainRoot, false);
        SetIfNotNull(lungsRoot, true);
        UpdateLiveMonitor("Lungs Surgery", 0, -1f);
    }

    private IEnumerator ShowSummaryScreen()
    {
        yield return new WaitForSeconds(4f);
        SetIfNotNull(lungsRoot,         false);
        SetIfNotNull(liveMonitorScreen, false);
        SetIfNotNull(summaryScreen,     true);
        StartCoroutine(AnimateSummaryUI());
    }

    public void RestartSimulation()
    {
        StopAllCoroutines();
        if (heartManager != null) heartManager.ResetHeartState();
        if (brainManager != null) brainManager.ResetBrainState();
        if (lungsManager != null) lungsManager.ResetLungsState();
        InitializeGame();
    }

    private IEnumerator AnimateSummaryUI()
    {
        float avgHeartScore = heartAttempts > 0 ? sumHeartScores / heartAttempts : 0f;
        float avgBrainScore = (brainStep1Attempts + brainStep2Attempts) > 0 ? sumBrainScores / (brainStep1Attempts + brainStep2Attempts) : 0f;
        float avgLungsScore = lungsAttempts > 0 ? sumLungsScores / lungsAttempts : 0f;

        float finalAverageScore = (avgHeartScore + avgBrainScore + avgLungsScore) / 3f;

        if (heartDetailsText != null) heartDetailsText.text = $"Heart | Attempts: {heartAttempts} | <b>{avgHeartScore:F1}%</b>";
        if (brainDetailsText != null) brainDetailsText.text = $"Brain | Attempts: {brainStep1Attempts + brainStep2Attempts} | <b>{avgBrainScore:F1}%</b>";
        if (lungsDetailsText != null) lungsDetailsText.text = $"Lungs | Attempts: {lungsAttempts} | <b>{avgLungsScore:F1}%</b>";

        if (heartScoreSlider != null) heartScoreSlider.value = 0;
        if (brainScoreSlider != null) brainScoreSlider.value = 0;
        if (lungsScoreSlider != null) lungsScoreSlider.value = 0;
        if (finalScoreText != null) finalScoreText.text = "";

        float duration = 1.5f; float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            if (heartScoreSlider != null) heartScoreSlider.value = Mathf.Lerp(0, avgHeartScore, p);
            if (brainScoreSlider != null) brainScoreSlider.value = Mathf.Lerp(0, avgBrainScore, p);
            if (lungsScoreSlider != null) lungsScoreSlider.value = Mathf.Lerp(0, avgLungsScore, p);
            yield return null;
        }

        string feedbackMsg = "";
        string gradeColor = "";

        if (finalAverageScore >= 90f)
        {
            feedbackMsg = "Outstanding! You are a master Surgeon! The patient is in excellent hands.";
            gradeColor = "#2ECC71";
        }
        else if (finalAverageScore >= 60f)
        {
            feedbackMsg = "Steady hands! You're on your way to being a great Surgeon, but there's room for more precision.";
            gradeColor = "#F1C40F";
        }
        else
        {
            feedbackMsg = "Critical performance. You need significant practice to become a skilled Surgeon.";
            gradeColor = "#E74C3C";
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"<size=130%><b>Final Assessment:</b> <color={gradeColor}>{finalAverageScore:F1}%</color></size>\n" +
                                  $"<size=90%><color=#FFFFFF>{feedbackMsg}</color></size>";
        }
    }
}