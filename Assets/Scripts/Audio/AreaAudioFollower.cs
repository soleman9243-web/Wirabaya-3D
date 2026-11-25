using UnityEngine;

/// <summary>
/// Mengatur volume suara laut/area secara otomatis berdasarkan jarak pemain ke Collider:
/// - Dekat / di dalam collider -> suara penuh (maxVolume)
/// - Menjauh -> suara pelan-pelan mengecil sampai benar-benar hilang (0)
/// Bisa dipasang langsung di GameObject yang sama dengan BoxCollider & AudioSource,
/// atau dengan AudioSource di child object.
/// </summary>
public class AreaAudioFollower : MonoBehaviour
{
    [Header("=== Pengaturan Jarak Suara ===")]
    [Tooltip("Jarak dari tepi laut di mana suara sudah 100% keras penuh.")]
    [SerializeField] private float fullVolumeDistance = 15f;

    [Tooltip("Jarak maksimal dari tepi laut. Lebih jauh dari ini, suara PASTI 0 (hilang total).")]
    [SerializeField] private float maxHearDistance = 80f;

    [Range(0f, 1f)]
    [Tooltip("Volume maksimal saat berada di dekat laut.")]
    [SerializeField] private float maxVolume = 1f;

    [Header("=== Referensi (Boleh Kosong / Otomatis) ===")]
    [Tooltip("Target yang diukur jaraknya (Player / Main Camera). Kosongkan untuk otomatis.")]
    [SerializeField] private Transform listenerTarget;

    [Tooltip("Collider area laut. Kosongkan untuk otomatis mengambil Collider di objek ini/children.")]
    [SerializeField] private Collider[] areaColliders;

    [Tooltip("AudioSource yang memutar suara. Kosongkan untuk otomatis.")]
    [SerializeField] private AudioSource targetAudioSource;

    [SerializeField] private float smoothSpeed = 12f;

    private bool canMoveAudioTransform = false;

    private void Awake()
    {
        if (targetAudioSource == null)
        {
            targetAudioSource = GetComponentInChildren<AudioSource>();
        }

        if (areaColliders == null || areaColliders.Length == 0)
        {
            areaColliders = GetComponentsInChildren<Collider>();
        }

        CheckTransformSeparation();

        if (targetAudioSource != null)
        {
            if (!canMoveAudioTransform)
            {
                // Jika satu GameObject dengan Collider, pakai volume-fade murni agar tidak terhitung dari titik tengah kotak
                targetAudioSource.spatialBlend = 0f;
            }
            else
            {
                targetAudioSource.spatialBlend = 1f;
                targetAudioSource.rolloffMode = AudioRolloffMode.Linear;
                targetAudioSource.minDistance = fullVolumeDistance;
                targetAudioSource.maxDistance = maxHearDistance;
            }
        }
    }

    private void Start()
    {
        ResolveListenerTarget();
        UpdateAudioByDistance(instant: true);
    }

    private void Update()
    {
        if (targetAudioSource == null || areaColliders == null || areaColliders.Length == 0) return;

        if (listenerTarget == null)
        {
            ResolveListenerTarget();
            if (listenerTarget == null) return;
        }

        UpdateAudioByDistance(instant: false);
    }

    private void UpdateAudioByDistance(bool instant)
    {
        if (listenerTarget == null || targetAudioSource == null) return;

        Vector3 closest = GetClosestPointAcrossColliders(listenerTarget.position);
        float distance = Vector3.Distance(listenerTarget.position, closest);

        if (canMoveAudioTransform)
        {
            Transform audioTr = targetAudioSource.transform;
            audioTr.position = (instant || smoothSpeed <= 0f)
                ? closest
                : Vector3.Lerp(audioTr.position, closest, Time.deltaTime * smoothSpeed);
        }

        float t = Mathf.InverseLerp(maxHearDistance, fullVolumeDistance, distance);
        targetAudioSource.volume = Mathf.Clamp01(t) * maxVolume;
    }

    private Vector3 GetClosestPointAcrossColliders(Vector3 point)
    {
        Vector3 bestPoint = transform.position;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < areaColliders.Length; i++)
        {
            Collider col = areaColliders[i];
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

            Vector3 candidate = (col is MeshCollider mc && !mc.convex)
                ? col.ClosestPointOnBounds(point)
                : col.ClosestPoint(point);

            float distSqr = (candidate - point).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestPoint = candidate;
            }
        }

        return bestPoint;
    }

    private void CheckTransformSeparation()
    {
        canMoveAudioTransform = targetAudioSource != null;
        if (targetAudioSource == null || areaColliders == null) return;

        for (int i = 0; i < areaColliders.Length; i++)
        {
            if (areaColliders[i] != null && areaColliders[i].transform == targetAudioSource.transform)
            {
                canMoveAudioTransform = false;
                return;
            }
        }
    }

    private void ResolveListenerTarget()
    {
        if (listenerTarget != null) return;

        AudioListener activeListener = FindAnyObjectByType<AudioListener>();
        if (activeListener != null && activeListener.enabled)
        {
            listenerTarget = activeListener.transform;
            return;
        }

        if (Camera.main != null)
        {
            listenerTarget = Camera.main.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            listenerTarget = playerObj.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (areaColliders == null || areaColliders.Length == 0) return;

        Vector3 refPos = listenerTarget != null
            ? listenerTarget.position
            : (Camera.main != null ? Camera.main.transform.position : transform.position);

        Vector3 closest = GetClosestPointAcrossColliders(refPos);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(refPos, closest);
        Gizmos.DrawWireSphere(closest, 1.5f);
    }
}
