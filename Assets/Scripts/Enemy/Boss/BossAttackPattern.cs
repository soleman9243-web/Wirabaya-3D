using UnityEngine;

[CreateAssetMenu(fileName = "New Boss Attack", menuName = "Boss/Attack Pattern")]
public class BossAttackPattern : ScriptableObject
{
    [Header("Attack Properties")]
    public string attackName = "Basic Attack";
    public string animationTrigger = "Attack1";
    
    [Tooltip("Damage yang diberikan ke player")]
    public float damage = 20f;
    
    [Tooltip("Jarak maksimal serangan ini bisa mengenai player")]
    public float attackRange = 3f;

    [Tooltip("Waktu animasi mengayun sebelum damage masuk (detik)")]
    public float windupTime = 0.5f;

    [Tooltip("Waktu bos diam setelah menyerang, memberi kesempatan player memulihkan stamina (detik)")]
    public float recoveryDelay = 2f;

    [Tooltip("Apakah serangan ini bisa di-parry oleh player?")]
    public bool canBeParried = true;
}
