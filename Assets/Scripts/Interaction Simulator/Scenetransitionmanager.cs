using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string mainSceneName = "vr_meeting";
    public string surgerySceneName = "main";

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public CanvasGroup fadeCanvas;

    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance => _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToSurgery()
    {
        StartCoroutine(LoadScene(surgerySceneName));
    }

    public void GoToMain()
    {
        StartCoroutine(LoadScene(mainSceneName));
    }

    private IEnumerator LoadScene(string sceneName)
    {
        // Fade out
        if (fadeCanvas != null)
        {
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvas.alpha = t / fadeDuration;
                yield return null;
            }
        }

        yield return SceneManager.LoadSceneAsync(sceneName);

        // Fade in
        if (fadeCanvas != null)
        {
            float t = fadeDuration;
            while (t > 0)
            {
                t -= Time.deltaTime;
                fadeCanvas.alpha = t / fadeDuration;
                yield return null;
            }
            fadeCanvas.alpha = 0;
        }
    }
}