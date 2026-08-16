using UnityEngine;

[CreateAssetMenu(fileName = "DocumentLibrary", menuName = "KnowledgeRoom/Library")]
public class DocumentLibrary : ScriptableObject
{
    public DocumentData[] allDocuments;
}