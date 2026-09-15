using UnityEngine;

/// <summary>
/// Trigger zone untuk menampilkan animasi Region Discovery saat player masuk area.
/// Attach ke GameObject dengan BoxCollider (isTrigger = true).
/// </summary>
[RequireComponent(typeof(Collider))]
public class RegionTrigger : MonoBehaviour
{
    [Header("Region Data")]
    [Tooltip("Data region yang akan ditampilkan saat player masuk")]
    [SerializeField] private RegionData regionData;

    [Header("Settings")]
    [Tooltip("Tag player untuk deteksi trigger (default: Player)")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Apakah trigger ini hanya bisa aktif sekali?")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("(Opsional) Reference langsung ke RegionDiscoveryUI. Kalau kosong, pakai singleton.")]
    [SerializeField] private RegionDiscoveryUI discoveryUI;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;
        if (regionData == null) return;

        // Cari UI
        RegionDiscoveryUI ui = discoveryUI != null ? discoveryUI : RegionDiscoveryUI.Instance;

        if (ui == null)
        {
            Debug.LogWarning($"[RegionTrigger] RegionDiscoveryUI not found! Assign reference or pastikan ada instance di scene.", this);
            return;
        }

        ui.Show(regionData);
        hasTriggered = true;
    }

    /// <summary>
    /// Reset trigger supaya bisa aktif lagi (berguna untuk testing).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualisasi trigger zone di editor
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Collider col = GetComponent<Collider>();

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
        }

        // Label nama region
        if (regionData != null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Region: {regionData.latinName}\n{regionData.aksaraJawaName}",
                new GUIStyle
                {
                    normal = { textColor = Color.cyan },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                }
            );
#endif
        }
    }
}
