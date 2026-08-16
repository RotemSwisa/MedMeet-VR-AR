using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // וודא שזה קיים

[RequireComponent(typeof(XRGrabInteractable))]
public class DualHandController : MonoBehaviour
{
    private XRGrabInteractable _grab;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();

        // נרשמים לאירוע "מישהו תפס אותי"
        _grab.selectEntered.AddListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // בודקים מי היד שתפסה (לפי השם שלה)
        string handName = args.interactorObject.transform.name.ToLower();
        string parentName = args.interactorObject.transform.parent != null ? args.interactorObject.transform.parent.name.ToLower() : "";

        // האם זו יד שמאל?
        bool isLeftHand = handName.Contains("left") || parentName.Contains("left");

        if (isLeftHand)
        {
            // יד שמאל: מזיזה את הלב איתך (כמו תיק)
            _grab.trackPosition = true;
            _grab.trackRotation = true;
        }
        else
        {
            // יד ימין (או כל דבר אחר): רק מסובבת במקום!
            _grab.trackPosition = false; // מבטל את ההזזה
            _grab.trackRotation = true;  // משאיר את הסיבוב
        }
    }

    void OnDestroy()
    {
        if (_grab != null)
            _grab.selectEntered.RemoveListener(OnGrab);
    }
}