using UnityEngine;

public class BillboardNameTag : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // וודא ש-Canvas ב-World Space
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // הסיבוב לכיוון המצלמה
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }
}