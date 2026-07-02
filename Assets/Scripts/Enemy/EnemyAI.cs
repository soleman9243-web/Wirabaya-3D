using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Cinemachine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public Collider weaponCollider;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }
    public bool isDead = false;

    [Header("Quest Integration")]
    public string deathObjectiveId = "defeat_dummy";

    [Header("Vision & Stealth")]
    public float visionRange = 10f;
    [Range(0, 360)] public float visionAngle = 90f;
    public LayerMask obstacleLayer;
    public bool isAlerted = false;
    [Tooltip("Waktu (detik) sebelum musuh kembali ke mode normal jika kehilangan jejak player")]
    public float alertCooldown = 5f;
    private float currentAlertTimer = 0f;
    public UnityEvent onPlayerDetected;

    [Header("Attack Settings")]
    public float attackInterval = 4f;
    public float attackRange = 3f;

    [Header("Parry Knockback Settings")]
    [Tooltip("Jarak musuh terpental ke belakang saat di-parry")]
    public float knockbackDistance = 2.5f;
    [Tooltip("Kecepatan musuh terpental (semakin kecil semakin cepat)")]
    public float knockbackDuration = 0.25f;

    [Header("Freeflow Settings")]
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject activeTargetObject;

    [Header("Takedown Settings")]
    [Tooltip("Kosongkan jika musuh ini bebas diculik. Isi dengan ID objektif jika musuh HANYA BISA diculik saat objektif tersebut aktif!")]
    public string requireTakedownObjectiveId;
    [SerializeField] private float takedownRadius = 2f;
    [SerializeField] private float takedownAngle = 120f;
    [SerializeField] private Transform takedownPoint;
    [SerializeField] private GameObject takedownCanvas;
    [SerializeField] private CinemachineVirtualCamera takedownCamera;

    private bool isStaggered = false;
    public bool isBeingTakenDown = false;
    
    private PlayerParry playerParryComponent;
    private Coroutine attackCoroutine;
    private Coroutine knockbackCoroutine; // Referensi coroutine knockback

    public Transform TakedownPoint => takedownPoint;
    public CinemachineVirtualCamera TakedownCamera => takedownCamera;
    public Animator EnemyAnimator => animator;

    private void Awake()
    {
        if (takedownCamera != null)
        {
            takedownCamera.gameObject.SetActive(false);
        }

        if (takedownCanvas != null)
        {
            takedownCanvas.SetActive(false);
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (weaponCollider != null) weaponCollider.enabled = false;
        
        if (player != null)
        {
            playerParryComponent = player.GetComponent<PlayerParry>();
        }

        attackCoroutine = StartCoroutine(AttackRoutine());
        ActiveTarget(false);
    }

    private void Update()
    {
        if (!isBeingTakenDown && !isDead && player != null)
        {
            // Selalu cek vision agar timer keriset jika player masih terlihat
            CheckVision();

            if (isAlerted)
            {
                currentAlertTimer -= Time.deltaTime;
                if (currentAlertTimer <= 0f)
                {
                    // Player berhasil kabur! Musuh kembali ke state normal.
                    isAlerted = false;
                }
            }
        }
    }

    private void CheckVision()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= visionRange)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleToPlayer <= visionAngle / 2f)
            {
                // Raycast to check for walls. We use RaycastHit to see WHAT we hit.
                if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, distanceToPlayer, obstacleLayer))
                {
                    // Jika yang tertabrak raycast adalah player (atau child dari player), berarti tidak terhalang tembok
                    if (hit.transform == player || hit.transform.IsChildOf(player) || hit.transform.CompareTag("Player"))
                    {
                        if (!isAlerted) onPlayerDetected?.Invoke();
                        isAlerted = true;
                        currentAlertTimer = alertCooldown; // Riset timer setiap kali melihat player
                    }
                }
                else
                {
                    // Jika tidak nabrak apa-apa di obstacleLayer, berarti clear
                    if (!isAlerted) onPlayerDetected?.Invoke();
                    isAlerted = true;
                    currentAlertTimer = alertCooldown; // Riset timer
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log(gameObject.name + " took damage. Remaining HP: " + currentHealth);

        // Optional: Trigger hit animation
        // if (animator != null && !isStaggered && !isBeingTakenDown) animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        EnemyDissolveController dissolveCtrl = GetComponent<EnemyDissolveController>();
        if (dissolveCtrl != null)
        {
            dissolveCtrl.TriggerDissolveAndDestroy();
        }
        else
        {
            Destroy(gameObject); // Fallback jika controller lupa dipasang
        }


        if (animator != null) animator.SetTrigger("Die"); // Pastikan ada trigger Die di Animator

        // Quest Integration
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(deathObjectiveId) && QuestManager.Instance.IsObjectiveActive(deathObjectiveId))
        {
            QuestManager.Instance.AddProgress(deathObjectiveId, 1);
        }

        // Disable behaviors
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

        Collider[] colliders = GetComponents<Collider>();
        foreach(var col in colliders) col.enabled = false;

        // Agar mayatnya tidak tembus/tenggelam ke lantai saat collider dimatikan
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        this.enabled = false;
        
        StartCoroutine(HideBodyAfterDelay(3f));
    }

    private IEnumerator HideBodyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            if (isAlerted && !isStaggered && !isBeingTakenDown)
            {
                float distanceToPlayer = 0f;
                if (player != null)
                {
                    distanceToPlayer = Vector3.Distance(transform.position, player.position);
                }

                if (distanceToPlayer <= attackRange && playerParryComponent != null)
                {
                    playerParryComponent.EnableSpiderSense(this);
                }

                if (animator != null) animator.SetTrigger("Attack");

                yield return new WaitForSeconds(0.5f);

                if (weaponCollider != null) weaponCollider.enabled = true;

                yield return new WaitForSeconds(0.5f);

                if (weaponCollider != null) weaponCollider.enabled = false;

                if (playerParryComponent != null)
                {
                    playerParryComponent.DisableSpiderSense();
                }
            }
        }
    }

    public void TriggerStagger()
    {
        if (isBeingTakenDown) return;

        if (playerParryComponent != null)
        {
            playerParryComponent.DisableSpiderSense();
        }

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        // Jalankan animasi stagger dan efek kepental secara bersamaan
        StartCoroutine(StaggerRoutine());
        knockbackCoroutine = StartCoroutine(KnockbackRoutine());
    }

    // Coroutine khusus untuk menggeser musuh ke belakang
    private IEnumerator KnockbackRoutine()
    {
        Vector3 startPos = transform.position;

        // 1. Hitung arah kepental (posisi musuh dikurangi posisi player)
        Vector3 knockbackDir = (transform.position - player.position).normalized;
        knockbackDir.y = 0; // Pastikan musuh tidak terbang ke atas atau nembus tanah

        // Cek tembok di belakang agar tidak nembus
        float actualKnockbackDist = knockbackDistance;
        if (Physics.Raycast(startPos + Vector3.up * 1f, knockbackDir, out RaycastHit hit, knockbackDistance, obstacleLayer))
        {
            // Berhenti sekitar 0.5 unit sebelum menyentuh tembok
            actualKnockbackDist = Mathf.Max(0, hit.distance - 0.5f);
        }

        // 2. Tentukan posisi akhir setelah terpental
        Vector3 targetPos = startPos + (knockbackDir * actualKnockbackDist);

        float time = 0f;

        // 3. Geser posisi secara perlahan selama knockbackDuration
        while (time < knockbackDuration)
        {
            time += Time.deltaTime;

            // Menggunakan SmoothStep agar efek dorongannya terasa lebih natural (cepat di awal, melambat di akhir)
            float t = Mathf.SmoothStep(0f, 1f, time / knockbackDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // Pastikan posisi akhirnya pas
        transform.position = targetPos;
    }

    private IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        if (weaponCollider != null) weaponCollider.enabled = false;

        if (animator != null) animator.SetTrigger("Stagger");

        // Tunggu animasi stagger selesai
        yield return new WaitForSeconds(2f);

        isStaggered = false;
        if (animator != null) animator.SetTrigger("Recover");

        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    // --- Freeflow Combat ---
    public void SpawnHitVfx(Vector3 Pos_)
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, Pos_, Quaternion.identity);
        }
    }

    public void ActiveTarget(bool isActive)
    {
        if (activeTargetObject != null)
        {
            activeTargetObject.SetActive(isActive);
        }
    }

    // --- Takedown System ---
    public void ShowTakedownUI(bool show)
    {
        if (takedownCanvas != null)
        {
            takedownCanvas.SetActive(show);
        }
    }

    public bool IsPlayerInTakedownArea
    {
        get { return CanTakedown(); }
    }

    private bool CanTakedown()
    {
        if (player == null || isBeingTakenDown || isAlerted)
        {
            return false;
        }

        // Cek syarat quest (jika dikunci oleh objektif tertentu)
        if (!string.IsNullOrEmpty(requireTakedownObjectiveId) && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.IsObjectiveActive(requireTakedownObjectiveId))
            {
                return false;
            }
        }

        Vector3 directionToPlayer = player.position - transform.position;

        if (directionToPlayer.sqrMagnitude > takedownRadius * takedownRadius)
        {
            return false;
        }

        float angle = Vector3.Angle(-transform.forward, directionToPlayer);
        return angle <= takedownAngle * 0.5f;
    }

    public void OnTakedownStart()
    {
        isBeingTakenDown = true;
        if (weaponCollider != null) weaponCollider.enabled = false;
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        
        if (animator != null) animator.SetTrigger("GetTakedown");
    }

    private void OnDrawGizmosSelected()
    {
        // Takedown Area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, takedownRadius);

        Vector3 tkLeft = Quaternion.Euler(0f, takedownAngle * 0.5f, 0f) * -transform.forward;
        Vector3 tkRight = Quaternion.Euler(0f, -takedownAngle * 0.5f, 0f) * -transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + tkLeft * takedownRadius);
        Gizmos.DrawLine(transform.position, transform.position + tkRight * takedownRadius);

        // Vision Area
        Gizmos.color = Color.blue;
        Vector3 visLeft = Quaternion.Euler(0f, -visionAngle / 2f, 0f) * transform.forward;
        Vector3 visRight = Quaternion.Euler(0f, visionAngle / 2f, 0f) * transform.forward;
        
        Gizmos.DrawRay(transform.position + Vector3.up, visLeft * visionRange);
        Gizmos.DrawRay(transform.position + Vector3.up, visRight * visionRange);
        Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * visionRange, 0.2f);
    }
}