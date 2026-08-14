using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cinemachine;
using System;

public class PlayerStatus : MonoBehaviour
{
    public static PlayerStatus Instance { get; private set; }

    [Header("Settings")]
    public float lerpSpeed = 5f;

    [Header("Health & Blood Screen")]
    public float maxHealth = 100f;
    [field: SerializeField] public float health { get; private set; }

    [SerializeField] private Image bloodOverlay;
    public float healthRegenRate = 15f;
    public float regenDelay = 3f;
    private float timeSinceLastHit;

    [Header("Stamina")]
    [SerializeField] private Image staminaImage;
    [SerializeField] private Image easeStaminaImage;
    public float maxStamina = 100f;
    [field: SerializeField] public float stamina { get; private set; }
    public float staminaRegenDelay = 1.5f;
    private float timeSinceLastStaminaUse = 0f;

    [Header("Mana")]
    [SerializeField] private Image manaImage;
    [SerializeField] private Image easeManaImage;
    public float maxMana = 100f;
    public float manaRegenRate = 5f;
    [field: SerializeField] public float mana { get; private set; }

    [Header("Bar Auto-Hide")]
    [Tooltip("Aktifkan auto-hide untuk stamina bar (butuh CanvasGroup di-assign)")]
    [SerializeField] private bool autoHideStaminaBar = true;
    [Tooltip("Aktifkan auto-hide untuk mana bar (butuh CanvasGroup di-assign)")]
    [SerializeField] private bool autoHideManaBar = true;
    [Tooltip("Berapa detik bar tetap terlihat setelah perubahan terakhir")]
    [SerializeField] private float barVisibleDuration = 2f;
    [Tooltip("Kecepatan fade in (makin besar makin cepat muncul)")]
    [SerializeField] private float barFadeInSpeed = 8f;
    [Tooltip("Kecepatan fade out (makin besar makin cepat hilang)")]
    [SerializeField] private float barFadeOutSpeed = 3f;
    [Tooltip("CanvasGroup parent UI stamina bar. Kosongkan jika tidak mau auto-hide.")]
    [SerializeField] private CanvasGroup staminaBarGroup;
    [Tooltip("CanvasGroup parent UI mana bar. Kosongkan jika tidak mau auto-hide.")]
    [SerializeField] private CanvasGroup manaBarGroup;

    [Header("Damage Instances")]
    public float damage1 = 10f;
    public float damage2 = 15f;
    public float damage3 = 25f;

    [Header("Death & Cameras")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private CinemachineVirtualCamera deathCamera;
    [SerializeField] private string sceneToLoadOnDeath = "LoseScene";

    [Header("VFX")]
    [Tooltip("Prefab efek darah atau benturan saat player terkena serangan")]
    [SerializeField] private GameObject hitVfx;

    private bool isDead = false;

    // Bar auto-hide tracking
    private float staminaLastChangedTime;
    private float manaLastChangedTime;
    private float lastStaminaValue;
    private float lastManaValue;

    private StarterAssets.ThirdPersonController tpc;
    private PlayerParry playerParry;
    private MonoBehaviour starterController;
    private CharacterController charController;

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
        InitializeStatus();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        tpc = GetComponent<StarterAssets.ThirdPersonController>();
        playerParry = GetComponent<PlayerParry>();
        charController = GetComponent<CharacterController>();
        
        starterController = GetComponent("FirstPersonController") as MonoBehaviour;
        if (starterController == null)
        {
            starterController = GetComponent("ThirdPersonController") as MonoBehaviour;
        }
    }

    private void Update()
    {
        if (isDead) return;

        HandleHealthRegenAndUI();
        HandleStamina();
        HandleMana();
    }

    private void InitializeStatus()
    {
        health = maxHealth;
        stamina = maxStamina;
        mana = maxMana;
        timeSinceLastHit = 0f;

        if (staminaImage) staminaImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);
        if (easeStaminaImage) easeStaminaImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);

        if (manaImage) manaImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);
        if (easeManaImage) easeManaImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);

        if (bloodOverlay != null)
        {
            Color c = bloodOverlay.color;
            c.a = 0f;
            bloodOverlay.color = c;
        }

        // Inisialisasi tracking auto-hide
        lastStaminaValue = stamina;
        lastManaValue = mana;
        staminaLastChangedTime = -barVisibleDuration; // Agar langsung hidden saat start
        manaLastChangedTime = -barVisibleDuration;

        // Set alpha awal: hidden jika auto-hide aktif dan CanvasGroup ada
        if (staminaBarGroup != null && autoHideStaminaBar)
            staminaBarGroup.alpha = 0f;

        if (manaBarGroup != null && autoHideManaBar)
            manaBarGroup.alpha = 0f;
    }

    private void HandleHealthRegenAndUI()
    {
        timeSinceLastHit += Time.deltaTime;

        if (timeSinceLastHit >= regenDelay && health < maxHealth)
        {
            health += healthRegenRate * Time.deltaTime;
            health = Mathf.Clamp(health, 0, maxHealth);
        }

        if (bloodOverlay != null)
        {
            float targetAlpha = 1f - (health / maxHealth);

            Color currentColor = bloodOverlay.color;
            currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, lerpSpeed * Time.deltaTime);
            bloodOverlay.color = currentColor;
        }
    }

    private void HandleStamina()
    {
        // Tambahkan timer agar stamina tidak langsung regen saat dipakai lari
        timeSinceLastStaminaUse += Time.deltaTime;

        if (stamina < maxStamina && timeSinceLastStaminaUse >= staminaRegenDelay)
        {
            stamina += 5 * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0, maxStamina);
        }

        if (staminaImage != null)
        {
            staminaImage.rectTransform.localScale = new Vector3(stamina / maxStamina, 1f, 1f);
        }

        if (easeStaminaImage != null)
        {
            float targetScale = stamina / maxStamina;
            float currentScale = easeStaminaImage.rectTransform.localScale.x;

            if (currentScale != targetScale)
            {
                float newScale = Mathf.Lerp(currentScale, targetScale, lerpSpeed * Time.deltaTime);
                easeStaminaImage.rectTransform.localScale = new Vector3(newScale, 1f, 1f);
            }
        }

        // Deteksi perubahan nilai stamina untuk auto-hide
        if (!Mathf.Approximately(stamina, lastStaminaValue))
        {
            staminaLastChangedTime = Time.time;
            lastStaminaValue = stamina;
        }

        UpdateBarVisibility(staminaBarGroup, autoHideStaminaBar, staminaLastChangedTime);
    }

    public void UseStamina(float amount)
    {
        if (isDead) return;

        stamina -= amount;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);

        // Reset timer setiap kali stamina digunakan (nyerang/lari)
        timeSinceLastStaminaUse = 0f;

        // Langsung trigger show bar saat dipake
        staminaLastChangedTime = Time.time;
        lastStaminaValue = stamina;
    }

    private void HandleMana()
    {
        if (mana < maxMana)
        {
            mana += manaRegenRate * Time.deltaTime;
            mana = Mathf.Clamp(mana, 0, maxMana);
        }

        if (manaImage != null)
        {
            manaImage.rectTransform.localScale = new Vector3(mana / maxMana, 1f, 1f);
        }

        if (easeManaImage != null)
        {
            float targetScale = mana / maxMana;
            float currentScale = easeManaImage.rectTransform.localScale.x;

            if (currentScale != targetScale)
            {
                float newScale = Mathf.Lerp(currentScale, targetScale, lerpSpeed * Time.deltaTime);
                easeManaImage.rectTransform.localScale = new Vector3(newScale, 1f, 1f);
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            mana -= 20;
        }

        // Deteksi perubahan nilai mana untuk auto-hide
        if (!Mathf.Approximately(mana, lastManaValue))
        {
            manaLastChangedTime = Time.time;
            lastManaValue = mana;
        }

        UpdateBarVisibility(manaBarGroup, autoHideManaBar, manaLastChangedTime);
    }


    public void UseMana(float amount)
    {
        if (isDead) return;

        mana -= amount;
        mana = Mathf.Clamp(mana, 0, maxMana);

        // Langsung trigger show bar saat dipake
        manaLastChangedTime = Time.time;
        lastManaValue = mana;
    }

    public void RestoreMana(float amount)
    {
        if (isDead) return;

        mana += amount;
        mana = Mathf.Clamp(mana, 0, maxMana);

        // Langsung trigger show bar saat di-restore
        manaLastChangedTime = Time.time;
        lastManaValue = mana;
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    // === Save System Setters ===
    // Digunakan oleh PlayerSaveProvider untuk restore state dari save data.

    /// <summary>
    /// Set health secara langsung (untuk Save System).
    /// </summary>
    public void SetHealth(float value)
    {
        health = Mathf.Clamp(value, 0, maxHealth);
    }

    /// <summary>
    /// Set stamina secara langsung (untuk Save System).
    /// </summary>
    public void SetStamina(float value)
    {
        stamina = Mathf.Clamp(value, 0, maxStamina);
    }

    /// <summary>
    /// Set mana secara langsung (untuk Save System).
    /// </summary>
    public void SetMana(float value)
    {
        mana = Mathf.Clamp(value, 0, maxMana);
    }

    /// <summary>
    /// Handle fade in/out bar berdasarkan waktu terakhir nilai berubah.
    /// Jika CanvasGroup null atau autoHide false, tidak melakukan apa-apa (bar tetap visible).
    /// </summary>
    private void UpdateBarVisibility(CanvasGroup group, bool autoHide, float lastChangedTime)
    {
        // Tanpa CanvasGroup atau auto-hide mati → skip, bar tetap visible seperti biasa
        if (group == null || !autoHide) return;

        float timeSinceChange = Time.time - lastChangedTime;
        float targetAlpha;

        if (timeSinceChange < barVisibleDuration)
        {
            // Masih dalam durasi visible → fade in
            targetAlpha = 1f;
        }
        else
        {
            // Sudah lewat durasi → fade out
            targetAlpha = 0f;
        }

        // Pilih speed berdasarkan arah fade
        float speed = targetAlpha > group.alpha ? barFadeInSpeed : barFadeOutSpeed;
        group.alpha = Mathf.Lerp(group.alpha, targetAlpha, speed * Time.deltaTime);

        // Snap ke target jika sudah sangat dekat (hindari floating point terus-terusan)
        if (Mathf.Abs(group.alpha - targetAlpha) < 0.01f)
        {
            group.alpha = targetAlpha;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // Kebal (invincible) saat sedang melakukan eksekusi (Takedown/Finisher)
        if (tpc != null && tpc.IsInFinisher) return;

        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position + Vector3.up * 1.2f, Quaternion.identity);
        }

        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        timeSinceLastHit = 0f;

        // Batalin kesempatan parry kalau sudah terlanjur kena pukul
        if (playerParry != null)
        {
            playerParry.DisableSpiderSense();
        }

        Debug.Log("Player kena damage: " + damage + " | Sisa HP: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    [ContextMenu("Debug: Kill Player")]
    public void TestDie()
    {
        if (Application.isPlaying)
        {
            Die();
        }
        else
        {
            Debug.LogWarning("TestDie hanya bisa dijalankan saat Play Mode!");
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log("Player Mati");

        gameObject.tag = "Untagged";

        if (starterController != null)
        {
            starterController.enabled = false;
        }

        if (charController != null)
        {
            charController.enabled = false;
        }

        // Animasi death player
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Dead");
        }

        // Nyalakan Death Camera biasa jika ada
        if (deathCamera != null)
        {
            deathCamera.gameObject.SetActive(true);
            deathCamera.Priority = 999;
        }

        yield return new WaitForSeconds(2.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Fade out biasa menggunakan ScreenFader
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoadOnDeath);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
    }
}