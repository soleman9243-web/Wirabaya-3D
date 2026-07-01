using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PremiumButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Text Settings")]
    public TextMeshProUGUI buttonText;
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Abu-abu
    public Color hoverColor = Color.white; // Putih terang atau emas
    public float hoverScale = 1.1f; // Membesar 10%
    
    [Header("Optional Elements")]
    [Tooltip("Gambar garis glow atau background (Image) yang muncul perlahan saat hover. (Kosongkan jika tidak ada)")]
    public Image underlineGlow;

    [Header("Animation Settings")]
    public float animSpeed = 12f;
    
    // Internal states
    private Vector3 originalScale;
    private Coroutine animCoroutine;

    private void Awake()
    {
        if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
        
        originalScale = transform.localScale;

        // Reset awal
        if (buttonText != null) buttonText.color = normalColor;
        
        if (underlineGlow != null)
        {
            Color c = underlineGlow.color;
            c.a = 0f;
            underlineGlow.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateButton(hoverColor, originalScale * hoverScale, 1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateButton(normalColor, originalScale, 0f));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Efek klik ("pop"): Menciut sedikit lalu membesar lagi ke skala hover
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        transform.localScale = originalScale * 0.95f; 
        animCoroutine = StartCoroutine(AnimateButton(hoverColor, originalScale * hoverScale, 1f));
    }

    private IEnumerator AnimateButton(Color targetColor, Vector3 targetScale, float targetGlowAlpha)
    {
        float t = 0;
        
        Color startColor = buttonText != null ? buttonText.color : targetColor;
        Vector3 startScale = transform.localScale;
        float startGlowAlpha = underlineGlow != null ? underlineGlow.color.a : targetGlowAlpha;

        while (t < 1f)
        {
            t += Time.deltaTime * animSpeed;
            float smoothT = Mathf.SmoothStep(0, 1, t); // Bikin transisi lebih elegan

            if (buttonText != null)
                buttonText.color = Color.Lerp(startColor, targetColor, smoothT);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            if (underlineGlow != null)
            {
                Color c = underlineGlow.color;
                c.a = Mathf.Lerp(startGlowAlpha, targetGlowAlpha, smoothT);
                underlineGlow.color = c;
            }

            yield return null;
        }
        
        // Pastikan hasil akhirnya tepat
        if (buttonText != null) buttonText.color = targetColor;
        transform.localScale = targetScale;
        if (underlineGlow != null)
        {
            Color c = underlineGlow.color;
            c.a = targetGlowAlpha;
            underlineGlow.color = c;
        }
    }
}
