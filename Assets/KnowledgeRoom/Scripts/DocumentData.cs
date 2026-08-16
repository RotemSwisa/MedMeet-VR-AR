using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NewDocument", menuName = "KnowledgeRoom/Document")]
public class DocumentData : ScriptableObject
{
    public string documentTitle;
    public string category;
    public bool isVideo;

    [Tooltip("למסמכים - תמונות העמודים")]
    public Texture2D[] pages;

    [Tooltip("לסרטונים - קובץ הוידאו")]
    public VideoClip videoClip;
}