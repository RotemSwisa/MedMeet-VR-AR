using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Normal.Realtime;

public class VRNetworkGrab : MonoBehaviour
{
    private RealtimeTransform _rtTransform;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;

    void Awake()
    {
        // ���� �� ������� ������� �� �����
        _rtTransform = GetComponent<RealtimeTransform>();
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    }

    void OnEnable()
    {
        // ���� �������: ������� ���� �� ��������, ����� �� �������� RequestOwnership
        if (_interactable != null)
            _interactable.selectEntered.AddListener(RequestOwnership);
    }

    void OnDisable()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(RequestOwnership);
    }

    private void RequestOwnership(SelectEnterEventArgs args)
    {
        // ���� �-Normcore ����� �� �������� ��� ������� ������� ������
        if (_rtTransform != null)
        {
            _rtTransform.RequestOwnership();
        }
    }
}