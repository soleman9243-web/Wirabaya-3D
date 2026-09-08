using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// PlayerFootIK: Sistem IK Penempatan Kaki & Penyesuaian Pinggul (Foot IK & Pelvis Offset).
    /// - 100% Native Mecanim Humanoid OnAnimatorIK (Bebas Error Burst).
    /// - Telapak kaki otomatis menempel dan berotasi sesuai kemiringan lereng, tanah, batu, tangga.
    /// - Menurunkan panggul (Pelvis drop) otomatis saat kaki beda ketinggian.
    /// - Bobot IK otomatis berkurang saat berlari agar animasi lari tetap natural.
    /// - Menggunakan SmoothDamp untuk transisi yang sangat halus tanpa patah-patah.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerFootIK : MonoBehaviour
    {
        [Header("Status & Kontrol Utama")]
        [Tooltip("Aktifkan atau nonaktifkan Foot IK.")]
        public bool enableFootIK = true;

        [Range(0f, 1f)]
        [Tooltip("Bobot IK saat diam/jalan pelan (1 = aktif penuh).")]
        public float ikWeight = 1f;

        [Header("Layer & Deteksi Permukaan Tanah")]
        [Tooltip("Layer tanah/lingkungan yang bisa diinjak.")]
        public LayerMask groundLayer = ~0;

        [Tooltip("Jarak sensor raycast mendeteksi tanah dari atas telapak kaki.")]
        public float raycastDistance = 1.2f;

        [Tooltip("Offset ketinggian telapak kaki agar sol sepatu menempel pas.")]
        public float footHeightOffset = 0.06f;

        [Header("Pengaturan Pinggul / Pelvis (Hips Drop)")]
        [Tooltip("Aktifkan pelvis drop otomatis saat kaki beda ketinggian.")]
        public bool adjustPelvis = true;

        [Tooltip("Batas maksimum penurunan pinggul (meter).")]
        public float maxPelvisDrop = 0.35f;

        [Header("Kehalusan Gerakan (Smoothing)")]
        [Tooltip("Waktu smoothing posisi kaki (semakin besar = semakin halus).")]
        [Range(0.01f, 0.3f)]
        public float footPosSmoothTime = 0.08f;

        [Tooltip("Waktu smoothing rotasi kaki mengikuti kemiringan tanah.")]
        [Range(0.01f, 0.3f)]
        public float footRotSmoothTime = 0.12f;

        [Tooltip("Waktu smoothing penurunan pinggul.")]
        [Range(0.01f, 0.5f)]
        public float pelvisSmoothTime = 0.15f;

        [Header("Pengaturan Saat Bergerak / Berlari")]
        [Tooltip("Kecepatan animator di mana IK mulai memudar (biasanya kecepatan jalan).")]
        public float fadeStartSpeed = 1.5f;

        [Tooltip("Kecepatan animator di mana IK sepenuhnya nonaktif (biasanya kecepatan sprint).")]
        public float fadeEndSpeed = 4.0f;

        [Range(0f, 1f)]
        [Tooltip("Bobot IK minimum saat berlari (0 = mati total, 0.3 = sedikit menyesuaikan).")]
        public float minRunningWeight = 0.15f;

        [Header("Integrasi Karakter")]
        [Tooltip("ThirdPersonController untuk mendeteksi lompat/grounded.")]
        public StarterAssets.ThirdPersonController thirdPersonController;

        [Header("Visualisasi Debug (Scene View)")]
        [Tooltip("Tampilkan garis sensor dan titik pijakan kaki di Scene view saat Play.")]
        public bool showGizmos = true;

        // ==================== INTERNAL ====================
        private Animator animator;
        private int speedParamHash;

        // Kaki kiri
        private float leftFootYOffset;
        private float leftFootYVelocity; // untuk SmoothDamp
        private Quaternion leftFootTargetRot;
        private Quaternion leftFootCurrentRot;
        private bool leftFootHasHit;

        // Kaki kanan
        private float rightFootYOffset;
        private float rightFootYVelocity;
        private Quaternion rightFootTargetRot;
        private Quaternion rightFootCurrentRot;
        private bool rightFootHasHit;

        // Pelvis
        private float pelvisOffset;
        private float pelvisVelocity;

        // Weight blending
        private float smoothedWeight;
        private float weightVelocity;

        // Flags
        private bool initialized;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            speedParamHash = Animator.StringToHash("Speed");

            if (thirdPersonController == null)
                thirdPersonController = GetComponent<StarterAssets.ThirdPersonController>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;

            // === HITUNG TARGET WEIGHT ===
            float targetWeight = 0f;

            if (enableFootIK)
            {
                // Cek grounded
                bool isGrounded = true;
                if (thirdPersonController != null)
                    isGrounded = thirdPersonController.Grounded;

                if (isGrounded)
                {
                    // Ambil kecepatan dari Animator parameter "Speed"
                    float animSpeed = animator.GetFloat(speedParamHash);

                    // IK penuh saat diam/jalan, berkurang saat lari
                    if (animSpeed <= fadeStartSpeed)
                    {
                        targetWeight = ikWeight;
                    }
                    else if (animSpeed >= fadeEndSpeed)
                    {
                        targetWeight = minRunningWeight;
                    }
                    else
                    {
                        // Lerp antara penuh dan minimum
                        float t = (animSpeed - fadeStartSpeed) / (fadeEndSpeed - fadeStartSpeed);
                        targetWeight = Mathf.Lerp(ikWeight, minRunningWeight, t);
                    }
                }
                // Jika di udara, targetWeight tetap 0
            }

            // Smooth weight transition
            smoothedWeight = Mathf.SmoothDamp(smoothedWeight, targetWeight, ref weightVelocity, 0.15f);

            // Jika bobot sangat kecil, matikan semua IK dan return
            if (smoothedWeight < 0.005f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);

                // Reset pelvis smoothly
                pelvisOffset = Mathf.SmoothDamp(pelvisOffset, 0f, ref pelvisVelocity, pelvisSmoothTime);
                return;
            }

            // === RAYCAST KAKI KIRI ===
            leftFootHasHit = SolveFootIK(
                AvatarIKGoal.LeftFoot,
                ref leftFootYOffset, ref leftFootYVelocity,
                ref leftFootTargetRot, ref leftFootCurrentRot
            );

            // === RAYCAST KAKI KANAN ===
            rightFootHasHit = SolveFootIK(
                AvatarIKGoal.RightFoot,
                ref rightFootYOffset, ref rightFootYVelocity,
                ref rightFootTargetRot, ref rightFootCurrentRot
            );

            // === PELVIS DROP ===
            if (adjustPelvis)
            {
                SolvePelvis();
            }

            // === TERAPKAN IK KAKI KIRI ===
            ApplyFootIK(AvatarIKGoal.LeftFoot, leftFootHasHit, leftFootYOffset, leftFootCurrentRot);

            // === TERAPKAN IK KAKI KANAN ===
            ApplyFootIK(AvatarIKGoal.RightFoot, rightFootHasHit, rightFootYOffset, rightFootCurrentRot);

            initialized = true;
        }

        /// <summary>
        /// Raycast dari posisi kaki ke bawah, hitung offset Y dan rotasi permukaan.
        /// Menggunakan SmoothDamp untuk transisi super halus.
        /// </summary>
        private bool SolveFootIK(
            AvatarIKGoal foot,
            ref float yOffset, ref float yVelocity,
            ref Quaternion targetRot, ref Quaternion currentRot)
        {
            Vector3 footPos = animator.GetIKPosition(foot);

            // Tembak raycast dari atas kaki ke bawah
            Vector3 rayOrigin = new Vector3(footPos.x, transform.position.y + raycastDistance * 0.6f, footPos.z);
            float totalRayLength = raycastDistance + 0.5f;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, totalRayLength, groundLayer, QueryTriggerInteraction.Ignore))
            {
                // Hitung offset Y: seberapa jauh kaki harus naik/turun dari posisi animasi
                float groundY = hit.point.y + footHeightOffset;
                float desiredOffset = groundY - footPos.y;

                // Clamp agar tidak terlalu ekstrem
                desiredOffset = Mathf.Clamp(desiredOffset, -maxPelvisDrop - 0.1f, maxPelvisDrop + 0.1f);

                // SmoothDamp untuk transisi sangat halus (anti patah-patah)
                yOffset = Mathf.SmoothDamp(yOffset, desiredOffset, ref yVelocity, footPosSmoothTime);

                // Rotasi kaki mengikuti kemiringan permukaan tanah
                Quaternion surfaceRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                targetRot = surfaceRot;

                // Slerp rotasi dengan smooth time
                float rotLerp = 1f - Mathf.Exp(-10f / Mathf.Max(footRotSmoothTime * 60f, 1f));
                if (!initialized)
                    currentRot = targetRot;
                else
                    currentRot = Quaternion.Slerp(currentRot, targetRot, rotLerp);

                return true;
            }

            // Tidak ada tanah terdeteksi, kembalikan offset ke 0 secara halus
            yOffset = Mathf.SmoothDamp(yOffset, 0f, ref yVelocity, footPosSmoothTime);
            currentRot = Quaternion.Slerp(currentRot, Quaternion.identity, Time.deltaTime * 5f);
            return false;
        }

        /// <summary>
        /// Menurunkan pelvis/pinggul agar kaki yang lebih rendah bisa menapak tanah.
        /// </summary>
        private void SolvePelvis()
        {
            // Ambil offset terendah (kaki yang paling rendah posisinya)
            float lowestOffset = Mathf.Min(leftFootYOffset, rightFootYOffset);

            // Hanya turunkan (negatif), jangan angkat
            float targetPelvis = Mathf.Clamp(lowestOffset, -maxPelvisDrop, 0f);

            // SmoothDamp untuk pelvis
            pelvisOffset = Mathf.SmoothDamp(pelvisOffset, targetPelvis, ref pelvisVelocity, pelvisSmoothTime);

            // Terapkan ke body position
            Vector3 bodyPos = animator.bodyPosition;
            bodyPos.y += pelvisOffset * smoothedWeight;
            animator.bodyPosition = bodyPos;
        }

        /// <summary>
        /// Terapkan posisi dan rotasi IK ke kaki Mecanim.
        /// </summary>
        private void ApplyFootIK(AvatarIKGoal foot, bool hasHit, float yOffset, Quaternion surfaceRot)
        {
            float posWeight = hasHit ? smoothedWeight : 0f;
            float rotWeight = hasHit ? smoothedWeight * 0.7f : 0f; // Rotasi sedikit lebih ringan agar natural

            animator.SetIKPositionWeight(foot, posWeight);
            animator.SetIKRotationWeight(foot, rotWeight);

            if (hasHit)
            {
                // Geser posisi kaki berdasarkan offset Y
                Vector3 footPos = animator.GetIKPosition(foot);
                footPos.y += yOffset;
                animator.SetIKPosition(foot, footPos);

                // Terapkan rotasi permukaan tanah
                Quaternion footRot = animator.GetIKRotation(foot);
                animator.SetIKRotation(foot, surfaceRot * footRot);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || !Application.isPlaying || animator == null) return;

            // Visualisasi titik pijakan kaki kiri
            Vector3 leftPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            leftPos.y += leftFootYOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftPos, 0.04f);

            // Visualisasi titik pijakan kaki kanan
            Vector3 rightPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
            rightPos.y += rightFootYOffset;
            Gizmos.DrawWireSphere(rightPos, 0.04f);

            // Visualisasi arah kemiringan
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(leftPos, leftFootCurrentRot * Vector3.forward * 0.2f);
            Gizmos.DrawRay(rightPos, rightFootCurrentRot * Vector3.forward * 0.2f);
        }
    }
}
