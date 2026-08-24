using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// TerrainGrassSpawner: Sistem Padang Rumput Ultra Padat Tanpa Jarak (Ultra-Dense Carpet).
    /// - Jarak Super Rapat (Grid Step 11cm & Min Spacing 2cm).
    /// - Rumpun Saling Bertumpuk Padat (2.200 - 4.000 Rumpun).
    /// - Rumput Tinggi Ramping Berdiri Tegak.
    /// - Kuas Scene View Super Lebat.
    /// </summary>
    [ExecuteAlways]
    public class TerrainGrassSpawner : MonoBehaviour
    {
        [Header("Clean Low-Poly Grass Prefabs (PT_Grass)")]
        public GameObject[] grassPrefabs;

        [Header("Meadow Density & Area (Ultra Padat)")]
        [Range(5f, 35f)]
        [Tooltip("Jari-jari area padang rumput di sekitar karakter.")]
        public float meadowRadius = 14f;

        [Range(300, 4500)]
        [Tooltip("Target jumlah rumpun (2.200+ = karpet hijau tebal saling menempel erat).")]
        public int targetClumpCount = 2200;

        [Range(0.01f, 0.25f)]
        [Tooltip("Jarak minimal antar rumpun (0.02m = rumput saling menempel erat tanpa celah tanah).")]
        public float minSpacing = 0.02f;

        [Header("Grass Dimensions (Tinggi & Lebar)")]
        [Range(0.5f, 2.5f)]
        [Tooltip("Pengali tinggi rumput.")]
        public float heightMultiplier = 1.55f;

        [Range(0.4f, 2.0f)]
        public float minScale = 1.0f;

        [Range(0.4f, 2.5f)]
        public float maxScale = 1.5f;

        [Range(0.0f, 1.0f)]
        [Tooltip("Tingkat kemiringan terhadap tanah (0 = Tegak Lurus, 1 = Miring Lereng).")]
        public float alignWithGroundNormal = 0.25f;

        [Range(0.0f, 0.2f)]
        [Tooltip("Kedalaman akar menancap ke tanah.")]
        public float rootSinkDepth = 0.04f;

        [Header("Scene View Brush Painter (Super Lebat)")]
        public bool enableBrushMode = true;
        [Range(1.0f, 10f)]
        public float brushRadius = 2.5f;
        [Range(1, 50)]
        [Tooltip("Kepadatan sekali sapuan kuas.")]
        public int brushDensity = 25;

        [Header("Player Trample Interaction (Reaksi Injak Kaki)")]
        public bool enableTrample = true;
        [Range(0.4f, 2.5f)]
        [Tooltip("Radius sentuhan tapak kaki ke helai rumput.")]
        public float interactionRadius = 1.1f;

        [Range(5f, 30f)]
        [Tooltip("Sudut kemiringan saat diinjak.")]
        public float maxBendAngle = 18f;

        public Transform playerTransform;

        [Header("Surface Protection")]
        [Range(15f, 55f)]
        public float maxSlopeAngle = 42f;
        public LayerMask groundLayer = ~0;

        private struct GrassClumpData
        {
            public Transform transform;
            public Quaternion baseRotation;
        }
        private List<GrassClumpData> activeClumps = new List<GrassClumpData>();

        private void Awake()
        {
            RemoveOldLegacyMeshComponents();
            ForceLoadPTGrassPrefabs();
            DeleteAllDemoTilesInScene();
            CacheClumpData();
        }

        private void OnEnable()
        {
            RemoveOldLegacyMeshComponents();
            ForceLoadPTGrassPrefabs();
            DeleteAllDemoTilesInScene();
            CacheClumpData();
        }

        private void Start()
        {
            RemoveOldLegacyMeshComponents();
            ForceLoadPTGrassPrefabs();
            FindPlayer();
            DeleteAllDemoTilesInScene();
            CacheClumpData();
        }

        public void RemoveOldLegacyMeshComponents()
        {
            #if UNITY_EDITOR
            var mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = null;
                Undo.DestroyObjectImmediate(mf);
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Undo.DestroyObjectImmediate(mr);
            }
            #else
            var mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = null;
                DestroyImmediate(mf);
            }
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                DestroyImmediate(mr);
            }
            #endif
        }

        public static void DeleteAllDemoTilesInScene()
        {
            #if UNITY_EDITOR
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int deletedCount = 0;
            foreach (var go in allObjects)
            {
                if (go != null && (go.name.Contains("ExampleDemoTile") || go.name.Contains("GrassRendererGreen") || go.name == "DemoTile"))
                {
                    Undo.DestroyObjectImmediate(go);
                    deletedCount++;
                }
            }
            if (deletedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            #endif
        }

        public void CacheClumpData()
        {
            activeClumps.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                activeClumps.Add(new GrassClumpData
                {
                    transform = child,
                    baseRotation = child.rotation
                });
            }
        }

        public void ForceLoadPTGrassPrefabs()
        {
            #if UNITY_EDITOR
            string[] guids = new string[]
            {
                "53083e9d6ba91ba4cad309a95b8c8f20", // PT_Grass_02_v1
                "3ade7fcf53c8246418eb3c493d6d2178", // PT_Grass_02_v2
                "aa8ea3d66a87ed449832e8709ec90517"  // PT_High_Grass_02_v1
            };

            List<GameObject> loaded = new List<GameObject>();
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!string.IsNullOrEmpty(path))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) loaded.Add(prefab);
                }
            }

            if (loaded.Count > 0)
            {
                grassPrefabs = loaded.ToArray();
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
                    surfacePoint = hit.point - new Vector3(0f, rootSinkDepth, 0f);
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
                        surfacePoint = new Vector3(x, y - rootSinkDepth, z);
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
                Vector3 diff = transform.GetChild(i).position - pos;
                if ((diff.x * diff.x + diff.z * diff.z) < radiusSqr)
                {
                    return true;
                }
            }
            return false;
        }

        public void SpawnClump(Vector3 pos, Vector3 normal)
        {
            if (grassPrefabs == null || grassPrefabs.Length == 0 || !grassPrefabs[0].name.StartsWith("PT_"))
            {
                ForceLoadPTGrassPrefabs();
            }

            GameObject prefab = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
            if (prefab == null) return;

            #if UNITY_EDITOR
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Dense Grass");
            #else
            GameObject instance = Instantiate(prefab, transform);
            #endif

            instance.transform.position = pos;

            // Orientasi tegak lurus alami (Upright blend)
            Vector3 blendedUp = Vector3.Lerp(Vector3.up, normal, alignWithGroundNormal).normalized;
            Quaternion targetRot = Quaternion.FromToRotation(Vector3.up, blendedUp) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.rotation = targetRot;

            float baseScale = Random.Range(minScale, maxScale);
            Vector3 finalScale = new Vector3(baseScale, baseScale * heightMultiplier, baseScale);
            instance.transform.localScale = finalScale;

            activeClumps.Add(new GrassClumpData
            {
                transform = instance.transform,
                baseRotation = targetRot
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
                Vector3 diff = child.position - centerPoint;
                if ((diff.x * diff.x + diff.z * diff.z) <= radiusSqr)
                {
                    #if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(child.gameObject);
                    #else
                    DestroyImmediate(child.gameObject);
                    #endif
                }
            }
            CacheClumpData();
        }

        public void ClearAll()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child.gameObject);
                #else
                DestroyImmediate(child.gameObject);
                #endif
            }
            activeClumps.Clear();
        }

        public void QuickFillArea()
        {
            ClearAll();
            ForceLoadPTGrassPrefabs();
            FindPlayer();

            Vector3 origin = (playerTransform != null) ? playerTransform.position : transform.position;
            Random.InitState(77777);

            int target = targetClumpCount;
            float radius = meadowRadius;

            // Grid Jitter Super Rapat (Jarak per titik hanya 11 cm)
            float step = 0.11f;
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

            // Lapisan Penutup Tambahan (Random Dense Scatter)
            int attempts = 0;
            while (transform.childCount < target && attempts < target * 3)
            {
                attempts++;
                Vector2 circle = Random.insideUnitCircle * radius;
                float targetX = origin.x + circle.x;
                float targetZ = origin.z + circle.y;

                if (SampleSurfaceAt(targetX, targetZ, origin.y, out Vector3 surfPoint, out Vector3 surfNormal))
                {
                    if (!IsPositionOccupied(surfPoint, minSpacing))
                    {
                        SpawnClump(surfPoint, surfNormal);
                    }
                }
            }

            CacheClumpData();
            Debug.Log($"[TerrainGrassSpawner] Berhasil menggelar {transform.childCount} rumpun rumput super rapat tanpa jarak!");
        }

        private void Update()
        {
            if (!enableTrample || !Application.isPlaying) return;

            FindPlayer();
            if (playerTransform == null) return;

            Vector3 pPos = playerTransform.position;
            float radiusSqr = interactionRadius * interactionRadius;

            for (int i = 0; i < activeClumps.Count; i++)
            {
                var clump = activeClumps[i];
                if (clump.transform == null) continue;

                Vector3 diff = clump.transform.position - pPos;
                float distSqr = diff.x * diff.x + diff.z * diff.z;

                if (distSqr < radiusSqr && Mathf.Abs(diff.y) < 1.6f)
                {
                    float dist = Mathf.Sqrt(distSqr);
                    float factor = 1.0f - (dist / interactionRadius);
                    Vector3 pushDir = (dist > 0.01f) ? new Vector3(diff.x, 0f, diff.z).normalized : playerTransform.forward;

                    Vector3 bendAxis = Vector3.Cross(Vector3.up, pushDir);
                    Quaternion bendRot = Quaternion.AngleAxis(factor * maxBendAngle, bendAxis);

                    clump.transform.rotation = Quaternion.Slerp(clump.transform.rotation, bendRot * clump.baseRotation, Time.deltaTime * 12f);
                }
                else
                {
                    if (clump.transform.rotation != clump.baseRotation)
                    {
                        clump.transform.rotation = Quaternion.Slerp(clump.transform.rotation, clump.baseRotation, Time.deltaTime * 6f);
                    }
                }
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
                spawner.RemoveOldLegacyMeshComponents();
                spawner.ForceLoadPTGrassPrefabs();
                spawner.CacheClumpData();
                TerrainGrassSpawner.DeleteAllDemoTilesInScene();
            }
        }

        public override void OnInspectorGUI()
        {
            spawner.RemoveOldLegacyMeshComponents();

            if (spawner.grassPrefabs == null || spawner.grassPrefabs.Length == 0 || !spawner.grassPrefabs[0].name.StartsWith("PT_"))
            {
                spawner.ForceLoadPTGrassPrefabs();
                EditorUtility.SetDirty(spawner);
            }

            DrawDefaultInspector();

            EditorGUILayout.Space(14);
            EditorGUILayout.LabelField($"🌿 Total Rumpun Rumput: {spawner.transform.childCount} Rumpun", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 1f, 0.45f);
            if (GUILayout.Button($"🌾 Gelar Karpet Ultra Rapat ({spawner.targetClumpCount} Rumpun)", GUILayout.Height(42)))
            {
                Undo.RecordObject(spawner, "Install Ultra Dense PT Grass");
                spawner.ForceLoadPTGrassPrefabs();
                spawner.QuickFillArea();
                EditorUtility.SetDirty(spawner);
            }
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("🗑️ Hapus Semua Rumput", GUILayout.Height(42)))
            {
                if (EditorUtility.DisplayDialog("Hapus Rumput", "Hapus semua rumput di scene?", "Ya, Hapus", "Batal"))
                {
                    Undo.RecordObject(spawner, "Clear Grass");
                    spawner.ClearAll();
                    EditorUtility.SetDirty(spawner);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            if (spawner.enableBrushMode)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("🖌️ KUAS SCENE VIEW (SUPER LEBAT)!\n- Tahan & Geser Klik Kiri: Menyemburkan 25 rumpun rumput padat tanpa celah.\n- Shift + Klik Kiri: Menghapus rumput di bawah lingkaran kuas.", MessageType.Info);
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
                            Undo.RecordObject(spawner, "Erase Grass");
                            spawner.EraseAt(hit.point, spawner.brushRadius);
                            EditorUtility.SetDirty(spawner);
                        }
                        else
                        {
                            double now = EditorApplication.timeSinceStartup;
                            if (now - lastPaintTime > 0.08 || (hit.point - lastMousePos).sqrMagnitude > 0.10f)
                            {
                                lastPaintTime = now;
                                lastMousePos = hit.point;
                                Undo.RecordObject(spawner, "Paint Ultra Dense Grass");
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
