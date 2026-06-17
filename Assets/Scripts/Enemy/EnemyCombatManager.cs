using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatManager : MonoBehaviour
{
    public static EnemyCombatManager Instance;

    [Header("Combat Settings")]
    [Tooltip("Waktu tunggu (detik) sebelum musuh lain boleh menyerang setelah serangan musuh sebelumnya selesai.")]
    public float globalAttackCooldown = 2f;
    [Tooltip("Maksimal musuh yang boleh menyerang secara bersamaan.")]
    public int maxSimultaneousAttackers = 2;
    private float currentCooldown = 0f;

    private List<EnemyPatrol> engagedEnemies = new List<EnemyPatrol>();
    private List<EnemyPatrol> currentAttackers = new List<EnemyPatrol>();

    [Header("Player Reference")]
    public Transform player;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void RegisterEnemy(EnemyPatrol enemy)
    {
        if (!engagedEnemies.Contains(enemy))
        {
            engagedEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyPatrol enemy)
    {
        if (engagedEnemies.Contains(enemy))
        {
            engagedEnemies.Remove(enemy);
        }

        if (currentAttackers.Contains(enemy))
        {
            currentAttackers.Remove(enemy);
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Cek apakah player sedang takedown/parry (kebal)
        bool isPlayerInvincible = false;
        StarterAssets.ThirdPersonController tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null && tpc.IsInFinisher)
        {
            isPlayerInvincible = true;
        }

        if (isPlayerInvincible)
        {
            // Batalkan semua serangan jika player sedang kebal (takedown/parry)
            for (int i = currentAttackers.Count - 1; i >= 0; i--)
            {
                if (currentAttackers[i] != null)
                {
                    currentAttackers[i].CancelAttack();
                }
            }
            currentAttackers.Clear();
            return; // Jangan hitung cooldown atau beri token baru saat player kebal
        }

        // Hapus musuh yang sudah mati atau stagger dari list active
        for (int i = engagedEnemies.Count - 1; i >= 0; i--)
        {
            if (engagedEnemies[i] == null || engagedEnemies[i].IsDeadOrStaggered())
            {
                if (currentAttackers.Contains(engagedEnemies[i]))
                {
                    engagedEnemies[i].RevokeAttackToken();
                    currentAttackers.Remove(engagedEnemies[i]);
                }
                engagedEnemies.RemoveAt(i);
            }
        }

        // Hitung cooldown dan assign token jika masih ada slot penyerang kosong
        if (currentAttackers.Count < maxSimultaneousAttackers && engagedEnemies.Count > 0)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                AssignAttackToken();
            }
        }
    }

    private void AssignAttackToken()
    {
        // Cari kandidat penyerang (yang terdekat dengan player dan belum menyerang)
        EnemyPatrol bestCandidate = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in engagedEnemies)
        {
            // Lewati jika sudah menyerang
            if (enemy == null || enemy.IsDeadOrStaggered() || currentAttackers.Contains(enemy)) continue;

            float dist = Vector3.Distance(enemy.transform.position, player.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestCandidate = enemy;
            }
        }

        if (bestCandidate != null)
        {
            currentAttackers.Add(bestCandidate);
            bestCandidate.GiveAttackToken();
            currentCooldown = globalAttackCooldown;
        }
    }

    public void ReportAttackFinished(EnemyPatrol enemy)
    {
        if (currentAttackers.Contains(enemy))
        {
            currentAttackers.Remove(enemy);
            
            // Jika slot mulai kosong, langsung mulai cooldown agar musuh berikutnya siap-siap
            if (currentAttackers.Count < maxSimultaneousAttackers && currentCooldown <= 0f)
            {
                currentCooldown = globalAttackCooldown; 
            }
        }
    }

    // Mendapatkan posisi mengelilingi player
    public Vector3 GetCirclePosition(EnemyPatrol enemy, float circleRadius)
    {
        if (player == null) return enemy.transform.position;

        int index = engagedEnemies.IndexOf(enemy);
        if (index == -1) return enemy.transform.position;

        // Base angle terdistribusi merata
        float baseAngle = (360f / engagedEnemies.Count) * index;
        
        // Tambahkan rotasi konstan keseluruhan lingkaran
        float globalRotation = Time.time * 10f;

        // Gunakan Perlin Noise berdasarkan ID instance agar setiap musuh punya gerakan acak/maju-mundur sendiri di lintasannya
        float noise = Mathf.PerlinNoise(enemy.GetInstanceID(), Time.time * 0.3f);
        // Map noise (0 s.d 1) ke (-30 derajat s.d +30 derajat) untuk offset
        float randomOffset = (noise - 0.5f) * 60f;

        float finalAngle = baseAngle + globalRotation + randomOffset;

        Vector3 offset = new Vector3(Mathf.Sin(finalAngle * Mathf.Deg2Rad), 0, Mathf.Cos(finalAngle * Mathf.Deg2Rad)) * circleRadius;
        return player.position + offset;
    }
}
