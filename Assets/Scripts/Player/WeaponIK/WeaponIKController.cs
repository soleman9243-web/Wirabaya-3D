using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Unity.FantasyKingdom.WeaponIK
{
    /// <summary>
    /// Controller utama yang mengelola:
    /// 1. Weight interpolation pada TwoBoneIKConstraint (Animation Rigging) untuk lengan.
    /// 2. Post-FK Finger Blending (LateUpdate) untuk 15 ruas jari tangan.
    /// 3. Penerima Animation Event: ActivateGripEvent(Transform) dan DeactivateGripEvent().
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponIKController : MonoBehaviour
    {
        [Header("Animation Rigging Constraints")]
        [Tooltip("Constraint TwoBoneIK untuk lengan kanan pada Rig Layer.")]
        [SerializeField] private TwoBoneIKConstraint rightArmIK;

        [Tooltip("Constraint TwoBoneIK untuk lengan kiri (opsional two-handed).")]
        [SerializeField] private TwoBoneIKConstraint leftArmIK;

        [Header("Blend Durations (Seconds)")]
        [Tooltip("Durasi transisi saat tangan mulai menggenggam gagang senjata.")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float blendInDuration = 0.15f;

        [Tooltip("Durasi transisi saat tangan melepas gagang senjata.")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float blendOutDuration = 0.12f;

        [Header("Fallback / Debug")]
        [Tooltip("Preset pose default jika senjata tidak menyertakan preset spesifik.")]
        [SerializeField] private HandGripPoseData defaultGripPreset;

        // Current runtime weight (0 = Full Animation Clip FK, 1 = Full Weapon IK & Finger Preset)
        private float currentWeight = 0f;
        private Coroutine activeBlendCoroutine;
        private WeaponGripPoint currentGripPoint;

        // Cache 15 Transform tulang jari tangan kanan & kiri untuk eliminasi string lookup / GC alloc di runtime
        private readonly Transform[] cachedRightFingers = new Transform[15];
        private readonly Transform[] cachedLeftFingers = new Transform[15];
        private bool isBoneCacheReady = false;

        private void Awake()
        {
            // HARD CONSTRAINT: Default weight WAJIB 0 saat inisialisasi awal
            currentWeight = 0f;
            SetRigConstraintsWeight(0f);

            CacheFingerBones();
        }

        /// <summary>
        /// Cache 15 transform jari tangan sekali saat Awake agar LateUpdate 100% bebas Garbage Collection.
        /// </summary>
        private void CacheFingerBones()
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            var map = new Dictionary<string, Transform>(allChildren.Length);
            for (int i = 0; i < allChildren.Length; i++)
            {
                if (!map.ContainsKey(allChildren[i].name))
                {
                    map.Add(allChildren[i].name, allChildren[i]);
                }
            }

            for (int i = 0; i < 15; i++)
            {
                string rName = HandGripPoseData.RightFingerBoneNames[i];
                if (map.TryGetValue(rName, out Transform rTf))
                {
                    cachedRightFingers[i] = rTf;
                }

                string lName = HandGripPoseData.LeftFingerBoneNames[i];
                if (map.TryGetValue(lName, out Transform lTf))
                {
                    cachedLeftFingers[i] = lTf;
                }
            }

            isBoneCacheReady = true;
        }

        // =========================================================================================
        // ANIMATION EVENT API (Dipanggil dari AnimationClip Draw/Sheath Mixamo)
        // =========================================================================================

        /// <summary>
        /// Dipanggil via Animation Event di tengah clip "Draw Sword" saat tangan menyentuh gagang.
        /// </summary>
        /// <param name="heldObject">Transform root senjata / item yang sedang dipegang.</param>
        public void ActivateGripEvent(Transform heldObject)
        {
            if (heldObject == null)
            {
                Debug.LogWarning("[WeaponIKController] ActivateGripEvent dipanggil dengan parameter null!", this);
                return;
            }

            WeaponGripPoint grip = heldObject.GetComponentInChildren<WeaponGripPoint>();
            if (grip == null)
            {
                // Fallback: Jika objek tidak punya komponen marker khusus, pasang transform root sebagai target
                Debug.LogWarning($"[WeaponIKController] {heldObject.name} tidak memiliki WeaponGripPoint marker. Menggunakan transform root.", heldObject);
            }

            ActivateGrip(grip);
        }

        /// <summary>
        /// Dipanggil via Animation Event di clip "Sheath Sword" saat tangan melepaskan gagang senjata.
        /// </summary>
        public void DeactivateGripEvent()
        {
            DeactivateGrip();
        }

        // =========================================================================================
        // LOGIKA TRANSISI & BLENDING
        // =========================================================================================

        /// <summary>
        /// Mengaktifkan IK dan Pose Jari secara halus menuju target WeaponGripPoint.
        /// Menangani edge-case interupsi (mid-blend) secara mulus.
        /// </summary>
        public void ActivateGrip(WeaponGripPoint gripPoint)
        {
            currentGripPoint = gripPoint;

            // Sambungkan transform target ke TwoBoneIKConstraint sebelum blending
            if (currentGripPoint != null)
            {
                if (rightArmIK != null && currentGripPoint.RightHandGripTransform != null)
                {
                    rightArmIK.data.target = currentGripPoint.RightHandGripTransform;
                    if (currentGripPoint.RightElbowHintTransform != null)
                    {
                        rightArmIK.data.hint = currentGripPoint.RightElbowHintTransform;
                    }
                }

                if (leftArmIK != null && currentGripPoint.UseTwoHandedGrip && currentGripPoint.LeftHandGripTransform != null)
                {
                    leftArmIK.data.target = currentGripPoint.LeftHandGripTransform;
                    if (currentGripPoint.LeftElbowHintTransform != null)
                    {
                        leftArmIK.data.hint = currentGripPoint.LeftElbowHintTransform;
                    }
                }
            }

            StartBlendRoutine(1.0f, blendInDuration);
        }

        /// <summary>
        /// Menonaktifkan IK dan mengembalikan kontrol lengan + jari 100% ke animasi Mixamo.
        /// </summary>
        public void DeactivateGrip()
        {
            StartBlendRoutine(0.0f, blendOutDuration);
        }

        /// <summary>
        /// Emergency Reset jika karakter terkena Hit/Stagger/Death saat sedang dalam proses Draw/Sheath.
        /// </summary>
        public void ForceInstantReset()
        {
            if (activeBlendCoroutine != null)
            {
                StopCoroutine(activeBlendCoroutine);
                activeBlendCoroutine = null;
            }

            currentWeight = 0f;
            SetRigConstraintsWeight(0f);
            currentGripPoint = null;
        }

        private void StartBlendRoutine(float targetWeight, float duration)
        {
            // Hentikan coroutine sebelumnya agar tidak bertabrakan saat terjadi interrupt mid-blend
            if (activeBlendCoroutine != null)
            {
                StopCoroutine(activeBlendCoroutine);
            }

            activeBlendCoroutine = StartCoroutine(BlendWeightRoutine(targetWeight, duration));
        }

        private IEnumerator BlendWeightRoutine(float targetWeight, float duration)
        {
            float startWeight = currentWeight;
            float elapsed = 0f;

            // Menggunakan interpolasi SmoothStep agar transisi natural tanpa sentakan mendadak
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                currentWeight = Mathf.Lerp(startWeight, targetWeight, smoothT);
                SetRigConstraintsWeight(currentWeight);

                yield return null;
            }

            currentWeight = targetWeight;
            SetRigConstraintsWeight(currentWeight);

            if (Mathf.Approximately(currentWeight, 0f))
            {
                currentGripPoint = null;
            }

            activeBlendCoroutine = null;
        }

        private void SetRigConstraintsWeight(float weight)
        {
            if (rightArmIK != null)
            {
                rightArmIK.weight = weight;
            }

            if (leftArmIK != null)
            {
                // Jika senjata single-handed, tangan kiri tetap 0
                bool isLeftActive = currentGripPoint != null && currentGripPoint.UseTwoHandedGrip;
                leftArmIK.weight = isLeftActive ? weight : 0f;
            }
        }

        // =========================================================================================
        // POST-FK FINGER BLEND EVALUATION (LateUpdate)
        // =========================================================================================

        /// <summary>
        /// LateUpdate dieksekusi SETELAH Animator mengevaluasi AnimationClip Mixamo.
        /// Di sini kita menimpa rotasi lokal tulang jari sesuai currentWeight tanpa merusak animasi FK tubuh.
        /// </summary>
        private void LateUpdate()
        {
            // Jika weight 0, lewati proses sepenuhnya (CPU cost = 0 ms)
            if (currentWeight <= 0.0001f || !isBoneCacheReady)
            {
                return;
            }

            // EDGE CASE CHECK: Jika senjata dihancurkan/di-Destroy saat IK aktif
            if (currentGripPoint == null)
            {
                DeactivateGrip();
                return;
            }

            HandGripPoseData preset = currentGripPoint.GripPosePreset != null
                ? currentGripPoint.GripPosePreset
                : defaultGripPreset;

            if (preset == null) return;

            // Blend 15 tulang jari tangan kanan
            for (int i = 0; i < 15; i++)
            {
                Transform finger = cachedRightFingers[i];
                if (finger == null) continue;

                Quaternion fkAnimRotation = finger.localRotation;
                Quaternion targetGripRotation = preset.rightHandBones[i].localRotation;

                // Slerp dari rotasi animasi asli Mixamo ke rotasi preset genggaman
                finger.localRotation = Quaternion.Slerp(fkAnimRotation, targetGripRotation, currentWeight);
            }

            // Blend 15 tulang jari tangan kiri (jika two-handed aktif)
            if (currentGripPoint.UseTwoHandedGrip)
            {
                for (int i = 0; i < 15; i++)
                {
                    Transform finger = cachedLeftFingers[i];
                    if (finger == null) continue;

                    Quaternion fkAnimRotation = finger.localRotation;
                    Quaternion targetGripRotation = preset.leftHandBones[i].localRotation;

                    finger.localRotation = Quaternion.Slerp(fkAnimRotation, targetGripRotation, currentWeight);
                }
            }
        }
    }
}
