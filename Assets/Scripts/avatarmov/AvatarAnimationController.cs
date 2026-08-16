using UnityEngine;
using Normal.Realtime;

public class AvatarAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private RealtimeView _realtimeView;
    private Vector3 _lastPosition;

    void Awake()
    {
        _realtimeView = GetComponent<RealtimeView>();

        // מחפש אנימטור - גם על האובייקט וגם בילדים
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        _lastPosition = transform.position;

        // Debug - נראה מה מוצא
        Debug.Log($"RealtimeView found: {_realtimeView != null}");
        Debug.Log($"Animator found: {animator != null}");
        if (animator != null) Debug.Log($"Animator object: {animator.gameObject.name}");
    }

    void Update()
    {
        Vector3 currentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastPos = new Vector3(_lastPosition.x, 0, _lastPosition.z);

        float speed = Vector3.Distance(currentPos, lastPos) / Time.deltaTime;

        // Debug כל שנייה בערך
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Position: {transform.position}, Speed: {speed}");
            if (_realtimeView != null)
                Debug.Log($"isOwnedLocally: {_realtimeView.isOwnedLocallyInHierarchy}");
        }

        if (animator != null)
            animator.SetFloat("Speed", speed);

        _lastPosition = transform.position;
    }
}