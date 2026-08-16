using UnityEngine;
using Normal.Realtime;
using TMPro;
using System.Collections;

public class AvatarNameTag : RealtimeComponent<AvatarNameModel>
{
    [SerializeField] private TMP_Text nameDisplay;

    private void Start()
    {
        // מחקנו מכאן את הלוגיקה - היא עברה למטה כדי למנוע באגים
    }

    protected override void OnRealtimeModelReplaced(AvatarNameModel previousModel, AvatarNameModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.playerNameDidChange -= OnNameChanged;
        }

        if (currentModel != null)
        {
            if (currentModel.isFreshModel)
            {
                // המודל חדש לגמרי
                OnNameChanged(currentModel, currentModel.playerName);
            }

            // *** התיקון הקריטי: הגדרת השם כאן ולא ב-Start ***
            // בודקים אם המודל הזה שייך לי (המחשב המקומי)
            if (realtimeView.isOwnedLocallySelf)
            {
                string myName = PlayerPrefs.GetString("PlayerName", "Guest");
                // מעדכנים את המודל רק אם השם שונה ממה שיש בו כבר
                if (currentModel.playerName != myName)
                {
                    currentModel.playerName = myName;
                }
            }

            // הרשמה לשינויים
            currentModel.playerNameDidChange += OnNameChanged;

            // עדכון ראשוני של התצוגה (חשוב למי שמצטרף מאוחר ורואה שחקנים קיימים)
            OnNameChanged(currentModel, currentModel.playerName);
        }
    }

    private void OnNameChanged(AvatarNameModel model, string name)
    {
        // הגנה מפני Null
        if (string.IsNullOrEmpty(name)) return;

        // 1. שינוי שם האובייקט
        gameObject.name = $"Player_{name}";

        // 2. עדכון הטקסט
        if (nameDisplay != null)
            nameDisplay.text = name;

        // 3. עדכון ה-Audit
        if (MeetingAuditor.Instance != null)
        {
            MeetingAuditor.Instance.RegisterPlayerName(realtimeView.ownerIDSelf, name);
        }
        else
        {
            StartCoroutine(WaitForAuditorAndRegister(realtimeView.ownerIDSelf, name));
        }
    }

    private IEnumerator WaitForAuditorAndRegister(int id, string name)
    {
        while (MeetingAuditor.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        MeetingAuditor.Instance.RegisterPlayerName(id, name);
    }

    public string GetPlayerName()
    {
        // הגנה נוספת במקרה שהמודל ריק
        if (model == null) return "Loading...";
        return string.IsNullOrEmpty(model.playerName) ? "Unknown" : model.playerName;
    }
}