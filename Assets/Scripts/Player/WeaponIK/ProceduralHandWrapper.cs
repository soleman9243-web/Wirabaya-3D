using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.FantasyKingdom.WeaponIK
{
    /// <summary>
    /// ProceduralHandWrapper:
    /// Menekuk dan membentuk 15 ruas jari secara otomatis mengikuti permukaan 3D (Collider/Mesh)
    /// dari objek apapun yang didekatkan ke tangan (Pedang, Tongkat, Kapak, Botol, dll).
    /// Menggunakan contact-detection & raycasting per-ruas jari.
    /// </summary>
    [ExecuteAlways]
    public class ProceduralHandWrapper : MonoBehaviour
    {
        [Header("Target 3D Object")]
        [Tooltip("Collider dari objek 3D yang ingin digenggam (misal: BoxCollider/CapsuleCollider di gagang pedang).")]
        public Collider targetObjectCollider;

        [Header("Wrap Settings")]
        [Range(1f, 120f)]
        [Tooltip("Batas maksimal tekukan tiap ruas jari (derajat).")]
        public float maxCurlAngle = 90f;

        [Range(0.5f, 5f)]
        [Tooltip("Akurasi step raycast saat mencari permukaan 3D objek.")]
        public float stepAngle = 2f;

        [Tooltip("Tebal offset jari dari permukaan objek agar tidak tembus (meter).")]
        public float fingerRadius = 0.008f;

        [Header("Status / Preset Output")]
        [Tooltip("ScriptableObject tujuan jika ingin langsung menyimpan hasil wrap ke preset.")]
        public HandGripPoseData outputPosePreset;

        // Cache 15 ruas jari tangan kanan
        private Transform[] rightFingers = new Transform[15];

        public void FindFingersOnCharacter()
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            var map = new Dictionary<string, Transform>(allTransforms.Length);
            foreach (var t in allTransforms)
            {
                if (!map.ContainsKey(t.name)) map.Add(t.name, t);
            }

            for (int i = 0; i < 15; i++)
            {
                string bName = HandGripPoseData.RightFingerBoneNames[i];
                if (map.TryGetValue(bName, out Transform tf))
                {
                    rightFingers[i] = tf;
                }
            }
        }

        /// <summary>
        /// Algoritma Auto-Wrap: Menekuk ruas jari satu per satu sampai menyentuh permukaan 3D collider objek!
        /// </summary>
        public void AutoWrapFingersAroundTarget()
        {
            if (targetObjectCollider == null)
            {
                Debug.LogWarning("[ProceduralHandWrapper] Target Object Collider belum dimasukkan!", this);
                return;
            }

            FindFingersOnCharacter();

#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(gameObject, "Auto Wrap Hand to 3D Object");
#endif

            // Reset rotasi jari ke pose terbuka (0 derajat)
            ResetFingersToOpenPose();

            // 1. Wrap 4 Jari (Telunjuk, Tengah, Manis, Kelingking)
            // Indeks array: Index (3,4,5), Middle (6,7,8), Ring (9,10,11), Pinky (12,13,14)
            for (int fingerIndex = 0; fingerIndex < 4; fingerIndex++)
            {
                int baseIndex = 3 + (fingerIndex * 3);
                WrapSingleFingerChain(baseIndex);
            }

            // 2. Wrap Jempol (Thumb 0, 1, 2)
            WrapThumbChain();

            Debug.Log($"[ProceduralHandWrapper] 15 Ruas Jari berhasil otomatis membentuk permukaan objek: {targetObjectCollider.gameObject.name}!");
        }

        private void ResetFingersToOpenPose()
        {
            for (int i = 0; i < 15; i++)
            {
                if (rightFingers[i] != null)
                {
                    rightFingers[i].localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Menekuk 3 ruas dari 1 rantai jari sampai menyentuh permukaan collider objek.
        /// </summary>
        private void WrapSingleFingerChain(int baseIdx)
        {
            Transform joint1 = rightFingers[baseIdx];     // Proximal
            Transform joint2 = rightFingers[baseIdx + 1]; // Intermediate
            Transform joint3 = rightFingers[baseIdx + 2]; // Distal

            if (joint1 == null || joint2 == null || joint3 == null) return;

            // Tekuk Joint 1 (Pangkal) sampai menyentuh atau mendekati permukaan
            CurlJointUntilContact(joint1, joint2, Vector3.forward, -1f, maxCurlAngle * 0.7f);

            // Tekuk Joint 2 (Tengah)
            CurlJointUntilContact(joint2, joint3, Vector3.forward, -1f, maxCurlAngle * 0.85f);

            // Tekuk Joint 3 (Ujung)
            CurlJointUntilContact(joint3, null, Vector3.forward, -1f, maxCurlAngle * 0.65f);
        }

        private void WrapThumbChain()
        {
            Transform thumb1 = rightFingers[0];
            Transform thumb2 = rightFingers[1];
            Transform thumb3 = rightFingers[2];

            if (thumb1 == null || thumb2 == null || thumb3 == null) return;

            // Orientasikan jempol ke arah berlawanan (opposable thumb)
            thumb1.localRotation = Quaternion.Euler(-20f, 25f, 15f);

            // Tekuk ruas jempol sampai mendekati gagang
            CurlJointUntilContact(thumb2, thumb3, Vector3.forward, -1f, maxCurlAngle * 0.55f);
            CurlJointUntilContact(thumb3, null, Vector3.forward, -1f, maxCurlAngle * 0.6f);
        }

        private void CurlJointUntilContact(Transform joint, Transform childJoint, Vector3 axis, float direction, float maxAngle)
        {
            if (joint == null) return;

            Quaternion originalRot = joint.localRotation;
            float currentAngle = 0f;

            while (currentAngle < maxAngle)
            {
                currentAngle += stepAngle;
                joint.localRotation = originalRot * Quaternion.AngleAxis(currentAngle * direction, axis);

                // Cek jarak posisi ruas ke collider objek
                Vector3 checkPoint = childJoint != null ? childJoint.position : joint.position + (joint.forward * 0.02f);
                Vector3 closestPointOnCollider = targetObjectCollider.ClosestPoint(checkPoint);
                float distance = Vector3.Distance(checkPoint, closestPointOnCollider);

                // Jika sudah menyentuh permukaan collider objek
                if (distance <= fingerRadius)
                {
                    // Mundurkan sedikit 1 step agar tidak tembus (clipping)
                    currentAngle = Mathf.Max(0f, currentAngle - stepAngle);
                    joint.localRotation = originalRot * Quaternion.AngleAxis(currentAngle * direction, axis);
                    break;
                }
            }
        }

        /// <summary>
        /// Simpan bentuk jari hasil wrap otomatis ini ke dalam asset ScriptableObject HandGripPoseData.
        /// </summary>
        public void SaveResultToPoseAsset()
        {
            if (outputPosePreset == null)
            {
                Debug.LogWarning("[ProceduralHandWrapper] Output Pose Preset belum dipilih!", this);
                return;
            }

            FindFingersOnCharacter();

            if (outputPosePreset.rightHandBones == null || outputPosePreset.rightHandBones.Length != 15)
            {
                outputPosePreset.rightHandBones = new FingerBoneRotation[15];
            }

            for (int i = 0; i < 15; i++)
            {
                outputPosePreset.rightHandBones[i].boneName = HandGripPoseData.RightFingerBoneNames[i];
                outputPosePreset.rightHandBones[i].localRotation = rightFingers[i] != null
                    ? rightFingers[i].localRotation
                    : Quaternion.identity;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(outputPosePreset);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Sukses", $"Hasil Auto-Wrap bentuk 3D berhasil disimpan ke '{outputPosePreset.name}'!", "OK");
#endif
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ProceduralHandWrapper))]
    public class ProceduralHandWrapperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProceduralHandWrapper wrapper = (ProceduralHandWrapper)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🧲 Auto-Shape Fingers to 3D Geometry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Geser pedang/objek 3D ke telapak tangan karakter di Scene View.\n" +
                "2. Masukkan Collider objek tersebut ke slot 'Target Object Collider'.\n" +
                "3. Klik tombol di bawah: 15 jari akan otomatis menekuk mendeteksi permukaan objek 3D!",
                MessageType.Info);

            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.5f);
            if (GUILayout.Button("🧲 BENTUK JARI OTOMATIS IKUTI 3D OBJEK", GUILayout.Height(38)))
            {
                wrapper.AutoWrapFingersAroundTarget();
            }

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("💾 Simpan Hasil Bentuk ke Preset Asset", GUILayout.Height(30)))
            {
                wrapper.SaveResultToPoseAsset();
            }
            GUI.backgroundColor = Color.white;
        }
    }
#endif
}
