using System;
using UnityEngine;

namespace Unity.FantasyKingdom.WeaponIK
{
    /// <summary>
    /// Data struktur penyimpanan rotasi lokal untuk 15 ruas tulang jari (Mixamo standard).
    /// </summary>
    [Serializable]
    public struct FingerBoneRotation
    {
        public string boneName;
        public Quaternion localRotation;
    }

    /// <summary>
    /// ScriptableObject untuk menyimpan preset bentuk genggaman jari (misal: Sword/CylinderGrip, PistolGrip, OpenHand).
    /// Mengisolasi data pose dari logika runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHandGripPose", menuName = "Combat/Hand Grip Pose Data", order = 100)]
    public class HandGripPoseData : ScriptableObject
    {
        [Tooltip("Nama identifikasi preset grip (misal: CylinderGrip, HeavyWeaponGrip, PistolGrip).")]
        public string poseName = "CylinderGrip";

        [Tooltip("Daftar snapshot rotasi lokal 15 tulang jari tangan kanan.")]
        public FingerBoneRotation[] rightHandBones = new FingerBoneRotation[15];

        [Tooltip("Daftar snapshot rotasi lokal 15 tulang jari tangan kiri (opsional untuk two-handed).")]
        public FingerBoneRotation[] leftHandBones = new FingerBoneRotation[15];

        /// <summary>
        /// Nama standar 15 tulang jari tangan kanan sesuai rig Mixamo.
        /// </summary>
        public static readonly string[] RightFingerBoneNames = new string[]
        {
            "mixamorig:RightHandThumb1",  "mixamorig:RightHandThumb2",  "mixamorig:RightHandThumb3",
            "mixamorig:RightHandIndex1",  "mixamorig:RightHandIndex2",  "mixamorig:RightHandIndex3",
            "mixamorig:RightHandMiddle1", "mixamorig:RightHandMiddle2", "mixamorig:RightHandMiddle3",
            "mixamorig:RightHandRing1",   "mixamorig:RightHandRing2",   "mixamorig:RightHandRing3",
            "mixamorig:RightHandPinky1",  "mixamorig:RightHandPinky2",  "mixamorig:RightHandPinky3"
        };

        /// <summary>
        /// Nama standar 15 tulang jari tangan kiri sesuai rig Mixamo.
        /// </summary>
        public static readonly string[] LeftFingerBoneNames = new string[]
        {
            "mixamorig:LeftHandThumb1",  "mixamorig:LeftHandThumb2",  "mixamorig:LeftHandThumb3",
            "mixamorig:LeftHandIndex1",  "mixamorig:LeftHandIndex2",  "mixamorig:LeftHandIndex3",
            "mixamorig:LeftHandMiddle1", "mixamorig:LeftHandMiddle2", "mixamorig:LeftHandMiddle3",
            "mixamorig:LeftHandRing1",   "mixamorig:LeftHandRing2",   "mixamorig:LeftHandRing3",
            "mixamorig:LeftHandPinky1",  "mixamorig:LeftHandPinky2",  "mixamorig:LeftHandPinky3"
        };
    }
}
