using UnityEngine;
using Cinemachine;

public class SnapTrapSkill : MonoBehaviour
{
    [Header("Input")]
    public KeyCode skillKey = KeyCode.R;

    [Header("Mana Cost")]
    public float manaCost = 45f;

    [Header("Cooldown")]
    public float cooldownTime = 10f;
    private float lastUsedTime = -999f;

    [Header("Skill Prefab")]
    public GameObject trapPrefab;

    [Header("Dependencies")]
    public Animator animator;
    public PlayerControl playerControl;
    public CinemachineImpulseSource impulseSource;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerControl == null) playerControl = GetComponent<PlayerControl>();
    }

    private void Update()
    {
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.health <= 0) return;

        if (Input.GetKeyDown(skillKey))
        {
            TryCastTrap();
        }
    }

    private void TryCastTrap()
    {
        if (playerControl != null && !playerControl.canUseSkills) return;

        if (Time.time - lastUsedTime < cooldownTime)
        {
            Debug.Log($"[SnapTrap] Cooldown: {cooldownTime - (Time.time - lastUsedTime):F1}s tersisa.");
            return;
        }

        if (PlayerStatus.Instance.mana < manaCost)
        {
            Debug.Log("[SnapTrap] Mana tidak cukup!");
            return;
        }

        PlayerStatus.Instance.UseMana(manaCost);
        lastUsedTime = Time.time;

        if (animator != null) animator.SetTrigger("CastGeyser"); 

        SpawnTrap();
    }

    private void SpawnTrap()
    {
        if (trapPrefab == null)
        {
            Debug.LogWarning("[SnapTrap] Prefab Jebakan belum di-assign!");
            return;
        }

        Vector3 spawnPos;
        Transform targetTransform = null;

        // Spawn tepat di posisi musuh
        if (playerControl != null && playerControl.target != null)
        {
            spawnPos = playerControl.target.position;
            targetTransform = playerControl.target;
        }
        else
        {
            spawnPos = transform.position + transform.forward * 3f;
        }

        // Raycast untuk nempel ke tanah
        if (Physics.Raycast(spawnPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Default", "Ground", "Terrain")))
        {
            spawnPos.y = hit.point.y;
        }

        GameObject trapInstance = Instantiate(trapPrefab, spawnPos, Quaternion.identity);

        // Beritahu trap siapa targetnya agar bisa di-stun
        CrocodileTrap trapScript = trapInstance.GetComponent<CrocodileTrap>();
        if (trapScript != null)
        {
            trapScript.SetTarget(targetTransform, impulseSource);
        }
    }
}
