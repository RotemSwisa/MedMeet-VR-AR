using UnityEngine;

public class TongueDepressorExam : MonoBehaviour
{
    [Header("BlendShape Settings")]
    public SkinnedMeshRenderer patientFaceRenderer;
    public SkinnedMeshRenderer patientTeethRenderer;
    public string openMouthBlendShapeName = "mouthOpen";
    public float maxMouthOpenWeight = 100f;

    // הפה מתחיל סגור
    private bool shouldMouthBeOpen = false;
    private float currentMouthOpenWeight = 0f;

    private int headBlendShapeIndex = -1;
    private int teethBlendShapeIndex = -1;

    private void Start()
    {
        if (patientFaceRenderer != null)
            headBlendShapeIndex = patientFaceRenderer.sharedMesh.GetBlendShapeIndex(openMouthBlendShapeName);

        if (patientTeethRenderer != null)
            teethBlendShapeIndex = patientTeethRenderer.sharedMesh.GetBlendShapeIndex(openMouthBlendShapeName);
    }

    private void Update()
    {
        // המערכת בודקת האם הבדיקה התחילה לפי מצב כפתור ההתחלה של הבוס
        if (ExamSequenceManager.Instance != null && ExamSequenceManager.Instance.startButton != null)
        {
            // אם כפתור הסטארט מכובה, זה אומר שאנחנו באמצע בדיקה, ולכן הפה צריך להיות פתוח
            shouldMouthBeOpen = !ExamSequenceManager.Instance.startButton.activeSelf;
        }

        float targetWeight = shouldMouthBeOpen ? maxMouthOpenWeight : 0f;
        currentMouthOpenWeight = Mathf.Lerp(currentMouthOpenWeight, targetWeight, Time.deltaTime * 5f);

        if (headBlendShapeIndex != -1 && patientFaceRenderer != null)
            patientFaceRenderer.SetBlendShapeWeight(headBlendShapeIndex, currentMouthOpenWeight);

        if (teethBlendShapeIndex != -1 && patientTeethRenderer != null)
            patientTeethRenderer.SetBlendShapeWeight(teethBlendShapeIndex, currentMouthOpenWeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PatientMouth"))
        {
            Debug.Log("Examining throat...");
        }
    }
}