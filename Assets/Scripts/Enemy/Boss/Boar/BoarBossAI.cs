using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AI Boss Babi (Boar) — 2 Fase dengan state machine berbasis coroutine.
/// 
/// ========== ANIMATOR SETUP ==========
/// Parameter yang dibutuhkan di Animator Controller:
/// 
///   Float:
///     "Speed"  → 0 = idle, 1 = jalan
///   
///   Trigger:
///     "Aim"         → Ancang-ancang sebelum charge (boss fokus ke player)
///     "Charge"      → Animasi charge/berlari maju
///     "Roar"        → Transisi fase (mengamuk)
///     "MeleeAttack" → Serangan melee jarak dekat
///     "DigRock"     → Mencungkil batu dari tanah
///     "ThrowRock"   → Melempar batu
///     "Die"         → Animasi mati
/// 
/// ========== FLOW BOSS ==========
/// INTRO → FASE 1 (Idle→Aim→Charge loop) → TRANSISI → FASE 2 (Charge+Melee+Ranged)
/// </summary>
public class BoarBossAI : MonoBehaviour
{
    // ==========================================
    // ENUMS
    // ==========================================

    public enum BossState
    {
        Inactive,        // Sebelum fight dimulai
        Intro,           // Nama fade in + health bar isi
        Idle,            // Diam, menghadap player
        AimCharge,       // Membidik sebelum charge (rotasi mengikuti player)
        Charging,        // Melesat lurus ke arah yang dikunci
        PhaseTransition, // Transisi Fase 1 → 2 (roar)
        Decision,        // Fase 2: menentukan melee atau ranged
        MeleeChase,      // Mengejar player untuk melee
        MeleeAttack,     // Serangan melee
        RangedAim,       // Membidik untuk lempar batu
        RangedDig,       // Mencungkil batu dari tanah
        RangedThrow,     // Melempar batu
        Dead             // Mati
    }

    // ==========================================
    // INSPECTOR FIELDS
    // ==========================================

    [Header("Data & References")]
    public BoarBossData data;
    public Animator animator;
    public Transform player;
    public BossUI bossUI;

    [Header("Melee Combat")]
    [Tooltip("Titik kosong di depan boss sebagai pusat deteksi damage melee/charge")]
    public Transform meleeAttackPoint;
    [Tooltip("Radius area deteksi damage melee/charge (OverlapSphere)")]
    public float meleeHitRadius = 1.5f;

    [Header("Ranged Combat")]
    [Tooltip("Titik spawn batu (posisi tangan/mulut boss)")]
    public Transform rockSpawnPoint;
    [Tooltip("Prefab batu yang dilempar")]
    public GameObject rockPrefab;
    [Tooltip("Prefab lingkaran telegraph (TelegraphIndicator)")]
    public GameObject telegraphPrefab;

    [Header("Wall Detection (Charge)")]
    [Tooltip("Layer yang dianggap tembok. Charge berhenti jika menabrak.")]
    public LayerMask wallLayer;
    [Tooltip("Jarak raycast ke depan untuk deteksi tembok")]
    public float wallDetectDistance = 1.5f;

    [Header("Physics & Tracking")]
    [Tooltip("Kecepatan rotasi boss menghadap player")]
    public float lookAtSpeed = 8f;

    [Header("Visual Feedback")]
    [Tooltip("Opsional: Lingkaran target di bawah kaki boss")]
    [SerializeField] private GameObject activeTargetObject;
    [Tooltip("Opsional: Prefab efek saat boss dipukul player")]
    [SerializeField] private GameObject hitVfx;
    [Tooltip("Efek saat boss melakukan charge (sistem On/Off, misal debu di kaki)")]
    public GameObject chargeVfx;
    [Tooltip("Efek ledakan/impact sekali muncul saat babi mulai melesat maju")]
    public GameObject chargeBurstVfx;

    [Header("Auto Start")]
    [Tooltip("Jika true, boss fight dimulai otomatis saat scene dimuat")]
    public bool autoStart = true;

    [Header("Events")]
    public UnityEvent<float, float> OnBossHealthChanged = new UnityEvent<float, float>();
    public UnityEvent OnBossDied = new UnityEvent();

    [Header("Debug (Read Only)")]
    [SerializeField] private string debugState = "Inactive";
    [SerializeField] private int debugPhase = 0;

    // ==========================================
    // PUBLIC PROPERTIES
    // ==========================================

    [Header("Debug / Testing")]
    [Tooltip("Ubah nilai ini saat Play Mode untuk testing pindah fase (Health)")]
    public float currentHealth;
    public float CurrentHealth 
    { 
        get { return currentHealth; } 
        private set { currentHealth = value; } 
    }
    public BossState CurrentState { get; private set; } = BossState.Inactive;

    // ==========================================
    // PRIVATE STATE
    // ==========================================

    private int currentPhase = 0; // 0=belum mulai, 1=Fase 1, 2=Fase 2
    private bool isDead = false;
    private bool canTakeDamage = false;
    private float lastChargeTime = -999f;

    // Components
    private Rigidbody rb;
    private Collider bossCollider;
    private Collider playerCollider;
    private PlayerParry playerParry;

    // Coroutine tracking
    private Coroutine stateMachineCoroutine;

    // ==========================================
    // UNITY LIFECYCLE
    // ==========================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();

        // === FIX BUG: Pastikan gravitasi aktif ===
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            // Freeze rotasi agar boss tidak jungkir balik saat nabrak
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            // Buat boss sangat berat agar tidak mudah terlempar oleh CharacterController Player
            rb.mass = 1000f; 
        }
    }

    private void Start()
    {
        if (data != null)
        {
            CurrentHealth = data.maxHealth;
        }

        // Auto-find player
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerCollider = player.GetComponent<Collider>();
            playerParry = player.GetComponent<PlayerParry>();
        }

        // Wire health events ke BossUI
        if (bossUI != null)
        {
            OnBossHealthChanged.AddListener(bossUI.UpdateHealth);
        }

        // Matikan indikator target di awal
        ActiveTarget(false);
        if (chargeVfx != null) chargeVfx.SetActive(false);

        if (autoStart)
        {
            StartBossFight();
        }
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    private void OnValidate()
    {
        // Hindari OnValidate terpanggil saat pertama kali Play (sebelum Start jalan)
        // currentPhase > 0 menandakan boss sudah selesai intro dan masuk pertarungan
        if (Application.isPlaying && data != null && currentPhase > 0)
        {
            // Jika diubah manual di Inspector saat Play Mode, update UI & cek fase
            OnBossHealthChanged?.Invoke(CurrentHealth, data.maxHealth);
            
            // Cek transisi fase jika Health diset manual ke bawah threshold
            CheckPhaseTransition();
            if (CurrentHealth <= 0 && !isDead) Die();
        }
    }

    /// <summary>
    /// Mulai boss fight. Dipanggil otomatis jika autoStart = true,
    /// atau manual dari BossArenaManager / BossTrigger.
    /// </summary>
    public void StartBossFight()
    {
        if (stateMachineCoroutine != null) return;
        stateMachineCoroutine = StartCoroutine(BossStateMachine());
    }

    /// <summary>
    /// Dipanggil dari script player saat menyerang boss.
    /// </summary>
    public void TakeDamage(float amount)
    {
        Debug.Log($"[BoarBossAI] TakeDamage called: {amount}. isDead: {isDead}, canTakeDamage: {canTakeDamage}");
        
        if (isDead || !canTakeDamage) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, data.maxHealth);

        OnBossHealthChanged?.Invoke(CurrentHealth, data.maxHealth);

        // Cek transisi fase
        CheckPhaseTransition();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Tampilkan/sembunyikan lingkaran target di bawah boss.
    /// </summary>
    public void ActiveTarget(bool isActive)
    {
        if (activeTargetObject != null)
        {
            activeTargetObject.SetActive(isActive);
        }
    }

    /// <summary>
    /// Spawn efek saat boss dipukul player (darah/percikan).
    /// </summary>
    public void SpawnHitVfx(Vector3 hitPos)
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, hitPos + Vector3.up * 1.5f, Quaternion.identity);
        }
    }

    // ==========================================
    // PARRY LOGIC
    // ==========================================

    public void TriggerParryStagger()
    {
        if (isDead) return;

        Debug.Log("[BoarBossAI] Boss terkena Parry!");
        
        // Hentikan semua aksi yang sedang berjalan
        StopAllCoroutines();
        StopMovement();
        
        if (chargeVfx != null) chargeVfx.SetActive(false);
        if (playerParry != null) playerParry.DisableSpiderSense();

        // Kembalikan collision dengan player jika tadi sempat dimatikan saat Charge
        if (bossCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(bossCollider, playerCollider, false);
        }

        // Mulai proses pemulihan (boss diam aja tanpa kepental)
        stateMachineCoroutine = StartCoroutine(ParryRecoveryRoutine());
    }

    private IEnumerator ParryRecoveryRoutine()
    {
        // Boss diam
        SetState(BossState.Idle);
        SetAnimSpeed(0f);
        
        // Paksa animasi serangannya berhenti dan kembali ke blend tree Locomotion (sehingga otomatis jadi Idle)
        if (animator != null)
        {
            animator.Play("Locomotion Fase " + currentPhase);
        }

        // Tunggu 2 detik (stunned)
        yield return new WaitForSeconds(2f);

        // Lanjut ke boss state machine lagi
        stateMachineCoroutine = StartCoroutine(BossStateMachine());
    }

    // ==========================================
    // STATE MACHINE (MAIN FLOW)
    // ==========================================

    private void FixedUpdate()
    {
        // === FIX BUG: Mencegah babi terbang ke atas atau nyangkut di udara ===
        if (rb != null && !isDead)
        {
            // Tambahkan gravitasi ekstra agar boss selalu menempel ke tanah dengan kuat (kecuali sedang mati/jatuh natural)
            rb.AddForce(Vector3.down * 100f, ForceMode.Acceleration);

            // Jika kecepatan naiknya (Y) tidak normal (akibat physics bug/kepental dari player)
            if (rb.linearVelocity.y > 1f)
            {
                // Pangkas kecepatan naiknya agar tidak bisa terbang
                Vector3 vel = rb.linearVelocity;
                vel.y = 1f; // Maksimal lompatan gaib hanya 1 unit
                rb.linearVelocity = vel;
            }
        }
    }

    private IEnumerator BossStateMachine()
    {
        // ========== INTRO ==========
        yield return StartCoroutine(IntroSequence());

        // ========== FASE 1 ==========
        currentPhase = 1;
        canTakeDamage = true;

        while (!isDead && currentPhase == 1)
        {
            // IDLE → AIM → CHARGE → loop
            yield return StartCoroutine(IdleState(data.idleDuration));
            if (isDead || currentPhase != 1) break;

            yield return StartCoroutine(AimChargeState());
            if (isDead || currentPhase != 1) break;

            yield return StartCoroutine(ChargeState());
            // TakeDamage() bisa set currentPhase=2, loop condition akan exit
        }

        // ========== TRANSISI FASE ==========
        if (!isDead)
        {
            yield return StartCoroutine(PhaseTransitionSequence());
        }

        // ========== FASE 2 ==========
        currentPhase = 2;
        lastChargeTime = -999f; // Charge langsung tersedia di awal Fase 2

        while (!isDead)
        {
            bool chargeReady = Time.time - lastChargeTime >= data.chargeCooldown;

            if (chargeReady)
            {
                // Charge diprioritaskan saat cooldown selesai
                yield return StartCoroutine(AimChargeState());
                if (isDead) break;

                yield return StartCoroutine(ChargeState());
                lastChargeTime = Time.time;
            }
            else
            {
                // Selama cooldown charge: keputusan melee/ranged
                yield return StartCoroutine(DecisionState());
            }

            // Jeda singkat antar aksi agar tidak terlalu robotik
            if (!isDead)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    // ==========================================
    // STATES
    // ==========================================

    // ---------- INTRO ----------

    private IEnumerator IntroSequence()
    {
        SetState(BossState.Intro);
        canTakeDamage = false;

        // Boss idle selama intro
        SetAnimSpeed(0f);

        // Play intro sequence di BossUI (fade in nama + isi health bar)
        if (bossUI != null)
        {
            yield return StartCoroutine(bossUI.PlayIntroSequence(
                data.bossName,
                data.nameFadeInDuration,
                data.healthFillDuration
            ));
        }

        // Jeda sebelum aksi pertama
        yield return new WaitForSeconds(0.5f);
    }

    // ---------- IDLE ----------

    private IEnumerator IdleState(float duration)
    {
        SetState(BossState.Idle);
        SetAnimSpeed(0f);
        StopMovement();

        // Boss perlahan menghadap player saat idle
        float elapsed = 0f;
        while (elapsed < duration && !isDead)
        {
            RotateTowardsPlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ---------- AIM CHARGE ----------

    private IEnumerator AimChargeState()
    {
        SetState(BossState.AimCharge);
        StopMovement();

        if (animator != null) animator.SetTrigger("Aim");

        // Rotasi terus mengikuti player selama durasi aim
        float elapsed = 0f;
        while (elapsed < data.aimDuration && !isDead)
        {
            RotateTowardsPlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Di akhir aim: arah DIKUNCI (direction disimpan di ChargeState)
    }

    // ---------- CHARGE ----------

    private IEnumerator ChargeState()
    {
        SetState(BossState.Charging);

        // KUNCI arah charge di awal (tidak berubah selama charge)
        Vector3 chargeDirection = transform.forward;
        chargeDirection.y = 0f;
        chargeDirection.Normalize();

        // Paksa animasi locomotion masuk ke mode lari (menggunakan Blend Tree)
        if (animator != null) 
        {
            animator.CrossFade("Locomotion Phase 1", 0.1f);
            SetAnimSpeed(data.chargeSpeed);
        }

        // Efek Ledakan/Impact awal saat mulai melesat
        if (chargeBurstVfx != null)
        {
            Instantiate(chargeBurstVfx, transform.position, transform.rotation);
        }

        // Nyalakan efek charge (On/Off) yang menempel di badan (misal angin/debu)
        if (chargeVfx != null) chargeVfx.SetActive(true);
        
        // Aktifkan spider sense (Bisa di-parry saat charge)
        if (playerParry != null) playerParry.EnableBoarSpiderSense(this);

        // === FIX BUG: Ignore collision dengan player agar boss tidak tergeser ===
        if (bossCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(bossCollider, playerCollider, true);
        }

        float distanceCovered = 0f;
        bool hasHitPlayer = false;

        while (distanceCovered < data.chargeMaxDistance && !isDead)
        {
            // Cek tembok di depan → stop charge
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, chargeDirection, wallDetectDistance, wallLayer))
            {
                break;
            }

            // Gerak maju via rigidbody (override X/Z setiap frame, preservasi Y untuk gravitasi)
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(
                    chargeDirection.x * data.chargeSpeed,
                    rb.linearVelocity.y,
                    chargeDirection.z * data.chargeSpeed
                );
            }
            else
            {
                transform.position += chargeDirection * data.chargeSpeed * Time.deltaTime;
            }

            distanceCovered += data.chargeSpeed * Time.deltaTime;

            // Deteksi damage (1x hit per charge)
            if (!hasHitPlayer && meleeAttackPoint != null)
            {
                Collider[] hits = Physics.OverlapSphere(meleeAttackPoint.position, meleeHitRadius);
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        PlayerStatus ps = hit.GetComponentInParent<PlayerStatus>();
                        if (ps != null)
                        {
                            ps.TakeDamage(data.chargeDamage);
                            hasHitPlayer = true;
                            break;
                        }
                    }
                }
            }

            yield return null;
        }

        // Stop
        StopMovement();

        // Matikan efek charge & spider sense
        if (chargeVfx != null) chargeVfx.SetActive(false);
        if (playerParry != null) playerParry.DisableSpiderSense();

        // Kembalikan collision dengan player
        if (bossCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(bossCollider, playerCollider, false);
        }

        // Recovery singkat setelah charge (boss diam sebentar)
        yield return new WaitForSeconds(0.5f);
    }

    // ---------- PHASE TRANSITION ----------

    private IEnumerator PhaseTransitionSequence()
    {
        SetState(BossState.PhaseTransition);
        canTakeDamage = false; // Immune selama transisi

        StopMovement();

        if (animator != null) animator.SetTrigger("Roar");

        yield return new WaitForSeconds(data.phaseTransitionDuration);

        canTakeDamage = true;
    }

    // ---------- DECISION (Fase 2) ----------

    private IEnumerator DecisionState()
    {
        SetState(BossState.Decision);

        if (player == null) yield break;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= data.meleeDecisionRange)
        {
            // Player dekat → melee
            yield return StartCoroutine(MeleeSequence());
        }
        else
        {
            // Player jauh → lempar batu
            yield return StartCoroutine(RangedSequence());
        }
    }

    // ---------- MELEE SEQUENCE ----------

    private IEnumerator MeleeSequence()
    {
        // --- CHASE ---
        SetState(BossState.MeleeChase);
        SetAnimSpeed(1f);

        while (!isDead)
        {
            if (player == null) yield break;

            float dist = Vector3.Distance(transform.position, player.position);

            // Player menjauh melewati meleeDecisionRange → kembali ke Decision
            if (dist > data.meleeDecisionRange)
            {
                StopMovement();
                yield break;
            }

            // Sudah dalam jarak serang → attack
            if (dist <= data.meleeAttackRange)
            {
                break;
            }

            // Jalan mengejar player
            RotateTowardsPlayer();
            MoveForward(data.meleeMoveSpeed);

            yield return null;
        }

        if (isDead) yield break;

        // --- MELEE ATTACK ---
        SetState(BossState.MeleeAttack);
        StopMovement();

        if (animator != null) animator.SetTrigger("MeleeAttack");

        // Aktifkan spider sense (Bisa di-parry sebelum hit)
        if (playerParry != null) playerParry.EnableBoarSpiderSense(this);

        // Windup singkat
        yield return new WaitForSeconds(0.3f);
        
        if (playerParry != null) playerParry.DisableSpiderSense();

        // Deteksi damage
        if (meleeAttackPoint != null)
        {
            Collider[] hits = Physics.OverlapSphere(meleeAttackPoint.position, meleeHitRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerStatus ps = hit.GetComponentInParent<PlayerStatus>();
                    if (ps != null)
                    {
                        ps.TakeDamage(data.meleeDamage);
                        break;
                    }
                }
            }
        }

        // Cooldown setelah melee attack
        yield return new WaitForSeconds(data.meleeAttackCooldown);
    }

    // ---------- RANGED SEQUENCE (Lempar Batu) ----------

    private IEnumerator RangedSequence()
    {
        if (player == null) yield break;

        // --- Step 1: AIM (boss menghadap player) ---
        SetState(BossState.RangedAim);
        StopMovement();

        float elapsed = 0f;
        while (elapsed < data.rangedAimDuration && !isDead)
        {
            RotateTowardsPlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (isDead) yield break;

        // --- Step 2: DIG (mencungkil batu) + TELEGRAPH muncul ---
        SetState(BossState.RangedDig);

        if (animator != null) animator.SetTrigger("DigRock");

        // Spawn telegraph di posisi player (akan TRACKING player terus-menerus)
        GameObject telegraph = null;
        TelegraphIndicator telegraphIndicator = null;

        if (telegraphPrefab != null)
        {
            telegraph = Instantiate(telegraphPrefab, player.position + Vector3.up * 0.05f, Quaternion.identity);
            telegraphIndicator = telegraph.GetComponent<TelegraphIndicator>();

            if (telegraphIndicator != null)
            {
                // Initialize dengan tracking ke player. Lock 0.3 detik sebelum impact.
                telegraphIndicator.Initialize(data.rockTelegraphRadius, player, 0.3f);
            }
        }

        // Tunggu animasi cungkil selesai
        yield return new WaitForSeconds(data.rockDigDuration);

        if (isDead)
        {
            if (telegraph != null) Destroy(telegraph);
            yield break;
        }

        // --- Step 3: THROW (lempar batu) ---
        SetState(BossState.RangedThrow);

        if (animator != null) animator.SetTrigger("ThrowRock");

        // Mulai isi telegraph (sinkron dengan batu terbang)
        // Selama fill, telegraph MASIH mengikuti player.
        // Di 0.3 detik terakhir, telegraph lock posisi → batu mengarah ke situ.
        if (telegraphIndicator != null)
        {
            telegraphIndicator.StartFill(data.rockFillDuration);
        }

        // Spawn batu di depan boss (rockSpawnPoint)
        if (rockPrefab != null && rockSpawnPoint != null)
        {
            GameObject rock = Instantiate(rockPrefab, rockSpawnPoint.position, Quaternion.identity);
            RockProjectile projectile = rock.GetComponent<RockProjectile>();

            if (projectile != null)
            {
                // Batu naik ke atas dulu (peakHeight), lalu bezier ke posisi telegraph
                projectile.Launch(
                    rockSpawnPoint.position,    // Start: depan boss
                    data.rockArcHeight,         // Tinggi puncak di atas start
                    data.rockFillDuration,      // Durasi sinkron dengan telegraph fill
                    data.rockDamage,
                    data.rockTelegraphRadius,
                    telegraphIndicator          // Link ke telegraph untuk live tracking
                );
            }
        }

        // Tunggu batu mendarat
        yield return new WaitForSeconds(data.rockFillDuration);

        // Cleanup telegraph
        if (telegraph != null)
        {
            Destroy(telegraph, 0.5f);
        }

        // --- Step 4: Recovery ---
        yield return new WaitForSeconds(data.rangedRecoveryTime);
    }

    // ==========================================
    // INTERNAL HELPERS
    // ==========================================

    private void SetState(BossState newState)
    {
        CurrentState = newState;
        debugState = newState.ToString();
        debugPhase = currentPhase;
    }

    private void SetAnimSpeed(float speed)
    {
        if (animator != null) animator.SetFloat("Speed", speed);
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookAtSpeed);
    }

    private void MoveForward(float speed)
    {
        if (rb != null && !rb.isKinematic)
        {
            Vector3 moveVel = transform.forward * speed;
            rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private void StopMovement()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        SetAnimSpeed(0f);
    }

    private void CheckPhaseTransition()
    {
        if (currentPhase >= 2) return; // Sudah di Fase 2, tidak perlu cek lagi

        float healthPercent = CurrentHealth / data.maxHealth;

        if (healthPercent <= data.phase2ThresholdPercent)
        {
            currentPhase = 2; // Ini akan break loop Fase 1 di BossStateMachine
            Debug.Log($"[BoarBoss] HP {healthPercent:P0} <= threshold {data.phase2ThresholdPercent:P0} → Masuk Fase 2!");
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        canTakeDamage = false;

        // Stop semua coroutine (termasuk state machine)
        StopAllCoroutines();
        StopMovement();

        SetState(BossState.Dead);
        ActiveTarget(false);

        if (animator != null) animator.SetTrigger("Die");

        // Kembalikan collision jika masih di-ignore
        if (bossCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(bossCollider, playerCollider, false);
        }

        OnBossDied?.Invoke();

        // Matikan collider
        Collider[] cols = GetComponents<Collider>();
        foreach (var col in cols) col.enabled = false;

        if (rb != null) rb.isKinematic = true;

        this.enabled = false;
    }

    // ==========================================
    // DEBUG GIZMOS
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        // Area melee hit
        if (meleeAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleeAttackPoint.position, meleeHitRadius);
        }

        if (data != null)
        {
            // Melee decision range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.meleeDecisionRange);

            // Melee attack range
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(transform.position, data.meleeAttackRange);

            // Charge max distance (garis ke depan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * data.chargeMaxDistance);

            // Wall detect ray
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * wallDetectDistance);
        }
    }
}
