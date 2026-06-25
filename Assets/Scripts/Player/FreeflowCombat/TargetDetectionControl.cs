using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDetectionControl : MonoBehaviour
{
    public static TargetDetectionControl instance;

    [Header("Components")]
    public PlayerControl playerControl;

    [Header("Target Detection")]
    public LayerMask whatIsEnemy;
    public bool canChangeTarget = true;

    [Range(0f, 15f)]
    public float detectionRange = 10f;

    [Header("Debug")]
    public bool debug;

    private void Awake()
    {
        instance = this;
    }

    // Ubah: Deteksi dipanggil setiap frame secara otomatis
    private void Update()
    {
        DetectTargetByMouse();
    }

    public void DetectTargetByMouse()
    {
        // Jika sedang menyerang, atau pedang belum dicabut, atau sedang melakukan takedown
        if (!canChangeTarget || playerControl.isSwordSheathed || playerControl.GetComponent<StarterAssets.ThirdPersonController>().IsInFinisher)
        {
            playerControl.NoTarget();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, whatIsEnemy))
        {
            Transform targetTransform = null;
            
            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                targetTransform = enemy.transform;
            }
            else
            {
                BossAI boss = hit.collider.GetComponentInParent<BossAI>();
                if (boss != null)
                {
                    targetTransform = boss.transform;
                }
            }

            if (targetTransform != null)
            {
                float distance = Vector3.Distance(playerControl.transform.position, targetTransform.position);

                if (distance <= detectionRange)
                {
                    playerControl.ChangeTarget(targetTransform);

                    if (debug) Debug.Log("Target: " + targetTransform.name);

                    return; // Target valid ditemukan, keluar dari fungsi
                }
            }
        }

        // UBAH: Jika kursor tidak mengarah ke musuh atau di luar jarak, matikan indikator target
        playerControl.NoTarget();
    }
}