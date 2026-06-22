using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BossPhase
{
    [Tooltip("Fase ini aktif jika HP Bos berada DI BAWAH ATAU SAMA DENGAN persentase ini (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float healthThresholdPercentage = 1f;

    [Tooltip("Daftar serangan yang bisa digunakan boss pada fase ini")]
    public List<BossAttackPattern> allowedAttacks;
}

[CreateAssetMenu(fileName = "New Boss Data", menuName = "Boss/Boss Data")]
public class BossData : ScriptableObject
{
    public string bossName = "Unknown Boss";
    public float maxHealth = 500f;

    [Tooltip("Daftar fase bos, urutkan dari HP paling besar ke paling kecil (misal: Fase 1 threshold 1.0, Fase 2 threshold 0.5)")]
    public List<BossPhase> phases;
}
