using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// WeaponGripIK: Hanya mengurus grip tangan ke gagang pedang.
    /// Animasi sheath/draw sudah ada sendiri, script ini cuma memastikan
    /// tangan KANAN nempel presisi ke gagang pedang saat menggenggam.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WeaponGripIK : MonoBehaviour
    {
        [Header("Grip Target (Gagang Pedang)")]
        [Tooltip("Titik di gagang pedang tempat tangan kanan harus menempel.")]
        public Transform rightHandGripTarget;

        [Tooltip("(Opsional) Arah siku kanan supaya tekukan siku natural.")]
        public Transform rightElbowHint;

        [Header("Grip Weight")]
        [Range(0f, 1f)]
        [Tooltip("0 = tangan ikut animasi biasa, 1 = tangan nempel penuh ke gagang pedang.")]
        public float gripWeight = 1f;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || rightHandGripTarget == null) return;

            // Tangan kanan menempel ke gagang pedang
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, gripWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, gripWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandGripTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandGripTarget.rotation);

            // Arah siku
            if (rightElbowHint != null)
            {
                animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, gripWeight);
                animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowHint.position);
            }
        }
    }
}
