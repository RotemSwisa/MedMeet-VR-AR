#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// כלי Editor שמתקין את כל מערכת ה-Anatomy בלחיצה אחת.
///
/// תפריט:  MedMeet Tools  →  Setup Anatomy Explosion
///
/// המבנה הצפוי בסצנה:
///   ecorche_-_anatomy_study
///     ├── head            → 4 קבוצות אוטומטיות (Skeletal/Nervous/Muscular/Other)
///     ├── Lungs           → 4 קבוצות לפי שמות + כיוונים ידניים (R→שמאל, L→ימין, סימפונות→למטה)
///     ├── arm bones       → DraggableOrgan (זז + תווית + R כדי להחזיר)
///     └── Shoulder muscles → DraggableOrgan
/// </summary>
public static class AnatomySetupTool
{
    const string MENU_PATH = "MedMeet Tools/Setup Anatomy Explosion";
    const string ROOT_NAME = "ecorche_-_anatomy_study";

    [MenuItem(MENU_PATH)]
    public static void RunSetup()
    {
        var root = GameObject.Find(ROOT_NAME);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Anatomy Setup",
                $"לא מצאתי GameObject בשם '{ROOT_NAME}' בסצנה.", "אישור");
            return;
        }

        int total = 0;
        var organs = new List<OrganController>();

        // 1. head - 4 קבוצות לפי prefix
        var head = FindChild(root.transform, "head");
        if (head != null)
        {
            var oc = SetupHead(head.gameObject);
            organs.Add(oc);
            total += oc.transform.childCount;
        }

        // 2. Lungs - 4 קבוצות לפי שמות מדויקים + כיוונים
        var lungs = FindChild(root.transform, "Lungs");
        if (lungs == null) lungs = FindChild(root.transform, "lungs");
        if (lungs != null)
        {
            var oc = SetupLungs(lungs.gameObject);
            organs.Add(oc);
            total += oc.transform.childCount;
        }

        // 3. arm bones - DraggableOrgan
        var armBones = FindChild(root.transform, "arm bones");
        if (armBones != null)
        {
            SetupDraggable(armBones.gameObject,
                "Arm Bones",
                "Bones of the upper limb including humerus, radius, and ulna. They form the skeletal structure that allows arm movement.");
        }

        // 4. Shoulder muscles - DraggableOrgan
        var shoulderMuscles = FindChild(root.transform, "Shoulder muscles");
        if (shoulderMuscles == null) shoulderMuscles = FindChild(root.transform, "shoulder muscles");
        if (shoulderMuscles != null)
        {
            SetupDraggable(shoulderMuscles.gameObject,
                "Shoulder Muscles",
                "Muscles surrounding the shoulder joint including deltoid, trapezius, and rotator cuff. They enable arm rotation and stability.");
        }

        // BodyManager על השורש
        var bm = root.GetComponent<BodyManager>();
        if (bm == null) bm = Undo.AddComponent<BodyManager>(root);
        bm.organs = organs;
        EditorUtility.SetDirty(bm);

        // AnatomyHotkeys על השורש - לבדיקה ב-Editor
        var hk = root.GetComponent<AnatomyHotkeys>();
        if (hk == null) hk = Undo.AddComponent<AnatomyHotkeys>(root);
        EditorUtility.SetDirty(hk);

        // AnatomyInfoPanel - מסך הסבר צף
        EnsureInfoPanel(root);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Anatomy Setup הושלם! ✓",
            $"הותקנו {organs.Count} איברים מתפצצים + 2 איברים נגררים.\n" +
            $"עבדתי על {total} תת-חלקים.\n\n" +
            "🎹 קיצורי מקלדת לבדיקה ב-Editor:\n" +
            "1 = פיצוץ ראש\n" +
            "2 = פיצוץ ריאות\n" +
            "3 = arm bones\n" +
            "4 = shoulder muscles\n" +
            "0 = החזר הכל\n\n" +
            "כשתרצה VR - הרץ 'MedMeet Tools → Add XR Grab to Organs'", "מעולה!");

        Debug.Log($"[AnatomySetupTool] Setup complete. Organs: {organs.Count}, Parts: {total}");
    }

    // ════════════════════════════════════════════════════════════════════
    //  תפריטים נוספים
    // ════════════════════════════════════════════════════════════════════

    [MenuItem("MedMeet Tools/Refresh Anatomy Grab Components")]
    public static void RefreshGrab()
    {
        var root = GameObject.Find(ROOT_NAME);
        if (root == null) return;

        int refreshed = 0;
        var names = new[] { "head", "Lungs", "arm bones", "Shoulder muscles" };
        foreach (var n in names)
        {
            var c = FindChild(root.transform, n);
            if (c == null) continue;
            EnsureAnatomyGrab(c.gameObject);
            refreshed++;
        }

        EditorUtility.DisplayDialog("Anatomy Grab Refreshed",
            $"{refreshed} organs now have AnatomyGrabbable + XRSimpleInteractable.\nNo more jumping!", "OK");
    }

    // ════════════════════════════════════════════════════════════════════
    //  HEAD - 4 קבוצות לפי prefix
    // ════════════════════════════════════════════════════════════════════
    static OrganController SetupHead(GameObject headGO)
    {
        var oc = EnsureOrganController(headGO);

        // תיאור כללי של הראש (מוצג ב-AnatomyInfoPanel כשהאיבר לא מפוצץ)
        if (string.IsNullOrEmpty(oc.OverallDescription))
        {
            oc.OverallDescription =
                "The human head contains the brain - the body's control center - " +
                "along with the skull that protects it, key sensory organs (eyes, ears, nose, mouth), " +
                "and the muscular system that drives facial expressions, chewing and speech. " +
                "Explode it to inspect the skeletal, nervous, muscular and other systems individually.";
        }

        // אסוף את כל הילדים, סווג לפי prefix
        var skeletal = new List<Transform>();
        var nervous = new List<Transform>();
        var muscular = new List<Transform>();
        var other = new List<Transform>();

        for (int i = 0; i < headGO.transform.childCount; i++)
        {
            var child = headGO.transform.GetChild(i);
            string n = child.name.ToLower();
            if (n.Contains("colider") || n.Contains("collider")) continue;
            if (n.Contains("ghost") || n.Contains("label")) continue;

            EnsureColliderForHover(child.gameObject);

            if (n.Contains("skeletal")) skeletal.Add(child);
            else if (n.Contains("nervous")) nervous.Add(child);
            else if (n.Contains("muscular")) muscular.Add(child);
            else other.Add(child);
        }

        // פריסת מעוין: Skeletal=שמאל, Nervous=למעלה+קדימה, Muscular=ימין, Other=למטה
        oc.Groups = new List<AnatomyGroup>
        {
            new AnatomyGroup
            {
                GroupName = "Skeletal",
                Description = "Bones of the head and skull. The skeletal system forms the rigid framework that protects the brain and supports facial features.",
                Parts = skeletal,
                DirectionOverride = new Vector3(-1f, 0f, 0f), // שמאל
                distanceMultiplier = 1f
            },
            new AnatomyGroup
            {
                GroupName = "Nervous",
                Description = "The nervous system - including the brain and cranial nerves. It controls all bodily functions and processes sensory information.",
                Parts = nervous,
                DirectionOverride = new Vector3(0f, 0.7f, 0.7f), // למעלה + קדימה
                distanceMultiplier = 1f
            },
            new AnatomyGroup
            {
                GroupName = "Muscular",
                Description = "Facial and head muscles that control expressions, chewing, eye movement, and head positioning.",
                Parts = muscular,
                DirectionOverride = new Vector3(1f, 0f, 0f), // ימין
                distanceMultiplier = 1f
            },
            new AnatomyGroup
            {
                GroupName = "Vascular & Glandular",
                Description = "The soft tissue systems of the head: circulatory vessels (arteries and veins) that supply blood, the lymphatic system that fights infection, and digestive structures of the mouth and throat.",
                Parts = other,
                DirectionOverride = new Vector3(0f, -1f, 0f), // למטה
                distanceMultiplier = 1f
            }
        };

        EditorUtility.SetDirty(oc);
        return oc;
    }

    // ════════════════════════════════════════════════════════════════════
    //  LUNGS - 4 קבוצות לפי שמות + כיוונים ידניים
    // ════════════════════════════════════════════════════════════════════
    static OrganController SetupLungs(GameObject lungsGO)
    {
        var oc = EnsureOrganController(lungsGO);

        if (string.IsNullOrEmpty(oc.OverallDescription))
        {
            oc.OverallDescription =
                "The lungs are the primary organs of the respiratory system. " +
                "Air enters via the trachea, branches through the bronchi into each lung, " +
                "and reaches millions of tiny alveoli where oxygen enters the bloodstream " +
                "and carbon dioxide leaves it. Explode them to see the right lung, left lung, " +
                "and the trachea/bronchi airway separately.";
        }

        // קח את כל הילדים, ודא קוליידרים
        var allChildren = new List<Transform>();
        for (int i = 0; i < lungsGO.transform.childCount; i++)
        {
            var child = lungsGO.transform.GetChild(i);
            string n = child.name.ToLower();
            if (n.Contains("colider") || n.Contains("collider")) continue;
            if (n.Contains("ghost") || n.Contains("label")) continue;
            EnsureColliderForHover(child.gameObject);
            allChildren.Add(child);
        }

        // קבוצה 1: Right_lungs (זז שמאלה - הציר X-)
        var rightLungs = FindByName(allChildren, "Right_lungs", "right_lung", "rightlung");
        // קבוצה 2: Left_lungs (זז ימינה - הציר X+)
        var leftLungs = FindByName(allChildren, "Left_lungs", "left_lung", "leftlung");
        // קבוצה 3: Bronchi + Trachea (זזים למטה Y-)
        var bronchiTrachea = new List<Transform>();
        foreach (var c in allChildren)
        {
            string n = c.name.ToLower();
            if (n.Contains("bronchi") || n.Contains("trachea")) bronchiTrachea.Add(c);
        }
        // קבוצה 4: כל השאר
        var other = new List<Transform>();
        foreach (var c in allChildren)
        {
            if (rightLungs.Contains(c) || leftLungs.Contains(c) || bronchiTrachea.Contains(c)) continue;
            other.Add(c);
        }

        oc.Groups = new List<AnatomyGroup>
        {
            new AnatomyGroup
            {
                GroupName = "Right Lung",
                Description = "The right lung has three lobes (upper, middle, lower) and is slightly larger than the left lung. It exchanges oxygen and carbon dioxide with the bloodstream.",
                Parts = rightLungs,
                DirectionOverride = new Vector3(1f, 0f, 0f), // ימינה (תוקן)
                distanceMultiplier = 0.6f // לא רחוק מדי
            },
            new AnatomyGroup
            {
                GroupName = "Left Lung",
                Description = "The left lung has two lobes (upper, lower) and a cardiac notch to accommodate the heart. It performs the same gas exchange as the right lung.",
                Parts = leftLungs,
                DirectionOverride = new Vector3(-1f, 0f, 0f), // שמאלה (תוקן)
                distanceMultiplier = 0.6f
            },
            new AnatomyGroup
            {
                GroupName = "Trachea & Bronchi",
                Description = "The airway system: the trachea (windpipe) branches into the left and right main bronchi, which deliver air into each lung.",
                Parts = bronchiTrachea,
                DirectionOverride = new Vector3(0f, -1f, 0f), // למטה
                distanceMultiplier = 1f
            },
            new AnatomyGroup
            {
                GroupName = "Throat & Thyroid",
                Description = "Structures of the throat region: the hyoid bone that supports the tongue, the thyroid gland that regulates metabolism via hormones, and the membrane connecting them.",
                Parts = other,
                DirectionOverride = Vector3.zero,
                distanceMultiplier = 0.5f
            }
        };

        EditorUtility.SetDirty(oc);
        return oc;
    }

    // ════════════════════════════════════════════════════════════════════
    //  DRAGGABLE ORGAN - arm bones / Shoulder muscles
    // ════════════════════════════════════════════════════════════════════
    static void SetupDraggable(GameObject organGO, string organName, string description)
    {
        var d = organGO.GetComponent<DraggableOrgan>();
        if (d == null) d = Undo.AddComponent<DraggableOrgan>(organGO);
        d.OrganName = organName;
        d.Description = description;

        var rb = organGO.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(organGO);
        rb.useGravity = false;
        rb.isKinematic = true;

        // קוליידר על השורש - גדול כל ה-bounds (קל לפגוע)
        EnsureColliderForGrab(organGO);

        // קוליידרים גם לילדים (כדי שגם הלייזר/עכבר על תת-חלקים יזהה אותם)
        for (int i = 0; i < organGO.transform.childCount; i++)
        {
            var child = organGO.transform.GetChild(i);
            string n = child.name.ToLower();
            if (n.Contains("colider") || n.Contains("collider")) continue;
            if (n.Contains("ghost") || n.Contains("label")) continue;
            EnsureColliderForHover(child.gameObject);
        }

        EnsureAnatomyGrab(organGO);
        EditorUtility.SetDirty(d);
    }

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    static OrganController EnsureOrganController(GameObject organGO)
    {
        var oc = organGO.GetComponent<OrganController>();
        if (oc == null) oc = Undo.AddComponent<OrganController>(organGO);

        var rb = organGO.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(organGO);
        rb.useGravity = false;
        rb.isKinematic = true;

        EnsureColliderForGrab(organGO);

        // Custom grab pipeline - replaces XRGrabInteractable (which causes the head to jump).
        EnsureAnatomyGrab(organGO);

        return oc;
    }

    /// <summary>
    /// יוצר BoxCollider על האיבר בגודל ה-bounds המלא (מה שהלייזר/עכבר יכול לפגוע בו בקלות).
    /// AnatomyGrabbable שומר offset בעת תפיסה - גודל הקוליידר לא משנה לקפיצה.
    /// </summary>
    static void EnsureColliderForGrab(GameObject go)
    {
        var existing = go.GetComponent<Collider>();
        if (existing != null) return;

        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            var sc = Undo.AddComponent<SphereCollider>(go);
            sc.radius = 0.05f;
            sc.isTrigger = false;
            return;
        }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        var box = Undo.AddComponent<BoxCollider>(go);
        // local space: convert world bounds
        box.center = go.transform.InverseTransformPoint(b.center);
        // local size from world size + scale
        Vector3 lossy = go.transform.lossyScale;
        Vector3 invScale = new Vector3(
            lossy.x != 0 ? 1f / lossy.x : 1f,
            lossy.y != 0 ? 1f / lossy.y : 1f,
            lossy.z != 0 ? 1f / lossy.z : 1f
        );
        box.size = Vector3.Scale(b.size, invScale);
        box.isTrigger = false;
    }

    static void EnsureColliderForHover(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return;
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var mc = Undo.AddComponent<MeshCollider>(go);
            mc.convex = false;
        }
        else
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var bc = Undo.AddComponent<BoxCollider>(go);
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                bc.center = go.transform.InverseTransformPoint(b.center);
                bc.size = b.size;
            }
        }
    }

    /// <summary>
    /// Adds the custom grab pipeline (XRSimpleInteractable + AnatomyGrabbable),
    /// and removes any XRGrabInteractable - the source of the head's jump.
    /// </summary>
    static void EnsureAnatomyGrab(GameObject go)
    {
        // 1. Remove any XRGrabInteractable (replaced by our custom system)
        var grabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (grabType != null)
        {
            var oldGrab = go.GetComponent(grabType);
            if (oldGrab != null)
            {
                Object.DestroyImmediate(oldGrab);
                Debug.Log($"[AnatomySetupTool] {go.name}: removed legacy XRGrabInteractable (causes jumping)");
            }
        }

        // 2. Add XRSimpleInteractable (just an event source, no movement)
        var simpleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
        if (simpleType == null)
        {
            Debug.LogError("[AnatomySetupTool] XRSimpleInteractable type not found - XRI not installed?");
            return;
        }
        var simple = go.GetComponent(simpleType);
        if (simple == null) simple = Undo.AddComponent(go, simpleType);

        // 3. Populate XRSimpleInteractable's colliders list with all colliders on the GameObject + children
        var allColliders = new List<Collider>();
        allColliders.AddRange(go.GetComponents<Collider>());
        allColliders.AddRange(go.GetComponentsInChildren<Collider>(true));
        var distinctColliders = new List<Collider>();
        foreach (var c in allColliders)
        {
            if (c != null && !distinctColliders.Contains(c)) distinctColliders.Add(c);
        }
        // m_Colliders is List<Collider> on XRBaseInteractable
        var collidersField = simpleType.GetField("m_Colliders",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.FlattenHierarchy);
        if (collidersField == null)
        {
            // search up the inheritance chain
            var t = simpleType;
            while (t != null && collidersField == null)
            {
                collidersField = t.GetField("m_Colliders",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                t = t.BaseType;
            }
        }
        if (collidersField != null)
        {
            collidersField.SetValue(simple, distinctColliders);
        }

        // 4. Add AnatomyGrabbable (the actual grab logic)
        var grabbable = go.GetComponent<AnatomyGrabbable>();
        if (grabbable == null) grabbable = Undo.AddComponent<AnatomyGrabbable>(go);

        Debug.Log($"[AnatomySetupTool] {go.name}: AnatomyGrabbable + XRSimpleInteractable ready ({distinctColliders.Count} colliders)");
    }

    static void TrySetField(Component comp, string fieldName, object value)
    {
        if (comp == null) return;
        var f = comp.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f != null) f.SetValue(comp, value);
    }

    /// <summary>
    /// Creates (or reuses) the floating info panel GameObject.
    /// Positioned in front of the body so the user sees it when looking at the organs.
    /// </summary>
    static void EnsureInfoPanel(GameObject root)
    {
        // Look for an existing one
        var existing = Object.FindFirstObjectByType<AnatomyInfoPanel>();
        GameObject panelGO;
        if (existing != null)
        {
            panelGO = existing.gameObject;
        }
        else
        {
            panelGO = new GameObject("AnatomyInfoPanel");
            Undo.RegisterCreatedObjectUndo(panelGO, "Create AnatomyInfoPanel");
            // Position it to the right side of the body at a comfortable reading distance
            panelGO.transform.position = root.transform.position + new Vector3(1.2f, 0.6f, -1.0f);
            panelGO.transform.LookAt(root.transform.position + Vector3.up * 0.6f);
            // Rotate 180 so the panel faces the user (LookAt makes back face the target)
            panelGO.transform.Rotate(0, 180f, 0);
            Undo.AddComponent<AnatomyInfoPanel>(panelGO);
        }
        EditorUtility.SetDirty(panelGO);
    }

    static Transform FindChild(Transform parent, string name)
    {
        // ילדים ישירים קודם
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (string.Equals(c.name, name, System.StringComparison.OrdinalIgnoreCase)) return c;
        }
        // קינון עמוק
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            var sub = FindChild(c, name);
            if (sub != null) return sub;
        }
        return null;
    }

    static List<Transform> FindByName(List<Transform> children, params string[] candidates)
    {
        var result = new List<Transform>();
        foreach (var c in children)
        {
            string n = c.name.ToLower().Replace(" ", "").Replace("_", "");
            foreach (var cand in candidates)
            {
                string cc = cand.ToLower().Replace(" ", "").Replace("_", "");
                if (n == cc || n.Contains(cc))
                {
                    result.Add(c);
                    break;
                }
            }
        }
        return result;
    }
}
#endif
