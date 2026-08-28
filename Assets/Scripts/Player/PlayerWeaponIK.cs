using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// PlayerWeaponIK: Sistem Senjata & Hand Grip IK Humanoid Bawaan Unity.
    /// - 100% Menggunakan Mecanim Humanoid OnAnimatorIK bawaan resmi Unity (Bebas Error Burst).
    /// - Menempelkan tangan kiri/kanan ke gagang pedang secara presisi dan natural.
    /// - Memindahkan pedang antara Sarung (Sheath) dan Tangan (Equip) via Animation Event.
    /// - Offset posisi & rotasi bisa diatur di Inspector supaya pedang pas digenggam.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerWeaponIK : MonoBehaviour
    {
        [Header("Weapon Sockets")]
        [Tooltip("Objek pedang yang akan dipindah-pindah.")]
        public Transform swordTransform;

        [Tooltip("Socket tempat pedang digenggam (di tangan kanan).")]
        public Transform handSocket;

        [Tooltip("Socket tempat sarung pedang (di pinggang / punggung).")]
        public Transform sheathSocket;

        [Header("Posisi Pedang di Tangan (Equip Offset)")]
        [Tooltip("Offset posisi pedang saat digenggam di tangan.")]
        public Vector3 equipPositionOffset = Vector3.zero;

        [Tooltip("Offset rotasi pedang saat digenggam di tangan (dalam derajat).")]
        public Vector3 equipRotationOffset = Vector3.zero;

        [Header("Posisi Pedang di Sarung (Sheath Offset)")]
        [Tooltip("Offset posisi pedang saat disarungkan.")]
        public Vector3 sheathPositionOffset = Vector3.zero;

        [Tooltip("Offset rotasi pedang saat disarungkan (dalam derajat).")]
        public Vector3 sheathRotationOffset = Vector3.zero;

        [Header("Hand Grip IK (Gagang Pedang)")]
        [Tooltip("Titik target di gagang pedang tempat tangan KANAN menempel (genggaman utama).")]
        public Transform rightHandGripTarget;

        [Tooltip("Titik arah siku kanan agar tekukan siku natural.")]
        public Transform rightElbowHint;

        [Tooltip("(Opsional) Titik target untuk tangan KIRI jika pedang dipegang dua tangan.")]
        public Transform leftHandGripTarget;

        [Tooltip("(Opsional) Titik arah siku kiri untuk genggaman dua tangan.")]
        public Transform leftElbowHint;

        [Header("IK Weight Control")]
        [Range(0f, 1f)]
        [Tooltip("Kekuatan tarikan IK tangan kanan (0 = lepas, 1 = nempel penuh).")]
        public float rightHandIKWeight = 0f;

        [Range(0f, 1f)]
        [Tooltip("Kekuatan tarikan IK tangan kiri untuk genggaman dua tangan (0 = lepas, 1 = nempel).")]
        public float leftHandIKWeight = 0f;

        [Tooltip("Nama parameter Float di Animator untuk membaca curve animasi (misal: 'IKWeight').")]
        public string ikWeightParameterName = "IKWeight";

        [Tooltip("Jika dicentang, weight akan otomatis dibaca dari parameter Animator.")]
        public bool readWeightFromAnimator = true;

        [Header("Status")]
        [SerializeField] private bool isEquipped = false;

        private Animator animator;
        private int ikParamHash;
        private bool hasParam = false;

        public bool IsEquipped => isEquipped;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (!string.IsNullOrEmpty(ikWeightParameterName))
            {
                ikParamHash = Animator.StringToHash(ikWeightParameterName);
                CheckAnimatorParameter();
            }
        }

        private void CheckAnimatorParameter()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.name == ikWeightParameterName && p.type == AnimatorControllerParameterType.Float)
                {
                    hasParam = true;
                    return;
                }
            }
            hasParam = false;
        }

        private void Update()
        {
            if (readWeightFromAnimator && hasParam && animator != null)
            {
                rightHandIKWeight = animator.GetFloat(ikParamHash);
            }
        }

        // ====================================================================
        // UNITY HUMANOID IK PASS (Dipanggil otomatis oleh Unity saat IK Pass aktif)
        // ====================================================================
        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;

            // === TANGAN KANAN (Genggaman Utama) ===
            if (rightHandGripTarget != null && rightHandIKWeight > 0.001f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandGripTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandGripTarget.rotation);

                if (rightElbowHint != null)
                {
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, rightHandIKWeight);
                    animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowHint.position);
                }
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                if (rightElbowHint != null)
                {
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
                }
            }

            // === TANGAN KIRI (Opsional: Genggaman Dua Tangan) ===
            if (leftHandGripTarget != null && leftHandIKWeight > 0.001f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGripTarget.rotation);

                if (leftElbowHint != null)
                {
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, leftHandIKWeight);
                    animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowHint.position);
                }
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                if (leftElbowHint != null)
                {
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
                }
            }
        }


        // ====================================================================
        // ANIMATION EVENT FUNCTIONS (Dipanggil dari klip animasi Draw / Sheath)
        // ====================================================================

        /// <summary>
        /// Pedang pindah ke tangan kanan dengan offset yang sudah ditentukan.
        /// Panggil dari Animation Event atau tombol Inspector.
        /// </summary>
        [ContextMenu("⚔️ Equip Sword")]
        public void EquipSword()
        {
            if (swordTransform != null && handSocket != null)
            {
                swordTransform.SetParent(handSocket);
                swordTransform.localPosition = equipPositionOffset;
                swordTransform.localRotation = Quaternion.Euler(equipRotationOffset);
                swordTransform.localScale = Vector3.one;
                isEquipped = true;
            }
        }

        /// <summary>
        /// Pedang pindah ke sarung (pinggang/punggung) dengan offset yang sudah ditentukan.
        /// Panggil dari Animation Event atau tombol Inspector.
        /// </summary>
        [ContextMenu("🗡️ Sheath Sword")]
        public void SheathSword()
        {
            if (swordTransform != null && sheathSocket != null)
            {
                swordTransform.SetParent(sheathSocket);
                swordTransform.localPosition = sheathPositionOffset;
                swordTransform.localRotation = Quaternion.Euler(sheathRotationOffset);
                swordTransform.localScale = Vector3.one;
                isEquipped = false;
            }
        }

        /// <summary>
        /// Simpan posisi/rotasi pedang saat ini sebagai offset Equip.
        /// Berguna saat menyesuaikan posisi pedang secara visual di Scene View.
        /// </summary>
        [ContextMenu("📋 Simpan Posisi Saat Ini → Equip Offset")]
        public void SaveCurrentAsEquipOffset()
        {
            if (swordTransform != null)
            {
                equipPositionOffset = swordTransform.localPosition;
                equipRotationOffset = swordTransform.localRotation.eulerAngles;
                Debug.Log($"[PlayerWeaponIK] Equip offset disimpan: Pos={equipPositionOffset}, Rot={equipRotationOffset}");
            }
        }

        /// <summary>
        /// Simpan posisi/rotasi pedang saat ini sebagai offset Sheath.
        /// Berguna saat menyesuaikan posisi pedang secara visual di Scene View.
        /// </summary>
        [ContextMenu("📋 Simpan Posisi Saat Ini → Sheath Offset")]
        public void SaveCurrentAsSheathOffset()
        {
            if (swordTransform != null)
            {
                sheathPositionOffset = swordTransform.localPosition;
                sheathRotationOffset = swordTransform.localRotation.eulerAngles;
                Debug.Log($"[PlayerWeaponIK] Sheath offset disimpan: Pos={sheathPositionOffset}, Rot={sheathRotationOffset}");
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(PlayerWeaponIK))]
    public class PlayerWeaponIKEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlayerWeaponIK ik = (PlayerWeaponIK)target;
            if (ik == null) return;

            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("🎮 Quick Testing Tools", UnityEditor.EditorStyles.boldLabel);

            // Equip / Sheath buttons
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
            if (GUILayout.Button("⚔️ Pegang Pedang (Equip)", GUILayout.Height(32)))
            {
                ik.EquipSword();
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button("🗡️ Sarungkan Pedang (Sheath)", GUILayout.Height(32)))
            {
                ik.SheathSword();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            UnityEditor.EditorGUILayout.Space(5);
            UnityEditor.EditorGUILayout.LabelField("📋 Simpan Posisi Pedang Saat Ini", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.HelpBox(
                "Setelah kamu geser/putar pedang secara manual di Scene View,\n" +
                "klik tombol di bawah untuk menyimpan posisi tersebut sebagai offset.",
                UnityEditor.MessageType.Info);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button("💾 Simpan → Equip Offset", GUILayout.Height(28)))
            {
                ik.SaveCurrentAsEquipOffset();
                UnityEditor.EditorUtility.SetDirty(ik);
            }

            GUI.backgroundColor = new Color(1f, 1f, 0.4f);
            if (GUILayout.Button("💾 Simpan → Sheath Offset", GUILayout.Height(28)))
            {
                ik.SaveCurrentAsSheathOffset();
                UnityEditor.EditorUtility.SetDirty(ik);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }
    }
#endif
}
