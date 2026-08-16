using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class VoiceCommand
{
    [Tooltip("מילות המפתח — מופרדות בפסיק. לדוגמא: open menu,תפתח תפריט")]
    public string keywords;
    public UnityEvent action;
}

[System.Serializable]
public class VoiceTeleportCommand
{
    [Tooltip("מילות המפתח — מופרדות בפסיק. לדוגמא: surgery,חדר ניתוח,ניתוח")]
    public string keywords;

    [Tooltip("גרור לכאן את אותו Transform שיש לך ב-DoorTeleportSystem")]
    public Transform targetPoint;
}

public class VoiceCommandManager : MonoBehaviour
{
    public static VoiceCommandManager Instance;

    [Header("פקודות קוליות רגילות")]
    public List<VoiceCommand> commands = new List<VoiceCommand>();

    [Header("פקודות טלפורט")]
    public List<VoiceTeleportCommand> teleportCommands = new List<VoiceTeleportCommand>();

    [Header("Settings")]
    public float commandCooldown = 2f;

    // מצביע ל-DoorTeleportSystem — ימצא אוטומטית
    private DoorTeleportSystem teleportSystem;
    private Dictionary<string, float> lastCommandTime = new Dictionary<string, float>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        teleportSystem = FindFirstObjectByType<DoorTeleportSystem>();
        if (teleportSystem == null)
            Debug.LogWarning("⚠️ VoiceCommandManager: לא נמצא DoorTeleportSystem בסצנה");
    }

    public void ProcessText(string englishText)
    {
        if (string.IsNullOrEmpty(englishText)) return;
        string lower = englishText.ToLower().Trim();

        // בדיקת פקודות רגילות
        foreach (var command in commands)
        {
            if (string.IsNullOrEmpty(command.keywords)) continue;

            foreach (string keyword in command.keywords.Split(','))
            {
                string clean = keyword.Trim().ToLower();
                if (string.IsNullOrEmpty(clean)) continue;

                if (lower.Contains(clean))
                {
                    if (IsOnCooldown(command.keywords)) break;

                    SetCooldown(command.keywords);
                    command.action?.Invoke();
                    Debug.Log($"🎤 פקודה: '{clean}'");
                    break;
                }
            }
        }

        // בדיקת פקודות טלפורט
        foreach (var teleport in teleportCommands)
        {
            if (string.IsNullOrEmpty(teleport.keywords)) continue;
            if (teleport.targetPoint == null) continue;

            foreach (string keyword in teleport.keywords.Split(','))
            {
                string clean = keyword.Trim().ToLower();
                if (string.IsNullOrEmpty(clean)) continue;

                if (lower.Contains(clean))
                {
                    if (IsOnCooldown(teleport.keywords)) break;

                    SetCooldown(teleport.keywords);
                    teleportSystem?.TeleportTo(teleport.targetPoint);
                    Debug.Log($"🎤 טלפורט: '{clean}' → {teleport.targetPoint.name}");
                    break;
                }
            }
        }
    }

    private bool IsOnCooldown(string key)
    {
        return lastCommandTime.ContainsKey(key) &&
               Time.time - lastCommandTime[key] < commandCooldown;
    }

    private void SetCooldown(string key)
    {
        lastCommandTime[key] = Time.time;
    }
}