using UnityEngine;
using System.Collections;
using Cinemachine;

public class CrocodileTrap : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 50f;
    public float stunDuration = 2.0f;
    public float trapRadius = 2.5f;
    public LayerMask enemyLayer;
    
    [Header("Visual References")]
    public Transform leftJaw;
    public Transform rightJaw;
    
    private Transform targetEnemy;
    private bool hasSnapped = false;

    public void SetTarget(Transform target, CinemachineImpulseSource impulse)
    {
        targetEnemy = target;
        // Impulse source tidak disimpan lagi karena dimatikan
    }

    private void Start()
    {
        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");
        
        // Warnai rahang dan gigi saat runtime agar tidak ada error Material URP
        SetColors();

        if (leftJaw != null) leftJaw.localRotation = Quaternion.Euler(0, 0, -45f);
        if (rightJaw != null) rightJaw.localRotation = Quaternion.Euler(0, 0, 45f);
        
        StartCoroutine(SnapSequence());
    }

    private void SetColors()
    {
        // Warnai Hijau Gelap untuk Jaw_Base
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach(var r in renderers)
        {
            if (r.gameObject.name.Contains("Jaw_Base"))
            {
                r.material.color = new Color(0.1f, 0.3f, 0.1f);
            }
            else if (r.gameObject.name.Contains("Tooth"))
            {
                r.material.color = new Color(0.9f, 0.9f, 0.8f);
            }
        }
    }

    private IEnumerator SnapSequence()
    {
        yield return new WaitForSeconds(0.2f);
        
        float snapTime = 0.1f;
        float elapsed = 0f;
        
        Quaternion leftStart = leftJaw != null ? leftJaw.localRotation : Quaternion.identity;
        Quaternion leftEnd = Quaternion.Euler(0, 0, 0);
        
        Quaternion rightStart = rightJaw != null ? rightJaw.localRotation : Quaternion.identity;
        Quaternion rightEnd = Quaternion.Euler(0, 0, 0);

        while (elapsed < snapTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapTime;
            t = t * t; 
            
            if (leftJaw != null) leftJaw.localRotation = Quaternion.Lerp(leftStart, leftEnd, t);
            if (rightJaw != null) rightJaw.localRotation = Quaternion.Lerp(rightStart, rightEnd, t);
            
            yield return null;
        }

        if (leftJaw != null) leftJaw.localRotation = leftEnd;
        if (rightJaw != null) rightJaw.localRotation = rightEnd;

        if (!hasSnapped)
        {
            hasSnapped = true;
            ApplyDamageAndStun();
        }

        yield return new WaitForSeconds(stunDuration);
        Destroy(gameObject);
    }

    private void ApplyDamageAndStun()
    {
        if (targetEnemy != null)
        {
            ProcessEnemyHit(targetEnemy.gameObject);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, trapRadius, enemyLayer);
        foreach (Collider hit in hits)
        {
            if (targetEnemy != null && hit.transform == targetEnemy) continue;
            ProcessEnemyHit(hit.gameObject);
        }
    }

    private void ProcessEnemyHit(GameObject obj)
    {
        EnemyAI enemy = obj.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            StartCoroutine(StunRoutine(enemy.gameObject));
            return;
        }

        BossAI boss = obj.GetComponentInParent<BossAI>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
        }
    }

    private IEnumerator StunRoutine(GameObject enemyObj)
    {
        if (enemyObj == null) yield break;
        UnityEngine.AI.NavMeshAgent navAgent = enemyObj.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            float originalSpeed = navAgent.speed;
            navAgent.isStopped = true;
            navAgent.speed = 0f;

            yield return new WaitForSeconds(stunDuration);

            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.speed = originalSpeed;
            }
        }
    }
}
