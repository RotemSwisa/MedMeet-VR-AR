using UnityEngine;
using UnityEngine.UI;
using System.Collections;        // ← הוסף את זה!
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
// ✨ תוספת חשובה 1: הספרייה שמאפשרת לבקש אישור ממשקפי ה-Quest
using UnityEngine.Android;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
public class FilePickerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject fileListPanel;
    public Transform fileListContent;
    public GameObject fileButtonPrefab;
    public Button closeButton;
    public TextMeshProUGUI titleText;

    [Header("Settings")]
    public string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

    private System.Action<string> onFileSelected;
    private List<GameObject> fileButtons = new List<GameObject>();

    void Start()
    {
        // ודא שהפאנל מתחיל כבוי
        if (fileListPanel != null)
        {
            fileListPanel.SetActive(false);
            Debug.Log("FilePickerUI initialized");
        }
        else
        {
            Debug.LogError("FileListPanel is NULL!");
        }

        // חיבור כפתור הסגירה
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    // ✨ פונקציה חדשה: מטפלת בבירוקרטיה מול אנדרואיד
    void RequestAndroidPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif
    }

    public void ShowFilePicker(System.Action<string> callback)
    {
        Debug.Log("ShowFilePicker called!");

        onFileSelected = callback;

#if UNITY_ANDROID && !UNITY_EDITOR
    // בקשת הרשאות ב-Android
    if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
    {
        Debug.Log("Requesting storage permission...");
        Permission.RequestUserPermission(Permission.ExternalStorageRead);
        
        // המתנה קצרה והצגת הפאנל
        StartCoroutine(ShowPanelAfterPermission());
    }
    else
    {
        Debug.Log("Permission already granted");
        ShowPanel();
    }
#else
        ShowPanel();
#endif
    }

    System.Collections.IEnumerator ShowPanelAfterPermission()
    {
        yield return new WaitForSeconds(1f);
        ShowPanel();
    }

    void ShowPanel()
    {
        if (fileListPanel != null)
        {
            fileListPanel.SetActive(true);
            Debug.Log("Panel activated");
            LoadFiles();
        }
        else
        {
            Debug.LogError("Cannot show file picker - panel is null!");
        }
    }

    void LoadFiles()
    {
        Debug.Log("LoadFiles called");

        // ניקוי כפתורים קודמים
        foreach (var btn in fileButtons)
            Destroy(btn);
        fileButtons.Clear();

        List<string> foundFiles = new List<string>();

#if UNITY_EDITOR
        // באדיטור - נחפש בתיקיית Assets
        string editorPath = Path.Combine(Application.dataPath, "TestFiles");
        
        if(!Directory.Exists(editorPath))
        {
            Directory.CreateDirectory(editorPath);
            Debug.Log("Created TestFiles folder at: " + editorPath);
        }
        
        if(Directory.Exists(editorPath))
        {
            string[] files = Directory.GetFiles(editorPath);
            foreach(string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if(supportedExtensions.Contains(ext))
                {
                    foundFiles.Add(file);
                }
            }
        }
        
        // גם נחפש בתיקיות רגילות
        string[] desktopPaths = {
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
        };
        
        foreach(string path in desktopPaths)
        {
            if(Directory.Exists(path))
            {
                try
                {
                    string[] files = Directory.GetFiles(path);
                    foreach(string file in files)
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if(supportedExtensions.Contains(ext) && !foundFiles.Contains(file))
                        {
                            foundFiles.Add(file);
                        }
                    }
                }
                catch(System.Exception e)
                {
                    Debug.LogWarning($"Cannot access {path}: {e.Message}");
                }
            }
        }
        
#elif UNITY_ANDROID
        Debug.Log("Loading files on Android/Quest...");
        
        // ב-Quest - נתיבי Android 11+
        List<string> searchPaths = new List<string>();
        
        // נסה את כל האפשרויות
        string[] possiblePaths = {
            "/storage/emulated/0/Download",
            "/storage/emulated/0/Downloads",
            "/sdcard/Download",
            "/sdcard/Downloads",
            "/storage/self/primary/Download",
            "/storage/self/primary/Downloads",
            Application.persistentDataPath, // תיקיית האפליקציה עצמה
            "/sdcard/DCIM/Camera",
            "/sdcard/Pictures",
            "/sdcard/Documents"
        };
        
        // בדוק אילו נתיבים קיימים
        foreach(string path in possiblePaths)
        {
            if(Directory.Exists(path))
            {
                searchPaths.Add(path);
                Debug.Log($"Found path: {path}");
            }
            else
            {
                Debug.Log($"Path does not exist: {path}");
            }
        }
        
        Debug.Log($"Searching in {searchPaths.Count} directories...");
        
        foreach(string path in searchPaths)
        {
            try
            {
                Debug.Log($"Checking directory: {path}");
                string[] files = Directory.GetFiles(path);
                Debug.Log($"Found {files.Length} files in {path}");
                
                foreach(string file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if(supportedExtensions.Contains(ext))
                    {
                        if(!foundFiles.Contains(file))
                        {
                            foundFiles.Add(file);
                            Debug.Log($"Added file: {Path.GetFileName(file)}");
                        }
                    }
                }
            }
            catch(System.Exception e)
            {
                Debug.LogWarning($"Cannot access {path}: {e.Message}");
            }
        }
#endif

        Debug.Log($"Found {foundFiles.Count} files");

        if (foundFiles.Count == 0)
        {
            if (titleText != null)
                titleText.text = "No files found";
            CreateNoFilesMessage();
            return;
        }

        if (titleText != null)
            titleText.text = $"Select File ({foundFiles.Count} found)";

        // יצירת כפתור לכל קובץ
        foreach (string filePath in foundFiles)
        {
            CreateFileButton(filePath);
        }
    }

    void CreateFileButton(string filePath)
    {
        if (fileButtonPrefab == null)
        {
            Debug.LogError("FileButtonPrefab is NULL!");
            return;
        }

        GameObject buttonObj = Instantiate(fileButtonPrefab, fileListContent);

        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText == null)
        {
            // אם אין TMP, נשתמש ב-Text רגיל
            UnityEngine.UI.Text normalText = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (normalText != null)
                normalText.text = Path.GetFileName(filePath);
        }
        else
        {
            buttonText.text = Path.GetFileName(filePath);
        }

        button.onClick.AddListener(() => {
            OnFileSelected(filePath);
        });

        fileButtons.Add(buttonObj);
    }

    void CreateNoFilesMessage()
    {
        GameObject msgObj = new GameObject("NoFilesMessage");
        msgObj.transform.SetParent(fileListContent);
        msgObj.transform.localScale = Vector3.one;

#if UNITY_EDITOR
        TextMeshProUGUI text = msgObj.AddComponent<TextMeshProUGUI>();
        text.text = "No files found.\n\nPlace images or PDFs in:\n" +
                    "Assets/TestFiles/\n" +
                    "My Pictures/\n" +
                    "Desktop/\n" +
                    "or My Documents/";
#else
        TextMeshProUGUI text = msgObj.AddComponent<TextMeshProUGUI>();
        text.text = "No files found.\n\nPlace images or PDFs in:\n/sdcard/Download/";
#endif

        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform rect = msgObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 300);

        fileButtons.Add(msgObj);
    }

    void OnFileSelected(string filePath)
    {
        Debug.Log("File selected: " + filePath);
        // Fire the callback FIRST so the file loads while the button is alive.
        onFileSelected?.Invoke(filePath);
        // Defer the panel close by ONE frame so other onClick listeners on
        // the same button (specifically IconButtonController's animation
        // coroutine) get to finish before the GameObject is deactivated.
        // Without this, IconButtonController throws:
        //   "Coroutine couldn't be started because the game object is inactive"
        StartCoroutine(ClosePanelNextFrame());
    }

    System.Collections.IEnumerator ClosePanelNextFrame()
    {
        yield return null;
        if (fileListPanel != null) fileListPanel.SetActive(false);
    }

    public void ClosePanel()
    {
        Debug.Log("Closing file picker panel");
        if (fileListPanel != null)
            fileListPanel.SetActive(false);
    }
}