using UnityEngine;
using Cinemachine;

public class SkillSpawnBuaya : SkillBase
{
    [Header("Dependencies")]
    public PlayerControl playerControl;
    public CinemachineImpulseSource impulseSource; // Jika butuh screen shake

    [Header("Skill Specifics")]
    public GameObject buayaPrefab;

    private void Reset()
    {
        skillName = "Spawn Mulut Buaya";
        keyBind = KeyCode.B;
        manaCost = 50f;
    }

    private void Start()
    {
        if (playerControl == null)
            playerControl = GetComponent<PlayerControl>();
            
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public override void ActivateSkill()
    {
        if (playerControl == null)
        {
            Debug.LogError("[SpawnBuaya] PlayerControl belum di-assign!");
            return;
        }

        // 1. Cek apakah player sedang aim (punya target)
        if (playerControl.target == null)
        {
            Debug.Log("[SpawnBuaya] Kamu harus mengarahkan aim ke musuh terlebih dahulu!");
            return;
        }

        // 2. Cek Mana
        if (PlayerStatus.Instance != null && PlayerStatus.Instance.mana >= manaCost)
        {
            PlayerStatus.Instance.UseMana(manaCost);
            SpawnBuaya();
        }
        else
        {
            Debug.Log("[SpawnBuaya] Mana tidak cukup!");
        }
    }

    private void SpawnBuaya()
    {
        if (buayaPrefab == null)
        {
            Debug.LogWarning("[SpawnBuaya] Prefab Mulut Buaya belum di-assign!");
            return;
        }

        // Ambil posisi target yang sedang di-aim
        Transform targetTransform = playerControl.target;
        Vector3 spawnPos = targetTransform.position;

        // Raycast ke bawah sedikit dari posisi target agar jebakan nempel persis di tanah
        if (Physics.Raycast(spawnPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Default", "Ground", "Terrain")))
        {
            spawnPos.y = hit.point.y;
        }

        // Spawn prefab buaya
        GameObject trapInstance = Instantiate(buayaPrefab, spawnPos, Quaternion.identity);

        // (Opsional) Jika prefab punya script CrocodileTrap, beritahu siapa yang jadi targetnya
        CrocodileTrap trapScript = trapInstance.GetComponent<CrocodileTrap>();
        if (trapScript != null)
        {
            trapScript.SetTarget(targetTransform, impulseSource);
        }

        Debug.Log($"[SpawnBuaya] Berhasil di-spawn di bawah target: {targetTransform.name}");
    }
}
