using UnityEngine;

/// <summary>
/// Batu yang dilempar boss Babi.
/// 
/// Trajectory (Bezier Curve):
///   1. Muncul di depan boss (rockSpawnPoint)
///   2. Naik ke atas (peak) — seperti dicungkil/ditendang ke atas
///   3. Melengkung turun ke posisi telegraph (target)
///
/// Batu ini mengikuti posisi telegraph secara live (karena telegraph tracking player).
/// Saat telegraph lock posisi, batu otomatis mengarah ke titik final.
///
/// Setup Prefab:
///   RockProjectile (Model batu 3D, script ini, TANPA Rigidbody)
/// </summary>
public class RockProjectile : MonoBehaviour
{
    [Header("Impact VFX (Opsional)")]
    [Tooltip("Prefab efek saat batu mendarat (debu, pecahan batu, dll)")]
    public GameObject impactVfxPrefab;

    [Header("Collision Settings")]
    [Tooltip("Pilih layer yang membuat batu meledak (contoh: Player, Ground/Default)")]
    public LayerMask explodeLayers;

    // ====== TRAJECTORY DATA ======
    private Vector3 startPos;           // Titik awal (depan boss)
    private float peakHeight;           // Tinggi lengkungan arc
    private float duration;
    private float damage;
    private float damageRadius;

    // ====== STATE ======
    private float elapsed;
    private bool hasLanded = false;
    private bool isLaunched = false;

    // ====== TELEGRAPH LINK ======
    private TelegraphIndicator linkedTelegraph; // Untuk ambil posisi target live

    // ==========================================
    // PUBLIC API
    // ==========================================

    /// <summary>
    /// Luncurkan batu dari posisi spawn ke atas, lalu ke posisi telegraph.
    /// </summary>
    /// <param name="start">Posisi spawn (depan boss)</param>
    /// <param name="pHeight">Tinggi puncak di atas posisi spawn</param>
    /// <param name="travelDuration">Durasi total terbang (sinkron dengan telegraph fill)</param>
    /// <param name="dmg">Damage saat mendarat</param>
    /// <param name="radius">Radius damage AoE</param>
    /// <param name="telegraph">Telegraph indicator untuk tracking posisi target</param>
    public void Launch(Vector3 start, float pHeight, float travelDuration, float dmg, float radius, TelegraphIndicator telegraph)
    {
        startPos = start;
        peakHeight = pHeight;
        duration = travelDuration;
        damage = dmg;
        damageRadius = radius;
        linkedTelegraph = telegraph;
        elapsed = 0f;
        hasLanded = false;
        isLaunched = true;

        // Pastikan batu mulai tersembunyi
        ToggleMesh(false);
    }

    // ==========================================
    // UPDATE — FALL FROM SKY
    // ==========================================

    private void Update()
    {
        if (!isLaunched || hasLanded) return;

        // Ambil posisi target LIVE dari telegraph (bisa masih tracking player)
        Vector3 targetPos = linkedTelegraph != null
            ? linkedTelegraph.GetTargetPosition()
            : startPos + Vector3.forward * 10f; // Fallback

        // Jika belum waktunya jatuh (circle masih mengisi)
        if (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Sembunyikan batu (taruh tinggi di atas target)
            // Bisa pakai peakHeight sebagai acuan ketinggian spawn langit
            transform.position = targetPos + Vector3.up * (peakHeight + 10f);
            
            return;
        }

        // --- FASE JATUH ---
        ToggleMesh(true); // Munculkan batu

        // Kecepatan jatuh (bisa disesuaikan, misal 50 unit per detik)
        float fallSpeed = 50f;
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Rotasi batu supaya kelihatan natural saat jatuh
        transform.Rotate(Vector3.right, 720f * Time.deltaTime, Space.Self);
        
        // CATATAN: Pengecekan pendaratan manual berdasarkan ketinggian target
        if (transform.position.y <= targetPos.y)
        {
            OnImpact(targetPos);
        }
    }

    private void ToggleMesh(bool show)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = show;
        }
    }

    // ==========================================
    // IMPACT
    // ==========================================

    private void OnImpact(Vector3 impactPos)
    {
        hasLanded = true;

        // Cek damage ke player di dalam radius telegraph
        Collider[] hits = Physics.OverlapSphere(impactPos, damageRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerStatus ps = hit.GetComponentInParent<PlayerStatus>();
                if (ps != null)
                {
                    ps.TakeDamage(damage);
                }
                break; // 1x hit saja
            }
        }

        // Spawn impact VFX (Efek ledakan)
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, impactPos, Quaternion.identity);
        }

        // Hancurkan batu SEKEKITA setelah mendarat
        Destroy(gameObject);
    }

    // ==========================================
    // DEBUG GIZMOS
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        if (linkedTelegraph != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(linkedTelegraph.GetTargetPosition(), damageRadius);
        }
    }
}
