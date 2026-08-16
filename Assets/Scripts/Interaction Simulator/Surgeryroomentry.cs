using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// שים סקריפט זה על כפתור הכניסה לניתוח בסצנה הראשית
public class SurgeryRoomEntry : MonoBehaviour
{
    private XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnButtonPressed);
    }

    void OnButtonPressed(SelectEnterEventArgs args)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.GoToSurgery();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnButtonPressed);
    }
}