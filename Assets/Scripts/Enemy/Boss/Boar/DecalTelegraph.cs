using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Script untuk me-manage animasi perbesaran ukuran URP Decal Projector.
/// Dibuat sebagai pengganti TelegraphIndicator 3D Mesh.
/// </summary>
public class DecalTelegraph : MonoBehaviour
{
    [Header("Visual Settings")]
    public float yOffset = 0f;
    [Tooltip("Apakah decal ini akan mengunci ke posisi player?")]
    public bool trackPlayer = true;

    private DecalProjector projector;
    private Transform trackTarget;
    private float lockTimeBeforeImpact;
    private float targetSize;

    // Animasi
    private bool isFilling = false;
    private float fillDuration;
    private float fillElapsed;
    private bool isLocked = false;

    private void Awake()
    {
        projector = GetComponent<DecalProjector>();
        if (projector == null)
        {
            projector = GetComponentInChildren<DecalProjector>();
        }
    }

    /// <summary>
    /// Inisialisasi awal. Decal langsung muncul tapi ukurannya 0.
    /// </summary>
    public void Initialize(float radius, Transform target = null, float lockTime = 0.3f)
    {
        this.targetSize = radius * 2f; // Size decal adalah diameter (2x radius)
        this.trackTarget = target;
        this.lockTimeBeforeImpact = lockTime;
        this.isLocked = false;

        if (projector != null)
        {
            projector.size = new Vector3(0f, 0f, projector.size.z);
        }
    }

    /// <summary>
    /// Mulai animasi membesar dari tengah ke luar
    /// </summary>
    public void StartFill(float duration)
    {
        this.fillDuration = duration;
        this.fillElapsed = 0f;
        this.isFilling = true;
        this.isLocked = false;
    }

    public Vector3 GetTargetPosition()
    {
        return transform.position;
    }

    private void Update()
    {
        // 1. TRACKING
        if (trackPlayer && trackTarget != null && !isLocked)
        {
            Vector3 targetPos = trackTarget.position;
            targetPos.y += yOffset;
            
            // Proyektor Decal tidak perlu miring ngikuti tanah karena proyektornya akan otomatis nempel ke kontur tanah.
            // Yang penting posisinya tepat dan Z axis menghadap lurus ke bawah.
            transform.position = targetPos;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Menghadap lurus ke bawah
        }

        // 2. FILLING (Animasi membesar)
        if (!isFilling || projector == null) return;

        fillElapsed += Time.deltaTime;
        float remaining = fillDuration - fillElapsed;

        // Cek lock
        if (!isLocked && remaining <= lockTimeBeforeImpact)
        {
            isLocked = true;
        }

        // Hitung ukuran saat ini
        float t = Mathf.Clamp01(fillElapsed / fillDuration);
        float easedT = t * t; // Ease-in (pelan lalu cepat)
        
        float currentDim = Mathf.Lerp(0f, targetSize, easedT);
        
        // Ubah ukuran Decal Projector (X dan Y adalah panjang/lebar kotak sorotan, Z adalah kedalaman sorotan)
        projector.size = new Vector3(currentDim, currentDim, projector.size.z);

        if (fillElapsed >= fillDuration)
        {
            isFilling = false;
            projector.size = new Vector3(targetSize, targetSize, projector.size.z);
        }
    }
}
