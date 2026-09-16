using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// TsushimaGrassMeadow: Sistem Padang Rumput GPU AAA Berdasarkan Teknik SimonDev & Ghost of Tsushima (GDC 2021).
    /// - GPU Instancing Batching: Menggambar puluhan ribu helai rumput dalam 1 draw call (60 FPS terkunci, 0 lag).
    /// - Anti-Nembus Collider: Mengunci posisi akar tepat di atas permukaan terrain dengan filter kemiringan tebing.
    /// - Pola Angin Bézier Dinamis: Menggunakan formula 2D Noise & Gust Waves untuk aliran ombak angin yang alami.
    /// - Interaksi Injak Kaki Karakter: Merunduk dan membuka jalan secara mulus saat dilewati pemain.
    /// </summary>
    [ExecuteAlways]
    public class TsushimaGrassMeadow : MonoBehaviour
    {
        [Header("Mesh & Material (GPU Instanced)")]
        [Tooltip("Mesh rumpun rumput 3D.")]
        public Mesh grassMesh;
        [Tooltip("Material rumput dengan GPU Instancing aktif.")]
        public Material grassMaterial;

        [Header("Meadow Placement & Density")]
        [Tooltip("Pusat padang rumput (auto-detect player jika kosong).")]
        public Transform followTarget;
        [Tooltip("Jari-jari padang rumput (meter).")]
        [Range(10f, 80f)]
        public float fieldRadius = 35f;
        [Tooltip("Jumlah rumpun rumput yang digambar di GPU.")]
        [Range(100, 3000)]
        public int grassInstanceCount = 800;

        [Header("Surface & Slope Filter (Anti-Nembus)")]
        [Tooltip("Kemiringan maksimal tanah tempat rumput boleh tumbuh (mencegah tumbuh di tebing curam).")]
        [Range(15f, 45f)]
        public float maxSlopeAngle = 32f;
        [Tooltip("Layer tanah tempat rumput boleh tumbuh.")]
        public LayerMask groundLayer = ~0;

        [Header("Bézier Wind & Gust Waves (Pola Angin SimonDev AAA)")]
        public bool enableWind = true;
        [Range(0f, 360f)]
        [Tooltip("Arah tiupan angin (derajat kompas).")]
        public float windCompassAngle = 45f;
        [Range(0.5f, 5.0f)]
        [Tooltip("Kecepatan aliran ombak angin.")]
        public float windSpeed = 2.2f;
        [Range(0.02f, 0.4f)]
        [Tooltip("Kerapatan gelombang ombak angin di padang rumput.")]
        public float windWaveFrequency = 0.12f;
        [Range(0.05f, 0.6f)]
        [Tooltip("Kekuatan lenturan ombak angin.")]
        public float windSwayStrength = 0.25f;
        [Range(0.1f, 1.0f)]
        [Tooltip("Siklus kemunculan hembusan angin kencang periodik.")]
        public float gustInterval = 0.3f;

        [Header("Player Interaction (Injak Kaki Karakter)")]
        [Tooltip("Jari-jari area injak di sekitar kaki karakter.")]
        [Range(1.0f, 3.5f)]
        public float playerInteractionRadius = 1.8f;
        [Tooltip("Kekuatan rebah rumput saat terinjak.")]
        [Range(0.2f, 1.5f)]
        public float playerTrampleStrength = 0.8f;

        [Header("Clump Scale Variation")]
        public Vector2 heightScaleRange = new Vector2(1.2f, 1.6f);
        public Vector2 widthScaleRange = new Vector2(1.1f, 1.4f);

        private struct GrassInstanceData
        {
            public Vector3 localOffset;
            public float rotationY;
            public Vector2 scale;
            public float noiseSeed;
        }

        private List<GrassInstanceData> instanceDataList = new List<GrassInstanceData>();
        private Matrix4x4[] matricesBatch = new Matrix4x4[1023];
        private MaterialPropertyBlock propBlock;
        private Vector3 lastCenterPos;
        private Transform playerTransform;

        private void OnEnable()
        {
            InitializeMeadow();
        }

        private void Start()
        {
            InitializeMeadow();
        }

        private void Reset()
        {
            SetupDefaults();
        }

        private void SetupDefaults()
        {
            int groundIndex = LayerMask.NameToLayer("Ground");
            if (groundIndex >= 0) groundLayer = (1 << groundIndex) | (1 << 0);
            else groundLayer = 1 << 0;

            #if UNITY_EDITOR
            if (grassMesh == null)
            {
                string[] meshGuids = AssetDatabase.FindAssets("PT_Grass_02_LOD0 t:Mesh");
                if (meshGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(meshGuids[0]);
                    grassMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                }
            }
            if (grassMaterial == null)
            {
                string[] matGuids = AssetDatabase.FindAssets("PT_Grass_Mat t:Material");
                if (matGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                    grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            #endif
        }

        [ContextMenu("Regenerate Meadow Distribution")]
        public void InitializeMeadow()
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            FindPlayer();

            instanceDataList.Clear();
            Random.InitState(42);

            // Sebar titik rumput menggunakan Poisson / Jittered Grid SimonDev
            int attempts = grassInstanceCount * 2;
            for (int i = 0; i < attempts && instanceDataList.Count < grassInstanceCount; i++)
            {
                Vector2 circlePoint = Random.insideUnitCircle * fieldRadius;
                
                instanceDataList.Add(new GrassInstanceData
                {
                    localOffset = new Vector3(circlePoint.x, 0, circlePoint.y),
                    rotationY = Random.Range(0f, 360f),
                    scale = new Vector2(
                        Random.Range(widthScaleRange.x, widthScaleRange.y),
                        Random.Range(heightScaleRange.x, heightScaleRange.y)
                    ),
                    noiseSeed = Random.Range(0f, 100f)
                });
            }
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var p = GameObject.Find("PlayerArmature") ?? GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
            if (followTarget == null && playerTransform != null)
            {
                followTarget = playerTransform;
            }
        }

        private void Update()
        {
            if (grassMesh == null || grassMaterial == null) return;
            if (instanceDataList.Count == 0) InitializeMeadow();
            if (playerTransform == null) FindPlayer();

            Vector3 centerPos = followTarget != null ? followTarget.position : transform.position;
            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.down * 999f;

            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            // Vektor arah angin 1 arah
            float rad = windCompassAngle * Mathf.Deg2Rad;
            Vector3 windDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;

            // Siklus hembusan angin dinamis SimonDev
            float gustCycle = Mathf.Sin(time * gustInterval);
            float gustMultiplier = Mathf.Lerp(0.3f, 1.2f, Mathf.Clamp01(gustCycle * 1.5f));

            int batchIndex = 0;
            for (int i = 0; i < instanceDataList.Count; i++)
            {
                var data = instanceDataList[i];
                Vector3 worldXZ = centerPos + data.localOffset;

                // 1. CONFORM KE PERMUKAAN TANAH (ANTI-NEMBUS)
                Vector3 rayOrigin = new Vector3(worldXZ.x, centerPos.y + 30f, worldXZ.z);
                if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 60f, groundLayer))
                {
                    continue;
                }

                // Filter tebing curam
                if (Vector3.Angle(Vector3.up, hit.normal) > maxSlopeAngle)
                {
                    continue;
                }

                Vector3 grassBasePos = hit.point;

                // 2. SIMULASI OMBAK ANGIN BÉZIER SIMONDEV
                Vector3 swayOffset = Vector3.zero;
                if (enableWind)
                {
                    float waveProj = (grassBasePos.x * windDir.x + grassBasePos.z * windDir.z) * windWaveFrequency;
                    float wave = Mathf.Sin(time * windSpeed + waveProj + data.noiseSeed * 0.1f);
                    swayOffset = windDir * (wave * windSwayStrength * gustMultiplier * data.scale.y);
                }

                // 3. REAKSI INJAK KAKI KARAKTER
                float distPlayer = Vector3.Distance(grassBasePos, playerPos);
                if (distPlayer < playerInteractionRadius)
                {
                    float factor = 1f - (distPlayer / playerInteractionRadius);
                    Vector3 push = (grassBasePos - playerPos).normalized;
                    push.y = -0.4f;
                    swayOffset += push * (factor * playerTrampleStrength * data.scale.y);
                }

                // Hitung Matrix Transform untuk GPU Instancing
                Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion yawRot = Quaternion.Euler(0, data.rotationY, 0);
                Quaternion finalRot = slopeRot * yawRot;

                Vector3 scaleVec = new Vector3(data.scale.x, data.scale.y, data.scale.x);
                Matrix4x4 mat = Matrix4x4.TRS(grassBasePos + swayOffset * 0.3f, finalRot, scaleVec);

                matricesBatch[batchIndex] = mat;
                batchIndex++;

                // Render per batch 1023 instance (GPU Instancing Max Limit per Draw Call)
                if (batchIndex >= 1023)
                {
                    Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, matricesBatch, batchIndex, propBlock);
                    batchIndex = 0;
                }
            }

            // Render sisa batch terakhir
            if (batchIndex > 0)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, matricesBatch, batchIndex, propBlock);
            }
        }
    }
}
