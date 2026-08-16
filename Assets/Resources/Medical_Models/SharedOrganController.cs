using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Normal.Realtime;

public class SharedOrganController : RealtimeComponent<SharedModelData>
{
    private XRSimpleInteractable interactable;
    private Transform activeController;

    // משתנים למניעת ההיפוך
    private Quaternion rotationOffset;
    private Transform _mainCameraTransform;

    private Vector3 _lockedPosition;
    private bool isHolding = false;

    protected override void OnRealtimeModelReplaced(SharedModelData previousModel, SharedModelData currentModel)
    {
        if (currentModel != null && currentModel.isFreshModel)
        {
            if (realtimeView.isOwnedLocallySelf)
            {
                currentModel.rotation = transform.rotation;
                currentModel.isVisible = true;
            }
        }
    }

    void Start()
    {
        _lockedPosition = transform.position;
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;

        // --- תיקון הרשת של קלוד ---
        // חייבים לבטל את החסימה כדי שהחבר יוכל לקחת
        if (realtimeView != null)
        {
            realtimeView.preventOwnershipTakeover = false;
        }

        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null) interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);

        // איפוס ילדים (חשוב למניעת בעיות גרפיות)
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                child.localPosition = Vector3.zero;
                child.gameObject.SetActive(true);
            }
        }
    }

    // --- הפונקציה שמונעת את ההיפוך (Anti-Flip) ---
    private Quaternion GetSmartRotation(Transform controller)
    {
        // אם אין מצלמה, נחזיר רגיל
        if (_mainCameraTransform == null)
        {
            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            return controller.rotation;
        }

        Vector3 laserForward = controller.forward;
        Vector3 cameraToHand = (controller.position - _mainCameraTransform.position).normalized;

        // בדיקה: האם הלייזר מצביע *אליך*? (זה הבאג שגורם להיפוך)
        if (Vector3.Dot(laserForward, cameraToHand) < 0)
        {
            // כן, הוא התהפך. נסובב אותו בחזרה בכוח.
            return controller.rotation * Quaternion.Euler(0, 180, 0);
        }

        return controller.rotation;
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        // 1. בקשת בעלות (רשת)
        if (realtimeView != null) realtimeView.RequestOwnership();

        isHolding = true;
        activeController = args.interactorObject.transform;

        // 2. חישוב נעילה עם התיקון החכם (סיבוב)
        Quaternion correctedRot = GetSmartRotation(activeController);
        rotationOffset = Quaternion.Inverse(correctedRot) * transform.rotation;
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        isHolding = false;
        activeController = null;
    }

    void Update()
    {
        if (model == null) return;

        transform.position = _lockedPosition;

        // --- תיקון רשת אגרסיבי (קלוד) ---
        if (isHolding && realtimeView != null && !realtimeView.isOwnedLocallySelf)
        {
            realtimeView.RequestOwnership();
        }

        // --- לוגיקת סיבוב מתוקנת ---
        if (realtimeView.isOwnedLocallySelf)
        {
            if (isHolding && activeController != null)
            {
                // שימוש בתיקון החכם גם בזמן אמת
                Quaternion correctedRot = GetSmartRotation(activeController);
                transform.rotation = correctedRot * rotationOffset;
            }

            model.rotation = transform.rotation;
        }
        else
        {
            transform.rotation = model.rotation;
        }

        // סנכרון נראות
        if (transform.childCount > 0)
        {
            GameObject visualChild = transform.GetChild(0).gameObject;
            if (visualChild.activeSelf != model.isVisible)
                visualChild.SetActive(model.isVisible);
        }
    }

    public void ToggleVisibility() { if (model != null) model.isVisible = !model.isVisible; }
    public bool IsVisible() => model != null && model.isVisible;
}