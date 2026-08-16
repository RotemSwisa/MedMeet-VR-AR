using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

// מקליט את כל הנתונים של שחקן אחד
public class PlayerRecorder : MonoBehaviour
{
    [Header("References")]
    public Transform bodyTransform;
    public Transform headTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform nameCanvas;

    private RealtimeAvatar realtimeAvatar;
    private AudioRecorder audioRecorder;

    private List<TransformFrame> bodyFrames = new List<TransformFrame>();
    private List<TransformFrame> headFrames = new List<TransformFrame>();
    private List<TransformFrame> leftHandFrames = new List<TransformFrame>();
    private List<TransformFrame> rightHandFrames = new List<TransformFrame>();
    private List<TransformFrame> nameCanvasFrames = new List<TransformFrame>();

    private bool isRecording = false;
    private float recordingStartTime;
    private float lastFrameTime;
    private float frameInterval;

    public string PlayerName { get; private set; }
    public int ClientID { get; private set; }
    public string AvatarGender { get; private set; }

    void Awake()
    {
        realtimeAvatar = GetComponent<RealtimeAvatar>();
        audioRecorder = gameObject.AddComponent<AudioRecorder>();
        frameInterval = 1f / 30f;

        // Set bodyTransform to ourselves NOW so RecordFrame can never get a
        // null reference. AutoFindTransforms runs 5 frames into Start() and
        // until then bodyTransform was null — if StartRecording fired in that
        // window it threw NullReferenceException on the first LateUpdate.
        if (bodyTransform == null) bodyTransform = transform;

        AvatarGender = PlayerPrefs.GetString("SelectedGender", "Male");
        Debug.Log($"PlayerRecorder: Avatar Gender = {AvatarGender}");
    }

    void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    private System.Collections.IEnumerator InitializeAfterDelay()
    {
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;

        AvatarNameTag nameTag = GetComponent<AvatarNameTag>();
        if (nameTag != null)
        {
            PlayerName = nameTag.GetPlayerName();
        }

        if (string.IsNullOrEmpty(PlayerName) || PlayerName == "Unknown")
        {
            if (gameObject.name.StartsWith("Player_"))
            {
                PlayerName = gameObject.name.Replace("Player_", "");
            }
            else
            {
                PlayerName = PlayerPrefs.GetString("PlayerName", "Unknown Player");
            }
        }

        if (realtimeAvatar != null && realtimeAvatar.isOwnedLocallySelf)
        {
            ClientID = realtimeAvatar.ownerIDSelf;
        }
        else if (realtimeAvatar != null && realtimeAvatar.isOwnedRemotelySelf)
        {
            ClientID = realtimeAvatar.ownerIDSelf;
        }

        AutoFindTransforms();
    }

    private void AutoFindTransforms()
    {
        if (bodyTransform == null)
            bodyTransform = transform;

        if (headTransform == null)
            headTransform = transform.Find("Head");

        if (leftHandTransform == null)
            leftHandTransform = transform.Find("LeftHand");

        if (rightHandTransform == null)
            rightHandTransform = transform.Find("RightHand");

        if (nameCanvas == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                nameCanvas = canvas.transform;
                Debug.Log($"PlayerRecorder: נמצא Canvas על {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"PlayerRecorder: לא נמצא Canvas על {gameObject.name}!");
            }
        }

        if (headTransform == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.name.Contains("Head") || child.name.Contains("head"))
                    headTransform = child;
                else if (child.name.Contains("LeftHand") || child.name.Contains("Left"))
                    leftHandTransform = child;
                else if (child.name.Contains("RightHand") || child.name.Contains("Right"))
                    rightHandTransform = child;
                else if (child.name == "Canvas" && nameCanvas == null)
                {
                    nameCanvas = child;
                    Debug.Log($"PlayerRecorder: נמצא Canvas בחיפוש: {child.name}");
                }
            }
        }

        Debug.Log($"PlayerRecorder [{PlayerName}]: Canvas = {(nameCanvas != null ? "נמצא" : "לא נמצא")}");
    }

    public void StartRecording()
    {
        bodyFrames.Clear();
        headFrames.Clear();
        leftHandFrames.Clear();
        rightHandFrames.Clear();
        nameCanvasFrames.Clear();

        isRecording = true;
        recordingStartTime = Time.time;
        lastFrameTime = 0f;

        audioRecorder?.StartRecording();

        Debug.Log($"PlayerRecorder: התחלת הקלטה עבור {PlayerName} (ID: {ClientID}, Gender: {AvatarGender})");
    }

    public void StopRecording()
    {
        isRecording = false;
        audioRecorder?.StopRecording();

        Debug.Log($"PlayerRecorder: סיום הקלטה. Body: {bodyFrames.Count}, Canvas: {nameCanvasFrames.Count} פריימים");
    }

    // LateUpdate במקום Update — כדי שההקלטה תתבצע לאחר שכל ה-IK,
    // RealtimeAvatarInputs ו-VRBodySync סיימו לדחוף את המיקומים הסופיים.
    // Update רץ לפני LateUpdate ולכן היה מקליט מיקומי אנימציה ישנים.
    void LateUpdate()
    {
        if (!isRecording) return;

        float currentTime = Time.time - recordingStartTime;

        if (currentTime - lastFrameTime >= frameInterval)
        {
            RecordFrame(currentTime);
            lastFrameTime = currentTime;
        }
    }

    private void RecordFrame(float time)
    {
        // Defensive: bodyTransform should already be 'transform' from Awake,
        // but if it got nulled out by someone we fall back to our own transform
        // instead of throwing.
        if (bodyTransform == null) bodyTransform = transform;
        if (bodyTransform == null) return;   // gameObject destroyed mid-record

        bodyFrames.Add(new TransformFrame
        {
            time = time,
            position = bodyTransform.position,
            rotation = bodyTransform.rotation
        });

        if (headTransform != null)
        {
            headFrames.Add(new TransformFrame
            {
                time = time,
                position = headTransform.position,
                rotation = headTransform.rotation
            });
        }

        if (leftHandTransform != null)
        {
            leftHandFrames.Add(new TransformFrame
            {
                time = time,
                position = leftHandTransform.position,
                rotation = leftHandTransform.rotation
            });
        }

        if (rightHandTransform != null)
        {
            rightHandFrames.Add(new TransformFrame
            {
                time = time,
                position = rightHandTransform.position,
                rotation = rightHandTransform.rotation
            });
        }

        if (nameCanvas != null)
        {
            nameCanvasFrames.Add(new TransformFrame
            {
                time = time,
                position = nameCanvas.position,
                rotation = nameCanvas.rotation
            });
        }
    }

    public PlayerRecordingData GetRecordingData()
    {
        return new PlayerRecordingData
        {
            playerName = PlayerName,
            clientID = ClientID,
            avatarGender = AvatarGender,
            bodyFrames = new List<TransformFrame>(bodyFrames),
            headFrames = new List<TransformFrame>(headFrames),
            leftHandFrames = new List<TransformFrame>(leftHandFrames),
            rightHandFrames = new List<TransformFrame>(rightHandFrames),
            nameCanvasFrames = new List<TransformFrame>(nameCanvasFrames),
            audioFrames = audioRecorder?.GetRecordedAudio() ?? new List<AudioFrame>()
        };
    }
}