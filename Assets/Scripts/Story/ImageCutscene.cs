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
        // 1. Transisi Gambar (Hanya fade in/out jika gambarnya berubah)
        if (imageComponent != null && frames[index].image != null)
        {
            if (imageComponent.sprite != frames[index].image)
            {
                // Fade out gambar lama jika ini bukan frame pertama
                if (index > 0 && imageComponent.color.a > 0)
                {
                    yield return StartCoroutine(FadeImage(0f));
                }

                // Pasang gambar baru dan fade in
                imageComponent.sprite = frames[index].image;
                yield return StartCoroutine(FadeImage(1f));
            }
        }

        // 2. Mengetik Teks
        if (textComponent != null)
        {
            textComponent.text = string.Empty;
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
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }
        else
        {
            Debug.Log("Cutscene selesai, tapi tidak ada Scene tujuan yang diset.");
        }
    }
}
