using System.Collections.Generic;
using UnityEngine;

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// GrassInteractivePhysics: Engine Fisika Rumput AAA (Pola Angin Hembusan Bertahap & Injak Rebah).
    /// - Menggunakan Pola Angin Dinamis Game AAA (Angin tenang -> Hembusan ombak besar periodik -> Mereda).
    /// - 1 Arah Teratur (Unidirectional Rolling Waves).
    /// - Ringan 60 FPS (Proximity Simulation).
    /// </summary>
    [ExecuteAlways]
    public class GrassInteractivePhysics : MonoBehaviour
    {
        [Header("Target Player")]
        [Tooltip("Transform Player yang menginjak rumput.")]
        public Transform playerTransform;

        [Header("AAA Dynamic Wind Pattern (Pola Hembusan Angin Dinamis)")]
        public bool enableWind = true;
        [Tooltip("Arah tiupan angin (sudut kompas dalam derajat, misal 45 = Timur Laut).")]
        [Range(0f, 360f)]
        public float windCompassAngle = 45f;

        [Tooltip("Kecepatan rambatan gelombang angin melintasi padang rumput.")]
        [Range(0.5f, 4.0f)]
        public float windTravelSpeed = 1.8f;

        [Tooltip("Panjang gelombang ombak angin (kerapatan ombak).")]
        [Range(0.05f, 0.5f)]
        public float windWaveLength = 0.15f;

        [Tooltip("Goyangan sepoi-sepoi saat kondisi angin tenang (derajat).")]
        [Range(1f, 8f)]
        public float calmBreezeAngle = 3.5f;

        [Tooltip("Kekuatan hembusan ombak angin besar periodik (derajat).")]
        [Range(5f, 25f)]
        public float gustSurgeAngle = 14f;

        [Tooltip("Frekuensi kemunculan hembusan ombak angin besar (siklus periodik).")]
        [Range(0.1f, 1.0f)]
        public float gustInterval = 0.35f;

        [Header("Grass Dimensions (Dimensi Rumput)")]
        [Range(0.8f, 2.0f)]
        public float heightMultiplier = 1.35f;
        [Range(0.8f, 1.8f)]
        public float widthMultiplier = 1.1f;

        [Header("Player Trample (Rebah Diinjak Karakter)")]
        [Range(1.0f, 3.0f)]
        public float trampleRadius = 1.8f;
        [Range(30f, 80f)]
        public float maxBendAngle = 65f;
        [Range(0.15f, 0.5f)]
        public float squashedHeightRatio = 0.25f;
        [Range(3f, 15f)]
        public float recoverySpeed = 7f;

        [Header("Performance (Anti-Lag 60 FPS)")]
        [Range(15f, 50f)]
        public float activeDistance = 28f;

        private class GrassNode
        {
            public Transform transform;
            public Quaternion baseRotation;
            public Vector3 initialScale;
            public Vector3 worldPosition;
            public float currentBend;
            public Vector3 currentPushDir;
            public float currentSquash;
        }

        private List<GrassNode> nodes = new List<GrassNode>();
        private Vector3 lastPlayerPos;
        private Vector3 playerVelocity;
        private int lastChildCount = -1;

        private void OnEnable()
        {
            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        [ContextMenu("Scan Grass")]
        public void Initialize()
        {
            FindPlayer();
            ScanGrassNodes();
            if (playerTransform != null) lastPlayerPos = playerTransform.position;
        }

        public void ScanGrassNodes()
        {
            nodes.Clear();
            Transform[] children = GetComponentsInChildren<Transform>();

            foreach (var t in children)
            {
                if (t == transform) continue;
                if (t.name.Contains("Grass") || t.name.Contains("PT_") || t.GetComponent<MeshFilter>() != null)
                {
                    Vector3 s = t.localScale;
                    float avg = (s.x + s.z) * 0.5f;
                    if (avg <= 0.05f || avg > 3.5f) avg = 1.2f;

                    nodes.Add(new GrassNode
                    {
                        transform = t,
                        baseRotation = t.localRotation,
                        initialScale = Vector3.one * Mathf.Clamp(avg, 0.8f, 1.6f),
                        worldPosition = t.position,
                        currentBend = 0f,
                        currentPushDir = Vector3.forward,
                        currentSquash = 1f
                    });
                }
            }
            lastChildCount = transform.childCount;
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.Find("PlayerArmature") ?? GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            #if UNITY_EDITOR
            if (transform.childCount != lastChildCount)
            {
                ScanGrassNodes();
            }
            #endif

            if (playerTransform == null) FindPlayer();

            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.down * 9999f;
            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
            dt = Mathf.Clamp(dt, 0.001f, 0.05f);

            if (playerTransform != null)
            {
                playerVelocity = (playerPos - lastPlayerPos) / dt;
                lastPlayerPos = playerPos;
            }

            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            // Vektor arah angin 1 arah
            float rad = windCompassAngle * Mathf.Deg2Rad;
            Vector3 windDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;
            Vector3 windAxis = Vector3.Cross(Vector3.up, windDir);

            // Pola hembusan periodik AAA: Siklus hembusan ombak angin besar
            float gustCycle = Mathf.Sin(time * gustInterval);
            float gustFactor = Mathf.Clamp01((gustCycle - 0.2f) * 1.6f); // 0 saat tenang, naik ke 1 saat hembusan datang

            float activeDistSqr = activeDistance * activeDistance;
            float trampleDistSqr = trampleRadius * trampleRadius;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.transform == null) continue;

                Vector3 grassPos = node.worldPosition;
                float dPlayerSqr = (grassPos.x - playerPos.x) * (grassPos.x - playerPos.x) + (grassPos.z - playerPos.z) * (grassPos.z - playerPos.z);

                if (dPlayerSqr > activeDistSqr)
                {
                    continue; // Hemat CPU 60 FPS
                }

                float targetBend = 0f;
                Vector3 targetPushDir = node.currentPushDir;
                float targetSquash = 1f;

                // 1. REBAH INJAKAN KAKI PLAYER
                if (dPlayerSqr < trampleDistSqr && Mathf.Abs(grassPos.y - playerPos.y) < 2f)
                {
                    float dist = Mathf.Sqrt(dPlayerSqr);
                    float factor = 1f - (dist / trampleRadius);
                    factor = factor * factor;

                    targetBend = factor * maxBendAngle;

                    Vector3 diff = grassPos - playerPos;
                    diff.y = 0;
                    Vector3 push = dist > 0.01f ? diff.normalized : windDir;

                    if (playerVelocity.sqrMagnitude > 0.4f)
                    {
                        push = Vector3.Lerp(push, playerVelocity.normalized, 0.35f).normalized;
                    }

                    targetPushDir = push;
                    targetSquash = Mathf.Lerp(1f, squashedHeightRatio, factor);
                }

                node.currentBend = Mathf.Lerp(node.currentBend, targetBend, dt * recoverySpeed);
                node.currentPushDir = Vector3.Slerp(node.currentPushDir, targetPushDir, dt * 8f);
                node.currentSquash = Mathf.Lerp(node.currentSquash, targetSquash, dt * recoverySpeed);

                // 2. POLA ANGIN AAA (Sepoi-sepoi halus + Ombak Hembusan Periodik Mengalir)
                Quaternion windRot = Quaternion.identity;
                if (enableWind)
                {
                    float proj = (grassPos.x * windDir.x + grassPos.z * windDir.z) * windWaveLength;
                    
                    // Angin tenang bernapas
                    float calmWave = Mathf.Sin(time * 1.2f + proj) * calmBreezeAngle;
                    
                    // Ombak hembusan besar yang merayap di padang rumput
                    float gustTravel = Mathf.Sin(time * windTravelSpeed + proj);
                    float gustWave = Mathf.Clamp01(gustTravel) * (gustSurgeAngle * gustFactor);

                    float totalSway = calmWave + gustWave;
                    windRot = Quaternion.AngleAxis(totalSway, windAxis);
                }

                // 3. GABUNGKAN ROTASI & SKALA
                Quaternion trampleRot = Quaternion.identity;
                if (node.currentBend > 0.2f)
                {
                    Vector3 bendAxis = Vector3.Cross(Vector3.up, node.currentPushDir);
                    trampleRot = Quaternion.AngleAxis(node.currentBend, bendAxis);
                }

                node.transform.localRotation = trampleRot * windRot * node.baseRotation;

                node.transform.localScale = new Vector3(
                    node.initialScale.x * widthMultiplier * (1f + (1f - node.currentSquash) * 0.3f),
                    node.initialScale.y * heightMultiplier * node.currentSquash,
                    node.initialScale.z * widthMultiplier * (1f + (1f - node.currentSquash) * 0.3f)
                );
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.4f);
                Gizmos.DrawWireSphere(playerTransform.position, trampleRadius);
                Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.15f);
                Gizmos.DrawWireSphere(playerTransform.position, activeDistance);
            }
        }
    }
}
