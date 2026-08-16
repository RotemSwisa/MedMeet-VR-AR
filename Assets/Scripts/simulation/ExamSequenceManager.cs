using UnityEngine;
using TMPro;

public class ExamSequenceManager : MonoBehaviour
{
    public static ExamSequenceManager Instance;

    // משתנה סטטי שסופר באיזו פעם אנחנו
    public static int runCount = 1;

    // הוספנו את כל האפשרויות, כולל המסיחים
    public enum IllnessType { Healthy, Throat, Heart, Ear, Lungs }
    public IllnessType currentIllness;

    [Header("UI References")]
    public TextMeshProUGUI instructionsText;
    public GameObject diagnosisPanel;
    public GameObject startButton;

    [Header("Throat References")]
    public GameObject infectedTonsilsVisual;

    [Header("Heart References")]
    public AudioSource stethoscopeAudio;
    public AudioClip normalHeartSound;
    public AudioClip badHeartSound;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // מכבים את הפאנל בהתחלה ומדליקים את כפתור ה-START
        if (diagnosisPanel != null) diagnosisPanel.SetActive(false);
        if (startButton != null) startButton.SetActive(true);

        // ** חדש: מוודאים שהטקסט הראשי דולק בהתחלה **
        if (instructionsText != null) instructionsText.gameObject.SetActive(true);

        UpdateUI("Patient is ready.\nPress START to begin the examination.");
    }

    // מופעל כשלוחצים START
    public void StartExam()
    {
        if (startButton != null) startButton.SetActive(false);
        if (diagnosisPanel != null) diagnosisPanel.SetActive(true); // מציג את רשימת התשובות מיד

        // ** חדש: מעלימים את הטקסט הראשי לגמרי כשהבדיקה מתחילה **
        if (instructionsText != null) instructionsText.gameObject.SetActive(false);

        // הלוגיקה שלך: פעם 1 גרון, פעם 2 לב, פעם 3+ רנדומלי (חצי-חצי)
        if (runCount == 1)
            currentIllness = IllnessType.Throat;
        else if (runCount == 2)
            currentIllness = IllnessType.Heart;
        else
            currentIllness = (Random.value > 0.5f) ? IllnessType.Throat : IllnessType.Heart;

        ApplyIllness();

        UpdateUI($"Run #{runCount}: Examine the patient.\nClick your diagnosis on the panel when ready.");

        runCount++; // מקדם את הספירה לריצה הבאה
    }

    private void ApplyIllness()
    {
        // קודם כל מאפסים הכל למצב "בריא"
        if (infectedTonsilsVisual != null) infectedTonsilsVisual.SetActive(false);
        if (stethoscopeAudio != null && normalHeartSound != null) stethoscopeAudio.clip = normalHeartSound;

        // מדליקים רק את המחלה שנבחרה
        if (currentIllness == IllnessType.Throat)
        {
            if (infectedTonsilsVisual != null) infectedTonsilsVisual.SetActive(true);
        }
        else if (currentIllness == IllnessType.Heart)
        {
            if (stethoscopeAudio != null && badHeartSound != null) stethoscopeAudio.clip = badHeartSound;
        }
    }

    // --- הפונקציות שיתחברו ל-5 הכפתורים בפאנל ---
    public void GuessThroat() { CheckAnswer(IllnessType.Throat); }
    public void GuessHeart() { CheckAnswer(IllnessType.Heart); }
    public void GuessEar() { CheckAnswer(IllnessType.Ear); }
    public void GuessLungs() { CheckAnswer(IllnessType.Lungs); }
    public void GuessHealthy() { CheckAnswer(IllnessType.Healthy); }

    // בודק את התשובה של השחקן
    private void CheckAnswer(IllnessType playerGuess)
    {
        if (diagnosisPanel != null) diagnosisPanel.SetActive(false); // מעלים את הפאנל

        // ** חדש: מחזירים את הטקסט הראשי כדי להראות את התוצאה **
        if (instructionsText != null) instructionsText.gameObject.SetActive(true);

        if (playerGuess == currentIllness)
        {
            UpdateUI("<color=green>Excellent!</color> Your diagnosis was exactly right.\nPress START to examine a new patient.");
        }
        else
        {
            UpdateUI($"<color=red>Incorrect Diagnosis.</color>\nThe patient actually had a problem in the {currentIllness}.\nPress START to try again.");
        }

        // מחזיר את כפתור ההתחלה לסיבוב נוסף
        if (startButton != null) startButton.SetActive(true);
    }

    private void UpdateUI(string msg)
    {
        if (instructionsText != null) instructionsText.text = msg;
    }
}