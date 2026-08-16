using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// שים סקריפט זה על כפתור החזרה בסצנת הניתוח
public class SurgeryRoomExit : MonoBehaviour
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
            SceneTransitionManager.Instance.GoToMain();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("vr_meeting");
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnButtonPressed);
    }
}