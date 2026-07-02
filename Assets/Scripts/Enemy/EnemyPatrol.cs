using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Freeflow Combat Settings")]
    [Tooltip("Gunakan sistem keroyokan Arkham Style? Jika ya, pastikan attackInterval di EnemyAI lama di-set ke 9999.")]
    public bool useFreeflowCombat = true;
    public float circleRadius = 4f;
    [Tooltip("Jarak seberapa dekat musuh maju sebelum memukul. Biasanya 1.5 atau 2 meter.")]
    public float meleeAttackDistance = 1.5f;

    [Header("Patrol Settings")]
    [Tooltip("Matikan ini jika kamu ingin musuh diam mematung (cocok untuk musuh di area Tutorial).")]
    public bool enablePatrol = true;
    [Tooltip("Titik-titik untuk rute patrol. Musuh akan berkeliling sesuai urutan.")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTimeAtPoint = 2f;

    [Header("Random Patrol Settings")]
    [Tooltip("Radius area untuk patroli acak jika array Patrol Points dibiarkan kosong.")]
    public float randomPatrolRadius = 10f;

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    [Tooltip("Jarak berhenti saat mengejar. Akan otomatis disesuaikan dengan attackRange milik EnemyAI jika memungkinkan.")]
    public float stoppingDistance = 2f;
    public float rotationSpeed = 5f;

    [Header("Animation Settings")]
    [Tooltip("Nama parameter tipe float di Animator untuk animasi jalan/lari. Kosongkan jika tidak ada.")]
    public string speedAnimatorParameter = "Speed";
    [Tooltip("Parameter float untuk pergerakan kiri-kanan (Strafe). Biasanya untuk Blend Tree.")]
    public string strafeXAnimatorParameter = "InputX";
    [Tooltip("Parameter float untuk pergerakan maju-mundur. Biasanya untuk Blend Tree.")]
    public string strafeYAnimatorParameter = "InputY";

    public bool IsExecutingAttack { get; private set; } = false;
    private bool hasAttackToken = false;
    private bool hasDisabledOldAttack = false;
    private float nextClassicAttackTime = 0f;

    private EnemyAI enemyAI;
    private NavMeshAgent agent;
    
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private Vector3 initialPosition;

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
        initialPosition = transform.position;

        // Set awal speed
        agent.speed = patrolSpeed;

        // Coba sesuaikan stopping distance dengan attack range dari EnemyAI
        if (enemyAI != null && enemyAI.attackRange > 0)
        {
            // Berhenti sedikit lebih dekat dari ujung attack range agar serangannya kena
            float adjustedDistance = enemyAI.attackRange - 0.5f;
            if (adjustedDistance < 0) adjustedDistance = 0;
            stoppingDistance = adjustedDistance;
        }

        agent.stoppingDistance = stoppingDistance;

        // Mulai ke titik patrol pertama jika ada
        if (enablePatrol)
        {
            if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
            {
                agent.SetDestination(patrolPoints[0].position);
            }
            else
            {
                // Jika kosong, patroli acak
                SetRandomPatrolDestination();
            }
        }
    }

    private void Update()
    {
        if (enemyAI == null || agent == null) return;

        // SELALU matikan sistem auto-attack lama dari EnemyAI karena sekarang EnemyPatrol yang handle 100%
        if (!hasDisabledOldAttack)
        {
            System.Reflection.FieldInfo field = typeof(EnemyAI).GetField("attackCoroutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Coroutine routine = (Coroutine)field.GetValue(enemyAI);
                if (routine != null)
                {
                    enemyAI.StopCoroutine(routine);
                    hasDisabledOldAttack = true; // Cukup matikan sekali saja
                }
            }
        }

        UpdateAnimator();

        // 1. Cek apakah musuh sedang tidak bisa bergerak (Mati, Takedown, Stagger)
        if (IsDeadOrStaggered())
        {
            CancelAttack();

            if (!agent.isStopped && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
            }
            return;
        }

        // 2. Logic Movement Utama
        if (enemyAI.isAlerted && enemyAI.player != null)
        {
            ChasePlayer();
        }
        else
        {
            if (EnemyCombatManager.Instance != null) EnemyCombatManager.Instance.UnregisterEnemy(this);
            
            if (enablePatrol)
            {
                Patrol();
            }
            else
            {
                // Jika patroli dimatikan, pastikan agent berhenti
                if (!agent.isStopped && agent.isActiveAndEnabled) agent.isStopped = true;
            }
        }
    }

    public void GiveAttackToken() { hasAttackToken = true; }
    public void RevokeAttackToken() { hasAttackToken = false; }

    public void CancelAttack()
    {
        if (IsExecutingAttack)
        {
            StopAllCoroutines();
            IsExecutingAttack = false;
            hasAttackToken = false;
            
            if (enemyAI.weaponCollider != null) enemyAI.weaponCollider.enabled = false;
            if (enemyAI.player != null)
            {
                PlayerParry parry = enemyAI.player.GetComponent<PlayerParry>();
                if (parry != null) parry.DisableSpiderSense();
            }

            if (EnemyCombatManager.Instance != null) EnemyCombatManager.Instance.ReportAttackFinished(this);
        }
    }

    private void ChasePlayer()
    {
        if (useFreeflowCombat && EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.RegisterEnemy(this);

            if (hasAttackToken)
            {
                if (agent.isStopped) agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.stoppingDistance = meleeAttackDistance - 0.2f; // Berhenti sedikit lebih dekat dari jarak serang

                float distanceToPlayer = Vector3.Distance(transform.position, enemyAI.player.position);

                // Menggunakan meleeAttackDistance alih-alih attackRange lama
                if (distanceToPlayer <= meleeAttackDistance)
                {
                    if (!agent.isStopped) agent.isStopped = true;
                    FaceTarget(enemyAI.player.position);

                    if (!IsExecutingAttack)
                    {
                        StartCoroutine(ExecuteArkhamAttack());
                    }
                }
                else
                {
                    if (agent.isStopped) agent.isStopped = false;
                    agent.SetDestination(enemyAI.player.position);
                }
            }
            else
            {
                // Mengitari player
                if (agent.isStopped) agent.isStopped = false;
                agent.speed = patrolSpeed; // Jalan pelan saat mengelilingi
                agent.stoppingDistance = 0.5f;

                Vector3 circlePos = EnemyCombatManager.Instance.GetCirclePosition(this, circleRadius);
                agent.SetDestination(circlePos);
                FaceTarget(enemyAI.player.position);
            }
        }
        else
        {
            // Logic lama jika tidak pakai Freeflow Combat Manager
            if (enablePatrol)
            {
                if (agent.isStopped) agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.stoppingDistance = stoppingDistance;

                float distanceToPlayer = Vector3.Distance(transform.position, enemyAI.player.position);

                if (distanceToPlayer <= enemyAI.attackRange)
                {
                    if (!agent.isStopped) agent.isStopped = true;
                    FaceTarget(enemyAI.player.position);

                    // Langsung serang jika sudah waktunya
                    if (!IsExecutingAttack && Time.time >= nextClassicAttackTime)
                    {
                        StartCoroutine(ExecuteClassicAttack());
                    }
                }
                else
                {
                    if (agent.isStopped) agent.isStopped = false;
                    agent.SetDestination(enemyAI.player.position);
                }
            }
            else
            {
                // JIKA PATROL MATI (Tutorial Dummy), musuh HANYA diam dan menyerang jika player dekat
                if (!agent.isStopped) agent.isStopped = true;

                float distanceToPlayer = Vector3.Distance(transform.position, enemyAI.player.position);
                
                if (distanceToPlayer <= enemyAI.attackRange)
                {
                    FaceTarget(enemyAI.player.position);

                    // Langsung serang jika sudah waktunya
                    if (!IsExecutingAttack && Time.time >= nextClassicAttackTime)
                    {
                        StartCoroutine(ExecuteClassicAttack());
                    }
                }
            }
        }
    }

    private IEnumerator ExecuteClassicAttack()
    {
        IsExecutingAttack = true;
        nextClassicAttackTime = Time.time + enemyAI.attackInterval; // Set delay untuk serangan berikutnya

        PlayerParry parry = enemyAI.player.GetComponent<PlayerParry>();
        if (parry != null) parry.EnableSpiderSense(enemyAI);
        
        enemyAI.EnemyAnimator.SetTrigger("Attack");
        
        yield return new WaitForSeconds(0.5f);
        if (enemyAI.weaponCollider != null) enemyAI.weaponCollider.enabled = true;
        
        yield return new WaitForSeconds(0.5f);
        if (enemyAI.weaponCollider != null) enemyAI.weaponCollider.enabled = false;
        if (parry != null) parry.DisableSpiderSense();
        
        IsExecutingAttack = false;
    }

    private IEnumerator ExecuteArkhamAttack()
    {
        IsExecutingAttack = true;

        PlayerParry parry = enemyAI.player.GetComponent<PlayerParry>();
        if (parry != null) parry.EnableSpiderSense(enemyAI);
        
        enemyAI.EnemyAnimator.SetTrigger("Attack");
        
        // --- Arkham Lunge (Meluncur ke arah player saat memukul) ---
        float lungeDuration = 0.5f; // Waktu wind-up sebelum pedang memukul (0.5 detik)
        float timer = 0f;
        
        while (timer < lungeDuration)
        {
            timer += Time.deltaTime;
            
            // Batalkan lunge jika mati/stagger di tengah jalan
            if (IsDeadOrStaggered()) yield break;

            if (agent.isActiveAndEnabled && enemyAI.player != null)
            {
                // Target posisi lunge: 1 meter di depan player agar pedang pas kena dan tidak nembus badan
                Vector3 directionToPlayer = (enemyAI.player.position - transform.position).normalized;
                directionToPlayer.y = 0;
                Vector3 targetLungePos = enemyAI.player.position - (directionToPlayer * 1.0f);
                
                // Slide mulus mendekati player
                agent.Move((targetLungePos - transform.position) * (Time.deltaTime * 6f));
                FaceTarget(enemyAI.player.position);
            }
            
            yield return null;
        }
        
        if (enemyAI.weaponCollider != null) enemyAI.weaponCollider.enabled = true;
        
        yield return new WaitForSeconds(0.5f);
        if (enemyAI.weaponCollider != null) enemyAI.weaponCollider.enabled = false;
        if (parry != null) parry.DisableSpiderSense();
        
        hasAttackToken = false;
        IsExecutingAttack = false;

        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.ReportAttackFinished(this);
        }
    }

    private void Patrol()
    {
        if (agent.isStopped) agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f; // Pas patrol, target point harus dicapai

        bool usePoints = (patrolPoints != null && patrolPoints.Length > 0);

        // Cek apakah sudah sampai di titik tujuan
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                if (usePoints)
                {
                    // Pindah ke titik selanjutnya
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    if (patrolPoints[currentPatrolIndex] != null)
                    {
                        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    }
                }
                else
                {
                    // Patroli acak
                    SetRandomPatrolDestination();
                }
                waitTimer = 0f;
            }
        }
        else
        {
            waitTimer = 0f;
        }
    }

    private void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * randomPatrolRadius;
        randomDirection += initialPosition;
        NavMeshHit hit;
        
        // Cari titik terdekat di NavMesh dari posisi acak (berpusat di posisi awal)
        if (NavMesh.SamplePosition(randomDirection, out hit, randomPatrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Fallback: kembali ke posisi awal
            agent.SetDestination(initialPosition);
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Jangan mendongak/menunduk
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private bool IsStaggeredOrHit()
    {
        if (enemyAI.EnemyAnimator == null) return false;

        AnimatorStateInfo stateInfo = enemyAI.EnemyAnimator.GetCurrentAnimatorStateInfo(0);
        
        // Cek nama state animasi (Sesuaikan dengan nama State di Animator Musuh jika berbeda)
        if (stateInfo.IsName("Stagger") || stateInfo.IsName("Hit") || stateInfo.IsName("GetTakedown"))
        {
            return true;
        }

        return false;
    }

    public bool IsDeadOrStaggered()
    {
        if (enemyAI == null) return false;
        return enemyAI.isDead || enemyAI.isBeingTakenDown || IsStaggeredOrHit();
    }

    private void UpdateAnimator()
    {
        if (enemyAI.EnemyAnimator != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            
            if (!string.IsNullOrEmpty(speedAnimatorParameter))
            {
                enemyAI.EnemyAnimator.SetFloat(speedAnimatorParameter, currentSpeed);
            }

            if (!string.IsNullOrEmpty(strafeXAnimatorParameter) && !string.IsNullOrEmpty(strafeYAnimatorParameter))
            {
                // Cek apakah musuh sedang dalam mode mengitari player (circling)
                bool isCircling = (useFreeflowCombat && enemyAI.isAlerted && !hasAttackToken && EnemyCombatManager.Instance != null);

                if (isCircling)
                {
                    // Saat mengitari, gunakan kecepatan lokal (kiri/kanan/maju/mundur)
                    Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
                    enemyAI.EnemyAnimator.SetFloat(strafeXAnimatorParameter, localVelocity.x);
                    enemyAI.EnemyAnimator.SetFloat(strafeYAnimatorParameter, localVelocity.z);
                }
                else
                {
                    // Saat jalan biasa (patroli) atau lari mengejar (punya token), matikan gerakan menyamping (X = 0)
                    // Lempar semua kecepatan ke sumbu Y (Maju) agar animasi lurusnya terlihat bagus
                    enemyAI.EnemyAnimator.SetFloat(strafeXAnimatorParameter, 0f);
                    enemyAI.EnemyAnimator.SetFloat(strafeYAnimatorParameter, currentSpeed);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Gambar lingkaran area patroli acak di Editor jika tidak pakai titik
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 center = Application.isPlaying ? initialPosition : transform.position;
            Gizmos.DrawWireSphere(center, randomPatrolRadius);
        }
    }
}
