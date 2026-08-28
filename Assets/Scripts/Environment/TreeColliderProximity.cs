using UnityEngine;
using System.Collections.Generic;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// TreeColliderProximity: Mengaktifkan collider pohon terrain saat player mendekat.
    /// - Pohon terrain di Unity tidak memiliki collider runtime secara default jika dimatikan.
    /// - Script ini men-spawn CapsuleCollider sementara di posisi pohon terdekat, lalu menghapusnya saat player menjauh.
    /// - Sangat ringan: hanya mengecek pohon dalam radius tertentu setiap interval waktu.
    /// </summary>
    public class TreeColliderProximity : MonoBehaviour
    {
        [Header("Player Reference")]
        [Tooltip("Transform karakter pemain. Otomatis dicari jika kosong.")]
        public Transform playerTransform;

        [Header("Proximity Settings")]
        [Range(5f, 50f)]
        [Tooltip("Radius deteksi pohon dari posisi player (meter).")]
        public float activationRadius = 20f;

        [Range(0.1f, 2f)]
        [Tooltip("Interval pengecekan (detik). Semakin kecil semakin responsif tapi lebih berat.")]
        public float checkInterval = 0.3f;

        [Header("Collider Settings")]
        [Range(0.3f, 3f)]
        [Tooltip("Radius kapsul collider batang pohon.")]
        public float colliderRadius = 0.5f;

        [Range(1f, 15f)]
        [Tooltip("Tinggi kapsul collider batang pohon.")]
        public float colliderHeight = 5f;

        [Range(0f, 5f)]
        [Tooltip("Offset Y collider dari dasar pohon.")]
        public float colliderYOffset = 0f;

        [Header("Performance")]
        [Range(10, 100)]
        [Tooltip("Jumlah maksimal collider aktif secara bersamaan.")]
        public int maxActiveColliders = 40;

        private Terrain terrain;
        private TerrainData terrainData;
        private Vector3 terrainPos;

        // Pool collider aktif: key = index pohon di TreeInstance[], value = GameObject collider
        private Dictionary<int, GameObject> activeColliders = new Dictionary<int, GameObject>();

        // Object pool untuk daur ulang GameObject collider
        private Queue<GameObject> colliderPool = new Queue<GameObject>();

        private float nextCheckTime;

        private void Start()
        {
            terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogWarning("[TreeColliderProximity] Tidak ada Terrain aktif di scene!");
                enabled = false;
                return;
            }

            terrainData = terrain.terrainData;
            terrainPos = terrain.transform.position;

            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.Find("PlayerArmature");
                if (player == null)
                {
                    var tagged = GameObject.FindGameObjectWithTag("Player");
                    if (tagged != null) player = tagged;
                }
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            UpdateProximityColliders();
        }

        private void UpdateProximityColliders()
        {
            Vector3 playerPos = playerTransform.position;
            float radiusSqr = activationRadius * activationRadius;

            TreeInstance[] trees = terrainData.treeInstances;
            Vector3 terrainSize = terrainData.size;

            // 1. Tandai pohon mana saja yang masih dalam radius
            HashSet<int> treesInRange = new HashSet<int>();

            for (int i = 0; i < trees.Length; i++)
            {
                // Konversi posisi normalized pohon ke world space
                Vector3 worldPos = new Vector3(
                    trees[i].position.x * terrainSize.x + terrainPos.x,
                    trees[i].position.y * terrainSize.y + terrainPos.y,
                    trees[i].position.z * terrainSize.z + terrainPos.z
                );

                float dx = worldPos.x - playerPos.x;
                float dz = worldPos.z - playerPos.z;
                float distSqr = dx * dx + dz * dz;

                if (distSqr <= radiusSqr)
                {
                    treesInRange.Add(i);
                }
            }

            // 2. Hapus collider yang sudah di luar radius
            List<int> toRemove = new List<int>();
            foreach (var kvp in activeColliders)
            {
                if (!treesInRange.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (int idx in toRemove)
            {
                ReturnColliderToPool(activeColliders[idx]);
                activeColliders.Remove(idx);
            }

            // 3. Tambahkan collider untuk pohon baru yang masuk radius
            foreach (int idx in treesInRange)
            {
                if (activeColliders.ContainsKey(idx)) continue;
                if (activeColliders.Count >= maxActiveColliders) break;

                Vector3 worldPos = new Vector3(
                    trees[idx].position.x * terrainSize.x + terrainPos.x,
                    trees[idx].position.y * terrainSize.y + terrainPos.y,
                    trees[idx].position.z * terrainSize.z + terrainPos.z
                );

                // Sesuaikan radius collider dengan skala pohon
                float treeScale = Mathf.Max(trees[idx].widthScale, trees[idx].heightScale);
                float scaledRadius = colliderRadius * treeScale;
                float scaledHeight = colliderHeight * trees[idx].heightScale;

                GameObject col = GetColliderFromPool();
                col.transform.position = worldPos + new Vector3(0f, colliderYOffset + (scaledHeight * 0.5f), 0f);

                CapsuleCollider capsule = col.GetComponent<CapsuleCollider>();
                capsule.radius = scaledRadius;
                capsule.height = scaledHeight;

                col.SetActive(true);
                activeColliders[idx] = col;
            }
        }

        private GameObject GetColliderFromPool()
        {
            if (colliderPool.Count > 0)
            {
                return colliderPool.Dequeue();
            }

            // Buat collider baru
            GameObject obj = new GameObject("TreeProximityCollider");
            obj.transform.SetParent(transform);
            obj.layer = gameObject.layer;

            CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();
            capsule.direction = 1; // Y-axis
            capsule.isTrigger = false;

            return obj;
        }

        private void ReturnColliderToPool(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            colliderPool.Enqueue(obj);
        }

        private void OnDisable()
        {
            // Bersihkan semua collider aktif saat script dimatikan
            foreach (var kvp in activeColliders)
            {
                if (kvp.Value != null)
                {
                    ReturnColliderToPool(kvp.Value);
                }
            }
            activeColliders.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;

            // Gambar radius aktivasi di Scene View
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.15f);
            Gizmos.DrawSphere(playerTransform.position, activationRadius);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(playerTransform.position, activationRadius);

            // Gambar collider aktif
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.7f);
            foreach (var kvp in activeColliders)
            {
                if (kvp.Value != null && kvp.Value.activeInHierarchy)
                {
                    CapsuleCollider cap = kvp.Value.GetComponent<CapsuleCollider>();
                    if (cap != null)
                    {
                        Gizmos.DrawWireSphere(kvp.Value.transform.position, cap.radius);
                    }
                }
            }
        }
    }
}
