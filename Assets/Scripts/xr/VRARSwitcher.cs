using UnityEngine;
using UnityEngine.UI;
using Normal.Realtime;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRARSwitcher : MonoBehaviour
{
    [Header("Systems")]
    public GameObject xrOriginGameObject;
    public GameObject ovrCameraRigGameObject;
    public GameObject xrDeviceSimulator;

    [Header("Cameras (VR)")]
    public Transform xrMainCameraTransform;
    public Camera xrMainCamera;

    [Header("Cameras (AR - Oculus)")]
    public Camera ovrCenterEyeCamera;

    [Header("Controllers (XR Toolkit)")]
    public Transform xrLeftController;
    public Transform xrRightController;

    [Header("OVR Controller Anchors (לtracking במצב AR)")]
    public Transform ovrLeftControllerAnchor;
    public Transform ovrRightControllerAnchor;

    [Header("OVR Hand Models (למחיקה/השבתה)")]
    public GameObject[] ovrHandModelsToDisable;

    [Header("UI & Avatar")]
    public Canvas mainCanvas;
    public CanvasFollowPlayer followScript;
    public RealtimeAvatarManager avatarManager;

    [Header("AR Settings")]
    public GameObject[] environmentObjects;

    [Header("Sign Language (AR only)")]
    public SignLanguageToggleButton signLanguageButton;

    [Header("VR Reset Settings")]
    public Vector3 vrResetPosition = Vector3.zero;
    public float vrResetRotationY = 0f;

    private OVRPassthroughLayer passthroughLayer;
    private SimpleOVRMovement movementScript;
    private bool isAR = false;

    public bool IsARMode => isAR;
    private CameraClearFlags originalClearFlags;
    private Color originalBackColor;

    // שמירת ה-Parent והמיקום המקורי של הידיים
    private Transform leftControllerOriginalParent;
    private Transform rightControllerOriginalParent;
    private Vector3 leftControllerOriginalLocalPos;
    private Quaternion leftControllerOriginalLocalRot;
    private Vector3 rightControllerOriginalLocalPos;
    private Quaternion rightControllerOriginalLocalRot;

    void Start()
    {
        if (followScript == null && mainCanvas != null)
            followScript = mainCanvas.GetComponent<CanvasFollowPlayer>();

        if (ovrCameraRigGameObject != null)
            movementScript = ovrCameraRigGameObject.GetComponent<SimpleOVRMovement>();

        passthroughLayer = FindFirstObjectByType<OVRPassthroughLayer>();

        if (passthroughLayer == null)
        {
            Debug.LogError("❌ OVRPassthroughLayer NOT FOUND!");
        }
        else
        {
            passthroughLayer.enabled = false;
        }

        if (avatarManager == null)
            avatarManager = FindFirstObjectByType<RealtimeAvatarManager>();

        if (ovrCenterEyeCamera != null)
        {
            originalClearFlags = ovrCenterEyeCamera.clearFlags;
            originalBackColor = ovrCenterEyeCamera.backgroundColor;
        }

        // 🔧 שמור את המיקום המקומי המקורי של הידיים בזמן Start
        if (xrLeftController != null)
        {
            leftControllerOriginalParent = xrLeftController.parent;
            leftControllerOriginalLocalPos = xrLeftController.localPosition;
            leftControllerOriginalLocalRot = xrLeftController.localRotation;
            Debug.Log($"💾 Saved Left Controller: localPos={leftControllerOriginalLocalPos}, localRot={leftControllerOriginalLocalRot.eulerAngles}");
        }

        if (xrRightController != null)
        {
            rightControllerOriginalParent = xrRightController.parent;
            rightControllerOriginalLocalPos = xrRightController.localPosition;
            rightControllerOriginalLocalRot = xrRightController.localRotation;
            Debug.Log($"💾 Saved Right Controller: localPos={rightControllerOriginalLocalPos}, localRot={rightControllerOriginalLocalRot.eulerAngles}");
        }

        // שמור את מיקום ההתחלה של XR Origin
        if (xrOriginGameObject != null)
        {
            vrResetPosition = xrOriginGameObject.transform.position;
            vrResetRotationY = xrOriginGameObject.transform.rotation.eulerAngles.y;
        }

        // כבה את מודלי הידיים של OVR
        foreach (var handModel in ovrHandModelsToDisable)
        {
            if (handModel != null)
            {
                handModel.SetActive(false);
            }
        }

        isAR = false;
        if (xrDeviceSimulator != null) xrDeviceSimulator.SetActive(true);
        if (xrOriginGameObject != null) xrOriginGameObject.SetActive(true);
        if (ovrCameraRigGameObject != null) ovrCameraRigGameObject.SetActive(false);
        if (xrMainCamera != null) xrMainCamera.enabled = true;

        // Hide every XR controller mesh model — only the teal laser ray (LineRenderer) remains.
        // Runs once at start; renderers stay hidden for the lifetime of the session.
        HideAllControllerMeshes();

        // Wire the initial VR references on the local avatar once Normcore spawns it
        StartCoroutine(WireLocalAvatarOnSpawn());
    }

    // Waits for Normcore to spawn the local avatar and then wires the initial tracking references
    private System.Collections.IEnumerator WireLocalAvatarOnSpawn()
    {
        // Wait until avatarManager has a localAvatar (Normcore spawns it after joining the room)
        float timeout = 30f;
        while ((avatarManager == null || avatarManager.localAvatar == null) && timeout > 0)
        {
            yield return new WaitForSeconds(0.2f);
            timeout -= 0.2f;
            if (avatarManager == null)
                avatarManager = FindFirstObjectByType<RealtimeAvatarManager>();
        }

        if (avatarManager == null || avatarManager.localAvatar == null)
        {
            Debug.LogWarning("[VRARSwitcher] Local avatar not found after 30s — IK tracking not wired");
            yield break;
        }

        // Default mode is VR — wire the XR controllers
        UpdateLocalAvatar(xrMainCameraTransform, xrLeftController, xrRightController);
        Debug.Log("[VRARSwitcher] ✅ Initial VR tracking wired to local avatar");
    }

    public void ToggleMode()
    {
        isAR = !isAR;
        if (isAR) EnableAR();
        else EnableVR();
    }

    void EnableAR()
    {
        Debug.Log("=== 🎥 Switching to AR ===");

        // 🔧 תיקון: העתק את מיקום ה-XR Origin עצמו (לא המצלמה!)
        if (xrOriginGameObject != null && ovrCameraRigGameObject != null)
        {
            ovrCameraRigGameObject.transform.position = xrOriginGameObject.transform.position;
            ovrCameraRigGameObject.transform.rotation = xrOriginGameObject.transform.rotation;
        }

        if (xrMainCamera != null)
        {
            xrMainCamera.enabled = false;
        }

        if (ovrCameraRigGameObject != null) ovrCameraRigGameObject.SetActive(true);

        if (ovrCenterEyeCamera != null)
        {
            ovrCenterEyeCamera.enabled = true;
            ovrCenterEyeCamera.clearFlags = CameraClearFlags.SolidColor;
            ovrCenterEyeCamera.backgroundColor = new Color(0, 0, 0, 0);

            // Mirror the near-clip that VRIKController set on Camera.main so the
            // local player can't see their own head in AR mode either.
            if (xrMainCamera != null && xrMainCamera.nearClipPlane > ovrCenterEyeCamera.nearClipPlane)
                ovrCenterEyeCamera.nearClipPlane = xrMainCamera.nearClipPlane;
        }

        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = true;
            passthroughLayer.textureOpacity = 1f;
        }

        // On a real device the OVR anchors provide actual controller tracking, so we
        // re-parent the XR controllers to them. On PC (editor / simulator) the OVR
        // anchors never move, so we leave the controllers under the XR origin and keep
        // the simulator alive so keyboard/mouse still drives them.
        if (!Application.isEditor)
        {
            if (ovrLeftControllerAnchor != null && xrLeftController != null)
            {
                xrLeftController.SetParent(ovrLeftControllerAnchor, false);
                xrLeftController.localPosition = Vector3.zero;
                xrLeftController.localRotation = Quaternion.identity;
            }

            if (ovrRightControllerAnchor != null && xrRightController != null)
            {
                xrRightController.SetParent(ovrRightControllerAnchor, false);
                xrRightController.localPosition = Vector3.zero;
                xrRightController.localRotation = Quaternion.identity;
            }

            if (xrDeviceSimulator != null) xrDeviceSimulator.SetActive(false);
        }
        // On a real Quest, passthrough replaces the 3-D room so we hide it.
        // In the editor there is no passthrough image — keep the room visible so
        // you can see SOMETHING when testing AR mode on PC.
        if (!Application.isEditor)
            foreach (var obj in environmentObjects) if (obj != null) obj.SetActive(false);

        // On PC the OVR runtime isn't present so SimpleOVRMovement would fight the
        // XR Device Simulator. Keep it disabled and let the simulator drive movement.
        if (movementScript != null) movementScript.enabled = !Application.isEditor;

        // הצג כפתור שפת הסימנים רק ב-AR
        if (signLanguageButton != null) signLanguageButton.SetVisible(true);

        // כבה HandSphereSync — ב-AR הקונטרולרים עוקבים אחרי OVR Anchors,
        // HandSphereSync יגרום לכפילות ידיים ויקלקל את הלייזרים
        SetHandSphereSyncEnabled(false);

        // On PC: track the XR camera so the simulator drives both the avatar and the
        // controllers (which live under XR Origin) in sync.
        // On Quest: track the OVR center-eye camera driven by real headset position.
        Transform arHeadCamera = Application.isEditor ? xrMainCameraTransform : ovrCenterEyeCamera.transform;

        // 🔧 תיקון Canvas: הוסף את ה-OVR Rig reference
        if (followScript != null && ovrCenterEyeCamera != null)
        {
            followScript.SetTargetCamera(arHeadCamera);
            followScript.SetOVRRig(Application.isEditor ? null : ovrCameraRigGameObject.transform);
        }
        if (mainCanvas != null && ovrCenterEyeCamera != null)
            mainCanvas.worldCamera = ovrCenterEyeCamera;

        UpdateLocalAvatar(arHeadCamera, xrLeftController, xrRightController);

        // גוף האוואטר יעקב אחרי המצלמה הנכונה בהתאם לפלטפורמה
        UpdateBodySyncCamera(arHeadCamera);
    }

    void EnableVR()
    {
        Debug.Log("=== 🥽 Switching to VR ===");

        // Restore XR controller parents only on device (we only re-parented on device)
        if (!Application.isEditor)
        {
            // Use worldPositionStays = true so XR Toolkit can resume tracking from
            // the current world pose without a one-frame jump to the floor.
            if (xrLeftController != null && leftControllerOriginalParent != null)
            {
                xrLeftController.SetParent(leftControllerOriginalParent, true);
                Debug.Log($"✅ Left Controller re-parented: worldPos={xrLeftController.position}");
            }

            if (xrRightController != null && rightControllerOriginalParent != null)
            {
                xrRightController.SetParent(rightControllerOriginalParent, true);
                Debug.Log($"✅ Right Controller re-parented: worldPos={xrRightController.position}");
            }
        }

        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = false;
        }

        if (ovrCenterEyeCamera != null)
        {
            ovrCenterEyeCamera.enabled = false;
            ovrCenterEyeCamera.clearFlags = originalClearFlags;
            ovrCenterEyeCamera.backgroundColor = originalBackColor;
        }

        if (ovrCameraRigGameObject != null) ovrCameraRigGameObject.SetActive(false);
        if (movementScript != null) movementScript.enabled = false;

        // 🔧 אפס את XR Origin למיקום ההתחלה
        if (xrOriginGameObject != null)
        {
            xrOriginGameObject.transform.position = vrResetPosition;
            xrOriginGameObject.transform.rotation = Quaternion.Euler(0, vrResetRotationY, 0);
        }

        if (xrMainCamera != null)
        {
            xrMainCamera.enabled = true;
        }

        if (xrDeviceSimulator != null) xrDeviceSimulator.SetActive(true);
        foreach (var obj in environmentObjects) if (obj != null) obj.SetActive(true);

        // הסתר כפתור שפת הסימנים ב-VR
        if (signLanguageButton != null) signLanguageButton.SetVisible(false);

        // הפעל מחדש HandSphereSync בחזרה למצב VR
        SetHandSphereSyncEnabled(true);

        // 🔧 תיקון Canvas: כבה את ה-OVR Rig reference
        if (followScript != null && xrMainCameraTransform != null)
        {
            followScript.SetTargetCamera(xrMainCameraTransform);
            followScript.SetOVRRig(null); // ← חדש!
        }
        if (mainCanvas != null && xrMainCamera != null)
            mainCanvas.worldCamera = xrMainCamera;

        UpdateLocalAvatar(xrMainCameraTransform, xrLeftController, xrRightController);

        // גוף האוואטר חוזר לעקוב אחרי ה-XR camera
        UpdateBodySyncCamera(xrMainCameraTransform);
    }

    // On PC the rendering camera is the OVR center-eye camera, which lives on the
    // OVR Camera Rig. In AR mode the XR Device Simulator moves the XR Origin, but the
    // OVR rig stays put — so the view freezes. Mirror XR Origin → OVR rig every frame
    // to keep the camera in sync with movement.
    void LateUpdate()
    {
        if (isAR && Application.isEditor && xrOriginGameObject != null && ovrCameraRigGameObject != null)
        {
            ovrCameraRigGameObject.transform.position = xrOriginGameObject.transform.position;
            ovrCameraRigGameObject.transform.rotation = xrOriginGameObject.transform.rotation;
        }
    }

    /// <summary>
    /// Disables every MeshRenderer and SkinnedMeshRenderer that lives under (or as a
    /// sibling of) each XRRayInteractor in the scene.  LineRenderer components (the
    /// teal laser ray) are NOT MeshRenderers and are therefore left untouched.
    /// Call once at Start — the renderers stay hidden for the lifetime of the session.
    /// </summary>
    void HideAllControllerMeshes()
    {
        var interactors = FindObjectsOfType<XRRayInteractor>(true);
        int count = 0;

        foreach (var ri in interactors)
        {
            // Search from the interactor's parent so we also catch controller-model
            // GameObjects that are siblings of the interactor (common in XR IT 3.x).
            Transform searchRoot = ri.transform.parent != null ? ri.transform.parent : ri.transform;

            foreach (var mr in searchRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                mr.enabled = false;
                count++;
            }
            foreach (var smr in searchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.enabled = false;
                count++;
            }
        }

        Debug.Log($"[VRARSwitcher] HideAllControllerMeshes: disabled {count} renderer(s) across {interactors.Length} interactor(s). Only laser rays remain.");
    }

    void SetHandSphereSyncEnabled(bool enabled)
    {
        if (avatarManager != null && avatarManager.localAvatar != null)
        {
            var hss = avatarManager.localAvatar.GetComponentInChildren<HandSphereSync>(true);
            if (hss != null) hss.enabled = enabled;
        }
    }

    // מחליף את ה-headCamera ב-VRBodySync כדי שהגוף יעקב אחרי המצלמה הנכונה בכל מצב
    void UpdateBodySyncCamera(Transform cameraTransform)
    {
        if (avatarManager == null || avatarManager.localAvatar == null) return;

        var bodySync = avatarManager.localAvatar.GetComponentInChildren<VRBodySync>(true);
        if (bodySync != null)
        {
            bodySync.headCamera = cameraTransform;
            Debug.Log($"[VRARSwitcher] VRBodySync.headCamera → {cameraTransform?.name ?? "null"}");
        }
        else
        {
            Debug.LogWarning("[VRARSwitcher] VRBodySync not found on local avatar!");
        }
    }

    void UpdateLocalAvatar(Transform head, Transform leftHand, Transform rightHand)
    {
        if (avatarManager != null && avatarManager.localAvatar != null)
        {
            var avatarInputs = avatarManager.localAvatar.GetComponent<RealtimeAvatarInputs>();
            if (avatarInputs != null)
            {
                avatarInputs.head = head;
                avatarInputs.leftHand = leftHand;
                avatarInputs.rightHand = rightHand;
            }
        }
    }
}
