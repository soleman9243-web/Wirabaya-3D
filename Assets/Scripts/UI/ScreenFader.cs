using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance
    {
        get;
        private set;
    }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Pilih apakah layar otomatis pudar dari hitam ke terang saat scene dimulai.")]
    [SerializeField] private bool fadeInOnStart = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Pastikan layar hitam full di awal frame, lalu mulai memudar
        if (fadeInOnStart && fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            
            StartCoroutine(FadeIn());
        }
    }

    public IEnumerator FadeOut()
    {
        if (fadeImage != null) fadeImage.gameObject.SetActive(true);
        float timer = 0f;

        while (timer < fadeDuration)
        {
            // Batasi lag spike maksimal 0.05 detik per frame
            timer += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                fadeImage.color = color;
            }

            yield return null;
        }

        if (fadeImage != null)
        {
            Color finalColor = fadeImage.color;
            finalColor.a = 1f;
            fadeImage.color = finalColor;
        }
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage != null) fadeImage.gameObject.SetActive(true);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            // Batasi lag spike maksimal 0.05 detik per frame agar animasi tidak ter-skip!
            timer += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                fadeImage.color = color;
            }

            yield return null;
        }

        if (fadeImage != null)
        {
            Color finalColor = fadeImage.color;
            finalColor.a = 0f;
            fadeImage.color = finalColor;
        }
    }
}