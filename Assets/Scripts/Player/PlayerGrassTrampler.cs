using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// PlayerGrassTrampler mengirimkan posisi karakter pemain secara realtime ke shader rumput interaktif (InteractiveGrassPro).
    /// Membuat rumput merunduk, rebah, dan membuka jalan saat dilewati karakter.
    /// </summary>
    [ExecuteAlways]
    public class PlayerGrassTrampler : MonoBehaviour
    {
        [Header("Trample Settings")]
        [Tooltip("Jari-jari area rumput yang terinjak & merunduk di sekitar player.")]
        [Range(0.5f, 4.0f)]
        public float interactionRadius = 1.5f;

        [Tooltip("Kekuatan rebah rumput saat terinjak.")]
        [Range(0.5f, 3.0f)]
        public float trampleStrength = 1.4f;

        [Tooltip("Offset posisi kaki karakter dari titik pivot.")]
        public Vector3 footOffset = new Vector3(0f, 0.15f, 0f);

        [Header("Gizmo Visualization")]
        public bool showRadiusGizmo = true;
        public Color gizmoColor = new Color(0.3f, 1f, 0.2f, 0.35f);

        private static readonly int PlayerTramplePosID = Shader.PropertyToID("_PlayerTramplePos");

        private void Update()
        {
            SendPositionToShader();
        }

        private void OnEnable()
        {
            SendPositionToShader();
        }

        private void OnDisable()
        {
            // Reset posisi saat karakter dinonaktifkan agar rumput kembali tegak berdiri
            Shader.SetGlobalVector(PlayerTramplePosID, new Vector4(0f, -9999f, 0f, 0f));
        }

        private void SendPositionToShader()
        {
            Vector3 worldPos = transform.position + footOffset;
            Shader.SetGlobalVector(PlayerTramplePosID, new Vector4(worldPos.x, worldPos.y, worldPos.z, interactionRadius));
        }

        private void OnDrawGizmosSelected()
        {
            if (!showRadiusGizmo) return;

            Gizmos.color = gizmoColor;
            Vector3 center = transform.position + footOffset;
            Gizmos.DrawWireSphere(center, interactionRadius);
        }
    }
}
