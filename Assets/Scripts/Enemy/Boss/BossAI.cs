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
    public Collider weaponCollider;

    [Header("Vision & Tracking")]
    public float lookAtSpeed = 5f;
    public float engageDistance = 15f;

    [Header("Parry & Stagger")]

    [Header("Events")]
    public UnityEvent<float, float> OnBossHealthChanged;
    public UnityEvent OnBossDied;

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
            yield return new WaitForSeconds(0.5f);

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
                    if (distanceToPlayer <= atk.attackRange)
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

        // Tampilkan indikator parry (spider sense) ke player jika bisa di-parry
        if (attack.canBeParried && playerParry != null)
        {
            playerParry.EnableBossSpiderSense(this);
        }

        // Tunggu windup time
        yield return new WaitForSeconds(attack.windupTime);

        // Nyalakan collider weapon untuk damage
        if (weaponCollider != null) weaponCollider.enabled = true;

        // Damage dikendalikan oleh OnTriggerEnter di Weapon (atau bisa langsung spherecast di sini)
        // Kita nyalakan 0.5s sebagai contoh "active frames"
        yield return new WaitForSeconds(0.5f);

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

        isAttacking = false;
        StartCoroutine(StaggerRoutine());
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

        OnBossDied?.Invoke();

        Collider[] cols = GetComponents<Collider>();
        foreach(var col in cols) col.enabled = false;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null) rb.isKinematic = true;

        this.enabled = false;
    }
}
