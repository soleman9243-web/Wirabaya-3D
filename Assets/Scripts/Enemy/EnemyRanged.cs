using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRanged : MonoBehaviour
{
    [Header("Ranged Combat Settings")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowSpeed = 20f;
    
    [Tooltip("Jarak di mana musuh akan berhenti dan mulai membidik")]
    public float attackDistance = 15f;
    
    [Tooltip("Jika player terlalu dekat, musuh akan lari mundur")]
    public float retreatDistance = 5f;

    [Tooltip("Waktu (detik) musuh menahan bidikan sebelum menembak")]
    public float aimDuration = 2f;
    
    [Tooltip("Waktu (detik) jeda sebelum musuh bisa membidik lagi (Cooldown)")]
    public float reloadDuration = 2f;

    [Header("Visual Effects (UX)")]
    [Tooltip("Masukkan objek panah palsu (tanpa script/collider) yang di-parent ke tangan musuh")]
    public GameObject dummyArrowInHand;

    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float retreatSpeed = 4f;

    private EnemyAI enemyAI;
    private NavMeshAgent agent;
    private bool isAttacking = false;
    private bool hasDisabledOldAttack = false;

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (enemyAI == null || agent == null) return;

        // Nonaktifkan script auto-attack jadul bawaan EnemyAI (Sama seperti EnemyPatrol)
        if (!hasDisabledOldAttack)
        {
            System.Reflection.FieldInfo field = typeof(EnemyAI).GetField("attackCoroutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Coroutine routine = (Coroutine)field.GetValue(enemyAI);
                if (routine != null)
                {
                    enemyAI.StopCoroutine(routine);
                    hasDisabledOldAttack = true; 
                }
            }
        }

        // Jika musuh mati atau kena stagger
        if (enemyAI.isDead || enemyAI.isBeingTakenDown)
        {
            if (dummyArrowInHand != null && dummyArrowInHand.activeSelf) dummyArrowInHand.SetActive(false); // AUTO OFF
            if (!agent.isStopped && agent.isActiveAndEnabled) agent.isStopped = true;
            return;
        }

        // Logic Utama Ranged Enemy
        if (enemyAI.isAlerted && enemyAI.player != null)
        {
            HandleCombatPositioning();
        }
        else
        {
            // Musuh tidak sadar / rileks
            if (dummyArrowInHand != null && dummyArrowInHand.activeSelf) dummyArrowInHand.SetActive(false); // AUTO OFF
            
            if (!agent.isStopped && agent.isActiveAndEnabled) agent.isStopped = true;
            enemyAI.EnemyAnimator.SetFloat("InputY", 0f, 0.1f, Time.deltaTime);
            enemyAI.EnemyAnimator.SetBool("isAiming", false);
        }
    }

    private void HandleCombatPositioning()
    {
        if (isAttacking) return; // Jangan ubah pergerakan kalau lagi fokus nembak

        float distanceToPlayer = Vector3.Distance(transform.position, enemyAI.player.position);

        if (distanceToPlayer > attackDistance)
        {
            // Terlalu jauh -> Lari MAJU
            if (agent.isStopped) agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(enemyAI.player.position);
            
            enemyAI.EnemyAnimator.SetFloat("InputY", 1f, 0.1f, Time.deltaTime); // Animasi WalkFront
            enemyAI.EnemyAnimator.SetBool("isAiming", false); // Batalkan bidikan jika sedang jalan
        }
        else if (distanceToPlayer < retreatDistance)
        {
            // Terlalu dekat -> Lari MUNDUR (Menjaga jarak aman)
            if (agent.isStopped) agent.isStopped = false;
            agent.speed = retreatSpeed;
            
            // Cari titik di belakang musuh menjauhi player
            Vector3 dirAway = (transform.position - enemyAI.player.position).normalized;
            Vector3 retreatTarget = transform.position + dirAway * 2f;
            agent.SetDestination(retreatTarget);

            enemyAI.EnemyAnimator.SetFloat("InputY", -1f, 0.1f, Time.deltaTime); // Animasi WalkBack
            enemyAI.EnemyAnimator.SetBool("isAiming", false);

            FaceTarget(enemyAI.player.position); // Tetap lihat player walau mundur
        }
        else
        {
            // Jarak pas (di antara retreat dan attack) -> BERHENTI dan MULAI MEMBIDIK
            if (!agent.isStopped) agent.isStopped = true;
            enemyAI.EnemyAnimator.SetFloat("InputY", 0f, 0.1f, Time.deltaTime); // Animasi IdleStand

            FaceTarget(enemyAI.player.position);

            if (!isAttacking)
            {
                StartCoroutine(RangedAttackRoutine());
            }
        }
    }

    private IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;

        // 1. Mulai Aiming (Animator akan berpindah dari Empty -> DrawArrow -> Aiming)
        enemyAI.EnemyAnimator.SetBool("isAiming", true);

        // 2. Tunggu selama waktu bidik (Tangan menahan panah)
        yield return new WaitForSeconds(aimDuration);

        // Pastikan masih melihat player dan belum mati sebelum menembak
        if (!enemyAI.isDead && !enemyAI.isBeingTakenDown && enemyAI.player != null)
        {
            // 3. Tembak!
            enemyAI.EnemyAnimator.SetTrigger("Shoot");
            FireArrow();
        }
        else
        {
            // Jika batal menembak (karena mati/stagger), matikan panah
            if (dummyArrowInHand != null) dummyArrowInHand.SetActive(false);
        }

        // 4. Selesai nembak, matikan Aim (Kembali ke EmptyState)
        enemyAI.EnemyAnimator.SetBool("isAiming", false);
        if (dummyArrowInHand != null) dummyArrowInHand.SetActive(false); // SAFETY AUTO OFF

        // 5. Cooldown (Jeda sebelum musuh mengevaluasi jarak atau menembak lagi)
        yield return new WaitForSeconds(reloadDuration);

        isAttacking = false;
    }

    private void FireArrow()
    {
        // Sembunyikan panah palsu di tangan tepat saat menembak
        if (dummyArrowInHand != null)
        {
            dummyArrowInHand.SetActive(false);
        }

        if (arrowPrefab == null || firePoint == null) return;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Arahkan proyektil sedikit ke atas agar membentuk lengkungan (parabola) jika jauh
            Vector3 shootDirection = (enemyAI.player.position + Vector3.up * 1f) - firePoint.position;
            rb.linearVelocity = shootDirection.normalized * arrowSpeed;
        }
    }

    // Fungsi ini akan dipanggil oleh Animation Event dari dalam klip animasi DrawArrow!
    public void GrabArrowEvent()
    {
        if (dummyArrowInHand != null)
        {
            dummyArrowInHand.SetActive(true);
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
        }
    }
}
