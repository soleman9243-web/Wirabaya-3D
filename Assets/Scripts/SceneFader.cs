using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Komponen Image (hitam polos) yang akan dipakai untuk fade")]
    public Image fadeImage;

    [Tooltip("CanvasGroup (Opsional) jika kamu mau mengatur alpha lewat CanvasGroup")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    public float fadeDuration = 1f;

    private void Start()
    {
        // Otomatis Fade In (dari gelap ke terang) saat scene dimulai
        StartCoroutine(FadeIn());
    }

    // Fungsi ini bisa dipanggil untuk pindah scene dengan efek fade out
    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeIn()
    {
        // Pastikan mulai dari gelap sebelum fade ke terang
        SetAlpha(1f);
        yield return StartCoroutine(FadeRoutine(1f, 0f));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        // Fade dari terang ke gelap
        yield return StartCoroutine(FadeRoutine(0f, 1f));
        
        // Setelah gelap, load scene baru
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha)
    {
        // Cegah pemain menekan UI lain saat loading
        if (fadeCanvasGroup != null) fadeCanvasGroup.blocksRaycasts = true;

        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            SetAlpha(currentAlpha);
            yield return null;
        }

        SetAlpha(targetAlpha);

        // Buka blokir UI jika sudah terang sepenuhnya
        if (targetAlpha == 0f && fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = alpha;
        }
        else if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
}
