using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cinemachine;

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

    [Header("Damage Instances")]
    public float damage1 = 10f;
    public float damage2 = 15f;
    public float damage3 = 25f;

    [Header("Death & Cameras")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private CinemachineFreeLook deathCamera;

    private bool isDead = false;

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
    }

    public void UseStamina(float amount)
    {
        if (isDead) return;

        stamina -= amount;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);

        // Reset timer setiap kali stamina digunakan (nyerang/lari)
        timeSinceLastStaminaUse = 0f;
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
    }


    public void UseMana(float amount)
    {
        if (isDead) return;

        mana -= amount;
        mana = Mathf.Clamp(mana, 0, maxMana);
    }

    public void RestoreMana(float amount)
    {
        if (isDead) return;

        mana += amount;
        mana = Mathf.Clamp(mana, 0, maxMana);
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // Kebal (invincible) saat sedang melakukan eksekusi (Takedown/Finisher)
        StarterAssets.ThirdPersonController tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null && tpc.IsInFinisher) return;

        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        timeSinceLastHit = 0f;

        Debug.Log("Player kena damage: " + damage + " | Sisa HP: " + health);

        if (health <= 0)
        {
            Die();
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

        MonoBehaviour starterController = GetComponent("FirstPersonController") as MonoBehaviour;
        if (starterController == null)
        {
            starterController = GetComponent("ThirdPersonController") as MonoBehaviour;
        }

        if (starterController != null)
        {
            starterController.enabled = false;
        }

        if (TryGetComponent<CharacterController>(out var charController))
        {
            charController.enabled = false;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Dead");
        }

        if (deathCamera != null)
        {
            deathCamera.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(3f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("LoseScene");
    }
}