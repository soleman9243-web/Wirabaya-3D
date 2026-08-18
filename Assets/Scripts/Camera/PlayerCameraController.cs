using UnityEngine;
using Cinemachine;
using StarterAssets;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// PlayerCameraController — Script pusat untuk semua efek kamera third-person player.
    /// Tambahkan fitur kamera baru di sini (sprint zoom, shake, dll).
    /// Pasang di GameObject yang sama dengan CinemachineVirtualCamera.
    /// </summary>
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("Cinemachine Virtual Camera. Jika kosong, otomatis ambil dari GameObject ini.")]
        public CinemachineVirtualCamera virtualCamera;

        [Tooltip("Input System pemain. Jika kosong, otomatis dicari di Scene.")]
        public StarterAssetsInputs playerInputs;

        // ============================================================
        // SPRINT CAMERA (Zoom Out saat lari)
        // ============================================================
        [Header("=== Sprint Camera ===")]

        [Tooltip("Aktifkan efek FOV zoom-out saat sprint.")]
        public bool useDynamicFOV = true;

        [Tooltip("FOV kamera saat diam / jalan normal.")]
        [Range(20f, 100f)]
        public float normalFOV = 40f;

        [Tooltip("FOV kamera saat sprint (zoom out).")]
        [Range(20f, 120f)]
        public float sprintFOV = 55f;

        [Tooltip("Kecepatan transisi FOV.")]
        [Min(0.1f)]
        public float fovTransitionSpeed = 5f;

        [Space(5)]
        [Tooltip("Aktifkan perubahan jarak kamera saat sprint (Cinemachine 3rd Person Follow).")]
        public bool useDynamicDistance = true;

        [Tooltip("Jarak kamera saat diam / jalan normal.")]
        [Min(0.1f)]
        public float normalDistance = 4f;

        [Tooltip("Jarak kamera saat sprint.")]
        [Min(0.1f)]
        public float sprintDistance = 5.2f;

        [Tooltip("Kecepatan transisi jarak kamera.")]
        [Min(0.1f)]
        public float distanceTransitionSpeed = 5f;

        // ============================================================
        // AUDIO SFX
        // ============================================================
        [Header("=== Sprint Audio SFX ===")]

        [Tooltip("AudioSource untuk efek suara. Jika kosong, ambil dari GameObject ini.")]
        public AudioSource audioSource;

        [Tooltip("Audio clip saat mulai sprint (whoosh / derap kaki).")]
        public AudioClip sprintAudioClip;

        [Tooltip("Volume audio sprint.")]
        [Range(0f, 1f)]
        public float sprintAudioVolume = 0.8f;

        // ============================================================
        // TAMBAHKAN FITUR KAMERA BARU DI BAWAH SINI
        // (contoh: camera shake, aim zoom, cutscene, dll)
        // ============================================================


        // === Private ===
        private Cinemachine3rdPersonFollow _thirdPersonFollow;
        private bool _wasSprinting = false;

        private void Awake()
        {
            if (virtualCamera == null)
            {
                virtualCamera = GetComponent<CinemachineVirtualCamera>();
            }
        }

        private void Start()
        {
            // Cari PlayerInputs otomatis jika belum di-assign
            if (playerInputs == null)
            {
#if UNITY_2023_1_OR_NEWER
                playerInputs = Object.FindFirstObjectByType<StarterAssetsInputs>();
#else
                playerInputs = Object.FindObjectOfType<StarterAssetsInputs>();
#endif
            }

            if (virtualCamera != null)
            {
                if (normalFOV <= 0f)
                {
                    normalFOV = virtualCamera.m_Lens.FieldOfView;
                }

                _thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (_thirdPersonFollow != null && normalDistance <= 0f)
                {
                    normalDistance = _thirdPersonFollow.CameraDistance;
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Update()
        {
            if (virtualCamera == null) return;

            bool isSprinting = GetSprintState();

            HandleSprintFOV(isSprinting);
            HandleSprintDistance(isSprinting);
            HandleSprintAudio(isSprinting);

            // === Tambahkan panggilan fitur kamera baru di sini ===

            _wasSprinting = isSprinting;
        }

        // ============================================================
        // SPRINT CAMERA METHODS
        // ============================================================

        private bool GetSprintState()
        {
            if (playerInputs != null)
            {
                bool isMoving = playerInputs.move.sqrMagnitude > 0.01f;
                return playerInputs.sprint && isMoving;
            }
            else
            {
                bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
                return isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }
        }

        private void HandleSprintFOV(bool isSprinting)
        {
            if (!useDynamicFOV) return;

            float targetFOV = isSprinting ? sprintFOV : normalFOV;
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.m_Lens.FieldOfView,
                targetFOV,
                Time.deltaTime * fovTransitionSpeed
            );
        }

        private void HandleSprintDistance(bool isSprinting)
        {
            if (!useDynamicDistance || _thirdPersonFollow == null) return;

            float targetDistance = isSprinting ? sprintDistance : normalDistance;
            _thirdPersonFollow.CameraDistance = Mathf.Lerp(
                _thirdPersonFollow.CameraDistance,
                targetDistance,
                Time.deltaTime * distanceTransitionSpeed
            );
        }

        private void HandleSprintAudio(bool isSprinting)
        {
            if (isSprinting && !_wasSprinting)
            {
                if (audioSource != null && sprintAudioClip != null)
                {
                    audioSource.PlayOneShot(sprintAudioClip, sprintAudioVolume);
                }
            }
        }

        // ============================================================
        // PUBLIC HELPER METHODS
        // ============================================================

        public void SetNormalFOV(float fov)
        {
            normalFOV = fov;
        }

        public void SetSprintFOV(float fov)
        {
            sprintFOV = fov;
        }
    }
}
