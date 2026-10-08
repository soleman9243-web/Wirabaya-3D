using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wirabaya.UI
{
    /// <summary>
    /// Pengontrol Menu Settings & Pause kustom sesuai desain pengguna:
    /// - Mencegah tabrakan UI (Anti-Collision): Otomatis menyembunyikan HUD gameplay (NonDialogue, Quest, Minigame bar)
    ///   saat pause dibuka, dan mengembalikannya saat resume.
    /// - Sibling Order: Otomatis memanggil transform.SetAsLastSibling() agar Settings selalu dirender paling atas.
    /// - Timing & Input Protection: Mencegah tabrakan input tombol ESC dengan Minigame Tebang Pohon dan Dialog.
    /// - Dual Input System: Mendukung New Input System & Legacy Input.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SettingsMenuController : MonoBehaviour
    {
        public static SettingsMenuController Instance { get; private set; }
        public static float MouseSensitivity = 1f;

        [Header("=== KEY BINDING ===")]
        [Tooltip("Tombol keyboard alternatif untuk membuka/menutup menu.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        [Header("=== HUD YANG DISEMBUNYIKAN SAAT PAUSE (ANTI-TABRAKAN) ===")]
        [Tooltip("Objek HUD gameplay (misal: 'NonDialogue') yang akan disembunyikan saat pause agar tidak bertabrakan dengan menu. Kosongkan untuk auto-detect.")]
        [SerializeField] private GameObject hudToHide;
        [Tooltip("Daftar objek tambahan yang ingin disembunyikan saat pause.")]
        [SerializeField] private GameObject[] additionalHudElementsToHide;

        [Header("=== TAB BUTTONS ===")]
        [Tooltip("Tombol tab 'controls' di kanan atas")]
        [SerializeField] private Button buttonControls;
        [Tooltip("Tombol tab 'Audio' di kanan atas")]
        [SerializeField] private Button buttonAudio;

        [Header("=== TAB PARENT PANELS ===")]
        [Tooltip("Parent GameObject untuk pengaturan Controls & Screen (contoh: 'scren and loud')")]
        [SerializeField] private GameObject controlsParent;
        [Tooltip("Parent GameObject untuk pengaturan Suara/Audio (contoh: 'Button' / Audio Parent)")]
        [SerializeField] private GameObject audioParent;

        [Header("=== ANIMASI PANEL HIJAU (SLIDE-IN) ===")]
        [Tooltip("RectTransform dari objek warna hijau (contoh: 'Image' di sebelah kiri)")]
        [SerializeField] private RectTransform greenPanel;
        [Tooltip("Durasi animasi slide masuk dalam detik")]
        [SerializeField] private float slideDuration = 0.35f;
        [Tooltip("Jarak slide dari kiri layar (offset X)")]
        [SerializeField] private float slideDistance = 600f;

        [Header("=== BACKGROUND BURAM / BACKDROP ===")]
        [Tooltip("Objek background buram di belakang menu (contoh: 'Background')")]
        [SerializeField] private GameObject backgroundBlur;
        [Tooltip("Opsional: CanvasGroup pada background untuk fade-in halus")]
        [SerializeField] private CanvasGroup backgroundCanvasGroup;

        [Header("=== TOMBOL QUIT ===")]
        [SerializeField] private Button buttonQuit;
        [Tooltip("Jika dicentang, tombol Quit hanya menutup menu. Jika tidak, akan pindah scene atau keluar dari game.")]
        [SerializeField] private bool quitOnlyClosesMenu = false;
        [Tooltip("Nama scene tujuan saat tombol Quit diklik (contoh: 'MainMenu'). Jika dikosongkan, akan keluar dari game (Quit).")]
        [SerializeField] private string nextSceneName = "";

        [Header("=== SLIDERS: AUDIO TAB ===")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("=== CONTROLS & SCREEN TAB ===")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Dropdown screenModeDropdown;
        [SerializeField] private TMP_Dropdown motionBlurDropdown;

        // Internal State
        private CanvasGroup canvasGroup;
        private bool isMenuOpen = false;
        private Vector2 greenPanelOriginalPos;
        private bool hasRecordedOriginalPos = false;
        private Coroutine slideCoroutine;

        // State recorder untuk HUD agar dikembalikan saat resume
        private bool wasHudActiveBeforePause = false;
        private List<GameObject> activeAdditionalHudElements = new List<GameObject>();

        // PlayerPrefs Keys
        private const string KEY_MASTER_VOL = "Setting_MasterVol";
        private const string KEY_BGM_VOL = "Setting_BGMVol";
        private const string KEY_SFX_VOL = "Setting_SFXVol";
        private const string KEY_SENSITIVITY = "Setting_Sensitivity";
        private const string KEY_SCREEN_MODE = "Setting_ScreenMode";
        private const string KEY_MOTION_BLUR = "Setting_MotionBlur";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (greenPanel != null && !hasRecordedOriginalPos)
            {
                greenPanelOriginalPos = greenPanel.anchoredPosition;
                hasRecordedOriginalPos = true;
            }

            // Auto-detect HUD gameplay (NonDialogue) jika slot di Inspector kosong
            AutoDetectHudElements();

            // Muat konfigurasi tersimpan
            LoadSavedSettings();
        }

        private void AutoDetectHudElements()
        {
            if (hudToHide == null)
            {
                // Cari di parent Canvas
                if (transform.parent != null)
                {
                    Transform nonDiag = transform.parent.Find("NonDialogue");
                    if (nonDiag != null) hudToHide = nonDiag.gameObject;
                }

                // Fallback cari di scene
                if (hudToHide == null)
                {
                    GameObject found = GameObject.Find("NonDialogue");
                    if (found != null) hudToHide = found;
                }
            }

            // Cek jika ada bilah TreeMinigame yang tertinggal menyala di awal scene
            GameObject treeMinigame = GameObject.Find("TreeMinigame");
            if (treeMinigame != null)
            {
                // Jika tidak sedang memainkan minigame, pastikan bilah minigame mati di awal
                if (TreeCuttingMinigame.Instance == null || !TreeCuttingMinigame.Instance.IsPlaying)
                {
                    treeMinigame.SetActive(false);
                }
            }
        }

        private void Start()
        {
            // Daftarkan listener tombol tab
            if (buttonControls != null) buttonControls.onClick.AddListener(ShowControlsTab);
            if (buttonAudio != null) buttonAudio.onClick.AddListener(ShowAudioTab);
            if (buttonQuit != null) buttonQuit.onClick.AddListener(OnQuitClicked);

            // Daftarkan listener slider & dropdown
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            if (screenModeDropdown != null) screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
            if (motionBlurDropdown != null) motionBlurDropdown.onValueChanged.AddListener(SetMotionBlurQuality);

            // Buka tab default (Controls)
            ShowControlsTab();

            // Sembunyikan menu secara visual di awal
            SetMenuVisualState(false);
        }

        private void Update()
        {
            bool isEscapePressed = false;

            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isEscapePressed = true;
            }
            #endif

            try
            {
                if (!isEscapePressed && Input.GetKeyDown(toggleKey))
                {
                    isEscapePressed = true;
                }
            }
            catch { }

            if (isEscapePressed)
            {
                // ANTI-TABRAKAN TIMING:
                // Jika pemain sedang berada di dalam TreeCuttingMinigame dan menekan ESC,
                // biarkan TreeCuttingMinigame yang memproses ESC (batal menebang), JANGAN buka menu pause di frame yang sama!
                if (!isMenuOpen && TreeCuttingMinigame.Instance != null && TreeCuttingMinigame.Instance.IsPlaying)
                {
                    return;
                }

                ToggleMenu();
            }
        }

        // ==========================================
        // BUKA / TUTUP MENU & PAUSE LOGIC
        // ==========================================
        public void ToggleMenu()
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            isMenuOpen = true;

            // 1. Pindahkan SETTINGS ke paling depan di Canvas agar tidak tertutup objek lain
            transform.SetAsLastSibling();

            // 2. ANTI-TABRAKAN UI: Sembunyikan HUD Gameplay (Quest Text, HP/Mana/Stamina bar)
            HideGameplayHUD();

            // 3. Tampilkan visual menu
            SetMenuVisualState(true);

            // 4. Pause Game
            Time.timeScale = 0f;

            // 5. Buka Kursor Mouse
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 6. Nonaktifkan input look kamera pemain
            var inputs = FindAnyObjectByType<StarterAssets.StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorInputForLook = false;
                inputs.cursorLocked = false;
            }

            // 7. Aktifkan Background Buram
            if (backgroundBlur != null)
            {
                backgroundBlur.SetActive(true);
                if (backgroundCanvasGroup != null)
                {
                    StopAllCoroutines();
                    StartCoroutine(FadeCanvasGroup(backgroundCanvasGroup, 0f, 1f, 0.2f));
                }
            }

            // 8. Jalankan Animasi Slide Masuk Panel Hijau
            if (greenPanel != null)
            {
                if (!hasRecordedOriginalPos)
                {
                    greenPanelOriginalPos = greenPanel.anchoredPosition;
                    hasRecordedOriginalPos = true;
                }

                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(SlidePanelIn());
            }

            // Default buka tab controls
            ShowControlsTab();
        }

        public void CloseMenu()
        {
            isMenuOpen = false;

            // 1. Resume Game
            Time.timeScale = 1f;

            // 2. Kunci kembali Kursor Mouse
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 3. Aktifkan kembali input player
            var inputs = FindAnyObjectByType<StarterAssets.StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorInputForLook = true;
                inputs.cursorLocked = true;
            }

            // 4. ANTI-TABRAKAN UI: Kembalikan HUD Gameplay (Quest Text, HP/Mana/Stamina bar)
            RestoreGameplayHUD();

            // 5. Animasi Slide Keluar Panel Hijau lalu sembunyikan menu
            if (greenPanel != null && gameObject.activeInHierarchy)
            {
                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(SlidePanelOut(() =>
                {
                    SetMenuVisualState(false);
                    if (backgroundBlur != null) backgroundBlur.SetActive(false);
                }));
            }
            else
            {
                SetMenuVisualState(false);
                if (backgroundBlur != null) backgroundBlur.SetActive(false);
            }
        }

        private void HideGameplayHUD()
        {
            if (hudToHide == null) AutoDetectHudElements();

            if (hudToHide != null && hudToHide.activeSelf)
            {
                wasHudActiveBeforePause = true;
                hudToHide.SetActive(false);
            }

            activeAdditionalHudElements.Clear();
            if (additionalHudElementsToHide != null)
            {
                foreach (var el in additionalHudElementsToHide)
                {
                    if (el != null && el.activeSelf)
                    {
                        activeAdditionalHudElements.Add(el);
                        el.SetActive(false);
                    }
                }
            }

            // Sembunyikan bilah TreeMinigame jika sempat muncul
            GameObject treeMinigame = GameObject.Find("TreeMinigame");
            if (treeMinigame != null && treeMinigame.activeSelf)
            {
                treeMinigame.SetActive(false);
            }
        }

        private void RestoreGameplayHUD()
        {
            if (hudToHide != null && wasHudActiveBeforePause)
            {
                hudToHide.SetActive(true);
                wasHudActiveBeforePause = false;
            }

            if (activeAdditionalHudElements != null)
            {
                foreach (var el in activeAdditionalHudElements)
                {
                    if (el != null) el.SetActive(true);
                }
                activeAdditionalHudElements.Clear();
            }
        }

        private void SetMenuVisualState(bool visible)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        // ==========================================
        // TAB SWITCHING (Controls vs Audio)
        // ==========================================
        public void ShowControlsTab()
        {
            if (controlsParent != null) controlsParent.SetActive(true);
            if (audioParent != null) audioParent.SetActive(false);
        }

        public void ShowAudioTab()
        {
            if (audioParent != null) audioParent.SetActive(true);
            if (controlsParent != null) controlsParent.SetActive(false);
        }

        // ==========================================
        // ANIMASI SLIDE MASUK / KELUAR (UNSCALED TIME)
        // ==========================================
        private IEnumerator SlidePanelIn()
        {
            Vector2 startPos = greenPanelOriginalPos - new Vector2(slideDistance, 0f);
            greenPanel.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                greenPanel.anchoredPosition = Vector2.Lerp(startPos, greenPanelOriginalPos, smoothT);
                yield return null;
            }

            greenPanel.anchoredPosition = greenPanelOriginalPos;
        }

        private IEnumerator SlidePanelOut(System.Action onComplete)
        {
            Vector2 targetPos = greenPanelOriginalPos - new Vector2(slideDistance, 0f);
            Vector2 startPos = greenPanel.anchoredPosition;

            float elapsed = 0f;
            float duration = slideDuration * 0.7f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t;
                greenPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                yield return null;
            }

            greenPanel.anchoredPosition = targetPos;
            onComplete?.Invoke();
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            cg.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            cg.alpha = to;
        }

        // ==========================================
        // PENGATURAN LOGIC & PLAYERPREFS
        // ==========================================
        public void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, value);
            PlayerPrefs.Save();
        }

        public void SetBGMVolume(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
            PlayerPrefs.SetFloat(KEY_BGM_VOL, value);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            PlayerPrefs.SetFloat(KEY_SFX_VOL, value);
            PlayerPrefs.Save();
        }

        public void SetSensitivity(float value)
        {
            MouseSensitivity = Mathf.Max(0.1f, value);
            PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);
            PlayerPrefs.Save();
        }

        public void SetScreenMode(int index)
        {
            switch (index)
            {
                case 0:
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    break;
                case 1:
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    break;
                case 2:
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    break;
            }
            PlayerPrefs.SetInt(KEY_SCREEN_MODE, index);
            PlayerPrefs.Save();
        }

        public void SetMotionBlurQuality(int index)
        {
            PlayerPrefs.SetInt(KEY_MOTION_BLUR, index);
            PlayerPrefs.Save();
        }

        private void LoadSavedSettings()
        {
            float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
            float bgmVol = PlayerPrefs.GetFloat(KEY_BGM_VOL, 0.8f);
            float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);
            float sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 1f);
            int screenMode = PlayerPrefs.GetInt(KEY_SCREEN_MODE, 0);
            int motionBlur = PlayerPrefs.GetInt(KEY_MOTION_BLUR, 2);

            AudioListener.volume = masterVol;
            MouseSensitivity = sensitivity;
            SetScreenMode(screenMode);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(bgmVol);
                AudioManager.Instance.SetSFXVolume(sfxVol);
            }

            if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
            if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVol;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
            if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;
            if (screenModeDropdown != null) screenModeDropdown.value = screenMode;
            if (motionBlurDropdown != null) motionBlurDropdown.value = motionBlur;
        }

        public void OnQuitClicked()
        {
            if (quitOnlyClosesMenu)
            {
                CloseMenu();
                return;
            }

            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.ChangeScene(nextSceneName);
                }
                else
                {
                    SceneManager.LoadScene(nextSceneName);
                }
                return;
            }

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
