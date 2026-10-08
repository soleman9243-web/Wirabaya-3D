using UnityEngine;

/// <summary>
/// Data konfigurasi boss Babi. Semua parameter yang bisa di-tune ada di sini.
/// Buat asset via menu: Create > Boss > Boar Boss Data
/// </summary>
[CreateAssetMenu(fileName = "New Boar Boss Data", menuName = "Boss/Boar Boss Data")]
public class BoarBossData : ScriptableObject
{
    [Header("General")]
    public string bossName = "Babi Hutan";
    public float maxHealth = 500f;

    [Header("Intro Timing")]
    [Tooltip("Durasi nama boss fade in + slide dari kiri")]
    public float nameFadeInDuration = 1.5f;
    [Tooltip("Durasi health bar terisi dari kosong ke penuh")]
    public float healthFillDuration = 2f;

    [Header("Phase Threshold")]
    [Tooltip("Boss masuk Fase 2 ketika HP <= persentase ini (misal 0.5 = 50%)")]
    [Range(0.01f, 0.99f)]
    public float phase2ThresholdPercent = 0.5f;

    [Header("Phase Transition")]
    [Tooltip("Durasi animasi transisi antar fase (roar/mengamuk)")]
    public float phaseTransitionDuration = 3f;

    [Header("Fase 1 & 2 — Charge")]
    [Tooltip("Durasi boss idle sebelum mulai aim (Fase 1)")]
    public float idleDuration = 4f;
    [Tooltip("Durasi boss membidik player sebelum charge (arah dikunci di akhir)")]
    public float aimDuration = 2f;
    [Tooltip("Kecepatan charge boss")]
    public float chargeSpeed = 15f;
    [Tooltip("Jarak maksimal charge sebelum berhenti")]
    public float chargeMaxDistance = 20f;
    [Tooltip("Damage charge ke player (1x hit per charge)")]
    public float chargeDamage = 30f;

    [Header("Fase 2 — Charge Cooldown")]
    [Tooltip("Cooldown charge di Fase 2 (detik). Selama cooldown, boss pakai melee/ranged")]
    public float chargeCooldown = 10f;

    [Header("Fase 2 — Melee")]
    [Tooltip("Jarak threshold: <= ini = melee, > ini = ranged")]
    public float meleeDecisionRange = 5f;
    [Tooltip("Jarak boss mulai memukul (harus < meleeDecisionRange)")]
    public float meleeAttackRange = 2.5f;
    [Tooltip("Damage serangan melee")]
    public float meleeDamage = 20f;
    [Tooltip("Jeda antar serangan melee (detik)")]
    public float meleeAttackCooldown = 2f;
    [Tooltip("Kecepatan jalan boss mengejar player untuk melee")]
    public float meleeMoveSpeed = 4f;

    [Header("Fase 2 — Ranged (Lempar Batu)")]
    [Tooltip("Durasi boss membidik sebelum mencungkil batu")]
    public float rangedAimDuration = 1.5f;
    [Tooltip("Durasi animasi mencungkil batu dari tanah")]
    public float rockDigDuration = 1f;
    [Tooltip("Radius lingkaran telegraph merah di tanah")]
    public float rockTelegraphRadius = 3f;
    [Tooltip("Durasi lingkaran terisi (= waktu batu terbang sampai jatuh). Sinkron dengan lemparan.")]
    public float rockFillDuration = 2f;
    [Tooltip("Damage batu jatuh ke player di dalam lingkaran")]
    public float rockDamage = 40f;
    [Tooltip("Jeda setelah batu jatuh sebelum aksi berikutnya")]
    public float rangedRecoveryTime = 1.5f;
    [Tooltip("Tinggi lengkungan parabola batu di udara")]
    public float rockArcHeight = 15f;
}
