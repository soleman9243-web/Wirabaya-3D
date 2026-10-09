using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wirabaya.UI
{
    /// <summary>
    /// Pengontrol Menu Settings & Pause kustom sesuai desain asli pengguna:
    /// - Menggunakan visual, tata letak, posisi, dan tombol asli dari scene (tanpa mengubah desain/style pengguna).
    /// - Sistem Pause & Resume Stabil: ESC membuka dan menutup menu pause.
    /// - Freeze Pemain & Kamera: Mengunci input kamera dan gerakan karakter selama menu terbuka.
    /// - Anti-Tabrakan HUD Gameplay: Otomatis menyembunyikan NonDialogue, QuestUI, dan status bar saat pause, dan mengembalikannya saat resume.
    /// - Input Sensitivitas Maksimal 100%: Terhubung dua arah dengan TMP_InputField & Slider (1% - 100%).
    /// - Text Persentase Volume: Menampilkan persentase suara (0% - 100%) untuk Master, BGM, dan SFX.
    /// - Dropdown Tingkat Atas (Sorting Order Mandiri): Dropdown list selalu muncul menutupi UI di bawahnya, dan menutup otomatis jika klik sekitar / klik elemen lain.
    /// - Animasi Elemen UI Halus: Tombol/kontrol UI fade-out saat menu ditutup dan fade-in saat dibuka, berdampingan dengan animasi panel hijau.
    /// - Efek Blur Latar Belakang 3D (URP Depth of Field): Mengaburkan karakter pemain dan dunia 3D saat menu pause terbuka.
    /// - Dual Input System: Mendukung New Input System & Legacy Input.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SettingsMenuController : MonoBehaviour
    {
        public static SettingsMenuController Instance { get; private set; }
        public static float MouseSensitivity = 1f;

        public bool IsMenuOpen => isMenuOpen;
        public static bool IsOpen => Instance != null && Instance.isMenuOpen;

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
        [Tooltip("Parent GameObject untuk pengaturan Controls & Screen (contoh: 'scren and loud' / 'cntrl')")]
        [SerializeField] private GameObject controlsParent;
        [Tooltip("Parent GameObject untuk pengaturan Suara/Audio (contoh: 'Button' / 'empt' / Audio Parent)")]
        [SerializeField] private GameObject audioParent;

        [Header("=== ANIMASI PANEL HIJAU (SLIDE-IN) ===")]
        [Tooltip("RectTransform dari objek warna hijau (contoh: 'Image' di sebelah kiri)")]
        [SerializeField] private RectTransform greenPanel;
        [Tooltip("Durasi animasi slide masuk dalam detik")]
        [SerializeField] private float slideDuration = 0.35f;
        [Tooltip("Jarak slide dari kiri layar (offset X)")]
        [SerializeField] private float slideDistance = 600f;

        [Header("=== ANIMASI FADE-OUT TOMBOL UI ===")]
        [Tooltip("Durasi fade-out tombol/kontrol UI saat menu ditutup")]
        [SerializeField] private float uiButtonsFadeDuration = 0.2f;

        [Header("=== CAMERA / WORLD BLUR (URP POST-PROCESSING) ===")]
        [Tooltip("Volume global khusus blur pause. Jika kosong, akan otomatis dibuat saat runtime.")]
        [SerializeField] private Volume pauseBlurVolume;
        [Tooltip("Apakah mengaktifkan efek blur kamera URP saat menu pause terbuka.")]
        [SerializeField] private bool enableWorldBlur = true;
        [Tooltip("Intensitas blur maksimal (Gaussian Max Radius).")]
        [SerializeField] private float blurMaxRadius = 3.5f;

        [Header("=== BACKGROUND BURAM / BACKDROP ===")]
        [Tooltip("Objek background buram di belakang menu (contoh: 'Image' / 'Background')")]
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

        [Header("=== TEXT PERSENTASE AUDIO TAB (OPSIONAL / AUTO-DETECT) ===")]
        [Tooltip("Text untuk persentase Master Volume (contoh: '100%'). Kosongkan untuk auto-detect / auto-create.")]
        [SerializeField] private TextMeshProUGUI masterVolumePercentText;
        [Tooltip("Text untuk persentase BGM Volume. Kosongkan untuk auto-detect / auto-create.")]
        [SerializeField] private TextMeshProUGUI bgmVolumePercentText;
        [Tooltip("Text untuk persentase SFX Volume. Kosongkan untuk auto-detect / auto-create.")]
        [SerializeField] private TextMeshProUGUI sfxVolumePercentText;

        [Header("=== CONTROLS & SCREEN TAB ===")]
        [SerializeField] private Slider sensitivitySlider;
        [Tooltip("Input Field untuk angka persentase Sensitivitas (Maksimal 100%). Kosongkan untuk auto-detect.")]
        [SerializeField] private TMP_InputField sensitivityInputField;
        [SerializeField] private TMP_Dropdown screenModeDropdown;
        [SerializeField] private TMP_Dropdown motionBlurDropdown;

        // Internal State
        private CanvasGroup canvasGroup;
        private Canvas menuCanvas;
        private GraphicRaycaster menuRaycaster;
        private bool isMenuOpen = false;
        private bool isClosing = false;
        private bool isTransitioning = false;
        private Vector2 greenPanelOriginalPos;
        private bool hasRecordedOriginalPos = false;
        private Coroutine menuAnimationCoroutine;
        private bool isUpdatingSensitivityUI = false;

        private List<CanvasGroup> uiButtonCanvasGroups = new List<CanvasGroup>();
        private CanvasGroup backgroundBlurCanvasGroup;

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

            try
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                // Pastikan Canvas dan GraphicRaycaster mandiri terpasang agar klik mouse selalu diterima
                EnsureCanvasAndRaycaster();

                // Auto-detect greenPanel (Logo) jika belum di-assign di Inspector
                if (greenPanel == null)
                {
                    Transform logoTr = transform.Find("Background/Logo");
                    if (logoTr == null) logoTr = transform.Find("Logo");
                    if (logoTr == null)
                    {
                        foreach (Transform t in GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == "Logo") { logoTr = t; break; }
                        }
                    }
                    if (logoTr != null) greenPanel = logoTr.GetComponent<RectTransform>();
                }

                // Auto-detect backgroundBlur (Image) jika belum di-assign di Inspector
                if (backgroundBlur == null)
                {
                    Transform imgTr = transform.Find("Image");
                    if (imgTr == null)
                    {
                        foreach (Transform t in GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == "Image" && t.parent == transform) { imgTr = t; break; }
                        }
                    }
                    if (imgTr != null) backgroundBlur = imgTr.gameObject;
                }

                if (backgroundBlur != null)
                {
                    backgroundBlurCanvasGroup = backgroundBlur.GetComponent<CanvasGroup>();
                    if (backgroundBlurCanvasGroup == null)
                    {
                        backgroundBlurCanvasGroup = backgroundBlur.AddComponent<CanvasGroup>();
                    }

                    // Pastikan background backdrop membentang penuh di layar
                    RectTransform bgRt = backgroundBlur.GetComponent<RectTransform>();
                    if (bgRt != null)
                    {
                        bgRt.anchorMin = Vector2.zero;
                        bgRt.anchorMax = Vector2.one;
                        bgRt.offsetMin = Vector2.zero;
                        bgRt.offsetMax = Vector2.zero;
                    }
                }

                if (greenPanel != null && !hasRecordedOriginalPos)
                {
                    greenPanelOriginalPos = greenPanel.anchoredPosition;
                    if (greenPanelOriginalPos == Vector2.zero)
                    {
                        greenPanelOriginalPos = new Vector2(-251.74f, -428f);
                    }
                    hasRecordedOriginalPos = true;
                }

                // Setup pilihan dropdown riil dan tata letak sorting order
                SetupDropdownOptions();
                SetupDropdownTemplates();

                // Setup Volume blur kamera URP jika diaktifkan
                if (enableWorldBlur)
                {
                    EnsureBlurVolume();
                }

                // Auto-detect InputField dan Text persentase jika belum di-assign di Inspector
                AutoSetupPercentageTexts();

                // Kumpulkan semua CanvasGroup tombol/kontrol UI untuk animasi fade
                CollectUIButtonCanvasGroups();

                // Auto-detect HUD gameplay (NonDialogue) jika slot di Inspector kosong
                AutoDetectHudElements();

                // Muat konfigurasi tersimpan
                LoadSavedSettings();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SettingsMenuController] Awake warning: {ex.Message}");
            }
            finally
            {
                // Sembunyikan menu secara visual di awal sejak Awake
                SetMenuVisualState(false);
            }
        }

        private void EnsureCanvasAndRaycaster()
        {
            menuCanvas = GetComponent<Canvas>();
            if (menuCanvas == null)
            {
                menuCanvas = gameObject.AddComponent<Canvas>();
            }
            menuCanvas.overrideSorting = true;
            menuCanvas.sortingOrder = 30000;

            menuRaycaster = GetComponent<GraphicRaycaster>();
            if (menuRaycaster == null)
            {
                menuRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
            menuRaycaster.enabled = true;
        }

        private void EnsureEventSystemActive()
        {
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem_Fallback");
                var es = esObj.AddComponent<EventSystem>();
                #if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                esObj.AddComponent<StandaloneInputModule>();
                #endif
                es.UpdateModules();
            }
            else
            {
                if (!EventSystem.current.gameObject.activeInHierarchy)
                {
                    EventSystem.current.gameObject.SetActive(true);
                }
                EventSystem.current.enabled = true;
            }
        }

        private void SetupDropdownOptions()
        {
            if (screenModeDropdown != null && screenModeDropdown.options.Count <= 1)
            {
                screenModeDropdown.ClearOptions();
                screenModeDropdown.AddOptions(new List<string> { "Fullscreen", "Borderless Window", "Windowed" });
            }

            if (motionBlurDropdown != null && motionBlurDropdown.options.Count <= 1)
            {
                motionBlurDropdown.ClearOptions();
                motionBlurDropdown.AddOptions(new List<string> { "Off", "Low", "Medium", "High" });
            }
        }

        /// <summary>
        /// Mengkonfigurasi Template dropdown agar Dropdown List memiliki Canvas dengan sortingOrder = 32000.
        /// Ini memastikan dropdown list selalu dirender di atas elemen UI lain (seperti slider atau tombol),
        /// serta blocker yang dibuat otomatis berada di sortingOrder 31999 dan menangkap klik di luar/UI lain.
        /// </summary>
        private void SetupDropdownTemplates()
        {
            SetupSingleDropdown(screenModeDropdown);
            SetupSingleDropdown(motionBlurDropdown);
        }

        private void SetupSingleDropdown(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;

            if (dropdown.template != null)
            {
                Canvas templateCanvas = dropdown.template.GetComponent<Canvas>();
                if (templateCanvas == null)
                {
                    templateCanvas = dropdown.template.gameObject.AddComponent<Canvas>();
                }
                templateCanvas.overrideSorting = true;
                templateCanvas.sortingOrder = 32000;

                GraphicRaycaster gr = dropdown.template.GetComponent<GraphicRaycaster>();
                if (gr == null)
                {
                    dropdown.template.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            EventTrigger trigger = dropdown.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = dropdown.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            entry.callback.AddListener((data) =>
            {
                if (dropdown == screenModeDropdown && motionBlurDropdown != null)
                {
                    motionBlurDropdown.Hide();
                }
                else if (dropdown == motionBlurDropdown && screenModeDropdown != null)
                {
                    screenModeDropdown.Hide();
                }
            });
            trigger.triggers.Add(entry);
        }

        public void CloseAllDropdowns()
        {
            if (screenModeDropdown != null) screenModeDropdown.Hide();
            if (motionBlurDropdown != null) motionBlurDropdown.Hide();
        }

        private bool IsDropdownOpen(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return false;
            Transform list = dropdown.transform.Find("Dropdown List");
            if (list != null && list.gameObject.activeInHierarchy) return true;
            if (dropdown.transform.parent != null)
            {
                Transform pList = dropdown.transform.parent.Find("Dropdown List");
                if (pList != null && pList.gameObject.activeInHierarchy) return true;
            }
            return false;
        }

        private bool IsAnyDropdownOpen()
        {
            return IsDropdownOpen(screenModeDropdown) || IsDropdownOpen(motionBlurDropdown);
        }

        private bool IsClickOutsideDropdowns()
        {
            var es = EventSystem.current;
            if (es == null) return true;

            Vector2 mousePos = Vector2.zero;
            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) mousePos = Mouse.current.position.ReadValue();
            else
            #endif
            mousePos = Input.mousePosition;

            var pe = new PointerEventData(es) { position = mousePos };
            var results = new List<RaycastResult>();
            es.RaycastAll(pe, results);

            if (results.Count == 0) return true;

            foreach (var r in results)
            {
                if (r.gameObject == null) continue;

                // Jika klik mengenai Blocker, itu artinya klik sengaja di luar dropdown list
                if (r.gameObject.name == "Blocker") return true;

                // Jika klik mengenai bagian dalam list dropdown aktif, jangan tutup
                if (r.gameObject.name.Contains("Item") || 
                    r.gameObject.name == "Dropdown List" ||
                    r.gameObject.name == "Viewport" || 
                    r.gameObject.name == "Content" ||
                    r.gameObject.name == "Scrollbar")
                {
                    return false;
                }

                if (screenModeDropdown != null)
                {
                    Transform sList = screenModeDropdown.transform.Find("Dropdown List");
                    if (sList != null && r.gameObject.transform.IsChildOf(sList)) return false;
                }
                if (motionBlurDropdown != null)
                {
                    Transform mList = motionBlurDropdown.transform.Find("Dropdown List");
                    if (mList != null && r.gameObject.transform.IsChildOf(mList)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Mengumpulkan semua objek UI (tombol tab, kontrol, slider, tombol quit) selain panel background dan green panel
        /// untuk diberi animasi fade-out saat menutup menu.
        /// </summary>
        private void CollectUIButtonCanvasGroups()
        {
            uiButtonCanvasGroups.Clear();

            Transform[] directChildren = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                directChildren[i] = transform.GetChild(i);
            }

            foreach (var child in directChildren)
            {
                if (child == null) continue;

                // Abaikan background backdrop
                if (backgroundBlur != null && child.gameObject == backgroundBlur) continue;
                if (child.name == "Image") continue;

                // Abaikan background hijau
                if (greenPanel != null && (child == greenPanel || child == greenPanel.parent)) continue;
                if (child.name == "Background") continue;

                // Daftarkan sebagai elemen UI
                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = child.gameObject.AddComponent<CanvasGroup>();
                }
                if (!uiButtonCanvasGroups.Contains(cg))
                {
                    uiButtonCanvasGroups.Add(cg);
                }
            }

            if (buttonQuit != null)
            {
                CanvasGroup qCg = buttonQuit.GetComponent<CanvasGroup>();
                if (qCg == null) qCg = buttonQuit.gameObject.AddComponent<CanvasGroup>();
                if (!uiButtonCanvasGroups.Contains(qCg)) uiButtonCanvasGroups.Add(qCg);
            }
        }

        private void SetUIButtonsAlpha(float alpha)
        {
            for (int i = 0; i < uiButtonCanvasGroups.Count; i++)
            {
                if (uiButtonCanvasGroups[i] != null)
                {
                    uiButtonCanvasGroups[i].alpha = alpha;
                    uiButtonCanvasGroups[i].interactable = alpha >= 0.95f;
                    uiButtonCanvasGroups[i].blocksRaycasts = alpha >= 0.95f;
                }
            }
        }

        /// <summary>
        /// Menyiapkan URP Global Volume dengan Gaussian Depth of Field untuk mengaburkan dunia 3D dan pemain di belakang menu.
        /// </summary>
        private Volume EnsureBlurVolume()
        {
            if (pauseBlurVolume != null) return pauseBlurVolume;

            GameObject volObj = GameObject.Find("SettingsPauseBlurVolume");
            if (volObj == null)
            {
                volObj = new GameObject("SettingsPauseBlurVolume");
                volObj.layer = 0;
                DontDestroyOnLoad(volObj);
            }

            pauseBlurVolume = volObj.GetComponent<Volume>();
            if (pauseBlurVolume == null)
            {
                pauseBlurVolume = volObj.AddComponent<Volume>();
            }

            pauseBlurVolume.isGlobal = true;
            pauseBlurVolume.priority = 100f;

            if (pauseBlurVolume.profile == null)
            {
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "PauseBlurProfile";

                DepthOfField dof = profile.Add<DepthOfField>(true);
                dof.mode.value = DepthOfFieldMode.Gaussian;
                dof.mode.overrideState = true;
                dof.gaussianStart.value = 0.01f;
                dof.gaussianStart.overrideState = true;
                dof.gaussianEnd.value = 0.5f;
                dof.gaussianEnd.overrideState = true;
                dof.gaussianMaxRadius.value = blurMaxRadius;
                dof.gaussianMaxRadius.overrideState = true;
                dof.highQualitySampling.value = true;
                dof.highQualitySampling.overrideState = true;

                pauseBlurVolume.profile = profile;
            }

            pauseBlurVolume.weight = 0f;
            pauseBlurVolume.enabled = false;
            return pauseBlurVolume;
        }

        private void AutoSetupPercentageTexts()
        {
            if (sensitivityInputField == null && sensitivitySlider != null)
            {
                sensitivityInputField = sensitivitySlider.GetComponentInParent<TMP_InputField>();
                if (sensitivityInputField == null)
                {
                    sensitivityInputField = sensitivitySlider.transform.parent.GetComponentInChildren<TMP_InputField>(true);
                }
                if (sensitivityInputField == null && controlsParent != null)
                {
                    sensitivityInputField = controlsParent.GetComponentInChildren<TMP_InputField>(true);
                }
            }

            if (masterVolumePercentText == null && masterVolumeSlider != null)
            {
                masterVolumePercentText = EnsureSliderPercentText(masterVolumeSlider, "Master_PercentText");
            }

            if (bgmVolumePercentText == null && bgmVolumeSlider != null)
            {
                bgmVolumePercentText = EnsureSliderPercentText(bgmVolumeSlider, "BGM_PercentText");
            }

            if (sfxVolumePercentText == null && sfxVolumeSlider != null)
            {
                sfxVolumePercentText = EnsureSliderPercentText(sfxVolumeSlider, "SFX_PercentText");
            }
        }

        private TextMeshProUGUI EnsureSliderPercentText(Slider slider, string objName)
        {
            if (slider == null) return null;

            Transform parent = slider.transform.parent != null ? slider.transform.parent : slider.transform;

            Transform existing = parent.Find(objName);
            if (existing != null)
            {
                var tmp = existing.GetComponent<TextMeshProUGUI>();
                if (tmp != null) return tmp;
            }

            GameObject textObj = new GameObject(objName);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);

            RectTransform sliderRt = slider.GetComponent<RectTransform>();
            float sliderWidth = sliderRt != null ? sliderRt.rect.width : 200f;
            Vector2 sliderPos = sliderRt != null ? sliderRt.anchoredPosition : Vector2.zero;

            rt.anchoredPosition = new Vector2(sliderPos.x + (sliderWidth * 0.5f) + 15f, sliderPos.y);
            rt.sizeDelta = new Vector2(70f, 30f);

            TextMeshProUGUI newTmp = textObj.AddComponent<TextMeshProUGUI>();
            newTmp.fontSize = 20;
            newTmp.alignment = TextAlignmentOptions.MidlineLeft;
            newTmp.color = Color.white;
            newTmp.text = "100%";

            return newTmp;
        }

        private void AutoDetectHudElements()
        {
            if (hudToHide == null)
            {
                if (transform.parent != null)
                {
                    Transform nonDiag = transform.parent.Find("NonDialogue");
                    if (nonDiag != null) hudToHide = nonDiag.gameObject;
                }

                if (hudToHide == null)
                {
                    GameObject found = GameObject.Find("NonDialogue");
                    if (found != null) hudToHide = found;
                }
            }

            GameObject treeMinigame = GameObject.Find("TreeMinigame");
            if (treeMinigame != null)
            {
                if (TreeCuttingMinigame.Instance == null || !TreeCuttingMinigame.Instance.IsPlaying)
                {
                    treeMinigame.SetActive(false);
                }
            }
        }

        private void Start()
        {
            try
            {
                if (buttonControls != null)
                {
                    buttonControls.onClick.RemoveListener(ShowControlsTab);
                    buttonControls.onClick.AddListener(ShowControlsTab);
                    buttonControls.interactable = true;
                }

                if (buttonAudio != null)
                {
                    buttonAudio.onClick.RemoveListener(ShowAudioTab);
                    buttonAudio.onClick.AddListener(ShowAudioTab);
                    buttonAudio.interactable = true;
                }

                if (buttonQuit != null)
                {
                    buttonQuit.onClick.RemoveListener(OnQuitClicked);
                    buttonQuit.onClick.AddListener(OnQuitClicked);
                    buttonQuit.interactable = true;
                }

                if (masterVolumeSlider != null)
                {
                    masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
                    masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
                    masterVolumeSlider.interactable = true;
                }

                if (bgmVolumeSlider != null)
                {
                    bgmVolumeSlider.onValueChanged.RemoveListener(SetBGMVolume);
                    bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
                    bgmVolumeSlider.interactable = true;
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
                    sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
                    sfxVolumeSlider.interactable = true;
                }

                if (sensitivitySlider != null)
                {
                    sensitivitySlider.minValue = 1f;
                    sensitivitySlider.maxValue = 100f;
                    sensitivitySlider.wholeNumbers = true;
                    sensitivitySlider.onValueChanged.RemoveListener(OnSensitivitySliderChanged);
                    sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
                    sensitivitySlider.interactable = true;
                }

                if (sensitivityInputField != null)
                {
                    sensitivityInputField.onEndEdit.RemoveListener(OnSensitivityInputEndEdit);
                    sensitivityInputField.onEndEdit.AddListener(OnSensitivityInputEndEdit);
                    sensitivityInputField.interactable = true;
                }

                if (screenModeDropdown != null)
                {
                    screenModeDropdown.onValueChanged.RemoveListener(SetScreenMode);
                    screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
                    screenModeDropdown.interactable = true;
                }

                if (motionBlurDropdown != null)
                {
                    motionBlurDropdown.onValueChanged.RemoveListener(SetMotionBlurQuality);
                    motionBlurDropdown.onValueChanged.AddListener(SetMotionBlurQuality);
                    motionBlurDropdown.interactable = true;
                }

                ShowControlsTab();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SettingsMenuController] Start warning: {ex.Message}");
            }
            finally
            {
                SetMenuVisualState(false);
            }
        }

        private void Update()
        {
            if (isMenuOpen && !isClosing)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                }

                FreezePlayerAndCamera(true);

                // Jika ada dropdown yang sedang terbuka, dan pengguna mengklik di luar list atau di elemen lain, tutup dropdown!
                if (IsAnyDropdownOpen())
                {
                    bool mousePressed = false;
                    #if ENABLE_INPUT_SYSTEM
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        mousePressed = true;
                    }
                    #endif
                    try
                    {
                        if (!mousePressed && Input.GetMouseButtonDown(0))
                        {
                            mousePressed = true;
                        }
                    }
                    catch { }

                    if (mousePressed && IsClickOutsideDropdowns())
                    {
                        CloseAllDropdowns();
                    }
                }
            }

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
                if (!isMenuOpen && TreeCuttingMinigame.Instance != null && TreeCuttingMinigame.Instance.IsPlaying)
                {
                    return;
                }

                ToggleMenu();
            }
        }

        public void ToggleMenu()
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            if (isTransitioning)
            {
                if (menuAnimationCoroutine != null) StopCoroutine(menuAnimationCoroutine);
            }

            isMenuOpen = true;
            isClosing = false;

            EnsureCanvasAndRaycaster();
            transform.SetAsLastSibling();
            EnsureEventSystemActive();
            HideGameplayHUD();
            SetMenuVisualState(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            FreezePlayerAndCamera(true);

            // 1. Setup Background Buram (Image gelap peredup latar game)
            if (backgroundBlur == null)
            {
                Transform imgTr = transform.Find("Image");
                if (imgTr != null) backgroundBlur = imgTr.gameObject;
            }
            if (backgroundBlur != null)
            {
                backgroundBlur.transform.SetAsFirstSibling();
                backgroundBlur.SetActive(true);
                if (backgroundBlurCanvasGroup == null)
                {
                    backgroundBlurCanvasGroup = backgroundBlur.GetComponent<CanvasGroup>();
                    if (backgroundBlurCanvasGroup == null)
                    {
                        backgroundBlurCanvasGroup = backgroundBlur.AddComponent<CanvasGroup>();
                    }
                }
            }

            // 2. Setup Background & Logo (Panel Hijau Miring)
            Transform bgTr = transform.Find("Background");
            if (bgTr != null)
            {
                bgTr.gameObject.SetActive(true);
                bgTr.SetSiblingIndex(1);
                Image bgImg = bgTr.GetComponent<Image>();
                if (bgImg != null) bgImg.enabled = false;

                Transform logoChild = bgTr.Find("Logo");
                if (logoChild != null) logoChild.gameObject.SetActive(true);
            }

            if (greenPanel == null)
            {
                Transform logoTr = transform.Find("Background/Logo");
                if (logoTr == null) logoTr = transform.Find("Logo");
                if (logoTr != null) greenPanel = logoTr.GetComponent<RectTransform>();
            }

            if (greenPanel != null && !hasRecordedOriginalPos)
            {
                greenPanelOriginalPos = greenPanel.anchoredPosition;
                if (greenPanelOriginalPos == Vector2.zero)
                {
                    greenPanelOriginalPos = new Vector2(-251.74f, -428f);
                }
                hasRecordedOriginalPos = true;
            }

            CollectUIButtonCanvasGroups();
            SetupDropdownTemplates();

            // 3. Jalankan Animasi Buka Menu
            if (gameObject.activeInHierarchy)
            {
                if (menuAnimationCoroutine != null) StopCoroutine(menuAnimationCoroutine);
                menuAnimationCoroutine = StartCoroutine(OpenMenuRoutine());
            }

            ShowControlsTab();
        }

        private IEnumerator OpenMenuRoutine()
        {
            isTransitioning = true;

            Vector2 startPos = greenPanelOriginalPos - new Vector2(slideDistance, 0f);
            if (greenPanel != null)
            {
                greenPanel.gameObject.SetActive(true);
                greenPanel.anchoredPosition = startPos;
            }

            SetUIButtonsAlpha(0f);

            if (backgroundBlurCanvasGroup != null) backgroundBlurCanvasGroup.alpha = 0f;

            if (enableWorldBlur)
            {
                EnsureBlurVolume();
                if (pauseBlurVolume != null)
                {
                    pauseBlurVolume.enabled = true;
                    pauseBlurVolume.weight = 0f;
                }
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, slideDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                // 1. Slide Panel Hijau
                if (greenPanel != null)
                {
                    greenPanel.anchoredPosition = Vector2.Lerp(startPos, greenPanelOriginalPos, smoothT);
                }

                // 2. Fade In Backdrop (Image)
                if (backgroundBlurCanvasGroup != null)
                {
                    backgroundBlurCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
                }

                // 3. Fade In Blur Kamera 3D
                if (pauseBlurVolume != null && enableWorldBlur)
                {
                    pauseBlurVolume.weight = Mathf.Lerp(0f, 1f, smoothT);
                }

                // 4. Fade In Tombol UI
                float btnT = Mathf.Clamp01(t * 1.25f);
                SetUIButtonsAlpha(btnT);

                yield return null;
            }

            if (greenPanel != null) greenPanel.anchoredPosition = greenPanelOriginalPos;
            if (backgroundBlurCanvasGroup != null) backgroundBlurCanvasGroup.alpha = 1f;
            if (pauseBlurVolume != null && enableWorldBlur) pauseBlurVolume.weight = 1f;
            SetUIButtonsAlpha(1f);

            isTransitioning = false;
        }

        public void CloseMenu()
        {
            if (isClosing) return;
            isClosing = true;
            isMenuOpen = false;

            CloseAllDropdowns();

            // Nonaktifkan interaksi seketika agar tombol tidak bisa diklik saat fade-out
            for (int i = 0; i < uiButtonCanvasGroups.Count; i++)
            {
                if (uiButtonCanvasGroups[i] != null)
                {
                    uiButtonCanvasGroups[i].interactable = false;
                    uiButtonCanvasGroups[i].blocksRaycasts = false;
                }
            }

            if (gameObject.activeInHierarchy)
            {
                if (menuAnimationCoroutine != null) StopCoroutine(menuAnimationCoroutine);
                menuAnimationCoroutine = StartCoroutine(CloseMenuRoutine());
            }
            else
            {
                FinishCloseMenu();
            }
        }

        private IEnumerator CloseMenuRoutine()
        {
            isTransitioning = true;

            Vector2 startPos = greenPanel != null ? greenPanel.anchoredPosition : greenPanelOriginalPos;
            Vector2 targetPos = greenPanelOriginalPos - new Vector2(slideDistance, 0f);

            float duration = Mathf.Max(0.1f, slideDuration * 0.75f);
            float btnFadeDuration = Mathf.Min(duration, uiButtonsFadeDuration);
            float elapsed = 0f;

            float initialBlurWeight = pauseBlurVolume != null ? pauseBlurVolume.weight : 1f;
            float initialBgAlpha = backgroundBlurCanvasGroup != null ? backgroundBlurCanvasGroup.alpha : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t;

                // 1. Fade OUT Tombol UI
                float btnT = Mathf.Clamp01(elapsed / btnFadeDuration);
                SetUIButtonsAlpha(1f - btnT);

                // 2. Slide OUT Panel Hijau
                if (greenPanel != null)
                {
                    greenPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                }

                // 3. Fade OUT Backdrop
                if (backgroundBlurCanvasGroup != null)
                {
                    backgroundBlurCanvasGroup.alpha = Mathf.Lerp(initialBgAlpha, 0f, smoothT);
                }

                // 4. Fade OUT Blur Kamera 3D
                if (pauseBlurVolume != null && enableWorldBlur)
                {
                    pauseBlurVolume.weight = Mathf.Lerp(initialBlurWeight, 0f, smoothT);
                }

                yield return null;
            }

            FinishCloseMenu();
        }

        private void FinishCloseMenu()
        {
            SetUIButtonsAlpha(0f);
            if (greenPanel != null)
            {
                greenPanel.anchoredPosition = greenPanelOriginalPos - new Vector2(slideDistance, 0f);
            }
            if (backgroundBlur != null) backgroundBlur.SetActive(false);
            if (pauseBlurVolume != null)
            {
                pauseBlurVolume.weight = 0f;
                pauseBlurVolume.enabled = false;
            }

            SetMenuVisualState(false);
            RestoreGameplayHUD();

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FreezePlayerAndCamera(false);

            isClosing = false;
            isTransitioning = false;
        }

        private void FreezePlayerAndCamera(bool freeze)
        {
            var inputs = FindAnyObjectByType<StarterAssets.StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorInputForLook = !freeze;
                inputs.cursorLocked = !freeze;
            }

            var allControllers = FindObjectsByType<StarterAssets.ThirdPersonController>(FindObjectsSortMode.None);
            if (allControllers != null)
            {
                for (int i = 0; i < allControllers.Length; i++)
                {
                    if (allControllers[i] == null) continue;
                    allControllers[i].LockCameraPosition = freeze;
                    allControllers[i].DisableMovement = freeze;
                }
            }
        }

        private void HideGameplayHUD()
        {
            if (hudToHide == null) AutoDetectHudElements();

            activeAdditionalHudElements.Clear();

            if (hudToHide != null && hudToHide.activeSelf)
            {
                wasHudActiveBeforePause = true;
                hudToHide.SetActive(false);
            }

            QuestUI[] quests = FindObjectsByType<QuestUI>(FindObjectsSortMode.None);
            if (quests != null)
            {
                foreach (var q in quests)
                {
                    if (q != null && q.gameObject.activeSelf && !activeAdditionalHudElements.Contains(q.gameObject))
                    {
                        activeAdditionalHudElements.Add(q.gameObject);
                        q.gameObject.SetActive(false);
                    }
                }
            }

            if (additionalHudElementsToHide != null)
            {
                foreach (var el in additionalHudElementsToHide)
                {
                    if (el != null && el.activeSelf && !activeAdditionalHudElements.Contains(el))
                    {
                        activeAdditionalHudElements.Add(el);
                        el.SetActive(false);
                    }
                }
            }

            GameObject treeMinigame = GameObject.Find("TreeMinigame");
            if (treeMinigame != null && treeMinigame.activeSelf && !activeAdditionalHudElements.Contains(treeMinigame))
            {
                activeAdditionalHudElements.Add(treeMinigame);
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

            if (menuRaycaster != null)
            {
                menuRaycaster.enabled = visible;
            }
        }

        public void ShowControlsTab()
        {
            CloseAllDropdowns();
            if (controlsParent != null) controlsParent.SetActive(true);
            if (audioParent != null) audioParent.SetActive(false);
        }

        public void ShowAudioTab()
        {
            CloseAllDropdowns();
            if (audioParent != null) audioParent.SetActive(true);
            if (controlsParent != null) controlsParent.SetActive(false);
        }

        public void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, value);
            PlayerPrefs.Save();
            UpdateMasterVolumeText(value);
        }

        public void SetBGMVolume(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
            PlayerPrefs.SetFloat(KEY_BGM_VOL, value);
            PlayerPrefs.Save();
            UpdateBGMVolumeText(value);
        }

        public void SetSFXVolume(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            PlayerPrefs.SetFloat(KEY_SFX_VOL, value);
            PlayerPrefs.Save();
            UpdateSFXVolumeText(value);
        }

        private void UpdateMasterVolumeText(float val)
        {
            int pct = Mathf.RoundToInt(Mathf.Clamp01(val) * 100f);
            if (masterVolumePercentText != null) masterVolumePercentText.text = $"{pct}%";
        }

        private void UpdateBGMVolumeText(float val)
        {
            int pct = Mathf.RoundToInt(Mathf.Clamp01(val) * 100f);
            if (bgmVolumePercentText != null) bgmVolumePercentText.text = $"{pct}%";
        }

        private void UpdateSFXVolumeText(float val)
        {
            int pct = Mathf.RoundToInt(Mathf.Clamp01(val) * 100f);
            if (sfxVolumePercentText != null) sfxVolumePercentText.text = $"{pct}%";
        }

        private void OnSensitivitySliderChanged(float value)
        {
            if (isUpdatingSensitivityUI) return;
            isUpdatingSensitivityUI = true;

            float clampedPercent = Mathf.Clamp(value, 1f, 100f);
            SetSensitivityInternal(clampedPercent);

            if (sensitivityInputField != null)
            {
                sensitivityInputField.text = $"{Mathf.RoundToInt(clampedPercent)}%";
            }

            isUpdatingSensitivityUI = false;
        }

        private void OnSensitivityInputEndEdit(string text)
        {
            if (isUpdatingSensitivityUI) return;

            if (string.IsNullOrEmpty(text))
            {
                if (sensitivitySlider != null && sensitivityInputField != null)
                {
                    sensitivityInputField.text = $"{Mathf.RoundToInt(sensitivitySlider.value)}%";
                }
                return;
            }

            string cleanText = text.Replace("%", "").Trim();
            if (float.TryParse(cleanText, out float parsedVal))
            {
                parsedVal = Mathf.Clamp(parsedVal, 1f, 100f);
                isUpdatingSensitivityUI = true;

                if (sensitivitySlider != null)
                {
                    sensitivitySlider.value = parsedVal;
                }
                SetSensitivityInternal(parsedVal);

                if (sensitivityInputField != null)
                {
                    sensitivityInputField.text = $"{Mathf.RoundToInt(parsedVal)}%";
                }

                isUpdatingSensitivityUI = false;
            }
            else
            {
                if (sensitivitySlider != null && sensitivityInputField != null)
                {
                    sensitivityInputField.text = $"{Mathf.RoundToInt(sensitivitySlider.value)}%";
                }
            }
        }

        private void SetSensitivityInternal(float percent)
        {
            float clampedPercent = Mathf.Clamp(percent, 1f, 100f);
            MouseSensitivity = Mathf.Max(0.05f, clampedPercent / 50f);

            PlayerPrefs.SetFloat(KEY_SENSITIVITY, clampedPercent);
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

            float sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 50f);
            if (sensitivity <= 1f && sensitivity > 0f)
            {
                sensitivity = 50f;
            }
            sensitivity = Mathf.Clamp(sensitivity, 1f, 100f);

            int screenMode = PlayerPrefs.GetInt(KEY_SCREEN_MODE, 0);
            int motionBlur = PlayerPrefs.GetInt(KEY_MOTION_BLUR, 2);

            AudioListener.volume = masterVol;
            SetSensitivityInternal(sensitivity);
            SetScreenMode(screenMode);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(bgmVol);
                AudioManager.Instance.SetSFXVolume(sfxVol);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.interactable = true;
                masterVolumeSlider.value = masterVol;
            }
            UpdateMasterVolumeText(masterVol);

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.interactable = true;
                bgmVolumeSlider.value = bgmVol;
            }
            UpdateBGMVolumeText(bgmVol);

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.interactable = true;
                sfxVolumeSlider.value = sfxVol;
            }
            UpdateSFXVolumeText(sfxVol);

            isUpdatingSensitivityUI = true;
            if (sensitivitySlider != null)
            {
                sensitivitySlider.interactable = true;
                sensitivitySlider.minValue = 1f;
                sensitivitySlider.maxValue = 100f;
                sensitivitySlider.wholeNumbers = true;
                sensitivitySlider.value = sensitivity;
            }

            if (sensitivityInputField != null)
            {
                sensitivityInputField.interactable = true;
                sensitivityInputField.text = $"{Mathf.RoundToInt(sensitivity)}%";
            }
            isUpdatingSensitivityUI = false;

            if (screenModeDropdown != null)
            {
                screenModeDropdown.interactable = true;
                screenModeDropdown.value = screenMode;
            }

            if (motionBlurDropdown != null)
            {
                motionBlurDropdown.interactable = true;
                motionBlurDropdown.value = motionBlur;
            }
        }

        public void OnQuitClicked()
        {
            CloseAllDropdowns();

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
            if (pauseBlurVolume != null)
            {
                pauseBlurVolume.enabled = false;
            }
        }
    }
}
