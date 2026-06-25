using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossAI : MonoBehaviour
{
    [Header("Boss Configuration")]
    public BossData bossData;
    public Animator animator;
    public Transform player;

    [Header("Combat & Target References")]
    [Tooltip("Opsional: Collider lama jika masih butuh OnTriggerEnter")]
    public Collider weaponCollider; 
    [Tooltip("Objek kosong di depan boss sebagai pusat area pukulan (SphereCast)")]
    public Transform attackPos;
    [Tooltip("Radius area pukulan boss")]
    public float attackHitRadius = 2f;
    [Tooltip("Masukkan objek lingkaran target merah di bawah kaki boss")]
    [SerializeField] private GameObject activeTargetObject;
    [Tooltip("Masukkan prefab efek pukulan (darah/percikan api)")]
    [SerializeField] private GameObject hitVfx;

    [Header("Vision & Tracking")]
    public float lookAtSpeed = 5f;
    public float engageDistance = 15f;
    
    [Header("Behavior Settings")]
    [Tooltip("Jika dicentang, boss akan agresif (selalu mengejar player & jeda serang sangat minim)")]
    public bool isAggressive = false;
    
    [Tooltip("Jarak Boss berhenti maju untuk mulai menyerang (hanya jika tidak agresif)")]
    public float stoppingDistance = 2.5f;
    
    [Tooltip("Kecepatan jalan normal Boss")]
    public float walkSpeed = 3f;
    
    [Tooltip("Kecepatan lari Boss saat agresif mengejar player")]
    public float runSpeed = 6f;

    [Header("Parry & Stagger")]

    [Header("Events")]
    public UnityEvent<float, float> OnBossHealthChanged = new UnityEvent<float, float>();
    public UnityEvent OnBossDied = new UnityEvent();

    public float CurrentHealth { get; private set; }
    private int currentPhaseIndex = 0;
    
    private bool isDead = false;
    private bool isStaggered = false;
    private bool isAttacking = false;

    private Coroutine combatCoroutine;
    private PlayerParry playerParry;

    private void Start()
    {
        if (bossData != null)
        {
            CurrentHealth = bossData.maxHealth;
            OnBossHealthChanged?.Invoke(CurrentHealth, bossData.maxHealth);
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerParry = player.GetComponent<PlayerParry>();
        }

        if (weaponCollider != null) weaponCollider.enabled = false;

        ActiveTarget(false); // Matikan indikator visual saat game baru dimulai

        combatCoroutine = StartCoroutine(CombatRoutine());
    }

    private void Update()
    {
        if (isDead || isStaggered) return;

        // Boss selalu menghadap player jika dalam jarak aggro dan tidak sedang melakukan attack yang mengunci rotasi
        if (!isAttacking && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= engageDistance)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookAtSpeed);
                }

                // Bergerak mendekati player jika masih di luar jarak serang (atau jika agresif dan belum cukup dekat)
                float currentStopDist = isAggressive ? 1.5f : stoppingDistance; // Agresif = nempel terus
                
                if (dist > currentStopDist)
                {
                    float currentSpeed = isAggressive ? runSpeed : walkSpeed;
                    
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        // Set velocity untuk maju perlahan/lari
                        Vector3 moveDir = transform.forward * currentSpeed;
                        rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.z);
                    }
                    else
                    {
                        transform.position += transform.forward * currentSpeed * Time.deltaTime;
                    }

                    if (animator != null) animator.SetFloat("Speed", isAggressive ? 2f : 1f); // 2f = lari, 1f = jalan
                }
                else
                {
                    if (animator != null) animator.SetFloat("Speed", 0f); // Set animasi diam
                    
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Stop
                    }
                }
            }
            else
            {
                if (animator != null) animator.SetFloat("Speed", 0f); // Set animasi diam
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, bossData.maxHealth);

        OnBossHealthChanged?.Invoke(CurrentHealth, bossData.maxHealth);
        CheckPhaseChange();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void CheckPhaseChange()
    {
        float healthPercentage = CurrentHealth / bossData.maxHealth;

        // Cari fase yang sesuai dengan HP saat ini
        for (int i = 0; i < bossData.phases.Count; i++)
        {
            if (healthPercentage <= bossData.phases[i].healthThresholdPercentage)
            {
                if (currentPhaseIndex != i)
                {
                    currentPhaseIndex = i;
                    Debug.Log($"Boss {bossData.bossName} entering Phase {i + 1}");
                    // Opsional: Mainkan animasi transisi fase
                }
            }
        }
    }

    private IEnumerator CombatRoutine()
    {
        while (!isDead)
        {
            // Jika agresif, jeda mikir boss jauh lebih cepat
            float delay = isAggressive ? 0.1f : 0.5f;
            yield return new WaitForSeconds(delay);

            if (isStaggered || player == null) continue;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= engageDistance)
            {
                // Ambil daftar serangan yang diperbolehkan di fase ini
                var allowedAttacks = bossData.phases[currentPhaseIndex].allowedAttacks;
                if (allowedAttacks.Count == 0) continue;

                // Filter serangan berdasarkan jarak
                List<BossAttackPattern> validAttacks = new List<BossAttackPattern>();
                foreach (var atk in allowedAttacks)
                {
                    if (distanceToPlayer >= atk.minAttackRange && distanceToPlayer <= atk.attackRange)
                    {
                        validAttacks.Add(atk);
                    }
                }

                // Jika ada serangan yang valid, pilih secara acak
                if (validAttacks.Count > 0)
                {
                    BossAttackPattern selectedAttack = validAttacks[Random.Range(0, validAttacks.Count)];
                    yield return StartCoroutine(ExecuteAttack(selectedAttack));
                }
                else
                {
                    // Jika player terlalu jauh, boss bisa bergerak maju (bisa ditambah logic movement di sini)
                    // Untuk sekarang kita hanya tunggu
                }
            }
        }
    }

    private IEnumerator ExecuteAttack(BossAttackPattern attack)
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        // Tampilkan indikator parry
        if (attack.canBeParried && playerParry != null)
        {
            playerParry.EnableBossSpiderSense(this);
        }

        // Fase Windup: Boss tetap menengok mengikuti player (mengunci target)
        float windupTimer = 0f;
        while (windupTimer < attack.windupTime)
        {
            if (player != null)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    // Memutar boss agar selalu menghadap player selama ancang-ancang
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * (lookAtSpeed * 1.5f)); 
                }
            }
            windupTimer += Time.deltaTime;
            yield return null;
        }

        // Nyalakan collider weapon (opsional jika masih pakai cara lama)
        if (weaponCollider != null) weaponCollider.enabled = true;

        bool hasHitPlayer = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        Collider bossCollider = GetComponent<Collider>();
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;

        if (attack.isDashAttack)
        {
            float distanceCovered = 0f;
            Vector3 lockedDashDirection = transform.forward;

            while (distanceCovered < attack.dashDistance)
            {
                float moveStep = attack.dashSpeed * Time.deltaTime;
                transform.position += lockedDashDirection * moveStep;
                distanceCovered += moveStep;

                // Deteksi damage via OverlapSphere
                if (!hasHitPlayer && attackPos != null)
                {
                    Collider[] hits = Physics.OverlapSphere(attackPos.position, attackHitRadius);
                    foreach (Collider hit in hits)
                    {
                        if (hit.CompareTag("Player"))
                        {
                            PlayerStatus pStatus = hit.GetComponentInParent<PlayerStatus>();
                            if (pStatus != null)
                            {
                                pStatus.TakeDamage(attack.damage);
                                hasHitPlayer = true; 
                                break;
                            }
                        }
                    }
                }

                yield return null;
            }
        }
        else
        {
            float timer = 0f;
            while (timer < 0.5f) // 0.5 detik active frames untuk serangan biasa
            {
                // Sistem Damage Baru
                if (!hasHitPlayer && attackPos != null)
                {
                    Collider[] hits = Physics.OverlapSphere(attackPos.position, attackHitRadius);
                    foreach (Collider hit in hits)
                    {
                        if (hit.CompareTag("Player"))
                        {
                            PlayerStatus pStatus = hit.GetComponentInParent<PlayerStatus>();
                            if (pStatus != null)
                            {
                                pStatus.TakeDamage(attack.damage);
                                hasHitPlayer = true; 
                                break;
                            }
                        }
                    }
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        if (weaponCollider != null) weaponCollider.enabled = false;

        if (playerParry != null)
        {
            playerParry.DisableSpiderSense();
        }

        isAttacking = false;

        // Recovery Delay (Bos diam memberi kesempatan player memukul/isi stamina)
        yield return new WaitForSeconds(attack.recoveryDelay);
    }

    public void TriggerStagger()
    {
        if (isDead) return;

        if (combatCoroutine != null) StopCoroutine(combatCoroutine);

        if (playerParry != null) playerParry.DisableSpiderSense();

        if (weaponCollider != null) weaponCollider.enabled = false;

        ResetPlayerCollision(); // Jaga-jaga jika boss di-stagger pas lagi dash
        isAttacking = false;
        StartCoroutine(StaggerRoutine());
    }

    private void ResetPlayerCollision()
    {
        Collider bossCollider = GetComponent<Collider>();
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
        if (bossCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(bossCollider, playerCollider, false);
        }
    }



    private IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        if (animator != null) animator.SetTrigger("Stagger");

        yield return new WaitForSeconds(2.5f); // Waktu boss terdiam saat kena parry

        isStaggered = false;
        if (animator != null) animator.SetTrigger("Recover");

        // Mulai ulang rutinitas combat
        combatCoroutine = StartCoroutine(CombatRoutine());
    }

    private void Die()
    {
        isDead = true;
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);

        if (weaponCollider != null) weaponCollider.enabled = false;

        if (animator != null) animator.SetTrigger("Die");

        ActiveTarget(false); // Matikan indikator saat mati

        OnBossDied?.Invoke();

        Collider[] cols = GetComponents<Collider>();
        foreach(var col in cols) col.enabled = false;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null) rb.isKinematic = true;

        this.enabled = false;
    }

    public void ActiveTarget(bool isActive)
    {
        if (activeTargetObject != null)
        {
            activeTargetObject.SetActive(isActive);
        }
    }

    public void SpawnHitVfx(Vector3 hitPos)
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, hitPos + Vector3.up * 1.5f, Quaternion.identity); // Vector up biar gak nyentuh tanah banget
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPos == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackHitRadius);
    }
}
