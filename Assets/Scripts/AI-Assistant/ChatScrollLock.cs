using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop-in scroll behaviour for the AI chat ScrollRect.
///
/// Problem this fixes:
///   When AppendToChat() runs, the TextMeshPro inside the ScrollRect's Content
///   has its size recalculated by ContentSizeFitter on the next layout pass.
///   Unity's auto-layout resets verticalNormalizedPosition to 1 (= top) while
///   that pass is in flight, so the user — who was reading further down — sees
///   the conversation jump to the start the moment they release the trigger
///   (the EndDrag callback coincides with a layout rebuild).
///
/// What this component does:
///   • Hooks IBeginDragHandler / IEndDragHandler on the ScrollRect to know
///     when the user is actively dragging.
///   • Remembers the LAST stable scroll position the user was on.
///   • After every layout rebuild it restores that position — unless we were
///     pinned at the bottom, in which case we follow the new bottom.
///   • Exposes ScrollToBottom() / WasAtBottom for ClinicalAdvisorUI.
///
/// Attach this component to the GameObject that already has the ScrollRect.
/// No other wiring needed — it finds the ScrollRect via GetComponent.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ChatScrollLock : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Tooltip("Treat positions within this distance of the bottom as 'at bottom'. " +
             "0 = exactly at bottom, 0.1 = within 10% of bottom.")]
    [Range(0f, 0.5f)] public float bottomThreshold = 0.10f;

    [Tooltip("If true, when the user is at the bottom and a new message arrives, " +
             "auto-scroll to keep them at the bottom. If they had scrolled up, " +
             "their position is preserved.")]
    public bool followBottomWhenPinned = true;

    private ScrollRect _scroll;
    private bool   _dragging;
    private bool   _wasAtBottom = true;
    private float  _rememberedPos = 0f;   // 0 = bottom, 1 = top (Unity convention)
    private bool   _restoreRequested;

    void Awake()
    {
        _scroll = GetComponent<ScrollRect>();
        // NOTE: We deliberately do NOT touch movementType / inertia /
        // scrollSensitivity here any more — those are set by the fix tool
        // ONCE in edit-mode so the user can override them later from the
        // Inspector without us clobbering their choices each Play.
    }

    void OnEnable()
    {
        _scroll.onValueChanged.AddListener(OnScrollChanged);
    }

    void OnDisable()
    {
        _scroll.onValueChanged.RemoveListener(OnScrollChanged);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
        // Whatever position the user finished at is the new "anchor"
        _rememberedPos = _scroll.verticalNormalizedPosition;
        _wasAtBottom   = _rememberedPos <= bottomThreshold;
    }

    private void OnScrollChanged(Vector2 _)
    {
        // While the user is actively dragging, follow them in real time.
        if (_dragging)
        {
            _rememberedPos = _scroll.verticalNormalizedPosition;
            _wasAtBottom   = _rememberedPos <= bottomThreshold;
        }
    }

    /// <summary>
    /// Call this once after a chat message is appended / content height
    /// changes. The lock will restore the user's position on the next frame.
    /// </summary>
    public void NotifyContentChanged()
    {
        if (_restoreRequested) return;
        _restoreRequested = true;
        StartCoroutine(RestoreOnNextLayout());
    }

    /// <summary>Force-scrolls to the bottom of the chat, regardless of state.</summary>
    public void ScrollToBottom()
    {
        _wasAtBottom = true;
        _rememberedPos = 0f;
        NotifyContentChanged();
    }

    public bool WasAtBottom => _wasAtBottom;

    private IEnumerator RestoreOnNextLayout()
    {
        // Let ContentSizeFitter + LayoutGroups settle first
        yield return null;
        yield return new WaitForEndOfFrame();

        if (_scroll != null && !_dragging)
        {
            // If the user was at the bottom, stick to the new bottom.
            // Otherwise restore the remembered position so the text they
            // were reading doesn't jump away.
            _scroll.verticalNormalizedPosition =
                (_wasAtBottom && followBottomWhenPinned) ? 0f : _rememberedPos;

            // A second pass — some layout rebuilds happen one frame later
            yield return null;
            if (!_dragging)
                _scroll.verticalNormalizedPosition =
                    (_wasAtBottom && followBottomWhenPinned) ? 0f : _rememberedPos;
        }
        _restoreRequested = false;
    }
}
