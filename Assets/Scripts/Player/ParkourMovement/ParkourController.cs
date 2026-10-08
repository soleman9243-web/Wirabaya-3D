using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class ParkourController : MonoBehaviour
{
    EnvironmentScanner environmentScanner;
    Animator animator;
    ThirdPersonController thirdPersonController;
    StarterAssetsInputs _input;
    CharacterController characterController;

    [SerializeField] List<ParkourAction> parkourActions;
    [SerializeField] float rotationSpeed = 500f; // Sesuaikan dengan speed rotasi di ThirdPersonController

    [Header("Quest Integration")]
    [Tooltip("ID Objektif yang akan selesai secara otomatis SATU KALI saat berhasil melakukan parkour.")]
    public string parkourObjectiveId = "obj_SelesaikanParkour";

    [Header("Audio Setup")]
    [Tooltip("Slot AudioSource untuk memutar efek suara gerakan parkour.")]
    public AudioSource audioSource;

    private GameObject lastObstacle;
    public bool InAction => inAction;
    private bool inAction;

    private void Awake()
    {
        environmentScanner = GetComponent<EnvironmentScanner>();
        animator = GetComponent<Animator>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        characterController = GetComponent<CharacterController>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Fitur auto-sort dihapus agar urutan animasi di Inspector tidak teracak (gacha).
        // Pastikan Anda menaruh aksi spesifik (seperti Vault) di urutan paling atas di Unity Inspector.
    }

    /// <summary>
    /// Menerapkan root motion posisi saat parkour sedang berjalan.
    /// Ini diperlukan karena OnAnimatorMove di ThirdPersonController tidak menerapkan
    /// deltaPosition, sehingga karakter tidak bergerak tanpa handler ini.
    /// Catatan: Rotasi TIDAK diterapkan di sini karena sudah di-handle oleh
    /// ThirdPersonController.OnAnimatorMove() dan RotateToObstacle di coroutine.
    /// </summary>


    private void Update()
    {
        if (_input.jump && !inAction)
        {
            var hitData = environmentScanner.ObstacleCheck();

            if (hitData.forwardHitFound && hitData.heightHitFound)
            {
                foreach (var action in parkourActions)
                {
                    if (action.CheckIfPossible(hitData, transform))
                    {
                        StartCoroutine(DoParkourAction(action, hitData));
                        break;
                    }
                }
            }
        }
    }

    IEnumerator DoParkourAction(ParkourAction action, ObstacleHitData hitData)
    {
        inAction = true;
        _input.jump = false;
        thirdPersonController.SetControl(false);
        
        // SIMPAN UKURAN ASLI KAPSUL
        float originalHeight = characterController.height;
        float originalRadius = characterController.radius;
        Vector3 originalCenter = characterController.center;

        // PERKECIL KAPSUL (Tiny Capsule Mode)
        // Kita tidak mematikan CharacterController agar Fisika Unity tetap mencegah karakter berguling masuk ke dalam pohon.
        // Tapi kita perkecil ukurannya agar dia bisa menembus melewati tembok.
        characterController.height = 0.1f;
        characterController.radius = 0.1f;
        characterController.center = new Vector3(0, 0.05f, 0);
        // Putar audio SFX parkour jika ada
        if (audioSource != null && action.ActionAudioClip != null)
        {
            audioSource.PlayOneShot(action.ActionAudioClip, action.AudioVolume);
        }

        animator.SetBool("Grounded", true);
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Jump", false);
        animator.SetBool("FreeFall", false);

        animator.CrossFadeInFixedTime(action.AnimName, 0.2f);
        yield return null; // Tunggu satu frame agar transisi animasi terbaca

        var animState = animator.GetNextAnimatorStateInfo(0);
        if (!animState.IsName(action.AnimName))
        {
            animState = animator.GetCurrentAnimatorStateInfo(0);
            if (!animState.IsName(action.AnimName))
            {
                Debug.LogWarning($"Peringatan: Animasi '{action.AnimName}' tidak terdeteksi.");
            }
        }

        float timer = 0f;
        float animLength = animState.length;

        // 1. Setup Rotasi ke arah Rintangan
        Quaternion targetRotation = transform.rotation;
        if (action.RotateToObstacle)
        {
            // Ambil arah normal yang berlawanan dari permukaan rintangan
            targetRotation = Quaternion.LookRotation(-hitData.forwardHit.normal);
        }

        // 2. Setup Posisi Target Matching
        Vector3 matchPosition = hitData.heightHit.point;

        // Panggil MatchTarget SEKALI SAJA sebelum loop (bukan setiap frame di dalam loop)
        if (action.EnableTargetMatching)
        {
            animator.MatchTarget(matchPosition, transform.rotation, action.MatchBodyPart,
                new MatchTargetWeightMask(action.MatchPositionWeight, 0),
                action.MatchStartTime, action.MatchTargetTime);
        }

        // Loop utama pengganti Lerp
        while (timer < animLength)
        {
            timer += Time.deltaTime;

            // Transisi Break: Jika animator sedang pindah ke state awal/Locomotion (untuk Vault)
            if (animator.IsInTransition(0) && timer > 0.5f)
            {
                break;
            }

            // Eksekusi Rotasi
            if (action.RotateToObstacle)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // 3. Post Action Delay
        if (action.PostActionDelay > 0)
        {
            yield return new WaitForSeconds(action.PostActionDelay);
        }

        // KEMBALIKAN UKURAN ASLI KAPSUL
        characterController.height = originalHeight;
        characterController.radius = originalRadius;
        characterController.center = originalCenter;

        Physics.SyncTransforms(); // Update transform fisik sebelum mengembalikan kontrol
        yield return new WaitForEndOfFrame();

        _input.jump = false;
        thirdPersonController.SetControl(true);
        inAction = false;

        // SELESAIKAN QUEST PARKOUR
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(parkourObjectiveId))
        {
            if (QuestManager.Instance.IsObjectiveActive(parkourObjectiveId))
            {
                // Cek agar pemain tidak menyelesaikan quest dengan memanjat tembok yang SAMA berulang kali
                if (hitData.forwardHit.collider != null && hitData.forwardHit.collider.gameObject != lastObstacle)
                {
                    QuestManager.Instance.AddProgress(parkourObjectiveId, 1);
                    lastObstacle = hitData.forwardHit.collider.gameObject;
                }
            }
        }
    }
}