using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SignLanguagePlayer : MonoBehaviour
{
    public static SignLanguagePlayer Instance;

    [Header("Video Player")]
    public VideoPlayer videoPlayer;
    public RawImage displayImage; // התמונה שעליה יוצג הסרטון

    [Header("Settings")]
    public bool isActive = false; // מופעל רק אחרי לחיצה על הכפתור

    // תור הסרטונים שממתינים להשמעה
    private Queue<VideoClip> videoQueue = new Queue<VideoClip>();
    private bool isPlaying = false;

    // מילון: מילה באנגלית -> VideoClip
    private Dictionary<string, VideoClip> signDictionary = new Dictionary<string, VideoClip>();

    // ביטויים מרוכבים שצריך לזהות לפני מילים בודדות
    private List<(string phrase, string[] clips)> phraseMap = new List<(string, string[])>
    {
        ("headache",    new[] { "headache" }),
        ("stomachache", new[] { "stomachache" }),
        ("i have",      new[] { "i", "have" }),
        ("i need",      new[] { "i", "need" }),
        ("i am",        new[] { "i" }),
    };

    // מילות בסיס
    private readonly string[] knownWords = new[]
    {
        "pain", "headache", "stomachache",
        "have", "me", "i", "need",
        "doctor", "where", "you"
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // טעינת כל הסרטונים מתיקיית Resources/Signs
        foreach (string word in knownWords)
        {
            VideoClip clip = Resources.Load<VideoClip>("Signs/" + word);
            if (clip != null)
                signDictionary[word] = clip;
            else
                Debug.LogWarning($"SignLanguage: לא נמצא סרטון עבור '{word}' ב-Resources/Signs/");
        }

        // הגדרת ה-VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;

        // הסתרת התצוגה בהתחלה
        displayImage.gameObject.SetActive(false);
    }

    // נקרא מהכפתור במניו
    public void ToggleSignLanguage()
    {
        isActive = !isActive;

        if (!isActive)
        {
            // כיבוי — עוצרים הכל ומסתירים
            videoPlayer.Stop();
            videoQueue.Clear();
            isPlaying = false;
            displayImage.gameObject.SetActive(false);
        }

        Debug.Log($"Sign Language: {(isActive ? "ON" : "OFF")}");
    }

    public void ProcessText(string englishText)
    {
        if (!isActive) return;
        if (string.IsNullOrEmpty(englishText)) return;

        string lower = englishText.ToLower();
        List<string> clipsToPlay = new List<string>();

        // שלב 1: החלפת מילות גשר שלא רוצים בהן
        lower = lower.Replace(" a ", " ")
                     .Replace(" an ", " ")
                     .Replace(" the ", " ")
                     .Replace(" to ", " ")
                     .Replace(" is ", " ")
                     .Replace(" are ", " ");

        // שלב 2: חיפוש ביטויים מרוכבים קודם
        foreach (var (phrase, clips) in phraseMap)
        {
            if (lower.Contains(phrase))
            {
                clipsToPlay.AddRange(clips);
                lower = lower.Replace(phrase, "");
            }
        }

        // שלב 3: חיפוש מילים בודדות במה שנשאר
        string[] words = lower.Split(new char[] { ' ', ',', '.', '?', '!' },
                         System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            string clean = word.Trim().ToLower();
            if (signDictionary.ContainsKey(clean))
                clipsToPlay.Add(clean); // הסרנו את התנאי שחסם כפילויות
        }

        if (clipsToPlay.Count == 0) return;

        foreach (string clipName in clipsToPlay)
        {
            if (signDictionary.ContainsKey(clipName))
                videoQueue.Enqueue(signDictionary[clipName]);
        }

        if (!isPlaying)
            StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        isPlaying = true;
        displayImage.gameObject.SetActive(true);

        while (videoQueue.Count > 0)
        {
            VideoClip clip = videoQueue.Dequeue();
            videoPlayer.clip = clip;
            videoPlayer.Play();

            // המתנה עד שהסרטון נטען ומתחיל
            yield return new WaitUntil(() => videoPlayer.isPlaying);

            // המתנה עד שהסרטון מסיים
            yield return new WaitUntil(() => !videoPlayer.isPlaying);

            // הפסקה קצרה בין מילים
            yield return new WaitForSeconds(0.1f);
        }

        isPlaying = false;

        // אם אין עוד סרטונים, מסתירים אחרי 2 שניות
        yield return new WaitForSeconds(2f);
        if (videoQueue.Count == 0)
            displayImage.gameObject.SetActive(false);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        // טיפול ביציאה מ-loop אם נדרש בעתיד
    }
}