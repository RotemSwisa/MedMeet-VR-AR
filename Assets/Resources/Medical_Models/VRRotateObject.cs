using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Normal.Realtime;

public class VRRotateFixed : MonoBehaviour
{
    [Header("Settings")]
    public float rotationSpeed = 50f;
    public bool lockPosition = true;

    private Vector3 fixedPosition;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isBeingControlled = false;
    private Vector2 lastControllerRotation;
    private Transform controllerTransform;

    // 🔥 Normcore
    private RealtimeView realtimeView;

    void Start()
    {
        fixedPosition = transform.position;

        // XR
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }

        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);

        // Normcore
        realtimeView = GetComponent<RealtimeView>();
    }

    void OnSelectEnter(SelectEnterEventArgs args)
    {
        isBeingControlled = true;
        controllerTransform = args.interactorObject.transform;

        lastControllerRotation = new Vector2(
            controllerTransform.eulerAngles.x,
            controllerTransform.eulerAngles.y
        );

        // 🔑 בקשת בעלות — קריטי!
        if (realtimeView != null && !realtimeView.isOwnedLocally)
        {
            realtimeView.RequestOwnership();
        }
    }

    void OnSelectExit(SelectExitEventArgs args)
    {
        isBeingControlled = false;
        controllerTransform = null;
    }

    void LateUpdate()
    {
        if (lockPosition)
        {
            transform.position = fixedPosition;
        }

        if (isBeingControlled && controllerTransform != null)
        {
            Vector2 currentRotation = new Vector2(
                controllerTransform.eulerAngles.x,
                controllerTransform.eulerAngles.y
            );

            Vector2 deltaRotation = currentRotation - lastControllerRotation;

            if (deltaRotation.x > 180) deltaRotation.x -= 360;
            if (deltaRotation.x < -180) deltaRotation.x += 360;
            if (deltaRotation.y > 180) deltaRotation.y -= 360;
            if (deltaRotation.y < -180) deltaRotation.y += 360;

            // 🔄 סיבוב (יסתנכרן דרך RealtimeTransform)
            transform.Rotate(Vector3.up, deltaRotation.y * rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, -deltaRotation.x * rotationSpeed * Time.deltaTime, Space.World);

            lastControllerRotation = currentRotation;
        }
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEnter);
            interactable.selectExited.RemoveListener(OnSelectExit);
        }
    }
}
