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
    
    [Header("Camera Hard-Lock")]
    public bool isHardLocked = false;

    private void Awake()
    {
        instance = this;
    }

    // Ubah: Deteksi dipanggil setiap frame secara otomatis
    private void Update()
    {
        HandleHardLockInput();
        DetectTargetByMouse();
    }

    private void HandleHardLockInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isHardLocked)
            {
                isHardLocked = false;
                if (debug) Debug.Log("Hard-Lock Disabled");
            }
            else
            {
                // Hanya bisa mengunci jika sedang ada target valid
                if (playerControl.target != null)
                {
                    isHardLocked = true;
                    if (debug) Debug.Log("Hard-Lock Enabled on " + playerControl.target.name);
                }
            }
        }
    }

    public void DetectTargetByMouse()
    {
        // Jika pedang belum dicabut, atau sedang melakukan takedown
        if (playerControl.isSwordSheathed || playerControl.GetComponent<StarterAssets.ThirdPersonController>().IsInFinisher)
        {
            playerControl.NoTarget();
            return;
        }

        // Jika sedang di-lock (karena hold attack atau sedang dalam animasi serangan), 
        // pertahankan target saat ini dan jangan mencari target baru.
        if (!canChangeTarget)
        {
            return;
        }

        // Jika kamera sedang Hard-Locked, jangan ubah target berdasarkan kursor mouse
        if (isHardLocked)
        {
            // Cek apakah target yang di-lock sudah mati atau tidak ada
            if (playerControl.target == null)
            {
                isHardLocked = false;
            }
            else
            {
                EnemyAI enemy = playerControl.target.GetComponent<EnemyAI>();
                BossAI boss = playerControl.target.GetComponent<BossAI>();
                
                if ((enemy != null && enemy.isDead) || (boss != null && boss.CurrentHealth <= 0))
                {
                    isHardLocked = false;
                    playerControl.NoTarget();
                }
            }
            
            return; // Jangan lakukan raycast mouse jika sedang hard lock
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