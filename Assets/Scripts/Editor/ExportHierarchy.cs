using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ExportHierarchy
{
    [MenuItem("Tools/Export Hierarchy to Text")]
    public static void Export()
    {
        StringBuilder sb = new StringBuilder();
        foreach (GameObject obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Traverse(obj, sb, 0);
        }

        string path = EditorUtility.SaveFilePanel("Save Hierarchy", "", "Hierarchy.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("Hierarchy exported to: " + path);
        }
    }

    private static void Traverse(GameObject obj, StringBuilder sb, int level)
    {
        sb.Append('-', level * 2).Append(obj.name).AppendLine();
        foreach (Transform child in obj.transform)
        {
            Traverse(child.gameObject, sb, level + 1);
        }
    }
}