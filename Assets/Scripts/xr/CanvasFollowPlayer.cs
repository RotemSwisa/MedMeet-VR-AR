using UnityEngine;

public class CanvasFollowPlayer : MonoBehaviour
{
    public Transform playerCamera;
    public Transform ovrCameraRig; // 🔧 חדש - ה-Root של OVR
    public float distanceFromPlayer = 1.5f;
    public float heightOffset = 0f;
    public float smoothSpeed = 10f;

    void LateUpdate()
    {
        if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy)
        {
            if (Camera.main != null) playerCamera = Camera.main.transform;
            else return;
        }

        // 🔧 אם יש ovrCameraRig, תשתמש בה כ-"ממשק" לתנועה
        Transform referencePoint = (ovrCameraRig != null && ovrCameraRig.gameObject.activeInHierarchy)
            ? ovrCameraRig
            : playerCamera.parent; // Fallback

        // חשב מיקום מול המצלמה (תמיד)
        Vector3 targetPosition = playerCamera.position + playerCamera.forward * distanceFromPlayer + Vector3.up * heightOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // עדכן סיבוב כדי להסתכל למצלמה
        Vector3 directionToPlayer = playerCamera.position - transform.position;
        directionToPlayer.y = 0;
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-directionToPlayer), Time.deltaTime * smoothSpeed);
        }
    }

    public void SetTargetCamera(Transform newCamera)
    {
        playerCamera = newCamera;
        Debug.Log($"Canvas following: {newCamera?.name}");
    }

    public void SetOVRRig(Transform newRig)
    {
        ovrCameraRig = newRig;
        Debug.Log($"OVR Rig reference: {newRig?.name}");
    }
}
