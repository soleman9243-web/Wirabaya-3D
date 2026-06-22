using UnityEngine;
using System.Collections;
using Cinemachine;

public class PlayerParry : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode parryKey = KeyCode.Mouse1;
    public float parryWindow = 0.3f;

    [Header("Visuals")]
    [SerializeField] private GameObject spiderSenseIndicator;
    [SerializeField] private Animator animator;

    [Header("Hit-Stop & Camera Settings")]
    [SerializeField] private float hitStopTimeScale = 0.05f;
    [SerializeField] private float hitStopDuration = 0.2f;

    [Header("Quest Integration")]
    [Tooltip("ID objektif yang selesai saat berhasil parry (misal: obj_LakukanParry)")]
    public string parryObjectiveId;

    [Header("Cinematic Parry Cameras")]
    [SerializeField] private GameObject parryCamRight;
    [SerializeField] private GameObject parryCamLeft;
    [SerializeField] private float cinematicCamDuration = 0.5f;

    [Header("Impact Effects")]
    [SerializeField] private GameObject parryParticlePrefab;
    [SerializeField] private Transform parryImpactPoint;

    [Header("Positioning (Takedown Style)")]
    public float parryDistance = 1.5f;
    public float snapSpeed = 15f;

    public bool isParrying { get; private set; }
    private EnemyAI currentAttacker;
    private BossAI currentBossAttacker;

    private void Awake()
    {
        if (spiderSenseIndicator != null) spiderSenseIndicator.SetActive(false);

        if (parryCamRight != null) parryCamRight.SetActive(false);
        if (parryCamLeft != null) parryCamLeft.SetActive(false);
    }

    private void Update()
    {
        StarterAssets.ThirdPersonController tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null && tpc.IsInFinisher) return;

        if (Input.GetKeyDown(parryKey))
        {
            if (currentAttacker != null)
            {
                ExecuteParrySuccess(currentAttacker);
            }
            else if (currentBossAttacker != null)
            {
                ExecuteBossParrySuccess(currentBossAttacker);
            }
            else if (!isParrying)
            {
                StartCoroutine(ParryMissRoutine());
            }
        }
    }

    private IEnumerator ParryMissRoutine()
    {
        isParrying = true;
        animator.SetTrigger("Parry");
        yield return new WaitForSeconds(parryWindow);
        isParrying = false;
    }

    public void EnableSpiderSense(EnemyAI attacker)
    {
        currentAttacker = attacker;
        if (spiderSenseIndicator != null) spiderSenseIndicator.SetActive(true);
    }

    public void EnableBossSpiderSense(BossAI bossAttacker)
    {
        currentBossAttacker = bossAttacker;
        if (spiderSenseIndicator != null) spiderSenseIndicator.SetActive(true);
    }

    public void DisableSpiderSense()
    {
        currentAttacker = null;
        currentBossAttacker = null;
        if (spiderSenseIndicator != null) spiderSenseIndicator.SetActive(false);
    }

    private void ExecuteParrySuccess(EnemyAI enemy)
    {
        StarterAssets.ThirdPersonController tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.IsInFinisher = true;

        DisableSpiderSense();

        StartCoroutine(SnapToEnemy(enemy.transform));

        animator.SetTrigger("Parry");
        enemy.TriggerStagger();

        if (parryParticlePrefab != null && parryImpactPoint != null)
        {
            Instantiate(parryParticlePrefab, parryImpactPoint.position, Quaternion.identity);
        }

        StartCoroutine(HitStopAndCameraRoutine());

        Debug.Log("Parry Berhasil! Player menghadap dan menempel ke musuh.");

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(parryObjectiveId))
        {
            if (QuestManager.Instance.IsObjectiveActive(parryObjectiveId))
            {
                QuestManager.Instance.AddProgress(parryObjectiveId, 1);
            }
        }
    }

    private void ExecuteBossParrySuccess(BossAI boss)
    {
        StarterAssets.ThirdPersonController tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.IsInFinisher = true;

        DisableSpiderSense();

        StartCoroutine(SnapToEnemy(boss.transform));

        animator.SetTrigger("Parry");
        boss.TriggerStagger();

        if (parryParticlePrefab != null && parryImpactPoint != null)
        {
            Instantiate(parryParticlePrefab, parryImpactPoint.position, Quaternion.identity);
        }

        StartCoroutine(HitStopAndCameraRoutine());

        Debug.Log("Boss Parry Berhasil!");

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(parryObjectiveId))
        {
            if (QuestManager.Instance.IsObjectiveActive(parryObjectiveId))
            {
                QuestManager.Instance.AddProgress(parryObjectiveId, 1);
            }
        }
    }

    private IEnumerator SnapToEnemy(Transform enemyTransform)
    {
        Vector3 directionToEnemy = (enemyTransform.position - transform.position).normalized;
        directionToEnemy.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);

        Vector3 targetPosition = enemyTransform.position - (directionToEnemy * parryDistance);
        targetPosition.y = transform.position.y;

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * snapSpeed;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);

            yield return null;
        }

        transform.rotation = targetRotation;
        transform.position = targetPosition;
    }

    private IEnumerator HitStopAndCameraRoutine()
    {
        // 1. Deteksi Kamera Kanan/Kiri
        Vector3 camLocalPos = transform.InverseTransformPoint(Camera.main.transform.position);
        GameObject activeCinematicCam = (camLocalPos.x > 0) ? parryCamRight : parryCamLeft;

        // Ambil komponen Cinemachine Brain dari Main Camera
        CinemachineBrain cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        // Simpan update method aslinya buat dibalikin nanti
        CinemachineBrain.UpdateMethod originalUpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;

        if (cinemachineBrain != null)
        {
            originalUpdateMethod = cinemachineBrain.m_UpdateMethod;

        }

        if (activeCinematicCam != null) activeCinematicCam.SetActive(true);

        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = hitStopTimeScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);
        yield return new WaitForSecondsRealtime(cinematicCamDuration);

        if (activeCinematicCam != null) activeCinematicCam.SetActive(false);
        Time.timeScale = 1f;

        if (cinemachineBrain != null) cinemachineBrain.m_UpdateMethod = originalUpdateMethod;

        StarterAssets.ThirdPersonController tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.IsInFinisher = false;
    }
}