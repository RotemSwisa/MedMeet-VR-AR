using UnityEngine;
using UnityEngine.UI;
using TMPro; // וודא שיש לך TextMeshPro בפרויקט
using System.Collections.Generic;

public class TeleportManager : MonoBehaviour
{
    [System.Serializable]
    public struct TeleportLocation
    {
        public string roomName;     // שם החדר שיופיע בכפתור
        public Transform target;    // נקודת היעד
    }

    [Header("Settings")]
    public List<TeleportLocation> locations; // כאן תוכל להוסיף ב-+ ב-Inspector
    public GameObject player;                // האוואטר שלך
    public GameObject menuCanvas;            // הקנבס שיצרנו
    public GameObject buttonPrefab;          // פריפאב של כפתור (הוראות בהמשך)
    public Transform buttonContainer;        // הפאנל עם ה-Vertical Layout Group

    void Start()
    {
        GenerateButtons();
        menuCanvas.SetActive(false); // לוודא שהתפריט סגור בהתחלה
    }

    void GenerateButtons()
    {
        // מנקה כפתורים קיימים אם יש
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // יוצר כפתור לכל נקודה ברשימה
        foreach (TeleportLocation loc in locations)
        {
            GameObject newBtn = Instantiate(buttonPrefab, buttonContainer);

            // הגדרת הטקסט על הכפתור
            newBtn.GetComponentInChildren<TMP_Text>().text = loc.roomName;

            // הוספת לוגיקה ללחיצה
            newBtn.GetComponent<Button>().onClick.AddListener(() => {
                TeleportPlayer(loc.target);
            });
        }
    }

    public void ToggleMenu()
    {
        menuCanvas.SetActive(!menuCanvas.activeSelf);
    }

    void TeleportPlayer(Transform targetPoint)
    {
        player.transform.position = targetPoint.position;
        player.transform.rotation = targetPoint.rotation;
        menuCanvas.SetActive(false); // סגירת התפריט אחרי השיגור
    }
}