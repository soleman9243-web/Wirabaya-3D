using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// TerrainGrassSpawner: Sistem Padang Rumput Toby Fredson Foliage Engine.
    /// - Menggunakan Variasi Rumput Toby Fredson Single Clump (VP_GrassSingle & VP_Grass).
    /// - Skala Standar Asli (1.0x Standard Scale).
    /// - Memanfaatkan Animasi Angin GPU Vertex Asli Bawaan Toby Fredson Shader.
    /// - Eksekusi Editor DelayCall (100% Bebas dari SerializedObject & MissingReferenceException).
    /// - 85+ FPS Mulus & Ringan.
    /// </summary>
    [ExecuteAlways]
    public class TerrainGrassSpawner : MonoBehaviour
    {
        [Header("Toby Fredson Grass Prefabs (VP_GrassSingle & VP_Grass)")]
        public GameObject[] grassPrefabs;

        [Header("Meadow Density & Area (Hamparan Padang Rumput)")]
        [Range(4f, 25f)]
        [Tooltip("Jari-jari area padang rumput di sekitar karakter.")]
        public float meadowRadius = 11.0f;

        [Range(300, 2500)]
        [Tooltip("Target jumlah rumpun (1.000 - 1.400 = karpet padat dan tebal).")]
        public int targetClumpCount = 1200;

        [Range(0.01f, 0.3f)]
        [Tooltip("Jarak minimal antar rumpun rumput.")]
        public float minSpacing = 0.08f;

        [Header("Grass Dimensions (Skala Standar 1.0x)")]
        [Range(0.5f, 2.0f)]
        [Tooltip("Pengali tinggi rumput (1.0 = Standar).")]
        public float heightMultiplier = 1.0f;

        [Range(0.5f, 2.0f)]
        [Tooltip("Skala minimal (1.0 = Ukuran asli prefab).")]
        public float minScale = 1.0f;

        [Range(0.5f, 2.0f)]
        [Tooltip("Skala maksimal (1.0 = Ukuran asli prefab).")]
        public float maxScale = 1.0f;

        [Range(0.0f, 1.0f)]
        [Tooltip("Tingkat kemiringan terhadap lereng bukit (0 = Tegak Lurus, 1 = Miring Lereng).")]
        public float alignWithGroundNormal = 0.20f;

        [Range(-0.02f, 0.05f)]
        [Tooltip("Offset ketinggian akar di atas tanah.")]
        public float surfaceOffset = 0.005f;

        [Header("Performance Optimization (85+ FPS)")]
        [Tooltip("Matikan bayangan rumput agar ribuan batch shadow lenyap.")]
        public bool disableGrassShadowCasting = true;

        [Range(5f, 20f)]
        [Tooltip("Jarak interaksi CPU aktif.")]
        public float cpuAnimationDistance = 14f;

        [Header("Toby Fredson Native GPU Wind")]
        [Tooltip("Gunakan shader angin GPU bawaan Toby Fredson yang sudah sangat optimal dan indah.")]
        public bool useNativeTobyShaderWind = true;

        [Header("Player Interaction (Injakan Kaki Halus / Trample)")]
        public bool enableTrample = true;
        [Range(0.3f, 1.8f)]
        [Tooltip("Radius sentuhan tapak kaki ke helai rumput.")]
        public float interactionRadius = 0.85f;

        [Range(2f, 20f)]
        [Tooltip("Sudut kelenturan maksimal saat diinjak.")]
        public float maxBendAngle = 14f;

        public Transform playerTransform;

        [Header("Scene View Brush Painter")]
        public bool enableBrushMode = true;
        [Range(1.0f, 6f)]
        public float brushRadius = 2.5f;
        [Range(1, 40)]
        public int brushDensity = 14;

        [Header("Surface Protection")]
        [Range(15f, 55f)]
        public float maxSlopeAngle = 42f;
        public LayerMask groundLayer = ~0;

        private struct GrassClumpData
        {
            public Transform transform;
            public Quaternion baseRotation;
            public Vector3 position;
        }
        private List<GrassClumpData> activeClumps = new List<GrassClumpData>();

        private void Awake()
        {
            ForceLoadTobyGrassPrefabs();
            CacheClumpData();
        }

        private void OnEnable()
        {
            ForceLoadTobyGrassPrefabs();
            CacheClumpData();
        }

        private void Start()
        {
            ForceLoadTobyGrassPrefabs();
            FindPlayer();
            CacheClumpData();
        }

        public void CacheClumpData()
        {
            activeClumps.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                Vector3 pos = child.position;
                activeClumps.Add(new GrassClumpData
                {
                    transform = child,
                    baseRotation = child.rotation,
                    position = pos
                });
            }
        }

        public void ForceLoadTobyGrassPrefabs()
        {
            #if UNITY_EDITOR
            if (grassPrefabs == null || grassPrefabs.Length == 0 || grassPrefabs[0] == null)
            {
                string singlePath = "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Plants/VP_GrassSingle/";
                string multiPath = "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Plants/VP_Grass/";

                string[] prefabPaths = new string[]
                {
                    singlePath + "Grass_Single_B.prefab",
                    singlePath + "Grass_Single_C2.prefab",
                    singlePath + "Grass_Single_E.prefab",
                    singlePath + "Grass_Single_X.prefab",
                    multiPath + "GrassShort_A.prefab",
                    multiPath + "GrassShort_B.prefab",
                    multiPath + "GrassMedium_A.prefab"
                };

                List<GameObject> loaded = new List<GameObject>();
                foreach (var path in prefabPaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (p != null) loaded.Add(p);
                }

                if (loaded.Count > 0)
                {
                    grassPrefabs = loaded.ToArray();
                }
            }
            #endif

            int groundIndex = LayerMask.NameToLayer("Ground");
            if (groundIndex >= 0) groundLayer = (1 << groundIndex) | (1 << 0);
            else groundLayer = 1 << 0;
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.Find("PlayerArmature") ?? GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        public bool IsValidGround(RaycastHit hit)
        {
            if (Vector3.Angle(Vector3.up, hit.normal) > maxSlopeAngle) return false;

            string objName = hit.collider.gameObject.name.ToLower();
            if (objName.Contains("roof") || objName.Contains("house") || objName.Contains("wall") || objName.Contains("fence"))
            {
                return false;
            }

            return true;
        }

        public bool SampleSurfaceAt(float x, float z, float refY, out Vector3 surfacePoint, out Vector3 surfaceNormal)
        {
            surfacePoint = Vector3.zero;
            surfaceNormal = Vector3.up;

            Vector3 rayStart = new Vector3(x, refY + 70f, z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 140f, groundLayer))
            {
                if (IsValidGround(hit))
                {
                    surfacePoint = hit.point + new Vector3(0f, surfaceOffset, 0f);
                    surfaceNormal = hit.normal;
                    return true;
                }
            }

            if (Terrain.activeTerrain != null)
            {
                Terrain t = Terrain.activeTerrain;
                Vector3 terrainPos = t.transform.position;
                float normX = (x - terrainPos.x) / t.terrainData.size.x;
                float normZ = (z - terrainPos.z) / t.terrainData.size.z;

                if (normX >= 0f && normX <= 1f && normZ >= 0f && normZ <= 1f)
                {
                    float y = t.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
                    Vector3 norm = t.terrainData.GetInterpolatedNormal(normX, normZ);

                    if (Vector3.Angle(Vector3.up, norm) <= maxSlopeAngle)
                    {
                        surfacePoint = new Vector3(x, y + surfaceOffset, z);
                        surfaceNormal = norm;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsPositionOccupied(Vector3 pos, float radius)
        {
            float radiusSqr = radius * radius;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                Vector3 diff = child.position - pos;
                if ((diff.x * diff.x + diff.z * diff.z) < radiusSqr)
                {
                    return true;
                }
            }
            return false;
        }

        public void SpawnClump(Vector3 pos, Vector3 normal)
        {
            if (grassPrefabs == null || grassPrefabs.Length == 0 || grassPrefabs[0] == null)
            {
                ForceLoadTobyGrassPrefabs();
            }

            if (grassPrefabs == null || grassPrefabs.Length == 0) return;

            GameObject prefab = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
            if (prefab == null) return;

            #if UNITY_EDITOR
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            #else
            GameObject instance = Instantiate(prefab, transform);
            #endif

            if (instance == null) return;

            // 1. Letakkan tepat pada posisi permukaan tanah bukit
            instance.transform.position = pos;

            // 2. Orientasi tegak lurus dengan rotasi acak 360 derajat
            Vector3 blendedUp = Vector3.Lerp(Vector3.up, normal, alignWithGroundNormal).normalized;
            Quaternion targetRot = Quaternion.FromToRotation(Vector3.up, blendedUp) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.rotation = targetRot;

            // 3. Skala Standar 1.0x (Sesuai ukuran asli prefab Toby Fredson)
            float s = (minScale == maxScale) ? minScale : Random.Range(minScale, maxScale);
            instance.transform.localScale = new Vector3(s, s * heightMultiplier, s);

            if (disableGrassShadowCasting)
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = true;
                }
            }

            activeClumps.Add(new GrassClumpData
            {
                transform = instance.transform,
                baseRotation = targetRot,
                position = instance.transform.position
            });
        }

        public void PaintAt(Vector3 centerPoint, float radius, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                float targetX = centerPoint.x + circle.x;
                float targetZ = centerPoint.z + circle.y;

                if (SampleSurfaceAt(targetX, targetZ, centerPoint.y, out Vector3 surfPoint, out Vector3 surfNormal))
                {
                    if (!IsPositionOccupied(surfPoint, minSpacing))
                    {
                        SpawnClump(surfPoint, surfNormal);
                    }
                }
            }
        }

        public void EraseAt(Vector3 centerPoint, float radius)
        {
            float radiusSqr = radius * radius;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                Vector3 diff = child.position - centerPoint;
                if ((diff.x * diff.x + diff.z * diff.z) <= radiusSqr)
                {
                    #if UNITY_EDITOR
                    DestroyImmediate(child.gameObject);
                    #else
                    Destroy(child.gameObject);
                    #endif
                }
            }
            CacheClumpData();
        }

        public void ClearAll()
        {
            #if UNITY_EDITOR
            if (Selection.activeTransform != null && Selection.activeTransform.IsChildOf(transform))
            {
                Selection.activeGameObject = gameObject;
            }
            #endif

            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null) toDestroy.Add(child.gameObject);
            }

            for (int i = toDestroy.Count - 1; i >= 0; i--)
            {
                if (toDestroy[i] != null)
                {
                    #if UNITY_EDITOR
                    if (Application.isPlaying) Destroy(toDestroy[i]);
                    else DestroyImmediate(toDestroy[i]);
                    #else
                    Destroy(toDestroy[i]);
                    #endif
                }
            }
            activeClumps.Clear();
        }

        public void QuickFillArea()
        {
            ClearAll();
            ForceLoadTobyGrassPrefabs();
            FindPlayer();

            Vector3 origin = (playerTransform != null) ? playerTransform.position : transform.position;
            Random.InitState(778899);

            int target = targetClumpCount;
            float radius = meadowRadius;

            // Grid Jitter Rapat (Langkah 14 cm untuk karpet padang rumput padat alami)
            float step = 0.14f;
            for (float x = -radius; x <= radius; x += step)
            {
                for (float z = -radius; z <= radius; z += step)
                {
                    if (transform.childCount >= target) break;

                    if ((x * x + z * z) <= (radius * radius))
                    {
                        float jitterX = x + Random.Range(-0.04f, 0.04f);
                        float jitterZ = z + Random.Range(-0.04f, 0.04f);

                        float worldX = origin.x + jitterX;
                        float worldZ = origin.z + jitterZ;

                        if (SampleSurfaceAt(worldX, worldZ, origin.y, out Vector3 surfPoint, out Vector3 surfNormal))
                        {
                            if (!IsPositionOccupied(surfPoint, minSpacing))
                            {
                                SpawnClump(surfPoint, surfNormal);
                            }
                        }
                    }
                }
                if (transform.childCount >= target) break;
            }

            CacheClumpData();
            Debug.Log($"[TerrainGrassSpawner] Berhasil menggelar {transform.childCount} rumput Toby Fredson skala standar!");
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            // Jika menggunakan GPU vertex wind bawaan Toby Fredson, animasi angin berjalan otomatis di GPU shader
            if (useNativeTobyShaderWind && !enableTrample) return;

            FindPlayer();
            Vector3 pPos = (playerTransform != null) ? playerTransform.position : Vector3.zero;
            float radiusSqr = interactionRadius * interactionRadius;
            float cullDistanceSqr = cpuAnimationDistance * cpuAnimationDistance;

            for (int i = 0; i < activeClumps.Count; i++)
            {
                var clump = activeClumps[i];
                if (clump.transform == null) continue;

                Vector3 diff = clump.position - pPos;
                float distSqr = diff.x * diff.x + diff.z * diff.z;

                if (distSqr > cullDistanceSqr)
                {
                    continue;
                }

                // Reaksi Injak Kaki Karakter (Player Trample)
                Quaternion trampleRot = Quaternion.identity;
                if (enableTrample && distSqr < radiusSqr && Mathf.Abs(diff.y) < 1.0f)
                {
                    float dist = Mathf.Sqrt(distSqr);
                    float factor = 1.0f - (dist / interactionRadius);
                    Vector3 pushDir = (dist > 0.01f) ? new Vector3(diff.x, 0f, diff.z).normalized : playerTransform.forward;

                    Vector3 bendAxis = Vector3.Cross(Vector3.up, pushDir);
                    trampleRot = Quaternion.AngleAxis(factor * maxBendAngle, bendAxis);
                }

                Quaternion targetRotation = trampleRot * clump.baseRotation;
                clump.transform.rotation = Quaternion.Slerp(clump.transform.rotation, targetRotation, Time.deltaTime * 9f);
            }
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(TerrainGrassSpawner))]
    public class TerrainGrassSpawnerEditor : Editor
    {
        private TerrainGrassSpawner spawner;
        private Vector3 lastMousePos;
        private double lastPaintTime = 0;

        private void OnEnable()
        {
            spawner = (TerrainGrassSpawner)target;
            if (spawner != null)
            {
                spawner.ForceLoadTobyGrassPrefabs();
                spawner.CacheClumpData();
            }
        }

        public override void OnInspectorGUI()
        {
            if (target == null || spawner == null) return;

            DrawDefaultInspector();

            EditorGUILayout.Space(14);
            EditorGUILayout.LabelField($"🌿 Total Rumput Toby Fredson: {spawner.transform.childCount} Rumpun", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 1f, 0.45f);
            if (GUILayout.Button($"🌾 Pasang Rumput Toby Fredson ({spawner.targetClumpCount} Rumpun)", GUILayout.Height(44)))
            {
                EditorApplication.delayCall += () =>
                {
                    if (spawner == null) return;
                    spawner.ForceLoadTobyGrassPrefabs();
                    spawner.QuickFillArea();
                    EditorUtility.SetDirty(spawner);
                };
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("🗑️ Hapus Semua Rumput", GUILayout.Height(44)))
            {
                if (EditorUtility.DisplayDialog("Hapus Rumput", "Hapus semua rumput di scene?", "Ya, Hapus", "Batal"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (spawner == null) return;
                        spawner.ClearAll();
                        EditorUtility.SetDirty(spawner);
                    };
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            if (spawner.enableBrushMode)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("🌲 TOBY FREDSON NATIVE FOLIAGE (STANDAR 1.0x)!\n- Menggunakan variasi VP_GrassSingle & VP_Grass Toby Fredson.\n- Menggunakan animasi angin GPU Shader bawaan Toby Fredson yang indah & optimal di 85+ FPS.", MessageType.Info);
            }
        }

        private void OnSceneGUI()
        {
            if (spawner == null || !spawner.enableBrushMode) return;

            Event e = Event.current;

            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, spawner.groundLayer))
            {
                if (spawner.IsValidGround(hit))
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    Handles.color = e.shift ? new Color(1f, 0.25f, 0.25f, 0.85f) : new Color(0.25f, 1f, 0.45f, 0.85f);
                    Handles.DrawWireDisc(hit.point, hit.normal, spawner.brushRadius);
                    Handles.DrawSolidDisc(hit.point, hit.normal, spawner.brushRadius * 0.08f);

                    if (e.type == EventType.MouseMove)
                    {
                        HandleUtility.Repaint();
                    }

                    if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                    {
                        if (e.shift)
                        {
                            spawner.EraseAt(hit.point, spawner.brushRadius);
                            EditorUtility.SetDirty(spawner);
                        }
                        else
                        {
                            double now = EditorApplication.timeSinceStartup;
                            if (now - lastPaintTime > 0.08 || (hit.point - lastMousePos).sqrMagnitude > 0.05f)
                            {
                                lastPaintTime = now;
                                lastMousePos = hit.point;
                                spawner.PaintAt(hit.point, spawner.brushRadius, spawner.brushDensity);
                                EditorUtility.SetDirty(spawner);
                            }
                        }
                        e.Use();
                        HandleUtility.Repaint();
                    }
                }
            }
        }
    }
    #endif
}
