using UnityEngine;

public class ObjectToggler : MonoBehaviour
{
    // הפונקציה הזו מיועדת לשימוש ישיר מתוך ה-OnClick ב-Inspector
    public void ToggleVisibility(GameObject targetToToggle)
    {
        if (targetToToggle != null)
        {
            // קובע את המצב להפך ממה שהוא עכשיו
            targetToToggle.SetActive(!targetToToggle.activeSelf);

            // ✨ חדש! הקלט את השינוי מיד!
            ObjectRecorder recorder = targetToToggle.GetComponent<ObjectRecorder>();
            if (recorder != null)
            {
                recorder.RecordVisibilityChange();
                Debug.Log($"ObjectToggler: הקלטתי שינוי Visibility של {targetToToggle.name}");
            }
        }
    }
}