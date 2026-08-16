using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Normal.Realtime;

public class DoorTeleport : MonoBehaviour
{
    public Transform teleportTarget;
    public Transform player;
    public Transform rayOrigin; // היד הימנית עם ה-Ray
    public float maxDistance = 10f;

    private Keyboard keyboard;

    private void Start()
    {
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        bool keyboardPressed = keyboard != null && keyboard.gKey.wasPressedThisFrame;

        bool triggerPressed = false;
        var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid)
        {
            rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerPressed);
        }

        if (keyboardPressed || triggerPressed)
        {
            // בדיקה אם אתה מכוון לדלת
            if (rayOrigin != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, maxDistance))
                {
                    if (hit.collider.gameObject == gameObject) // האם פגע בדלת?
                    {
                        Debug.Log("Hit door! Teleporting!");
                        player.position = teleportTarget.position;
                    }
                }
            }
        }
    }
}