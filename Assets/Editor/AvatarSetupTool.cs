using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click setup tool for VR avatar body sync.
/// Run via: MedMeet Tools → Setup Avatar IK & Walk Animation
/// </summary>
public class AvatarSetupTool
{
    private static readonly string[] AvatarPrefabPaths = new[]
    {
        "Assets/Normal/Examples/VR Player/Resources/DoctorPlayer.prefab",
        "Assets/Normal/Examples/VR Player/Resources/Femaledoctor.prefab",
        "Assets/Normal/Examples/VR Player/Resources/Male_avatar.prefab",
    };

    [MenuItem("MedMeet Tools/Setup Avatar IK & Walk Animation")]
    public static void SetupAllAvatars()
    {
        int successCount = 0;

        foreach (string path in AvatarPrefabPaths)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[AvatarSetupTool] Prefab not found: {path}");
                continue;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                bool changed = SetupAvatarHierarchy(root, path);
                if (changed) successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Avatar Setup Complete",
            $"Successfully configured {successCount}/{AvatarPrefabPaths.Length} avatar prefabs.\n\n" +
            "What was done:\n" +
            "• Added VRIKController (hand IK sync)\n" +
            "• Added AvatarMovementSync (walk animation)\n" +
            "• Added HandSphereSync (controller sphere + laser color)\n" +
            "• Disabled VRHandTracker & AutoAssignRigTargets\n\n" +
            "NOTE: HandSphereSync auto-finds 'LeftHandModel' and 'RightHandModel' at runtime.\n\n" +
            "Press Play in Unity and test movement!",
            "OK");
    }

    private static bool SetupAvatarHierarchy(GameObject root, string prefabPath)
    {
        // --- Add HandSphereSync to avatar root (sphere refs auto-found at runtime) ---
        HandSphereSync hss = root.GetComponent<HandSphereSync>();
        if (hss == null)
        {
            hss = root.AddComponent<HandSphereSync>();
            Debug.Log($"[AvatarSetupTool] + Added HandSphereSync to root '{root.name}'");
        }
        hss.hideSpheres = false; // keep spheres visible by default

        // --- Disable conflicting scripts ---
        foreach (var ht in root.GetComponentsInChildren<VRHandTracker>(true))
        {
            ht.enabled = false;
            Debug.Log($"[AvatarSetupTool] Disabled VRHandTracker on '{ht.gameObject.name}'");
        }
        foreach (var aar in root.GetComponentsInChildren<AutoAssignRigTargets>(true))
        {
            aar.enabled = false;
            Debug.Log($"[AvatarSetupTool] Disabled AutoAssignRigTargets on '{aar.gameObject.name}'");
        }

        // Find RealtimeAvatarInputs (it's on the Normcore root, needed by VRIKController)
        RealtimeAvatarInputs avatarInputs = root.GetComponentInChildren<RealtimeAvatarInputs>(true);

        bool anySuccess = false;

        // Find ALL Animators in the hierarchy — the avatar may use one or two
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);

        foreach (Animator anim in animators)
        {
            GameObject go = anim.gameObject;

            // We want the Animator that controls a full humanoid body.
            // Key signal: same GO has VRBodySync (body avatar), OR it's the only Humanoid Animator.
            bool hasVRBodySync = go.GetComponent<VRBodySync>() != null;
            bool isHumanoid = anim.avatar != null && anim.avatar.isHuman;

            if (!isHumanoid)
            {
                Debug.LogWarning($"[AvatarSetupTool] Skipping non-Humanoid Animator on '{go.name}'");
                continue;
            }

            Debug.Log($"[AvatarSetupTool] Setting up Animator on '{go.name}' " +
                      $"(VRBodySync: {hasVRBodySync}, Humanoid: {isHumanoid})");

            // --- Add VRIKController ---
            VRIKController ikCtrl = go.GetComponent<VRIKController>();
            if (ikCtrl == null)
            {
                ikCtrl = go.AddComponent<VRIKController>();
                Debug.Log($"[AvatarSetupTool]   + Added VRIKController");
            }

            if (avatarInputs != null)
            {
                ikCtrl.avatarInputs = avatarInputs;
                Debug.Log($"[AvatarSetupTool]   VRIKController.avatarInputs wired");
            }
            else
            {
                Debug.LogWarning($"[AvatarSetupTool]   RealtimeAvatarInputs not found — assign manually");
            }

            // --- Add AvatarMovementSync ---
            AvatarMovementSync ms = go.GetComponent<AvatarMovementSync>();
            if (ms == null)
            {
                ms = go.AddComponent<AvatarMovementSync>();
                Debug.Log($"[AvatarSetupTool]   + Added AvatarMovementSync");
            }

            // trackedTransform stays null → script will track this GameObject's own position.
            // VRBodySync moves this object based on head camera → position delta = actual movement.
            ms.trackedTransform = null;
            ms.walkThreshold = 0.15f;
            ms.maxSpeed = 3f;
            ms.smoothing = 8f;

            // Wire HandSphereSync bodyAnimator to this humanoid Animator
            if (hss != null && hss.bodyAnimator == null && isHumanoid)
                hss.bodyAnimator = anim;

            anySuccess = true;
        }

        if (!anySuccess)
        {
            Debug.LogError($"[AvatarSetupTool] No Humanoid Animator found in {prefabPath}!");
        }
        else
        {
            Debug.Log($"[AvatarSetupTool] ✅ Done: {System.IO.Path.GetFileName(prefabPath)}");
        }

        return anySuccess;
    }

    [MenuItem("MedMeet Tools/Verify Avatar IK Setup")]
    public static void VerifySetup()
    {
        string report = "=== Avatar IK Verification ===\n\n";

        foreach (string path in AvatarPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string name = System.IO.Path.GetFileName(path);

            if (prefab == null)
            {
                report += $"❌ NOT FOUND: {name}\n\n";
                continue;
            }

            report += $"📦 {name}\n";

            var animators = prefab.GetComponentsInChildren<Animator>(true);
            report += $"  Animators found: {animators.Length}\n";

            foreach (var anim in animators)
            {
                bool human = anim.avatar != null && anim.avatar.isHuman;
                report += $"    • {anim.gameObject.name}: {(human ? "Humanoid ✅" : "NOT Humanoid ⚠️")}\n";
            }

            VRIKController ik = prefab.GetComponentInChildren<VRIKController>(true);
            report += ik != null ? "  VRIKController ✅\n" : "  VRIKController ❌ (run Setup)\n";

            AvatarMovementSync ms = prefab.GetComponentInChildren<AvatarMovementSync>(true);
            report += ms != null ? "  AvatarMovementSync ✅\n" : "  AvatarMovementSync ❌ (run Setup)\n";

            HandSphereSync hss = prefab.GetComponentInChildren<HandSphereSync>(true);
            report += hss != null ? "  HandSphereSync ✅\n" : "  HandSphereSync ❌ (run Setup)\n";

            VRHandTracker ht = prefab.GetComponentInChildren<VRHandTracker>(true);
            if (ht != null)
                report += ht.enabled ? "  VRHandTracker ⚠️ (STILL ENABLED — disable!)\n"
                                      : "  VRHandTracker disabled ✅\n";

            report += "\n";
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Verification", report, "OK");
    }
}
