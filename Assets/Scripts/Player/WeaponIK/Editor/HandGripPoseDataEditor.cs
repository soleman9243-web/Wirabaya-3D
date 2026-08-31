using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.FantasyKingdom.WeaponIK.EditorTools
{
    /// <summary>
    /// Custom Inspector Tool untuk HandGripPoseData ScriptableObject.
    /// Dilengkapi Generator Otomatis: Membuat pose genggaman jari (Pedang, Pistol, Kepalan)
    /// secara instan TANPA perlu memutar 15 tulang jari secara manual satu per satu!
    /// </summary>
    [CustomEditor(typeof(HandGripPoseData))]
    public class HandGripPoseDataEditor : Editor
    {
        private GameObject sourceCharacter;
        private float curlAmount = 0.85f;
        private float thumbWrap = 0.75f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HandGripPoseData data = (HandGripPoseData)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("⚡ Generator Genggaman Otomatis (1-Klik)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tidak perlu memutar 15 jari satu per satu!\n" +
                "Klik salah satu tombol di bawah untuk membuat pose jari melingkari gagang secara instan.",
                MessageType.Info);

            EditorGUILayout.BeginVertical("box");

            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f);
            if (GUILayout.Button("⚔️ 1-KLIK: Generate Pose Gagang Pedang (Cylinder Grip)", GUILayout.Height(36)))
            {
                GenerateProceduralGrip(data, GripType.SwordCylinder);
            }

            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            if (GUILayout.Button("🔫 1-KLIK: Generate Pose Pistol (Telunjuk Lurus)", GUILayout.Height(30)))
            {
                GenerateProceduralGrip(data, GripType.Pistol);
            }

            GUI.backgroundColor = new Color(0.8f, 0.4f, 1f);
            if (GUILayout.Button("✊ 1-KLIK: Generate Kepalan Tangan Penuh (Full Fist)", GUILayout.Height(30)))
            {
                GenerateProceduralGrip(data, GripType.Fist);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🎛️ Atur Kerapatan Genggaman (Slider)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            curlAmount = EditorGUILayout.Slider("Kerapatan Jari (Curl)", curlAmount, 0.1f, 1.2f);
            thumbWrap = EditorGUILayout.Slider("Tekukan Jempol (Thumb)", thumbWrap, 0.1f, 1.2f);

            sourceCharacter = (GameObject)EditorGUILayout.ObjectField("Preview Character (Opsional)", sourceCharacter, typeof(GameObject), true);

            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
            if (GUILayout.Button("✨ Terapkan Slider ke Preset & Simpan", GUILayout.Height(30)))
            {
                ApplyCustomCurl(data, curlAmount, thumbWrap);
                if (sourceCharacter != null)
                {
                    ApplyPoseToSceneCharacter(data, sourceCharacter, true);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("📸 Rekam Manual (Jika Ingin Custom Sendiri)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Rekam Tangan Kanan", GUILayout.Height(26)))
            {
                if (sourceCharacter == null)
                {
                    EditorUtility.DisplayDialog("Peringatan", "Tentukan Source Character dulu!", "OK");
                    return;
                }
                BakeHandPose(data, sourceCharacter, true);
            }

            if (GUILayout.Button("Rekam Tangan Kiri", GUILayout.Height(26)))
            {
                if (sourceCharacter == null)
                {
                    EditorUtility.DisplayDialog("Peringatan", "Tentukan Source Character dulu!", "OK");
                    return;
                }
                BakeHandPose(data, sourceCharacter, false);
            }
            EditorGUILayout.EndHorizontal();
        }

        private enum GripType { SwordCylinder, Pistol, Fist }

        private void GenerateProceduralGrip(HandGripPoseData data, GripType type)
        {
            Undo.RecordObject(data, "Generate Procedural Grip");

            data.rightHandBones = new FingerBoneRotation[15];
            data.leftHandBones = new FingerBoneRotation[15];

            float c = 0.85f;
            float t = 0.75f;
            bool triggerFinger = false;

            if (type == GripType.SwordCylinder)
            {
                c = 0.85f;
                t = 0.8f;
                triggerFinger = false;
            }
            else if (type == GripType.Pistol)
            {
                c = 0.9f;
                t = 0.7f;
                triggerFinger = true;
            }
            else if (type == GripType.Fist)
            {
                c = 1.1f;
                t = 1.0f;
                triggerFinger = false;
            }

            FillBonesArray(data.rightHandBones, HandGripPoseData.RightFingerBoneNames, c, t, triggerFinger, true);
            FillBonesArray(data.leftHandBones, HandGripPoseData.LeftFingerBoneNames, c, t, triggerFinger, false);

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Sukses!", $"Pose genggaman '{type}' berhasil di-generate secara otomatis untuk 15 tulang jari!", "Mantap");
        }

        private void ApplyCustomCurl(HandGripPoseData data, float curl, float thumb)
        {
            Undo.RecordObject(data, "Apply Custom Curl");
            data.rightHandBones = new FingerBoneRotation[15];
            data.leftHandBones = new FingerBoneRotation[15];

            FillBonesArray(data.rightHandBones, HandGripPoseData.RightFingerBoneNames, curl, thumb, false, true);
            FillBonesArray(data.leftHandBones, HandGripPoseData.LeftFingerBoneNames, curl, thumb, false, false);

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        private void FillBonesArray(FingerBoneRotation[] arr, string[] boneNames, float curl, float thumb, bool isTrigger, bool isRight)
        {
            // Sudut tekukan natural Mixamo Humanoid untuk gagang silinder
            // Ruas 1 (Proximal), Ruas 2 (Intermediate), Ruas 3 (Distal)
            float baseCurl = 48f * curl;
            float midCurl = 65f * curl;
            float tipCurl = 50f * curl;

            for (int i = 0; i < 15; i++)
            {
                arr[i].boneName = boneNames[i];
                Quaternion rot = Quaternion.identity;

                // Index 0, 1, 2 = Thumb
                if (i == 0) // Thumb1
                    rot = Quaternion.Euler(-18f * thumb, isRight ? 25f * thumb : -25f * thumb, 15f * thumb);
                else if (i == 1) // Thumb2
                    rot = Quaternion.Euler(0f, 0f, isRight ? -35f * thumb : 35f * thumb);
                else if (i == 2) // Thumb3
                    rot = Quaternion.Euler(0f, 0f, isRight ? -45f * thumb : 45f * thumb);

                // Index 3, 4, 5 = Index Finger
                else if (i == 3)
                    rot = Quaternion.Euler(0f, 0f, isTrigger ? -10f : (isRight ? -baseCurl : baseCurl));
                else if (i == 4)
                    rot = Quaternion.Euler(0f, 0f, isTrigger ? -15f : (isRight ? -midCurl : midCurl));
                else if (i == 5)
                    rot = Quaternion.Euler(0f, 0f, isTrigger ? -10f : (isRight ? -tipCurl : tipCurl));

                // Middle 6, 7, 8
                else if (i == 6) rot = Quaternion.Euler(0f, 0f, isRight ? -(baseCurl + 3f) : (baseCurl + 3f));
                else if (i == 7) rot = Quaternion.Euler(0f, 0f, isRight ? -(midCurl + 4f) : (midCurl + 4f));
                else if (i == 8) rot = Quaternion.Euler(0f, 0f, isRight ? -(tipCurl + 2f) : (tipCurl + 2f));

                // Ring 9, 10, 11
                else if (i == 9) rot = Quaternion.Euler(0f, 0f, isRight ? -(baseCurl + 5f) : (baseCurl + 5f));
                else if (i == 10) rot = Quaternion.Euler(0f, 0f, isRight ? -(midCurl + 5f) : (midCurl + 5f));
                else if (i == 11) rot = Quaternion.Euler(0f, 0f, isRight ? -(tipCurl + 3f) : (tipCurl + 3f));

                // Pinky 12, 13, 14
                else if (i == 12) rot = Quaternion.Euler(0f, 0f, isRight ? -(baseCurl + 8f) : (baseCurl + 8f));
                else if (i == 13) rot = Quaternion.Euler(0f, 0f, isRight ? -(midCurl + 6f) : (midCurl + 6f));
                else if (i == 14) rot = Quaternion.Euler(0f, 0f, isRight ? -(tipCurl + 5f) : (tipCurl + 5f));

                arr[i].localRotation = rot;
            }
        }

        private void ApplyPoseToSceneCharacter(HandGripPoseData data, GameObject character, bool isRight)
        {
            FingerBoneRotation[] bones = isRight ? data.rightHandBones : data.leftHandBones;
            Transform[] allT = character.GetComponentsInChildren<Transform>(true);
            var map = new Dictionary<string, Transform>();
            foreach (var t in allT) if (!map.ContainsKey(t.name)) map.Add(t.name, t);

            Undo.RegisterFullObjectHierarchyUndo(character, "Apply Grip Pose to Character");

            for (int i = 0; i < 15; i++)
            {
                if (map.TryGetValue(bones[i].boneName, out Transform tf))
                {
                    tf.localRotation = bones[i].localRotation;
                }
            }
        }

        private void BakeHandPose(HandGripPoseData data, GameObject character, bool isRightHand)
        {
            Undo.RecordObject(data, "Capture Hand Grip Pose");

            string[] boneNames = isRightHand ? HandGripPoseData.RightFingerBoneNames : HandGripPoseData.LeftFingerBoneNames;
            FingerBoneRotation[] targetArray = isRightHand ? data.rightHandBones : data.leftHandBones;

            if (targetArray == null || targetArray.Length != 15)
            {
                targetArray = new FingerBoneRotation[15];
            }

            Transform[] allTransforms = character.GetComponentsInChildren<Transform>(true);
            var map = new Dictionary<string, Transform>(allTransforms.Length);
            foreach (var t in allTransforms)
            {
                if (!map.ContainsKey(t.name)) map.Add(t.name, t);
            }

            int foundCount = 0;
            for (int i = 0; i < 15; i++)
            {
                string bName = boneNames[i];
                targetArray[i].boneName = bName;

                if (map.TryGetValue(bName, out Transform boneTf))
                {
                    targetArray[i].localRotation = boneTf.localRotation;
                    foundCount++;
                }
                else
                {
                    targetArray[i].localRotation = Quaternion.identity;
                }
            }

            if (isRightHand) data.rightHandBones = targetArray;
            else data.leftHandBones = targetArray;

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Sukses", $"Berhasil merekam {foundCount}/15 tulang jari!", "OK");
        }
    }
}
