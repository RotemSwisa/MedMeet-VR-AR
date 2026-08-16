using UnityEngine;
using Normal.Realtime;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
[RequireComponent(typeof(RealtimeView))]
[RequireComponent(typeof(RealtimeTransform))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class NetworkGrabOwnership : MonoBehaviour
{
    private RealtimeView _view;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

    private void Awake()
    {
        _view = GetComponent<RealtimeView>();
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // ���� ����� takeover
        if (_view != null)
            _view.preventOwnershipTakeover = false;
    }

    private void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // ���� ����� ������ ���� �����
        if (_view != null && !_view.isOwnedLocallySelf)
            _view.RequestOwnership();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // �� ������� ����� ��� ��������� ����� ����� ������
        // (RealtimeTransform ������ �� ���� ������)
    }

    private void Update()
    {
        // �� ��� �������, �� ������ ����� �� ���� � ���� ��� �� ������
        if (_grab != null && _grab.isSelected && _view != null && !_view.isOwnedLocallySelf)
            _view.RequestOwnership();
    }
}
