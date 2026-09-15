using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Sistem animasi teks cinematic ala God of War Ragnarök "REGION: DISCOVERED".
/// Menampilkan teks aksara Jawa yang di-decode per-karakter menjadi huruf Latin.
/// 
/// Menggunakan 2 TextMeshProUGUI terpisah (2 font berbeda):
/// - aksaraText: font aksara Jawa
/// - latinText: font Latin
/// 
/// Efek tambahan:
/// - Particle burst saat decode
/// - Sound effects (tick, whoosh, reveal)
/// - Garis dekoratif expand
/// - Wave animation per-karakter
/// - Glow pulse saat hold
/// - Vignette darken background
/// - Camera punch saat reveal
/// </summary>
public class RegionDiscoveryUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // References — UI Components
    // ─────────────────────────────────────────────
    [Header("═══ UI REFERENCES ═══")]
    [Tooltip("Label atas (contoh: REGION: DISCOVERED atau WILAYAH: DITEMUKAN)")]
    [SerializeField] private TextMeshProUGUI headerText;

    [Tooltip("TextMeshProUGUI untuk aksara Jawa / Rune")]
    [SerializeField] private TextMeshProUGUI aksaraText;

    [Tooltip("TextMeshProUGUI untuk teks Latin")]
    [SerializeField] private TextMeshProUGUI latinText;

    [Tooltip("TextMeshProUGUI untuk subtitle/deskripsi region")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("CanvasGroup pada root Canvas untuk master fade")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("(Opsional) Image background untuk subtitle")]
    [SerializeField] private Image subtitleBackground;

    // ─────────────────────────────────────────────
    // Vignette / Background Darken
    // ─────────────────────────────────────────────
    [Header("═══ VIGNETTE ═══")]
    [Tooltip("(Opsional) Image fullscreen hitam semi-transparan untuk vignette effect")]
    [SerializeField] private Image vignetteOverlay;

    [Tooltip("Kekuatan vignette (0 = tidak ada, 0.5 = gelap banget)")]
    [SerializeField] private float vignetteStrength = 0.3f;

    // ─────────────────────────────────────────────
    // Particles
    // ─────────────────────────────────────────────
    [Header("═══ PARTICLES ═══")]
    [Tooltip("(Opsional) ParticleSystem untuk efek spark saat decode")]
    [SerializeField] private ParticleSystem decodeParticles;

    [Tooltip("Jumlah partikel per karakter yang di-decode")]
    [SerializeField] private int particlesPerChar = 6;

    // ─────────────────────────────────────────────
    // Sound Effects
    // ─────────────────────────────────────────────
    [Header("═══ SOUND EFFECTS ═══")]
    [Tooltip("(Opsional) AudioSource untuk SFX")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("SFX saat aksara Jawa muncul (whoosh/reveal)")]
    [SerializeField] private AudioClip sfxReveal;

    [Tooltip("SFX tick kecil per karakter decode")]
    [SerializeField] private AudioClip sfxDecodeTick;

    [Tooltip("SFX saat decode selesai (chime/impact)")]
    [SerializeField] private AudioClip sfxDecodeComplete;

    [Tooltip("SFX saat subtitle muncul")]
    [SerializeField] private AudioClip sfxSubtitle;

    [Tooltip("Volume SFX decode tick")]
    [SerializeField] private float decodTickVolume = 0.4f;

    // ─────────────────────────────────────────────
    // Camera Punch
    // ─────────────────────────────────────────────
    [Header("═══ CAMERA PUNCH ═══")]
    [Tooltip("(Opsional) Transform kamera utama untuk camera punch effect")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Kekuatan camera punch saat reveal")]
    [SerializeField] private float cameraPunchStrength = 0.05f;

    [Tooltip("Durasi camera punch")]
    [SerializeField] private float cameraPunchDuration = 0.3f;

    // ─────────────────────────────────────────────
    // Timing
    // ─────────────────────────────────────────────
    [Header("═══ TIMING ═══")]
    [SerializeField] private float javaFadeInDuration = 1.0f;
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float decodeDelayPerChar = 0.10f;
    [SerializeField] private float latinHoldDuration = 3.0f;
    [SerializeField] private float subtitleFadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 1.2f;

    // ─────────────────────────────────────────────
    // Styling (God of War Ragnarök Aesthetic)
    // ─────────────────────────────────────────────
    [Header("═══ STYLING ═══")]
    [SerializeField] private float startSpacing = 30f;
    [SerializeField] private float endSpacing = 12f;
    [SerializeField] private float fadeOutSpacing = 35f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.10f;
    [SerializeField] private Color aksaraTextColor = new Color(0.85f, 0.92f, 1f, 0.8f);
    [SerializeField] private Color latinTextColor = Color.white;
    [SerializeField] private float ghostRuneAlpha = 0.25f;

    // ─────────────────────────────────────────────
    // Scramble Pool
    // ─────────────────────────────────────────────
    [Header("═══ SCRAMBLE ═══")]
    [SerializeField] private string scramblePool = "ꦏꦐꦑꦒꦓꦔꦕꦖꦗꦘꦙꦚꦛꦜꦝꦞꦟꦠꦡꦢꦣꦤꦥꦦꦧꦨꦩꦪꦫꦬꦭꦮꦯꦰꦱꦲ";

    // ─────────────────────────────────────────────
    // Wave Animation
    // ─────────────────────────────────────────────
    [Header("═══ WAVE ANIMATION ═══")]
    [Tooltip("Aktifkan wave animation per-karakter saat hold")]
    [SerializeField] private bool enableWaveAnimation = true;

    [Tooltip("Amplitudo wave (pixel)")]
    [SerializeField] private float waveAmplitude = 3f;

    [Tooltip("Kecepatan wave")]
    [SerializeField] private float waveSpeed = 3f;

    [Tooltip("Offset antar karakter")]
    [SerializeField] private float waveCharOffset = 0.3f;

    // ─────────────────────────────────────────────
    // Glow Pulse
    // ─────────────────────────────────────────────
    [Header("═══ GLOW PULSE ═══")]
    [Tooltip("Aktifkan glow pulse saat hold")]
    [SerializeField] private bool enableGlowPulse = true;

    [Tooltip("Warna glow pulse")]
    [SerializeField] private Color glowPulseColor = new Color(1f, 0.9f, 0.5f, 1f);

    [Tooltip("Kecepatan glow pulse")]
    [SerializeField] private float glowPulseSpeed = 1.5f;

    [Tooltip("Intensitas glow pulse (0-1)")]
    [SerializeField] private float glowPulseIntensity = 0.3f;

    // ─────────────────────────────────────────────
    // Breathing
    // ─────────────────────────────────────────────
    [Header("═══ BREATHING ═══")]
    [SerializeField] private float breathingAmplitude = 0.015f;
    [SerializeField] private float breathingSpeed = 2f;

    // ─────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────
    private Coroutine activeAnimation;
    private Coroutine waveCoroutine;
    private bool isAnimating;
    private RectTransform aksaraTextRect;
    private RectTransform latinTextRect;

    public static RegionDiscoveryUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (aksaraText != null)
            aksaraTextRect = aksaraText.GetComponent<RectTransform>();
        if (latinText != null)
            latinTextRect = latinText.GetComponent<RectTransform>();

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    public void Show(RegionData data)
    {
        if (data == null) return;
        Show(data.headerText, data.aksaraJawaName, data.latinName, data.subtitle);
    }

    public void Show(string aksaraJawa, string latin, string subtitle)
    {
        Show("WILAYAH: DITEMUKAN", aksaraJawa, latin, subtitle);
    }

    public void Show(string header, string aksaraJawa, string latin, string subtitle)
    {
        if (isAnimating)
        {
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        }
        activeAnimation = StartCoroutine(AnimationSequence(header, aksaraJawa, latin, subtitle));
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (headerText != null) { headerText.text = ""; headerText.alpha = 0f; }
        if (aksaraText != null) { aksaraText.text = ""; aksaraText.alpha = 0f; }
        if (latinText != null) { latinText.text = ""; latinText.alpha = 0f; }
        if (subtitleText != null) { subtitleText.text = ""; subtitleText.alpha = 0f; }

        SetSubtitleBackgroundAlpha(0f);
        SetVignetteAlpha(0f);

        isAnimating = false;
    }

    // ─────────────────────────────────────────────
    // Animasi Utama
    // ─────────────────────────────────────────────

    private IEnumerator AnimationSequence(string header, string aksaraJawa, string latin, string subtitle)
    {
        isAnimating = true;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Setup awal
        if (headerText != null)
        {
            headerText.text = header;
            headerText.alpha = 0f;
        }

        aksaraText.text = aksaraJawa;
        aksaraText.color = aksaraTextColor;
        aksaraText.characterSpacing = startSpacing;
        aksaraText.alpha = 0f;

        latinText.text = latin;
        latinText.color = latinTextColor;
        latinText.characterSpacing = startSpacing;
        latinText.alpha = 0f;

        subtitleText.text = "";
        subtitleText.alpha = 0f;
        SetSubtitleBackgroundAlpha(0f);
        SetVignetteAlpha(0f);

        canvasGroup.alpha = 1f;

        // ═══ VIGNETTE FADE IN ═══
        StartCoroutine(AnimateVignette(0f, vignetteStrength, javaFadeInDuration * 0.8f));

        // ═══ CAMERA PUNCH ═══
        if (cameraTransform != null)
            StartCoroutine(CameraPunch());

        // ═══ FASE 1: Header & Aksara Jawa Fade In (Tulisan Kuno Muncul Penuh) ═══
        PlaySFX(sfxReveal);
        yield return StartCoroutine(Phase_AksaraFadeIn(aksaraJawa));

        // ═══ FASE 2: Hold (Pemain Membaca Aksara Kuno) ═══
        yield return StartCoroutine(Phase_Hold());

        // ═══ FASE 3: Translasi Menyapu In-Place dengan Letupan Partikel ═══
        yield return StartCoroutine(Phase_Decode(aksaraJawa, latin));

        // ═══ FASE 4: Latin Hold + Subtitle ═══
        yield return StartCoroutine(Phase_LatinHoldWithSubtitle(subtitle));

        // ═══ FASE 5: Fade Out ═══
        StartCoroutine(AnimateVignette(vignetteStrength, 0f, fadeOutDuration));
        yield return StartCoroutine(Phase_FadeOut());

        HideImmediate();
    }

    // ─────────────────────────────────────────────
    // FASE 1: Aksara Jawa Fade In
    // ─────────────────────────────────────────────
    private IEnumerator Phase_AksaraFadeIn(string aksaraJawa)
    {
        aksaraText.text = aksaraJawa;
        float elapsed = 0f;

        while (elapsed < javaFadeInDuration)
        {
            float t = elapsed / javaFadeInDuration;
            float eased = EaseOutCubic(t);

            if (headerText != null) headerText.alpha = eased;
            aksaraText.alpha = eased;
            aksaraText.characterSpacing = Mathf.Lerp(startSpacing, endSpacing, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (headerText != null) headerText.alpha = 1f;
        aksaraText.alpha = 1f;
        aksaraText.characterSpacing = endSpacing;
    }

    // ─────────────────────────────────────────────
    // FASE 2: Hold (Breathing + Wave + Glow Pulse)
    // ─────────────────────────────────────────────
    private IEnumerator Phase_Hold()
    {
        float elapsed = 0f;
        Vector3 originalScale = aksaraTextRect != null ? aksaraTextRect.localScale : Vector3.one;

        if (enableWaveAnimation)
            waveCoroutine = StartCoroutine(WaveAnimation(aksaraText, holdDuration));

        while (elapsed < holdDuration)
        {
            if (aksaraTextRect != null)
            {
                float breath = 1f + Mathf.Sin(elapsed * breathingSpeed * Mathf.PI * 2f) * breathingAmplitude;
                aksaraTextRect.localScale = originalScale * breath;
            }

            if (enableGlowPulse)
            {
                float pulse = (Mathf.Sin(elapsed * glowPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                Color blended = Color.Lerp(aksaraTextColor, glowPulseColor, pulse * glowPulseIntensity);
                aksaraText.color = blended;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (aksaraTextRect != null)
            aksaraTextRect.localScale = originalScale;
        aksaraText.color = aksaraTextColor;
    }

    // ─────────────────────────────────────────────
    // FASE 3: Translasi Menyapu In-Place (God of War Ragnarök)
    // ─────────────────────────────────────────────
    private IEnumerator Phase_Decode(string aksaraJawa, string latin)
    {
        // Stop wave jika masih jalan
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
            ResetVertexPositions(aksaraText);
        }

        // Setup kedua teks pada posisi yang identik
        aksaraText.text = aksaraJawa;
        aksaraText.alpha = 1f;
        aksaraText.characterSpacing = endSpacing;
        aksaraText.ForceMeshUpdate();

        latinText.text = latin;
        latinText.alpha = 1f;
        latinText.characterSpacing = endSpacing;
        latinText.ForceMeshUpdate();

        // Inisialisasi: semua huruf Latin dimulai dari transparan (alpha = 0)
        SetAllCharacterAlpha(latinText, 0);

        int latinLen = latin.Length;

        // Menyapu per-karakter dari kiri ke kanan
        for (int i = 0; i < latinLen; i++)
        {
            if (latin[i] == ' ')
            {
                continue;
            }

            // 1. SFX tick per-karakter saat transisi
            PlaySFX(sfxDecodeTick, decodTickVolume);

            // 2. Transisi crossfade dissolve murni di tempatnya (tanpa flip / rotasi)
            StartCoroutine(CrossfadeCharacter(i, 0.12f));

            yield return new WaitForSeconds(decodeDelayPerChar);
        }

        // SFX complete
        PlaySFX(sfxDecodeComplete);

        // Camera punch kecil saat decode selesai
        if (cameraTransform != null)
            StartCoroutine(CameraPunch(cameraPunchStrength * 0.5f, cameraPunchDuration * 0.5f));
    }

    // ─────────────────────────────────────────────
    // FASE 4: Latin Hold + Subtitle
    // ─────────────────────────────────────────────
    private IEnumerator Phase_LatinHoldWithSubtitle(string subtitle)
    {
        // Start wave pada Latin text selama hold
        if (enableWaveAnimation)
            waveCoroutine = StartCoroutine(WaveAnimation(latinText, latinHoldDuration + subtitleFadeInDuration));

        if (!string.IsNullOrEmpty(subtitle))
        {
            subtitleText.text = subtitle;
            PlaySFX(sfxSubtitle, 0.6f);
            yield return StartCoroutine(SubtitleSlideIn());
        }

        yield return new WaitForSeconds(latinHoldDuration);

        // Stop wave
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
            ResetVertexPositions(latinText);
        }
    }

    private IEnumerator SubtitleSlideIn()
    {
        RectTransform subtitleRect = subtitleText.GetComponent<RectTransform>();
        Vector2 originalPos = subtitleRect.anchoredPosition;
        Vector2 startPos = originalPos + new Vector2(0, -30f);
        float elapsed = 0f;

        while (elapsed < subtitleFadeInDuration)
        {
            float t = elapsed / subtitleFadeInDuration;
            float eased = EaseOutCubic(t);

            subtitleText.alpha = eased;
            SetSubtitleBackgroundAlpha(eased * 0.6f);
            subtitleRect.anchoredPosition = Vector2.Lerp(startPos, originalPos, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        subtitleText.alpha = 1f;
        SetSubtitleBackgroundAlpha(0.6f);
        subtitleRect.anchoredPosition = originalPos;
    }

    // ─────────────────────────────────────────────
    // FASE 5: Fade Out
    // ─────────────────────────────────────────────
    private IEnumerator Phase_FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            float t = elapsed / fadeOutDuration;
            float eased = EaseInCubic(t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            latinText.characterSpacing = Mathf.Lerp(endSpacing, fadeOutSpacing, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    // ─────────────────────────────────────────────
    // EFEK: Wave Animation Per-Karakter
    // ─────────────────────────────────────────────

    /// <summary>
    /// Animasi gelombang halus — setiap karakter naik-turun dengan offset.
    /// Menggunakan TMP vertex position manipulation.
    /// </summary>
    private IEnumerator WaveAnimation(TextMeshProUGUI tmpText, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            tmpText.ForceMeshUpdate();
            TMP_TextInfo textInfo = tmpText.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int meshIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

                // Offset vertikal bergelombang
                float wave = Mathf.Sin((elapsed * waveSpeed) + (i * waveCharOffset)) * waveAmplitude;
                Vector3 offset = new Vector3(0, wave, 0);

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            // Apply perubahan vertex
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetVertexPositions(tmpText);
    }

    /// <summary>
    /// Reset vertex positions ke default.
    /// </summary>
    private void ResetVertexPositions(TextMeshProUGUI tmpText)
    {
        if (tmpText == null) return;
        tmpText.ForceMeshUpdate();
    }

    // ─────────────────────────────────────────────
    // EFEK: Camera Punch
    // ─────────────────────────────────────────────

    private IEnumerator CameraPunch()
    {
        yield return StartCoroutine(CameraPunch(cameraPunchStrength, cameraPunchDuration));
    }

    private IEnumerator CameraPunch(float strength, float duration)
    {
        if (cameraTransform == null) yield break;

        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float decay = 1f - EaseOutCubic(t);

            // Random shake direction yang decay
            Vector3 shake = new Vector3(
                Random.Range(-1f, 1f) * strength * decay,
                Random.Range(-1f, 1f) * strength * decay,
                0f
            );

            cameraTransform.localPosition = originalPos + shake;
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }

    // ─────────────────────────────────────────────
    // EFEK: Particle Burst di Posisi Karakter
    // ─────────────────────────────────────────────

    private void EmitParticlesAtChar(TextMeshProUGUI tmpText, int charIndex)
    {
        if (decodeParticles == null) return;

        TMP_TextInfo textInfo = tmpText.textInfo;
        if (charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        // Dapatkan posisi tengah karakter di world space
        int vertexIndex = charInfo.vertexIndex;
        int meshIndex = charInfo.materialReferenceIndex;
        Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

        Vector3 charCenter = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;
        Vector3 worldPos = tmpText.transform.TransformPoint(charCenter);

        // Pindah particle system ke posisi karakter dan emit
        decodeParticles.transform.position = worldPos;
        decodeParticles.Emit(particlesPerChar);
    }

    // ─────────────────────────────────────────────
    // EFEK: Vignette
    // ─────────────────────────────────────────────

    private IEnumerator AnimateVignette(float fromAlpha, float toAlpha, float duration)
    {
        if (vignetteOverlay == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = (toAlpha > fromAlpha) ? EaseOutCubic(t) : EaseInCubic(t);
            SetVignetteAlpha(Mathf.Lerp(fromAlpha, toAlpha, eased));

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetVignetteAlpha(toAlpha);
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteOverlay == null) return;
        var c = vignetteOverlay.color;
        c.a = alpha;
        vignetteOverlay.color = c;
    }

    // ─────────────────────────────────────────────
    // Per-Character Color Effects
    // ─────────────────────────────────────────────

    private void TintCharacter(TextMeshProUGUI tmpText, int charIndex, Color32 color)
    {
        TMP_TextInfo textInfo = tmpText.textInfo;
        if (charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int meshIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] vertexColors = textInfo.meshInfo[meshIndex].colors32;
        vertexColors[vertexIndex + 0] = color;
        vertexColors[vertexIndex + 1] = color;
        vertexColors[vertexIndex + 2] = color;
        vertexColors[vertexIndex + 3] = color;

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// Crossfade dissolve halus per-karakter di posisinya (tanpa membalik / rotasi 3D).
    /// </summary>
    private IEnumerator CrossfadeCharacter(int charIndex, float duration)
    {
        float elapsed = 0f;
        byte ghostByte = (byte)(ghostRuneAlpha * 255);

        StartCoroutine(FlashCharacter(latinText, charIndex));

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            byte lAlpha = (byte)Mathf.Lerp(0, 255, t);
            byte aAlpha = (byte)Mathf.Lerp(255, ghostByte, t);

            SetCharacterAlpha(latinText, charIndex, lAlpha);
            if (charIndex < aksaraText.textInfo.characterCount)
            {
                SetCharacterAlpha(aksaraText, charIndex, aAlpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetCharacterAlpha(latinText, charIndex, 255);
        if (charIndex < aksaraText.textInfo.characterCount)
        {
            SetCharacterAlpha(aksaraText, charIndex, ghostByte);
        }
    }

    private IEnumerator FlashCharacter(TextMeshProUGUI tmpText, int charIndex)
    {
        TintCharacter(tmpText, charIndex, flashColor);

        float elapsed = 0f;
        Color32 startColor = flashColor;
        Color32 endColor = latinTextColor;

        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            TintCharacter(tmpText, charIndex, Color32.Lerp(startColor, endColor, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        TintCharacter(tmpText, charIndex, endColor);
    }

    // ─────────────────────────────────────────────
    // Sound Effects
    // ─────────────────────────────────────────────

    private void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void SetSubtitleBackgroundAlpha(float alpha)
    {
        if (subtitleBackground == null) return;
        var c = subtitleBackground.color;
        c.a = alpha;
        subtitleBackground.color = c;
    }

    private Vector3 GetCharacterWorldPosition(TextMeshProUGUI tmpText, int charIndex)
    {
        if (tmpText == null) return Vector3.zero;
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;
        if (charIndex >= textInfo.characterCount) return tmpText.transform.position;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return tmpText.transform.position;

        int vertexIndex = charInfo.vertexIndex;
        int meshIndex = charInfo.materialReferenceIndex;
        Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

        Vector3 charCenter = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;
        return tmpText.transform.TransformPoint(charCenter);
    }

    private void SetCharacterAlpha(TextMeshProUGUI tmpText, int charIndex, byte alpha)
    {
        if (tmpText == null) return;
        TMP_TextInfo textInfo = tmpText.textInfo;
        if (charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int meshIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;
        Color32[] vertexColors = textInfo.meshInfo[meshIndex].colors32;

        vertexColors[vertexIndex + 0].a = alpha;
        vertexColors[vertexIndex + 1].a = alpha;
        vertexColors[vertexIndex + 2].a = alpha;
        vertexColors[vertexIndex + 3].a = alpha;

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetAllCharacterAlpha(TextMeshProUGUI tmpText, byte alpha)
    {
        if (tmpText == null) return;
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[meshIndex].colors32;

            vertexColors[vertexIndex + 0].a = alpha;
            vertexColors[vertexIndex + 1].a = alpha;
            vertexColors[vertexIndex + 2].a = alpha;
            vertexColors[vertexIndex + 3].a = alpha;
        }

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t) => t * t * t;
}
