using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct CutsceneFrame
{
    [TextArea(3, 5)]
    public string text;
    public Sprite image;
}

public class ImageCutscene : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI textComponent;
    public Image imageComponent;
    public AudioSource typingAudio;

    [Header("Cutscene Content")]
    [Tooltip("Isi frame cutscene berurutan. Masing-masing butuh Teks dan (opsional) Gambar.")]
    public CutsceneFrame[] frames;

    [Header("Settings")]
    public float textSpeed = 0.05f;
    public float imageFadeDuration = 0.5f;
    public float textFadeDuration = 0.5f;
    
    [Header("End Sequence")]
    [Tooltip("Nama scene selanjutnya yang akan diload saat cutscene habis.")]
    public string nextSceneName;

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        if (imageComponent != null)
        {
            Color c = imageComponent.color;
            c.a = 0f;
            imageComponent.color = c;
            imageComponent.sprite = null;
        }
        
        if (textComponent != null)
            textComponent.text = string.Empty;

        if (frames.Length > 0)
        {
            StartCoroutine(PlayFrame(0));
        }
    }

    void Update()
    {
        // Fitur Skip: Klik mouse, spasi, atau enter untuk mempercepat teks atau lanjut ke frame berikutnya
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                // Jika sedang mengetik, munculkan semua teks secara instan
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                textComponent.text = frames[currentIndex].text;
                isTyping = false;
                
                if (typingAudio != null) typingAudio.Stop();
            }
            else
            {
                // Jika teks sudah selesai, lanjut ke frame berikutnya
                NextFrame();
            }
        }
    }

    private void NextFrame()
    {
        if (currentIndex < frames.Length - 1)
        {
            currentIndex++;
            StartCoroutine(PlayFrame(currentIndex));
        }
        else
        {
            EndCutscene();
        }
    }

    private IEnumerator PlayFrame(int index)
    {
        bool needsImageTransition = imageComponent != null 
            && frames[index].image != null 
            && imageComponent.sprite != frames[index].image;

        // 1. Fade out: gambar lama + teks lama secara bersamaan
        if (index > 0)
        {
            Coroutine imageFadeOut = null;
            Coroutine textFadeOut = null;

            // Fade out gambar lama
            if (needsImageTransition && imageComponent.color.a > 0)
            {
                imageFadeOut = StartCoroutine(FadeImage(0f));
            }

            // Fade out teks lama
            if (textComponent != null && textComponent.text.Length > 0)
            {
                textFadeOut = StartCoroutine(FadeText(0f));
            }

            // Tunggu keduanya selesai
            if (imageFadeOut != null) yield return imageFadeOut;
            if (textFadeOut != null) yield return textFadeOut;
        }

        // 2. Fade in gambar baru
        if (needsImageTransition)
        {
            imageComponent.sprite = frames[index].image;
            yield return StartCoroutine(FadeImage(1f));
        }

        // 3. Mengetik Teks (masuk dengan animasi ngetik, alpha direset ke 1)
        if (textComponent != null)
        {
            textComponent.text = string.Empty;
            Color c = textComponent.color;
            c.a = 1f;
            textComponent.color = c;
            typingCoroutine = StartCoroutine(TypeSentence(frames[index].text));
        }
    }

    private IEnumerator TypeSentence(string text)
    {
        isTyping = true;
        if (typingAudio != null && !typingAudio.isPlaying) typingAudio.Play();

        foreach (char c in text.ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        if (typingAudio != null) typingAudio.Stop();
        isTyping = false;
    }

    private IEnumerator FadeImage(float targetAlpha)
    {
        if (imageComponent == null) yield break;

        float startAlpha = imageComponent.color.a;
        float time = 0f;

        while (time < imageFadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / imageFadeDuration);
            
            Color c = imageComponent.color;
            c.a = newAlpha;
            imageComponent.color = c;
            
            yield return null;
        }

        Color finalColor = imageComponent.color;
        finalColor.a = targetAlpha;
        imageComponent.color = finalColor;
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        if (textComponent == null) yield break;

        float startAlpha = textComponent.color.a;
        float time = 0f;

        while (time < textFadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / textFadeDuration);

            Color c = textComponent.color;
            c.a = newAlpha;
            textComponent.color = c;

            yield return null;
        }

        Color finalColor = textComponent.color;
        finalColor.a = targetAlpha;
        textComponent.color = finalColor;
    }

    private void EndCutscene()
    {
        // Matikan kontrol input agar pemain tidak bisa klik lagi
        this.enabled = false;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Terintegrasi langsung dengan GameSceneManager yang sudah kita rapikan sebelumnya
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ChangeScene(nextSceneName);
            }
            else
            {
                // Fallback: tetap async agar tidak freeze
                StartCoroutine(LoadSceneAsyncFallback(nextSceneName));
            }
        }
        else
        {
            Debug.Log("Cutscene selesai, tapi tidak ada Scene tujuan yang diset.");
        }
    }

    private System.Collections.IEnumerator LoadSceneAsyncFallback(string sceneName)
    {
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }

        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
    }
}
