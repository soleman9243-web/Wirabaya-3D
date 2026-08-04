using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimelineSceneLoader : MonoBehaviour
{
    [Tooltip("Panggil fungsi ini lewat Signal Track di Timeline")]
    public void LoadScene(string sceneName)
    {
        Debug.Log("Pindah ke scene: " + sceneName);
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // Jika ada ScreenFader, fade out dulu
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }

        // Async load agar tidak freeze — loading tersembunyi di balik layar gelap
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
    }
}
