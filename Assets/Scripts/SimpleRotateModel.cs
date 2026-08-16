using UnityEngine;
using Normal.Realtime;

public class SimpleRotateModel : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float autoRotationSpeed = 30f;
    public bool enableAutoRotation = true;

    [Header("Manual Rotation (Mouse)")]
    public float manualRotationSpeed = 200f;
    public bool enableManualRotation = true;

    private RealtimeTransform _realtimeTransform;
    private bool _isDragging = false;
    private Vector3 _lastMousePosition;

    void Start()
    {
        _realtimeTransform = GetComponent<RealtimeTransform>();
    }

    void Update()
    {
        // רק אם יש לנו ownership נוכל לסובב
        if (_realtimeTransform != null && _realtimeTransform.isOwnedLocallySelf)
        {
            // סיבוב אוטומטי
            if (enableAutoRotation && !_isDragging)
            {
                transform.Rotate(Vector3.up, autoRotationSpeed * Time.deltaTime, Space.World);
            }

            // סיבוב ידני
            if (enableManualRotation)
            {
                HandleManualRotation();
            }
        }
    }

    void HandleManualRotation()
    {
        // זיהוי לחיצה על המודל
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.transform == transform)
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;

                // תפיסת Ownership
                if (!_realtimeTransform.isOwnedLocallySelf)
                {
                    _realtimeTransform.RequestOwnership();
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        // ביצוע הסיבוב בזמן גרירה
        if (_isDragging)
        {
            Vector3 delta = Input.mousePosition - _lastMousePosition;

            // סיבוב אופקי (ימין-שמאל)
            transform.Rotate(Vector3.up, -delta.x * manualRotationSpeed * Time.deltaTime, Space.World);

            // סיבוב אנכי (למעלה-למטה)  
            Vector3 cameraRight = Camera.main.transform.right;
            transform.Rotate(cameraRight, delta.y * manualRotationSpeed * Time.deltaTime, Space.World);

            _lastMousePosition = Input.mousePosition;
        }
    }

    // פונקציות נוספות לשליטה
    public void ResetRotation()
    {
        if (_realtimeTransform != null && _realtimeTransform.isOwnedLocallySelf)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    public void SetAutoRotation(bool enabled)
    {
        enableAutoRotation = enabled;
    }
}