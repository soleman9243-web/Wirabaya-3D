using UnityEngine;

namespace Unity.FantasyKingdom.WeaponIK
{
    /// <summary>
    /// Komponen penanda (Marker) pada objek senjata atau item yang dapat digenggam.
    /// Menyediakan informasi transform grip target dan preset bentuk jari yang harus dipakai.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponGripPoint : MonoBehaviour
    {
        [Header("Grip Transforms")]
        [Tooltip("Transform spesifik tempat telapak tangan kanan menempel.")]
        [SerializeField] private Transform rightHandGripTransform;

        [Tooltip("Transform arah tekukan siku kanan.")]
        [SerializeField] private Transform rightElbowHintTransform;

        [Header("Finger Pose Preset")]
        [Tooltip("Data rotasi 15 jari yang akan diterapkan saat menggenggam objek ini.")]
        [SerializeField] private HandGripPoseData gripPosePreset;

        [Header("Two-Handed Option")]
        [Tooltip("Apakah senjata ini juga membutuhkan genggaman tangan kiri?")]
        [SerializeField] private bool useTwoHandedGrip = false;

        [SerializeField] private Transform leftHandGripTransform;
        [SerializeField] private Transform leftElbowHintTransform;

        // Public Read-Only Properties
        public Transform RightHandGripTransform => rightHandGripTransform != null ? rightHandGripTransform : transform;
        public Transform RightElbowHintTransform => rightElbowHintTransform;
        public HandGripPoseData GripPosePreset => gripPosePreset;
        public bool UseTwoHandedGrip => useTwoHandedGrip;
        public Transform LeftHandGripTransform => leftHandGripTransform;
        public Transform LeftElbowHintTransform => leftElbowHintTransform;

        private void Reset()
        {
            // Auto-assign self jika transform belum diset
            if (rightHandGripTransform == null)
            {
                rightHandGripTransform = transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualisasi titik grip di Editor untuk mempermudah level designer/animator
            if (rightHandGripTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(rightHandGripTransform.position, 0.035f);
                Gizmos.DrawRay(rightHandGripTransform.position, rightHandGripTransform.forward * 0.1f);
            }

            if (useTwoHandedGrip && leftHandGripTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(leftHandGripTransform.position, 0.035f);
                Gizmos.DrawRay(leftHandGripTransform.position, leftHandGripTransform.forward * 0.1f);
            }
        }
    }
}
