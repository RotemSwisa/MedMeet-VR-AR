using UnityEngine;

public class SimpleOVRMovement : MonoBehaviour
{
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 45.0f;
    public Transform cameraRig;
    public Transform centerEye;

    void Update()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        float rotateInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;

#if UNITY_EDITOR
        if (input == Vector2.zero)
        {
            input.y = Input.GetAxis("Vertical");
            input.x = Input.GetAxis("Horizontal");
        }
        if (rotateInput == 0)
        {
            if (Input.GetKey(KeyCode.Q)) rotateInput = -1;
            else if (Input.GetKey(KeyCode.E)) rotateInput = 1;
        }
#endif

        if (input.magnitude > 0.1f)
        {
            Vector3 forward = centerEye.forward; forward.y = 0; forward.Normalize();
            Vector3 right = centerEye.right; right.y = 0; right.Normalize();
            Vector3 moveDir = (forward * input.y + right * input.x).normalized;
            cameraRig.position += moveDir * moveSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(rotateInput) > 0.1f)
        {
            cameraRig.Rotate(0, rotateInput * rotateSpeed * Time.deltaTime, 0);
        }
    }
}