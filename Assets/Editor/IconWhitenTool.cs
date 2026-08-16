using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MedMeet Tools → Whiten Showcase Icons
///
/// One-shot utility: takes every PNG under
/// Assets/MedMeet/Dashboard/Sprites/icons/, replaces every visible pixel's RGB
/// with white (1,1,1) while keeping the alpha channel intact, and writes the
/// result back to disk.
///
/// This lets Unity's Image.color multiply tint the icon to any colour at
/// runtime — which doesn't work when the source PNG is pure black.
///
/// Safe to re-run: pixels already white stay white. Files are overwritten in
/// place; commit them through source control after running.
/// </summary>
public static class IconWhitenTool
{
    private const string IconsFolder = "Assets/MedMeet/Dashboard/Sprites/icons";

    [MenuItem("MedMeet Tools/Whiten Showcase Icons")]
    public static void Whiten()
    {
        if (!Directory.Exists(IconsFolder))
        {
            EditorUtility.DisplayDialog("Whiten Icons Failed",
                $"Folder not found: {IconsFolder}", "OK");
            return;
        }

        var pngs = Directory.GetFiles(IconsFolder, "*.png", SearchOption.TopDirectoryOnly);
        int changed = 0;
        foreach (var path in pngs)
        {
            if (WhitenFile(path)) changed++;
        }

        AssetDatabase.Refresh();

        // Force texture-importer settings so Image renders them with crisp alpha
        foreach (var path in pngs)
        {
            string assetPath = path.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) continue;
            bool dirty = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; dirty = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; dirty = true; }
            if (imp.mipmapEnabled)
            { imp.mipmapEnabled = false; dirty = true; }
            if (dirty) AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        EditorUtility.DisplayDialog("Whiten Icons ✅",
            $"Processed {pngs.Length} icon(s).\n" +
            $"Re-coloured to white: {changed}.\n\n" +
            "You can now run 'Setup Sustainability Showcase' — icons will be tinted via " +
            "Image.color at runtime.", "OK");
    }

    private static bool WhitenFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) return false;

        var pixels = tex.GetPixels32();
        bool anyChange = false;
        for (int i = 0; i < pixels.Length; i++)
        {
            // Keep alpha — paint RGB white wherever the pixel is visible at all
            if (pixels[i].a > 0)
            {
                if (pixels[i].r != 255 || pixels[i].g != 255 || pixels[i].b != 255)
                {
                    pixels[i].r = 255;
                    pixels[i].g = 255;
                    pixels[i].b = 255;
                    anyChange = true;
                }
            }
        }
        if (!anyChange) { Object.DestroyImmediate(tex); return false; }

        tex.SetPixels32(pixels);
        tex.Apply();
        byte[] outBytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(path, outBytes);
        return true;
    }
}
