using UnityEngine;
using System.Collections.Generic;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// ProximityColliderActivator: Mengaktifkan collider objek di scene saat player mendekat.
    /// - Cari semua objek dengan tag atau layer tertentu, atau semua Collider yang disabled.
    /// - Aktifkan collider saat player dalam radius, matikan saat menjauh.
    /// - Ringan: pengecekan berkala dengan interval yang bisa diatur.
    /// </summary>
    public class ProximityColliderActivator : MonoBehaviour
    {
        [Header("Player Reference")]
        [Tooltip("Transform karakter pemain. Otomatis dicari jika kosong.")]
        public Transform playerTransform;

        [Header("Proximity Settings")]
        [Range(5f, 80f)]
        [Tooltip("Radius aktivasi collider dari posisi player (meter).")]
        public float activationRadius = 25f;

        [Range(0.1f, 2f)]
        [Tooltip("Interval pengecekan (detik).")]
        public float checkInterval = 0.25f;

        [Header("Target Objects")]
        [Tooltip("Tag objek yang collider-nya akan dikontrol. Kosongkan jika ingin pakai mode manual / layer.")]
        public string targetTag = "";

        [Tooltip("Layer objek yang collider-nya akan dikontrol. Pilih 'Nothing' jika ingin pakai mode tag / manual.")]
        public LayerMask targetLayer = 0;

        [Tooltip("Drag & drop objek secara manual ke sini. Jika diisi, tag dan layer diabaikan.")]
        public GameObject[] manualTargets;

        private struct TrackedObject
        {
            public Transform transform;
            public Collider[] colliders;
            public bool[] wasAlreadyEnabled; // true = collider sudah aktif dari awal, JANGAN dikelola
            public Vector3 position;
        }

        private List<TrackedObject> trackedObjects = new List<TrackedObject>();
        private float nextCheckTime;
        private bool initialized = false;

        private void Start()
        {
            FindPlayer();
            CollectTargets();
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;

            var player = GameObject.Find("PlayerArmature");
            if (player == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) player = tagged;
            }
            if (player != null) playerTransform = player.transform;
        }

        /// <summary>
        /// Kumpulkan semua objek target berdasarkan prioritas: manual > tag > layer.
        /// </summary>
        public void CollectTargets()
        {
            trackedObjects.Clear();

            List<GameObject> targets = new List<GameObject>();

            // Prioritas 1: Manual targets
            if (manualTargets != null && manualTargets.Length > 0)
            {
                foreach (var obj in manualTargets)
                {
                    if (obj != null) targets.Add(obj);
                }
            }
            // Prioritas 2: Tag
            else if (!string.IsNullOrEmpty(targetTag))
            {
                var found = GameObject.FindGameObjectsWithTag(targetTag);
                targets.AddRange(found);
            }
            // Prioritas 3: Layer (cari semua Collider di scene lalu filter by layer)
            else if (targetLayer.value != 0)
            {
                Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
                HashSet<GameObject> uniqueObjects = new HashSet<GameObject>();

                foreach (var col in allColliders)
                {
                    if (((1 << col.gameObject.layer) & targetLayer.value) != 0)
                    {
                        uniqueObjects.Add(col.gameObject);
                    }
                }

                targets.AddRange(uniqueObjects);
            }

            // Daftarkan semua target beserta collider-nya
            foreach (var obj in targets)
            {
                Collider[] cols = obj.GetComponentsInChildren<Collider>(true);
                if (cols.Length == 0) continue;

                // Catat status awal setiap collider
                bool[] alreadyEnabled = new bool[cols.Length];
                bool hasAnyManaged = false;

                for (int i = 0; i < cols.Length; i++)
                {
                    alreadyEnabled[i] = cols[i].enabled;

                    // Hanya matikan collider yang memang sudah mati dari awal
                    // Collider yang sudah aktif = JANGAN disentuh
                    if (!cols[i].enabled)
                    {
                        hasAnyManaged = true;
                    }
                }

                // Hanya tracking jika ada minimal 1 collider yang perlu dikelola
                if (!hasAnyManaged) continue;

                trackedObjects.Add(new TrackedObject
                {
                    transform = obj.transform,
                    colliders = cols,
                    wasAlreadyEnabled = alreadyEnabled,
                    position = obj.transform.position
                });
            }

            initialized = true;
            Debug.Log($"[ProximityColliderActivator] Terdaftar {trackedObjects.Count} objek untuk proximity collider.");
        }

        private void Update()
        {
            if (!initialized || playerTransform == null)
            {
                FindPlayer();
                return;
            }

            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            UpdateColliders();
        }

        private void UpdateColliders()
        {
            Vector3 playerPos = playerTransform.position;
            float radiusSqr = activationRadius * activationRadius;

            for (int i = 0; i < trackedObjects.Count; i++)
            {
                var tracked = trackedObjects[i];
                if (tracked.transform == null) continue;

                // Gunakan posisi cached, update jika objek bisa bergerak
                Vector3 objPos = tracked.transform.position;
                float dx = objPos.x - playerPos.x;
                float dz = objPos.z - playerPos.z;
                float distSqr = dx * dx + dz * dz;

                bool shouldBeActive = distSqr <= radiusSqr;

                for (int j = 0; j < tracked.colliders.Length; j++)
                {
                    // Skip collider yang sudah aktif dari awal — jangan disentuh
                    if (tracked.wasAlreadyEnabled[j]) continue;

                    if (tracked.colliders[j] != null && tracked.colliders[j].enabled != shouldBeActive)
                    {
                        tracked.colliders[j].enabled = shouldBeActive;
                    }
                }
            }
        }

        private void OnDisable()
        {
            // Kembalikan state: matikan hanya collider yang awalnya memang mati
            for (int i = 0; i < trackedObjects.Count; i++)
            {
                var tracked = trackedObjects[i];
                if (tracked.colliders == null) continue;

                for (int j = 0; j < tracked.colliders.Length; j++)
                {
                    // Jangan matikan collider yang dari awal sudah aktif
                    if (tracked.wasAlreadyEnabled[j]) continue;

                    if (tracked.colliders[j] != null)
                    {
                        tracked.colliders[j].enabled = false;
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform reference = playerTransform;
            if (reference == null && Application.isPlaying) return;
            if (reference == null) reference = Camera.main != null ? Camera.main.transform : null;
            if (reference == null) return;

            // Radius aktivasi
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.12f);
            Gizmos.DrawSphere(reference.position, activationRadius);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(reference.position, activationRadius);

            // Tandai objek yang sedang aktif
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            for (int i = 0; i < trackedObjects.Count; i++)
            {
                if (trackedObjects[i].transform == null) continue;
                if (trackedObjects[i].colliders.Length > 0 && trackedObjects[i].colliders[0] != null && trackedObjects[i].colliders[0].enabled)
                {
                    Gizmos.DrawWireCube(trackedObjects[i].transform.position, Vector3.one * 0.5f);
                }
            }
        }
    }
}
