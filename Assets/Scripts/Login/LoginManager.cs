using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // ודא שזה קיים

public class LoginManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    // *** הוספה חדשה: שדה קלט לשם החדר ***
    public TMP_InputField roomInputField;

    public TMP_Dropdown roleDropdown;
    public Button joinButton;
    public TMP_Text statusText;

    [Header("Settings")]
    public string vrRoomSceneName = "vr meeting"; // שים לב שזה תואם לשם הסצנה שלך בדיוק

    [Header("Audio")]
    public AudioSource buttonClickSound;

    // משתנים פנימיים
    private string playerName = "";
    private string playerRole = "";
    // *** הוספה חדשה: שם החדר ***
    private string roomName = "";
    private bool isConnecting = false;
    private TouchScreenKeyboard keyboard;

    void Start()
    {
        // בדיקת תקינות (לא חובה אם אתה בטוח שחיברת הכל)
        if (nameInputField == null || roleDropdown == null || joinButton == null || roomInputField == null) // ודא שבודק גם את roomInputField
        {
            if (statusText != null) statusText.text = "Error: Missing UI Elements!";
            return;
        }

        // הגדרה קריטית: מונע מה-Input Field הרגיל לנסות לפתוח מקלדת ולהתנגש עם שלנו
        nameInputField.shouldHideMobileInput = true;

        // טעינת שמות קודמים והאזנה לשינויים
        nameInputField.text = PlayerPrefs.GetString("PlayerName", "");
        nameInputField.onValueChanged.AddListener(OnInputChanged);

        // *** הוספה חדשה: טיפול בשם החדר ***
        roomInputField.text = PlayerPrefs.GetString("RoomName", "MyRoom");
        roomInputField.onValueChanged.AddListener(OnInputChanged);
        roomInputField.shouldHideMobileInput = true; // גם לשדה הזה
        // **********************************

        // האזנה ללחיצה על כפתור ה-Join
        joinButton.onClick.AddListener(OnJoinButtonClicked);

        // מצב התחלתי
        OnInputChanged(""); // הפעל את הפונקציה כדי להגדיר את מצב הכפתור
        if (statusText != null) statusText.text = "Please enter your name and a room name";
    }

    // --- החלק של המקלדת (לשדה השם בלבד, לפי הקוד הקיים) ---

    // פונקציה זו תיקרא על ידי ה-Event Trigger שנכין עוד רגע
    public void OpenKeyboardForName()
    {
        // אם המקלדת כבר פתוחה, לא לעשות כלום
        if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible)
            return;

        // פתיחת המקלדת של אנדרואיד/Quest לשדה השם
        keyboard = TouchScreenKeyboard.Open(nameInputField.text, TouchScreenKeyboardType.Default);

        // התחלת מעקב אחרי מה שהמשתמש מקליד
        StartCoroutine(UpdateInputFromKeyboard(nameInputField));
    }

    // *** הוספה חדשה: פונקציה לפתיחת מקלדת עבור שם החדר ***
    public void OpenKeyboardForRoom()
    {
        if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible)
            return;

        keyboard = TouchScreenKeyboard.Open(roomInputField.text, TouchScreenKeyboardType.Default);

        StartCoroutine(UpdateInputFromKeyboard(roomInputField));
    }
    // *************************************************

    // *** עדכון הקורוטינה לקבלת משתנה ה-Input Field הפעיל ***
    IEnumerator UpdateInputFromKeyboard(TMP_InputField currentInputField)
    {
        while (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible)
        {
            // מעתיק מהמקלדת הוירטואלית לתוך התיבה ביוניטי בזמן אמת
            if (currentInputField.text != keyboard.text)
            {
                currentInputField.text = keyboard.text;
            }
            yield return null;
        }

        // וידוא אחרון כשהמקלדת נסגרת
        if (keyboard != null)
        {
            currentInputField.text = keyboard.text;
        }
    }

    // --- שאר הלוגיקה של ההתחברות ---

    // *** פונקציה מאוחדת לבדיקת תקינות שני השדות ***
    void OnInputChanged(string newText)
    {
        bool nameOK = !string.IsNullOrEmpty(nameInputField.text) && nameInputField.text.Length >= 2;
        bool roomOK = !string.IsNullOrEmpty(roomInputField.text) && roomInputField.text.Length >= 2;

        joinButton.interactable = nameOK && roomOK;
    }

    void OnJoinButtonClicked()
    {
        if (buttonClickSound != null) buttonClickSound.Play();
        if (isConnecting) return;

        playerName = nameInputField.text.Trim();
        // *** קריאת שם החדר שהוזן ***
        roomName = roomInputField.text.Trim();

        // בדיקה שהשם תקין
        if (string.IsNullOrEmpty(playerName) || playerName.Length < 2)
        {
            ShowStatus("Name too short!", Color.red);
            return;
        }

        // *** בדיקה שהחדר תקין ***
        if (string.IsNullOrEmpty(roomName) || roomName.Length < 2)
        {
            ShowStatus("Room name too short!", Color.red);
            return;
        }

        // שליפת התפקיד
        if (roleDropdown != null && roleDropdown.options.Count > 0)
            playerRole = roleDropdown.options[roleDropdown.value].text;
        else
            playerRole = "Guest";

        StartCoroutine(JoinMeetingProcess());
    }

    IEnumerator JoinMeetingProcess()
    {
        isConnecting = true;
        joinButton.interactable = false;
        ShowStatus("Connecting...", Color.green);

        // שמירת הנתונים למעבר בין סצנות
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerRole", playerRole);
        // *** הוספה קריטית: שמירת שם החדר ***
        PlayerPrefs.SetString("RoomName", roomName);

        PlayerPrefs.Save();

        // *** הוספת קריאה ל-CSV Log (אם יש לכם קוד חיצוני) ***
        // אם ה-CSV Log נמצא בסקריפט אחר בסצנת הפתיחה, הפעל אותו כאן
        // לדוגמה: AuditManager.LogPreConnection(playerName, playerRole, roomName); 

        yield return new WaitForSeconds(1.0f); // השהייה קטנה

        // טעינת הסצנה הבאה
        SceneManager.LoadScene(vrRoomSceneName);
    }

    void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
}