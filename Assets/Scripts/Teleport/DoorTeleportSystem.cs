using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DoorTeleportSystem : MonoBehaviour
{
    [System.Serializable]
    public struct TeleportLocation
    {
        public string roomName;
        public Transform target;
    }

    [Header("Player Reference")]
    public Transform player; // גרור את אותו Player כמו בסקריפט הישן

    [Header("Teleport Settings")]
    public List<TeleportLocation> locations;
    public float maxDistance = 10f;
    public Transform rayOrigin;

    [Header("UI References")]
    public GameObject menuCanvas;
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    private Keyboard keyboard;

    private void Start()
    {
        keyboard = Keyboard.current;
        if (menuCanvas != null) menuCanvas.SetActive(false);
        CreateButtons();
    }

    void CreateButtons()
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        foreach (TeleportLocation loc in locations)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonContainer);
            var textComp = btn.GetComponentInChildren<TMP_Text>();
            if (textComp != null) textComp.text = loc.roomName;

            Transform targetRef = loc.target;
            Button buttonComp = btn.GetComponent<Button>();

            Debug.Log("Registering button: " + loc.roomName + " -> " + (targetRef != null ? targetRef.name : "NULL!"));

            buttonComp.onClick.AddListener(() => {
                Debug.Log("Button clicked! Target: " + (targetRef != null ? targetRef.name : "NULL!"));
                TeleportTo(targetRef);
            });
        }
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

        // אם התפריט פתוח, G סוגר אותו
        if (menuCanvas != null && menuCanvas.activeSelf)
        {
            return; // פשוט תחזור - אל תסגור את התפריט מה-Update
        }

        // בדיקה אם לחצו על הדלת
        if (keyboardPressed || triggerPressed)
        {
            if (rayOrigin != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, maxDistance))
                {
                    if (hit.collider.gameObject == gameObject) // פגע בדלת
                    {
                        Debug.Log("Hit door! Opening menu!");
                        if (menuCanvas != null)
                        {
                            menuCanvas.SetActive(true);
                            menuCanvas.transform.LookAt(rayOrigin);
                            menuCanvas.transform.Rotate(0, 180, 0);
                        }
                    }
                }
            }
        }
    }

    public void TeleportTo(Transform targetTransform)
    {
        Debug.Log("TeleportTo called!");

        if (targetTransform == null)
        {
            Debug.LogError("Target is null!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("Player is null!");
            return;
        }

        Debug.Log("Player position before: " + player.position);
        Debug.Log("Target position: " + targetTransform.position);

        player.position = targetTransform.position;

        Debug.Log("Player position after: " + player.position);

        if (menuCanvas != null) menuCanvas.SetActive(false);
    }
}