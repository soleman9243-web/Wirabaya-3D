using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bossUIPanel;
    public TextMeshProUGUI bossNameText;
    public Image healthFill;
    public Image healthEaseFill;

    [Header("Settings")]
    public float easeSpeed = 2f;

    [Header("Intro Settings")]
    [Tooltip("Jarak slide nama boss dari kiri (pixel)")]
    public float nameSlideDistance = 120f;

    private float targetFillAmount = 1f;

    private void Awake()
    {
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(false); // Hide awalnya
        }
    }

    /// <summary>
    /// Inisialisasi UI boss langsung (tanpa animasi intro). Untuk boss generik.
    /// </summary>
    public void InitializeBossUI(string bossName)
    {
        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }

        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(true);
        }

        targetFillAmount = 1f;
        if (healthFill != null) healthFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
        if (healthEaseFill != null) healthEaseFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
    }

    /// <summary>
    /// Intro sequence animasi:
    ///   Step 1: Nama boss fade in + slide dari kiri
    ///   Step 2: Health bar muncul dalam keadaan KOSONG
    ///   Step 3: Health bar terisi dari kosong ke penuh (slider)
    ///   Step 4: Setelah penuh, coroutine selesai → boss mulai menyerang
    /// </summary>
    public IEnumerator PlayIntroSequence(string bossName, float fadeInDuration, float fillDuration)
    {
        // Tampilkan panel
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(true);
        }

        // Set health bar kosong di awal
        if (healthFill != null) healthFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        if (healthEaseFill != null) healthEaseFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);

        // ===== STEP 1: Fade in nama boss dengan slide dari kiri =====
        RectTransform nameRect = null;
        Vector2 originalNamePos = Vector2.zero;

        if (bossNameText != null)
        {
            bossNameText.text = bossName;
            bossNameText.alpha = 0f;
            nameRect = bossNameText.GetComponent<RectTransform>();

            if (nameRect != null)
            {
                originalNamePos = nameRect.anchoredPosition;
                nameRect.anchoredPosition = originalNamePos + new Vector2(-nameSlideDistance, 0f);
            }
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            float t = elapsed / fadeInDuration;
            // SmoothStep: pelan di awal & akhir, cepat di tengah
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (bossNameText != null)
            {
                bossNameText.alpha = smooth;
            }

            if (nameRect != null)
            {
                nameRect.anchoredPosition = Vector2.Lerp(
                    originalNamePos + new Vector2(-nameSlideDistance, 0f),
                    originalNamePos,
                    smooth
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Pastikan posisi & alpha final
        if (bossNameText != null) bossNameText.alpha = 1f;
        if (nameRect != null) nameRect.anchoredPosition = originalNamePos;

        // Jeda singkat antara nama muncul dan health bar mulai terisi
        yield return new WaitForSeconds(0.3f);

        // ===== STEP 2 & 3: Health bar terisi dari kosong ke penuh =====
        elapsed = 0f;
        while (elapsed < fillDuration)
        {
            float t = elapsed / fillDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (healthFill != null)
            {
                healthFill.rectTransform.localScale = new Vector3(smooth, 1f, 1f);
            }

            if (healthEaseFill != null)
            {
                healthEaseFill.rectTransform.localScale = new Vector3(smooth, 1f, 1f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Pastikan penuh di akhir
        if (healthFill != null) healthFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
        if (healthEaseFill != null) healthEaseFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
        targetFillAmount = 1f;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        targetFillAmount = currentHealth / maxHealth;
        
        if (healthFill != null)
        {
            healthFill.rectTransform.localScale = new Vector3(targetFillAmount, 1f, 1f);
        }
    }

    private void Update()
    {
        if (healthEaseFill != null && healthFill != null)
        {
            float targetScale = healthFill.rectTransform.localScale.x;
            float currentScale = healthEaseFill.rectTransform.localScale.x;

            if (currentScale != targetScale)
            {
                float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * easeSpeed);
                healthEaseFill.rectTransform.localScale = new Vector3(newScale, 1f, 1f);
            }
        }
    }

    public void HideUI()
    {
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(false);
        }
    }
}
