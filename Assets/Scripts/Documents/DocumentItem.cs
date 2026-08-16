using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Normal.Realtime;

public class DocumentItem : MonoBehaviour
{
    [Header("References")]
    public MeshRenderer documentRenderer;
    public Transform documentTransform;

    [Header("Document Data")]
    public Texture2D[] pages;
    public int currentPage = 0;
    public string fileName;
    public bool isPDF = false;

    [Header("Scale Settings")]
    public float minScale = 0.3f;
    public float maxScale = 3f;
    public float scaleSpeed = 0.1f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    void Start()
    {
        // 1. הגדרת Shader מואר (Unlit) כדי שהתמונה תהיה ברורה
        if (documentRenderer != null)
        {
            documentRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        }

        // 2. הגדרת אינטראקציה (XR Grab) כדי שתוכלו להזיז את המסמך
        grabInteractable = gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        // 3. הצגת עמוד ראשון
        if (pages != null && pages.Length > 0)
            ShowPage(0);

        // הסרתי את הפונקציה CreateCloseButton - לא יהיה איקס יותר.
    }

    void Update()
    {
        // טיפול בגרירה (סקייל ודפדוף) רק כשתופסים את המסמך
        if (isGrabbed)
        {
            HandleScaling();
            HandlePageNavigation();
        }
    }

    void HandleScaling()
    {
        float scaleInput = 0f;

        // שליטה בסקייל עם הג'ויסטיק
        if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y != 0)
            scaleInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;

        // שליטה למחשב (R/F)
        if (Input.GetKey(KeyCode.R)) scaleInput = 1f;
        if (Input.GetKey(KeyCode.F)) scaleInput = -1f;

        if (scaleInput != 0)
        {
            float newScale = transform.localScale.x + (scaleInput * scaleSpeed * Time.deltaTime);
            newScale = Mathf.Clamp(newScale, minScale, maxScale);
            transform.localScale = Vector3.one * newScale;
        }
    }

    void HandlePageNavigation()
    {
        if (!isPDF || pages.Length <= 1) return;

        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // דפדוף ימינה/שמאלה
        if (Input.GetKeyDown(KeyCode.LeftArrow) || (thumbstick.x < -0.8f)) PreviousPage();
        if (Input.GetKeyDown(KeyCode.RightArrow) || (thumbstick.x > 0.8f)) NextPage();
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    public void ShowPage(int pageIndex)
    {
        if (pages == null || pageIndex >= pages.Length) return;

        Texture currentTexture = pages[pageIndex];
        documentRenderer.material.mainTexture = currentTexture;

        // וידוא שהשיידר נשאר Unlit
        if (documentRenderer.material.shader.name != "Unlit/Texture")
            documentRenderer.material.shader = Shader.Find("Unlit/Texture");

        currentPage = pageIndex;
    }

    public void SetPageFromNetwork(int pageIndex)
    {
        if (pages == null || pageIndex >= pages.Length || pageIndex < 0) return;
        currentPage = pageIndex;
        ShowPage(pageIndex);
    }

    void OnGrabbed(SelectEnterEventArgs args) { isGrabbed = true; }
    void OnReleased(SelectExitEventArgs args) { isGrabbed = false; }

    // פונקציית הסגירה נשארת כאן למקרה שתרצו לקרוא לה ממקום אחר בעתיד, אבל היא לא מופעלת ע"י כפתור
    public void CloseDocument()
    {
        RealtimeView realtimeView = GetComponent<RealtimeView>();
        if (realtimeView != null && realtimeView.isOwnedLocallySelf)
        {
            Realtime.Destroy(gameObject);
        }
        else if (realtimeView == null)
        {
            Destroy(gameObject);
        }
        else
        {
            realtimeView.RequestOwnership();
            Realtime.Destroy(gameObject);
        }
    }
}