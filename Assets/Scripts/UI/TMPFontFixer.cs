using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Fixes TMP font atlases that go blank during runtime.
///
/// Uses [RuntimeInitializeOnLoadMethod] so it works automatically in every scene
/// without needing a GameObject or manual attachment.
///
/// Root cause: TMP SDF font assets outside Assets/TextMesh Pro/Resources/ can have
/// their atlas textures silently cleared when Unity finalizes asset loading.
/// Calling Apply() + ForceMeshUpdate() on every TMP component forces the GPU to
/// re-upload the atlas and re-stamp all glyph UVs.
/// </summary>
public static class TMPFontFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        // Delay one frame so all prefabs have fully instantiated before we scan
        var runner = new GameObject("[TMPFontFixer]");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<TMPFontFixerRunner>();
    }
}

// Helper MonoBehaviour — exists only to run the coroutine
internal class TMPFontFixerRunner : MonoBehaviour
{
    void Start() => StartCoroutine(Fix());

    IEnumerator Fix()
    {
        // First pass: wait one frame (prefabs finish instantiating)
        yield return null;
        ForceRebuild();

        // Second pass: wait 0.5 s (async-loaded canvases / late spawns)
        yield return new WaitForSeconds(0.5f);
        ForceRebuild();

        // Self-destruct — no ongoing overhead
        Destroy(gameObject);
    }

    static void ForceRebuild()
    {
        int count = 0;
        var allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var tmp in allTexts)
        {
            if (tmp == null || tmp.font == null) continue;
            try
            {
                // Re-upload each atlas slice to the GPU
                foreach (var tex in tmp.font.atlasTextures)
                    if (tex != null) tex.Apply(false, false);

                // Rebuild glyph geometry so UV references point at the re-uploaded atlas
                tmp.ForceMeshUpdate(true, true);
                count++;
            }
            catch { /* skip any component that errors */ }
        }
        if (count > 0)
            Debug.Log($"[TMPFontFixer] Refreshed {count} TMP_Text component(s).");
    }
}
