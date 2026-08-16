using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[ExecuteInEditMode]
public class DashedLineFixer : MonoBehaviour
{
    [Header("הגדרות קו")]
    public int dashCount = 5; // כמה מקפים יהיו על הקו
    public Color lineColor = Color.yellow; // צבע הקו

    void Update()
    {
        ApplyDashedEffect();
    }

    void ApplyDashedEffect()
    {
        LineRenderer line = GetComponent<LineRenderer>();

        // 1. יצירת תמונה (טקסטורה) של מקווקו ישירות בקוד!
        Texture2D dashedTexture = new Texture2D(2, 1);
        dashedTexture.SetPixel(0, 0, Color.white); // החלק הנראה
        dashedTexture.SetPixel(1, 0, new Color(1, 1, 1, 0)); // החלק השקוף (הרווח)
        dashedTexture.wrapMode = TextureWrapMode.Repeat;
        dashedTexture.filterMode = FilterMode.Point;
        dashedTexture.Apply();

        // 2. יצירת חומר (Material) שקוף שמתאים לקו
        Material dashedMat = new Material(Shader.Find("Sprites/Default"));
        dashedMat.mainTexture = dashedTexture;
        dashedMat.color = lineColor;

        // 3. החלה על ה-Line Renderer
        line.sharedMaterial = dashedMat;
        line.textureMode = LineTextureMode.Tile;
        line.sharedMaterial.mainTextureScale = new Vector2(dashCount, 1);

        // 4. החלת הצבעים והעובי
        line.startColor = lineColor;
        line.endColor = lineColor;
        if (line.startWidth <= 0) line.startWidth = 0.05f;
        if (line.endWidth <= 0) line.endWidth = 0.05f;
    }
}