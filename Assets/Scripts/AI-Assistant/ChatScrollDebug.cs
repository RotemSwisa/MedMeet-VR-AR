using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime diagnostic for the AI chat ScrollRect.
///
/// While in Play mode this component logs whether drag events are reaching
/// the ScrollRect, how big the content currently is, and whether scrolling
/// is even possible (content height &gt; viewport height).
///
/// Read the Console while you press G + drag in the editor — every entry is
/// tagged [ChatScrollDebug]. The diagnostic line tells you in one glance:
///   • IS scrollable      — content > viewport, so dragging can do something
///   • Drag receiver      — the GameObject that actually caught the click
///   • Pos                — current verticalNormalizedPosition
///
/// Remove this component once scrolling works. Safe to leave on — it only
/// writes to log when something actually happens.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ChatScrollDebug : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler, IScrollHandler
{
    private ScrollRect _scroll;
    private float _lastReportTime;

    void Awake() => _scroll = GetComponent<ScrollRect>();

    void Start() => ReportState("Start");

    void OnEnable()
    {
        if (_scroll == null) _scroll = GetComponent<ScrollRect>();
        Invoke(nameof(LateState), 0.5f);
    }

    void LateState() => ReportState("After 0.5s");

    public void OnPointerDown(PointerEventData e)
        => Debug.Log($"[ChatScrollDebug] PointerDown · hit={Name(e.pointerCurrentRaycast.gameObject)} · pos={e.position}");

    public void OnPointerUp(PointerEventData e)
        => Debug.Log($"[ChatScrollDebug] PointerUp   · scrollPos={_scroll.verticalNormalizedPosition:F3}");

    public void OnBeginDrag(PointerEventData e)
        => Debug.Log($"[ChatScrollDebug] OnBeginDrag · hit={Name(e.pointerCurrentRaycast.gameObject)}");

    public void OnDrag(PointerEventData e)
    {
        // Log at most ~5x per second to avoid flooding the console
        if (Time.unscaledTime - _lastReportTime < 0.2f) return;
        _lastReportTime = Time.unscaledTime;
        Debug.Log($"[ChatScrollDebug] OnDrag · delta={e.delta.y:F1} · pos={_scroll.verticalNormalizedPosition:F3}");
    }

    public void OnEndDrag(PointerEventData e)
        => Debug.Log($"[ChatScrollDebug] OnEndDrag · finalPos={_scroll.verticalNormalizedPosition:F3}");

    public void OnScroll(PointerEventData e)
        => Debug.Log($"[ChatScrollDebug] Wheel · scrollDelta={e.scrollDelta} · pos={_scroll.verticalNormalizedPosition:F3}");

    private void ReportState(string tag)
    {
        if (_scroll == null) return;
        float viewportH = _scroll.viewport != null ? _scroll.viewport.rect.height : -1f;
        float contentH  = _scroll.content  != null ? _scroll.content.rect.height  : -1f;
        bool canScroll  = contentH > viewportH + 1f;

        Debug.Log(
            $"[ChatScrollDebug] {tag}\n" +
            $"  ScrollRect.vertical  = {_scroll.vertical}\n" +
            $"  ScrollRect.horizontal= {_scroll.horizontal}\n" +
            $"  ScrollRect.inertia   = {_scroll.inertia}\n" +
            $"  movementType         = {_scroll.movementType}\n" +
            $"  Viewport height      = {viewportH:F0}\n" +
            $"  Content  height      = {contentH:F0}\n" +
            $"  IS SCROLLABLE        = {(canScroll ? "YES" : "NO  (content not bigger than viewport)")}");
    }

    private static string Name(GameObject go) => go != null ? go.name : "<none>";
}
