using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// PlayerLocomotionLeaning: Sistem kemiringan badan prosedural (Spine & Chest Leaning ala ALS / AAA Locomotion).
    /// - Menghasilkan efek kemiringan tubuh dinamis (Banking/Roll) saat berbelok di kecepatan tinggi.
    /// - Menyesuaikan postur tubuh (Pitch) saat menanjak bukit (condong ke depan) dan menuruni lereng (menahan ke belakang).
    /// - Menambahkan inersia start/stop (condong maju saat akselerasi, condong mundur saat mengerem).
    /// - Menerapkan pembagian rotasi 50%/50% pada tulang Spine dan Chest agar lekukan punggung alami.
    /// - Menggunakan SmoothDamp pada semua sumbu agar transisi super halus bebas getaran (jitter-free).
    /// </summary>
    public class PlayerLocomotionLeaning : MonoBehaviour
    {
        [Header("Status Utama")]
        [Tooltip("Aktifkan atau nonaktifkan efek procedural leaning.")]
        public bool enableLeaning = true;

        [Header("Turn Banking / Roll (Miring saat Menikung)")]
        [Tooltip("Sudut kemiringan badan maksimum saat berbelok tajam (derajat).")]
        [Range(2f, 15f)]
        public float maxTurnLeanAngle = 7.5f;

        [Tooltip("Sensitivitas respon belokan.")]
        public float turnLeanSensitivity = 0.05f;

        [Tooltip("Kehalusan transisi kemiringan belok (SmoothDamp time).")]
        [Range(0.05f, 0.3f)]
        public float turnLeanSmoothTime = 0.12f;

        [Header("Slope Pitch (Postur Tanjakan / Turunan)")]
        [Tooltip("Sudut condong maksimum saat mendaki/menuruni lereng (derajat).")]
        [Range(2f, 15f)]
        public float maxSlopePitchAngle = 8.0f;

        [Tooltip("Sensitivitas condong terhadap kemiringan lereng.")]
        public float slopePitchSensitivity = 8.0f;

        [Tooltip("Kehalusan transisi postur lereng.")]
        [Range(0.05f, 0.3f)]
        public float slopePitchSmoothTime = 0.15f;

        [Header("Inertia Acceleration Pitch (Akselerasi & Pengereman)")]
        [Tooltip("Sudut condong maju saat mulai lari (derajat).")]
        [Range(0f, 8f)]
        public float maxAccelPitchAngle = 3.5f;

        [Tooltip("Sudut condong mundur saat mengerem berhenti (derajat).")]
        [Range(0f, 8f)]
        public float maxBrakePitchAngle = 3.0f;

        [Tooltip("Kehalusan transisi akselerasi/pengereman.")]
        [Range(0.05f, 0.3f)]
        public float accelPitchSmoothTime = 0.15f;

        [Header("Distribusi Tulang (Bone Weight)")]
        [Tooltip("Persentase rotasi pada tulang Spine (0 = semua di Chest, 0.5 = seimbang 50:50).")]
        [Range(0f, 1f)]
        public float spineDistribution = 0.5f;

        [Header("Referensi Komponen")]
        public Animator animator;
        public StarterAssets.ThirdPersonController controller;

        // Internal Transforms
        private Transform spineBone;
        private Transform chestBone;

        // Internal State
        private float lastYaw;
        private float lastSpeed;
        private float currentRollLean;
        private float rollVelocity;
        private float currentPitchLean;
        private float pitchVelocity;
        private float currentAccelPitch;
        private float accelPitchVelocity;

        // Master Weight for smooth enable/disable and air/combat fade
        private float currentMasterWeight = 1f;
        private float weightVelocity;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (controller == null)
                controller = GetComponent<StarterAssets.ThirdPersonController>();

            lastYaw = transform.eulerAngles.y;
            if (controller != null)
                lastSpeed = controller.GetSpeed();
        }

        private void Start()
        {
            // Ambil referensi tulang Humanoid Mecanim
            if (animator != null && animator.isHuman)
            {
                spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
                chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);

                // Fallback jika tidak ada Chest, gunakan UpperChest atau biarkan Spine saja
                if (chestBone == null)
                    chestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            }
        }

        private void LateUpdate()
        {
            if (animator == null || (spineBone == null && chestBone == null))
                return;

            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            // === 1. EVALUASI BOBOT MASTER (FADE OUT SAAT LOMPAT / COMBAT) ===
            bool canLean = enableLeaning;
            if (controller != null)
            {
                // Nonaktifkan / fade out jika sedang melayang di udara atau saat combat movement di-disable
                if (!controller.Grounded || controller.DisableMovement)
                    canLean = false;
            }

            float targetWeight = canLean ? 1f : 0f;
            currentMasterWeight = Mathf.SmoothDamp(currentMasterWeight, targetWeight, ref weightVelocity, 0.15f);

            if (currentMasterWeight <= 0.001f)
            {
                lastYaw = transform.eulerAngles.y;
                if (controller != null) lastSpeed = controller.GetSpeed();
                return;
            }

            // === 2. HITUNG TURN BANKING (ROLL) ===
            float currentYaw = transform.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(lastYaw, currentYaw);
            lastYaw = currentYaw;

            float yawRate = yawDelta / dt; // derajat per detik
            float currentSpeed = controller != null ? controller.GetSpeed() : 0f;
            float speedFactor = Mathf.Clamp01(currentSpeed / 5.0f); // Kecepatan lari ~5 m/s

            // Arah banking: berbelok ke kiri (yawRate negatif) -> badan miring ke kiri (Z roll negatif)
            float targetRoll = Mathf.Clamp(-yawRate * turnLeanSensitivity * speedFactor, -maxTurnLeanAngle, maxTurnLeanAngle);
            currentRollLean = Mathf.SmoothDamp(currentRollLean, targetRoll, ref rollVelocity, turnLeanSmoothTime);

            // === 3. HITUNG INERTIA AKSILERASI & PENGEREMAN (PITCH) ===
            float accel = (currentSpeed - lastSpeed) / dt;
            lastSpeed = currentSpeed;

            float targetAccelPitch = 0f;
            if (accel > 0.5f)
            {
                // Akselerasi maju: condong ke depan (pitch positif)
                targetAccelPitch = Mathf.Clamp(accel * 0.4f, 0f, maxAccelPitchAngle);
            }
            else if (accel < -0.5f)
            {
                // Pengereman: condong ke belakang (pitch negatif)
                targetAccelPitch = Mathf.Clamp(accel * 0.35f, -maxBrakePitchAngle, 0f);
            }
            currentAccelPitch = Mathf.SmoothDamp(currentAccelPitch, targetAccelPitch, ref accelPitchVelocity, accelPitchSmoothTime);

            // === 4. HITUNG SLOPE PITCH (TANJAKAN & TURUNAN) ===
            float targetSlopePitch = 0f;
            if (controller != null && controller.Grounded && currentSpeed > 0.1f)
            {
                Vector3 slopeNormal = controller.GetSlopeNormal();
                if (slopeNormal != Vector3.zero && slopeNormal != Vector3.up)
                {
                    // Dot product antara arah hadap karakter dengan normal lereng
                    // Tanjakan (Uphill): forward menghadap ke lereng miring naik -> Dot bernilai negatif
                    // Turunan (Downhill): forward menuruni lereng -> Dot bernilai positif
                    float dot = Vector3.Dot(transform.forward, slopeNormal);
                    targetSlopePitch = Mathf.Clamp(-dot * slopePitchSensitivity, -maxSlopePitchAngle, maxSlopePitchAngle);
                }
            }
            currentPitchLean = Mathf.SmoothDamp(currentPitchLean, targetSlopePitch, ref pitchVelocity, slopePitchSmoothTime);

            // === 5. TOTAL ROTASI LEANING ===
            float totalPitch = (currentPitchLean + currentAccelPitch) * currentMasterWeight;
            float totalRoll = currentRollLean * currentMasterWeight;

            // Rotasi lokal: Pitch (sumbu X), Roll (sumbu Z)
            float spineFactor = (chestBone != null) ? spineDistribution : 1.0f;
            float chestFactor = (chestBone != null) ? (1.0f - spineDistribution) : 0.0f;

            Quaternion spineRotDelta = Quaternion.Euler(totalPitch * spineFactor, 0f, totalRoll * spineFactor);
            if (spineBone != null)
            {
                spineBone.localRotation = spineBone.localRotation * spineRotDelta;
            }

            if (chestBone != null)
            {
                Quaternion chestRotDelta = Quaternion.Euler(totalPitch * chestFactor, 0f, totalRoll * chestFactor);
                chestBone.localRotation = chestBone.localRotation * chestRotDelta;
            }
        }
    }
}
