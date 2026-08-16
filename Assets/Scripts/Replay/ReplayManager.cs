using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime; // ✨ תיקון: הוספת using!

// מנהל השמעת ההקלטות
public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }

    [Header("Avatar Prefabs")]
    public GameObject maleAvatarPrefab;   // ✨ Prefab לגבר
    public GameObject femaleAvatarPrefab; // ✨ Prefab לאישה

    [Header("Settings")]
    public bool allowFreeRoam = true;

    private List<ReplayAvatar> activeReplayAvatars = new List<ReplayAvatar>();
    private List<ReplayObject> activeReplayObjects = new List<ReplayObject>();
    private DynamicObjectReplayer dynamicReplayer;
    private GlobalAudioPlayer globalAudioPlayer;
    private AuditLogReplayer auditLogReplayer; // ✨ חדש!
    private StatsReplayer statsReplayer; // ✨ חדש!
    private PresentationReplayer presentationReplayer;

    private RecordingData currentRecording;
    private bool isPlaying = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadAndPlayRecording(string fileName)
    {
        currentRecording = RecordingManager.Instance.LoadRecording(fileName);

        if (currentRecording == null)
        {
            Debug.LogError("ReplayManager: לא ניתן לטעון את ההקלטה!");
            return;
        }

        PlayRecording(currentRecording);
    }

    public void PlayRecording(RecordingData recording)
    {
        if (isPlaying)
        {
            StopPlayback();
        }

        currentRecording = recording;
        CreateReplayAvatars();
        CreateReplayObjects();
        CreateDynamicReplayer();
        CreateGlobalAudioPlayer();
        CreateAuditLogReplayer();
        CreateStatsReplayer();
        CreatePresentationReplayer(); // ✨ חדש!
        StartPlayback();
    }

    private void CreateReplayAvatars()
    {
        foreach (ReplayAvatar avatar in activeReplayAvatars)
        {
            if (avatar != null)
                Destroy(avatar.gameObject);
        }
        activeReplayAvatars.Clear();

        foreach (PlayerRecordingData playerData in currentRecording.players)
        {
            GameObject avatarObj;

            // בחירת Prefab לפי מגדר
            GameObject selectedPrefab = null;
            if (playerData.avatarGender == "Female" && femaleAvatarPrefab != null)
            {
                selectedPrefab = femaleAvatarPrefab;
                Debug.Log($"ReplayManager: בחרתי Female Prefab עבור {playerData.playerName}");
            }
            else if (maleAvatarPrefab != null)
            {
                selectedPrefab = maleAvatarPrefab;
                Debug.Log($"ReplayManager: בחרתי Male Prefab עבור {playerData.playerName}");
            }

            // צור מ-Prefab
            if (selectedPrefab != null)
            {
                avatarObj = Instantiate(selectedPrefab);

                // ✨ תיקון: כבה את האובייקט מיד כדי למנוע Start()!
                avatarObj.SetActive(false);

                // עכשיו כבה את כל ה-Normcore components
                DisableNormcoreComponents(avatarObj);

                // הפעל בחזרה
                avatarObj.SetActive(true);
            }
            else
            {
                // fallback - אווטר בסיסי
                avatarObj = new GameObject($"ReplayAvatar_{playerData.playerName}");

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(avatarObj.transform);
                head.transform.localPosition = new Vector3(0, 1.7f, 0);
                head.transform.localScale = Vector3.one * 0.2f;

                GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftHand.name = "LeftHand";
                leftHand.transform.SetParent(avatarObj.transform);
                leftHand.transform.localScale = Vector3.one * 0.1f;

                GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightHand.name = "RightHand";
                rightHand.transform.SetParent(avatarObj.transform);
                rightHand.transform.localScale = Vector3.one * 0.1f;
            }

            ReplayAvatar replayAvatar = avatarObj.AddComponent<ReplayAvatar>();
            replayAvatar.Initialize(playerData, null); // ← תיקון: שלח null!

            // Add the replay-specific helper. It locks the head bone every
            // LateUpdate and drives the walking animation from root position
            // deltas, so the recording plays back with proper walk cycles and
            // a still head (matching the live behaviour the user expects).
            if (avatarObj.GetComponent<ReplayAvatarHelper>() == null)
                avatarObj.AddComponent<ReplayAvatarHelper>();

            activeReplayAvatars.Add(replayAvatar);
        }

        Debug.Log($"ReplayManager: נוצרו {activeReplayAvatars.Count} אווטרים לשחזור");
    }

    // ✨ חדש! כיבוי Normcore Components
    private void DisableNormcoreComponents(GameObject avatarObj)
    {
        // כבה RealtimeView
        RealtimeView[] realtimeViews = avatarObj.GetComponentsInChildren<RealtimeView>(true);
        foreach (RealtimeView view in realtimeViews)
        {
            view.enabled = false;
            Debug.Log($"ReplayManager: כיביתי RealtimeView על {view.gameObject.name}");
        }

        // כבה RealtimeAvatar
        RealtimeAvatar[] realtimeAvatars = avatarObj.GetComponentsInChildren<RealtimeAvatar>(true);
        foreach (RealtimeAvatar rtAvatar in realtimeAvatars)
        {
            rtAvatar.enabled = false;
            Debug.Log($"ReplayManager: כיביתי RealtimeAvatar על {rtAvatar.gameObject.name}");
        }

        // כבה RealtimeTransform
        RealtimeTransform[] realtimeTransforms = avatarObj.GetComponentsInChildren<RealtimeTransform>(true);
        foreach (RealtimeTransform rtTransform in realtimeTransforms)
        {
            rtTransform.enabled = false;
            Debug.Log($"ReplayManager: כיביתי RealtimeTransform על {rtTransform.gameObject.name}");
        }

        // כבה AvatarRoleSync
        var roleSyncs = avatarObj.GetComponentsInChildren<AvatarRoleSync>(true);
        foreach (var roleSync in roleSyncs)
        {
            roleSync.enabled = false;
        }

        // כבה AvatarTimeSync
        var timeSyncs = avatarObj.GetComponentsInChildren<AvatarTimeSync>(true);
        foreach (var timeSync in timeSyncs)
        {
            timeSync.enabled = false;
        }

        // Disable live components — VRIKController peeks at RealtimeView at
        // Start which throws "view doesn't have a model yet" once Normcore is
        // disconnected. AvatarMovementSync expects live tracking data too.
        // ReplayAvatarHelper (added below in CreateReplayAvatars) re-implements
        // head lock + walking animation in a replay-safe way.
        var ikControllers = avatarObj.GetComponentsInChildren<VRIKController>(true);
        foreach (var ik in ikControllers)
            ik.enabled = false;

        var movementSyncs = avatarObj.GetComponentsInChildren<AvatarMovementSync>(true);
        foreach (var ms in movementSyncs)
            ms.enabled = false;

        var sphereSyncs = avatarObj.GetComponentsInChildren<HandSphereSync>(true);
        foreach (var hs in sphereSyncs)
            hs.enabled = false;

        Debug.Log($"ReplayManager: כיביתי את כל ה-Normcore components על {avatarObj.name}");
    }

    private void CreateReplayObjects()
    {
        foreach (ReplayObject obj in activeReplayObjects)
        {
            if (obj != null)
                Destroy(obj.gameObject);
        }
        activeReplayObjects.Clear();

        if (currentRecording.objects == null || currentRecording.objects.Count == 0)
        {
            Debug.Log("ReplayManager: אין אובייקטים מוקלטים");
            return;
        }

        foreach (ObjectRecordingData objData in currentRecording.objects)
        {
            GameObject foundObject = FindObjectForReplay(objData);

            if (foundObject != null)
            {
                ReplayObject replayObj = foundObject.GetComponent<ReplayObject>();
                if (replayObj == null)
                {
                    replayObj = foundObject.AddComponent<ReplayObject>();
                }

                replayObj.Initialize(objData);
                activeReplayObjects.Add(replayObj);

                Debug.Log($"ReplayManager: מצאתי {objData.objectID} - יש {objData.frames.Count} פריימים");
            }
            else
            {
                Debug.LogWarning($"ReplayManager: לא מצאתי {objData.objectID}!");
            }
        }

        Debug.Log($"ReplayManager: {activeReplayObjects.Count} אובייקטים לשחזור");
    }


    // ✨ חיפוש מדויק עבור אובייקטים
    GameObject FindObjectForReplay(ObjectRecordingData objData)
    {
        // חיפוש לפי ObjectRecorder.objectID (התאמה מדויקת)
        ObjectRecorder[] allRecorders = FindObjectsOfType<ObjectRecorder>();

        foreach (ObjectRecorder recorder in allRecorders)
        {
            if (recorder.objectID == objData.objectID)
            {
                Debug.Log($"ReplayManager: ✅ התאמה מדויקת: {recorder.objectID}");
                return recorder.gameObject;
            }
        }

        // אם לא מצאנו - חיפוש לפי שם (fallback)
        GameObject found = GameObject.Find(objData.objectID);
        if (found != null)
        {
            Debug.Log($"ReplayManager: ✅ מצאתי לפי שם: {objData.objectID}");
            return found;
        }

        Debug.LogError($"ReplayManager: ❌ לא מצאתי: {objData.objectID}");
        return null;
    }


    private void CreateDynamicReplayer()
    {
        if (dynamicReplayer != null)
        {
            Destroy(dynamicReplayer.gameObject);
        }

        if (currentRecording.dynamicEvents == null || currentRecording.dynamicEvents.Count == 0)
        {
            Debug.Log("ReplayManager: אין אירועים דינמיים");
            return;
        }

        GameObject replayerObj = new GameObject("DynamicObjectReplayer");
        dynamicReplayer = replayerObj.AddComponent<DynamicObjectReplayer>();
        dynamicReplayer.Initialize(currentRecording.dynamicEvents);

        Debug.Log($"ReplayManager: נוצר Dynamic Replayer עם {currentRecording.dynamicEvents.Count} אירועים");
    }

    private void CreateGlobalAudioPlayer()
    {
        if (globalAudioPlayer != null)
        {
            Destroy(globalAudioPlayer.gameObject);
        }

        if (currentRecording.globalAudio == null || currentRecording.globalAudio.Count == 0)
        {
            Debug.Log("ReplayManager: אין אודיו גלובלי");
            return;
        }

        GameObject playerObj = new GameObject("GlobalAudioPlayer");
        globalAudioPlayer = playerObj.AddComponent<GlobalAudioPlayer>();
        globalAudioPlayer.Initialize(currentRecording.globalAudio);

        Debug.Log($"ReplayManager: נוצר Global Audio Player עם {currentRecording.globalAudio.Count} פריימים");
    }

    // ✨ חדש! יצירת AuditLogReplayer
    private void CreateAuditLogReplayer()
    {
        if (auditLogReplayer != null)
        {
            Destroy(auditLogReplayer.gameObject);
        }

        if (currentRecording.auditLog == null || currentRecording.auditLog.Count == 0)
        {
            Debug.Log("ReplayManager: אין Audit Log");
            return;
        }

        GameObject replayerObj = new GameObject("AuditLogReplayer");
        auditLogReplayer = replayerObj.AddComponent<AuditLogReplayer>();
        auditLogReplayer.Initialize(currentRecording.auditLog);

        Debug.Log($"ReplayManager: נוצר Audit Log Replayer עם {currentRecording.auditLog.Count} שורות");
    }

    // ✨ חדש! יצירת StatsReplayer
    private void CreateStatsReplayer()
    {
        if (statsReplayer != null)
        {
            Destroy(statsReplayer.gameObject);
        }

        if (currentRecording.statsSnapshots == null || currentRecording.statsSnapshots.Count == 0)
        {
            Debug.Log("ReplayManager: אין Stats Snapshots");
            return;
        }

        GameObject replayerObj = new GameObject("StatsReplayer");
        statsReplayer = replayerObj.AddComponent<StatsReplayer>();
        statsReplayer.Initialize(currentRecording.statsSnapshots);

        Debug.Log($"ReplayManager: נוצר Stats Replayer עם {currentRecording.statsSnapshots.Count} snapshots");
    }

    private GameObject FindObjectByID(string objectID)
    {
        ObjectRecorder[] allRecorders = FindObjectsOfType<ObjectRecorder>();
        foreach (ObjectRecorder recorder in allRecorders)
        {
            if (recorder.objectID == objectID)
            {
                return recorder.gameObject;
            }
        }

        if (objectID.StartsWith("Document_") && dynamicReplayer != null)
        {
            var spawnedDocs = GameObject.FindObjectsOfType<DocumentItem>();
            foreach (var docItem in spawnedDocs)
            {
                if (objectID.Contains(docItem.fileName))
                {
                    return docItem.gameObject;
                }
            }
        }

        return GameObject.Find(objectID);
    }

    public void StartPlayback()
    {
        isPlaying = true;

        foreach (ReplayAvatar avatar in activeReplayAvatars)
        {
            avatar.StartPlayback();
        }

        foreach (ReplayObject obj in activeReplayObjects)
        {
            obj.StartPlayback();
        }

        if (dynamicReplayer != null)
        {
            dynamicReplayer.StartPlayback();
        }

        if (globalAudioPlayer != null)
        {
            globalAudioPlayer.StartPlayback();
        }

        if (auditLogReplayer != null)
        {
            auditLogReplayer.StartPlayback();
        }

        if (statsReplayer != null)
        {
            statsReplayer.StartPlayback();
        }

        if (presentationReplayer != null) // ✨ חדש!
        {
            presentationReplayer.StartPlayback();
        }

        Debug.Log("ReplayManager: השמעה התחילה");
    }

    public void StopPlayback()
    {
        isPlaying = false;

        foreach (ReplayAvatar avatar in activeReplayAvatars)
        {
            avatar.StopPlayback();
        }

        foreach (ReplayObject obj in activeReplayObjects)
        {
            obj.StopPlayback();
        }

        if (dynamicReplayer != null)
        {
            dynamicReplayer.StopPlayback();
        }

        if (globalAudioPlayer != null)
        {
            globalAudioPlayer.StopPlayback();
        }

        if (auditLogReplayer != null)
        {
            auditLogReplayer.StopPlayback();
        }

        if (statsReplayer != null)
        {
            statsReplayer.StopPlayback();
        }

        if (presentationReplayer != null) // ✨ חדש!
        {
            presentationReplayer.StopPlayback();
        }

        Debug.Log("ReplayManager: השמעה נעצרה");
    }

    public void PausePlayback()
    {
        isPlaying = false;

        foreach (ReplayAvatar avatar in activeReplayAvatars)
        {
            avatar.PausePlayback();
        }

        foreach (ReplayObject obj in activeReplayObjects)
        {
            obj.PausePlayback();
        }
    }

    public void ResumePlayback()
    {
        isPlaying = true;

        foreach (ReplayAvatar avatar in activeReplayAvatars)
        {
            avatar.ResumePlayback();
        }

        foreach (ReplayObject obj in activeReplayObjects)
        {
            obj.ResumePlayback();
        }
    }

    void Update()
    {
        if (isPlaying && AllPlaybacksComplete())
        {
            Debug.Log("ReplayManager: השמעה הסתיימה");
            StopPlayback();
        }
    }

    private bool AllPlaybacksComplete()
    {
        foreach (ReplayAvatar avatar in activeReplayAvatars)
        {
            if (!avatar.IsPlaybackComplete())
                return false;
        }
        return true;
    }
    private void CreatePresentationReplayer()
    {
        if (presentationReplayer != null)
        {
            Destroy(presentationReplayer.gameObject);
        }

        if (currentRecording.presentationFrames == null || currentRecording.presentationFrames.Count == 0)
        {
            Debug.Log("ReplayManager: אין פריימי מצגת");
            return;
        }

        VRPresentationSystem presentation = FindObjectOfType<VRPresentationSystem>();
        if (presentation == null)
        {
            Debug.LogWarning("ReplayManager: לא נמצא VRPresentationSystem!");
            return;
        }

        presentationReplayer = presentation.gameObject.AddComponent<PresentationReplayer>();
        presentationReplayer.Initialize(currentRecording.presentationFrames);

        Debug.Log($"ReplayManager: נוצר Presentation Replayer עם {currentRecording.presentationFrames.Count} פריימים");
    }


    public bool IsPlaying => isPlaying;
    public RecordingData CurrentRecording => currentRecording;
}