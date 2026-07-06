using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Target yang diikuti. Kosongkan saja untuk otomatis mencari Player.")]
    public Transform target;

    [Tooltip("Apakah NPC ini sedang dalam mode mengikuti?")]
    public bool isFollowing = false;
    
    [Tooltip("Jarak berhenti NPC dari target.")]
    public float stoppingDistance = 2.5f;

    [Tooltip("Kecepatan NPC saat mengikuti.")]
    public float followSpeed = 3.5f;

    [Header("Animation Settings")]
    [Tooltip("Masukkan komponen Animator (contoh: model di dalam child). Jika kosong, script akan mencarinya otomatis.")]
    public Animator animator;

    [Tooltip("Parameter float di Animator untuk kecepatan jalan (kosongkan jika tidak ada)")]
    public string animatorSpeedParameter = "Speed";

    private NavMeshAgent agent;
    private bool isPaused = false; // Untuk pause saat sedang diajak bicara lagi

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Cari Animator di child jika belum diisi di inspector
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Setup dasar agen
        agent.stoppingDistance = stoppingDistance;
        agent.speed = followSpeed;

        // Otomatis mencari Player jika target belum diset
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void Update()
    {
        // Jika sedang mengikuti dan tidak di-pause
        if (isFollowing && !isPaused && target != null)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        else
        {
            // Hentikan agen jika tidak mengikuti atau sedang di-pause
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        // Integrasi Animasi Jalan
        if (animator != null && !string.IsNullOrEmpty(animatorSpeedParameter))
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                // Hitung kecepatan agen dan kirim ke parameter animator
                float speed = agent.velocity.magnitude;
                animator.SetFloat(animatorSpeedParameter, speed);
            }
            else
            {
                animator.SetFloat(animatorSpeedParameter, 0f);
            }
        }
    }

    // Panggil fungsi ini (lewat UnityEvent atau script lain) untuk mulai mengikuti
    public void StartFollowing()
    {
        isFollowing = true;
        isPaused = false;
    }

    // Panggil fungsi ini (lewat UnityEvent atau script lain) untuk berhenti mengikuti secara permanen
    public void StopFollowing()
    {
        isFollowing = false;
        isPaused = false;
    }

    // Panggil fungsi ini saat dialog dimulai (agar NPC berhenti sebentar untuk ngobrol)
    public void PauseFollowing()
    {
        isPaused = true;
    }

    // Panggil fungsi ini saat dialog selesai (melanjutkan mengikuti jika isFollowing true)
    public void ResumeFollowing()
    {
        isPaused = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Menggambar area Stopping Distance (Garis kuning)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // Menggambar garis panduan ke target jika ada (Garis hijau)
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
