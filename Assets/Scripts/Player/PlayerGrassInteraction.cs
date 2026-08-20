using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// PlayerGrassInteraction mengirimkan posisi karakter player secara realtime ke Interactive Grass Shader.
    /// Memungkinkan seluruh rumput di Terrain merunduk/membuka jalan saat dilewati karakter.
    /// </summary>
    [ExecuteAlways]
    public class PlayerGrassInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Jari-jari area rumput yang merunduk di sekitar player.")]
        [Range(0.5f, 4.0f)]
        public float bendRadius = 1.3f;

        [Tooltip("Offset posisi kaki karakter (biasanya sedikit di atas tanah).")]
        public Vector3 positionOffset = new Vector3(0f, 0.2f, 0f);

        [Header("Debug Visualization")]
        [Tooltip("Tampilkan lingkaran radius interaksi di Scene view.")]
        public bool showGizmos = true;
        public Color gizmoColor = new Color(0.2f, 1f, 0.3f, 0.4f);

        private static readonly int PlayerPositionShaderID = Shader.PropertyToID("_PlayerPosition");

        private void Update()
        {
            UpdatePlayerPosition();
        }

        private void OnEnable()
        {
            UpdatePlayerPosition();
        }

        private void OnDisable()
        {
            // Reset posisi saat dinonaktifkan agar rumput kembali tegak
            Shader.SetGlobalVector(PlayerPositionShaderID, new Vector4(0f, -9999f, 0f, 0f));
        }

        private void UpdatePlayerPosition()
        {
            Vector3 worldPos = transform.position + positionOffset;
            Shader.SetGlobalVector(PlayerPositionShaderID, new Vector4(worldPos.x, worldPos.y, worldPos.z, bendRadius));
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Gizmos.color = gizmoColor;
            Vector3 center = transform.position + positionOffset;
            Gizmos.DrawWireSphere(center, bendRadius);
        }
    }
}
