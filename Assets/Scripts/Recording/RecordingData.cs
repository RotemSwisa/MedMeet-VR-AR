using System;
using System.Collections.Generic;
using UnityEngine;

// מבנה נתוני ההקלטה - זה מה שנשמר לקובץ
[System.Serializable]
public class RecordingData
{
    public string recordingName;
    public float duration;
    public int frameRate = 30;
    public List<PlayerRecordingData> players = new List<PlayerRecordingData>();
    public List<ObjectRecordingData> objects = new List<ObjectRecordingData>();
    public List<DynamicObjectEvent> dynamicEvents = new List<DynamicObjectEvent>();
    public List<AudioFrame> globalAudio = new List<AudioFrame>();
    public List<AuditLogFrame> auditLog = new List<AuditLogFrame>();
    public List<StatsSnapshot> statsSnapshots = new List<StatsSnapshot>();
    public List<PresentationFrame> presentationFrames = new List<PresentationFrame>(); // ✨ חדש!
}

[System.Serializable]
public class PlayerRecordingData
{
    public string playerName;
    public int clientID;
    public string avatarGender = "Male"; // "Male" או "Female"
    public List<TransformFrame> bodyFrames = new List<TransformFrame>();
    public List<TransformFrame> headFrames = new List<TransformFrame>();
    public List<TransformFrame> leftHandFrames = new List<TransformFrame>();
    public List<TransformFrame> rightHandFrames = new List<TransformFrame>();
    public List<TransformFrame> nameCanvasFrames = new List<TransformFrame>();
    public List<AudioFrame> audioFrames = new List<AudioFrame>();
}

[System.Serializable]
public class TransformFrame
{
    public float time;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class AudioFrame
{
    public float time;
    public float[] audioData;
    public int frequency;
    public int channels;
}

[System.Serializable]
public class ObjectRecordingData
{
    public string objectID;
    public string objectType;
    public string prefabPath;
    public List<ObjectFrame> frames = new List<ObjectFrame>();
}

[System.Serializable]
public class ObjectFrame
{
    public float time;
    public string objectID;
    public string objectType;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public bool isVisible;
    public string textContent;
}

[System.Serializable]
public class AuditLogFrame
{
    public float time;
    public string logLine;
}

[System.Serializable]
public class StatsSnapshot
{
    public float time;
    public List<PlayerStatsData> playerStats = new List<PlayerStatsData>();
}

[System.Serializable]
public class PlayerStatsData
{
    public string playerName;
    public string role;
    public double joinTime;
    public float totalSpeechTime;
    public bool isOnline;
}

// ✨ מעודכן! תמיכה בציורים
public enum DynamicEventType
{
    Spawn,
    Destroy,
    DocumentSpawn,
    DrawingDotSpawn // ✨ חדש!
}

[System.Serializable]
public class DynamicObjectEvent
{
    public float time;
    public DynamicEventType eventType;
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;

    // תמונות/PDF
    public string textureBase64;
    public string fileName;
    public bool isPDF;
    public int pageCount;

    // ✨ חדש! נקודות ציור
    public int strokeID;
    public int pointIndex;
    public Color dotColor;
    public float dotSize;
}