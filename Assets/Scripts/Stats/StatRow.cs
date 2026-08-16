using UnityEngine;
using UnityEngine.UI;
using TMPro; // הוספנו את הספריה של TextMeshPro

public class StatRow : MonoBehaviour
{
    public Image colorIndicator;
    public TMP_Text playerNameText; // שינינו מ-Text ל-TMP_Text
    public TMP_Text stayTimeText;   // שינינו מ-Text ל-TMP_Text

    public void Setup(string name, Color playerColor, float speechPrc, float stayTimeSeconds)
    {
        // 1. הגדרת צבע ושם
        colorIndicator.color = playerColor;
        playerNameText.text = name;

        // 2. זמן שהות
        System.TimeSpan t = System.TimeSpan.FromSeconds(stayTimeSeconds);
        stayTimeText.text = string.Format("Stay: {0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }
}