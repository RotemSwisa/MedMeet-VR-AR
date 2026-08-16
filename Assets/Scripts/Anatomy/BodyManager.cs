using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BodyManager - על השורש (ecorche_-_anatomy_study).
/// מנהל את רשימת ה-OrganControllers ומקבל הודעות כשאיברים נשלפים/חוזרים.
/// כרגע תפקידו בעיקר תיעוד; ה-Ghost מנוהל ישירות ע"י כל OrganController.
/// </summary>
[DisallowMultipleComponent]
public class BodyManager : MonoBehaviour
{
    [Header("── Organs (filled by AnatomySetupTool) ──")]
    public List<OrganController> organs = new List<OrganController>();

    public void OnOrganPulledOut(OrganController organ)
    {
        // נקודת hook לעתיד - לדוגמה הודעה למסך, יישומונים אחרים וכו'
    }

    public void OnOrganRestored(OrganController organ)
    {
        // נקודת hook לעתיד
    }
}
