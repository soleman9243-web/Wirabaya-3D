using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.FantasyKingdom
{
    /// <summary>
    /// TerrainGrassSpawner: Generator Padang Rumput Lebat AAA.
    /// - Fitur Anti-Nembus Collider: Rumput tidak akan pernah menggantung di udara atau menembus bagian bawah tebing.
    /// - Filter Kemiringan Tebing (Slope Filter): Rumput hanya tumbuh di tanah datar dan bukit landai, tidak di tebing terjal.
    /// - Pola Padang Rumput Menyatu Rapi tanpa Tumpukan.
    /// </summary>
    [ExecuteAlways]
    public class TerrainGrassSpawner : MonoBehaviour
    {
        [Header("Grass Prefab & Layer Restriction")]
        [Tooltip("Prefab model rumput 3D yang akan dipasang di Terrain.")]
        public GameObject grassPrefab;

        [Tooltip("Layer tanah tempat rumput boleh ditanam (Default: Ground & Terrain).")]
        public LayerMask groundLayer;

        [Header("Surface & Anti-Clipping Protection (Anti-Nembus)")]
        [Tooltip("Kemiringan maksimal tanah tempat rumput boleh tumbuh (mencegah rumput nempel di dinding tebing).")]
        [Range(15f, 45f)]
        public float maxSlopeAngle = 35f;

        [Tooltip("Jarak cek penyangga tanah di sekeliling rumpun agar rumput tidak menggantung di tepi jurang.")]
        [Range(0.2f, 0.8f)]
        public float edgeFootprintCheck = 0.35f;

        [Header("Tsushima Meadow Settings (Kepadatan Karpet Rumput)")]
        [Tooltip("Pusat area padang rumput (kosongkan untuk auto-detect player).")]
        public Transform centerTransform;
        [Tooltip("Jari-jari area padang rumput (dalam meter).")]
        [Range(10f, 60f)]
        public float spawnRadius = 25f;
        [Tooltip("Jumlah rumpun untuk membentuk padang rumput lebat.")]
        [Range(50, 600)]
        public int targetGrassCount = 280;

        [Header("Natural Spacing & Blending")]
        [Tooltip("Jarak antar rumpun rumput agar menyatu tanpa tumpukan ganda.")]
        [Range(0.4f, 1.5f)]
        public float naturalSpacing = 0.6f;

        [Tooltip("Variasi skala agar rumpun rumput rimbun dan menyatu.")]
        public Vector2 scaleVariation = new Vector2(1.2f, 1.6f);
        public bool alignToTerrainSlope = true;

        [Header("Tsushima Brush Painting")]
        public bool enableBrushMode = true;
        [Range(1.5f, 12f)]
        public float brushRadius = 4.5f;
        [Range(1, 6)]
        public int brushDensity = 4;

        [Header("Hierarchy Management")]
        [SerializeField] private Transform grassContainer;

        public Transform Container
        {
            get
            {
                if (grassContainer == null)
                {
                    grassContainer = transform;
                }
                return grassContainer;
            }
        }

        private List<Vector3> placedPositions = new List<Vector3>();

        private void Reset()
        {
            SetupDefaultGroundLayer();

            if (grassPrefab == null)
            {
                #if UNITY_EDITOR
                string[] guids = AssetDatabase.FindAssets("PT_Grass_02_v1 t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
                #endif
            }
        }

        private void OnValidate()
        {
            if (naturalSpacing < 0.4f) naturalSpacing = 0.55f;
            if (groundLayer.value == 0 || groundLayer.value == ~0)
            {
                SetupDefaultGroundLayer();
            }
        }

        private void SetupDefaultGroundLayer()
        {
            int groundIndex = LayerMask.NameToLayer("Ground");
            if (groundIndex >= 0)
            {
                groundLayer = (1 << groundIndex) | (1 << 0);
            }
            else
            {
                groundLayer = 1 << 0;
            }
        }

        public void RebuildPositionsList()
        {
            placedPositions.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                placedPositions.Add(transform.GetChild(i).position);
            }
        }

        public bool IsSpotOccupied(Vector3 worldPos, float minDistance)
        {
            if (placedPositions.Count != transform.childCount)
            {
                RebuildPositionsList();
            }

            float minDistanceSqr = minDistance * minDistance;
            for (int i = 0; i < placedPositions.Count; i++)
            {
                Vector3 diff = placedPositions[i] - worldPos;
                if ((diff.x * diff.x + diff.z * diff.z) < minDistanceSqr)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Validasi ketat tanah: Tidak boleh tebing terjal, tidak boleh menggantung di tepi jurang, tidak boleh kena bangunan.
        /// </summary>
        public bool IsValidGroundPosition(RaycastHit hit, out Vector3 safePosition, out Vector3 safeNormal)
        {
            safePosition = hit.point;
            safeNormal = hit.normal;

            // 1. Cek sudut kemiringan (Filter Tebing Terjal)
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle > maxSlopeAngle)
            {
                return false; // Terlalu miring/tebing, jangan tanam rumput di sini
            }

            // 2. Cek Layer & Jenis Objek
            bool isTerrain = (hit.collider is TerrainCollider) || (hit.collider.GetComponent<Terrain>() != null);
            int groundLayerIndex = LayerMask.NameToLayer("Ground");
            bool isGroundLayer = (groundLayerIndex >= 0 && hit.collider.gameObject.layer == groundLayerIndex);
            bool isDefaultGround = (hit.collider.gameObject.layer == 0);

            if (!isTerrain && !isGroundLayer && !isDefaultGround)
            {
                return false;
            }

            string objName = hit.collider.gameObject.name.ToLower();
            if (objName.Contains("roof") || objName.Contains("house") || objName.Contains("wall") || objName.Contains("fence") || objName.Contains("building"))
            {
                return false;
            }

            // 3. Cek Penyangga Tanah 4 Sudut (Anti-Menggantung di Tepi Jurang / Nembus Bawah Tebing)
            Vector3 center = hit.point;
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(edgeFootprintCheck, 0.5f, 0),
                new Vector3(-edgeFootprintCheck, 0.5f, 0),
                new Vector3(0, 0.5f, edgeFootprintCheck),
                new Vector3(0, 0.5f, -edgeFootprintCheck)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 checkOrigin = center + offsets[i];
                if (!Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit cornerHit, 1.2f, groundLayer))
                {
                    return false; // Sudut menggantung di udara kosong
                }
                if (Mathf.Abs(cornerHit.point.y - center.y) > 0.45f)
                {
                    return false; // Ada patahan jurang curam di tepi rumpun
                }
            }

            return true;
        }

        /// <summary>
        /// Generate padang rumput lebat Tsushima yang tidak tembus collider dan tidak menggantung di tepi jurang.
        /// </summary>
        [ContextMenu("Generate Tsushima Meadow")]
        public void GenerateTsushimaMeadow()
        {
            if (grassPrefab == null)
            {
                Debug.LogWarning("[TerrainGrassSpawner] Pasang Grass Prefab terlebih dahulu!");
                return;
            }

            Vector3 origin = Vector3.zero;
            if (centerTransform != null)
            {
                origin = centerTransform.position;
            }
            else
            {
                var player = GameObject.Find("PlayerArmature") ?? GameObject.FindGameObjectWithTag("Player");
                if (player != null) origin = player.transform.position;
                else if (Camera.main != null) origin = Camera.main.transform.position;
                else origin = transform.position;
            }

            RebuildPositionsList();
            int spawned = 0;
            int maxAttempts = targetGrassCount * 8;
            float spacing = Mathf.Max(0.45f, naturalSpacing);

            for (int i = 0; i < maxAttempts && spawned < targetGrassCount; i++)
            {
                Vector2 circlePoint = Random.insideUnitCircle * spawnRadius;
                Vector3 samplePos = new Vector3(origin.x + circlePoint.x, origin.y + 100f, origin.z + circlePoint.y);

                if (Physics.Raycast(samplePos, Vector3.down, out RaycastHit hit, 250f, groundLayer))
                {
                    if (IsValidGroundPosition(hit, out Vector3 safePos, out Vector3 safeNormal))
                    {
                        if (!IsSpotOccupied(safePos, spacing))
                        {
                            SpawnGrassClump(safePos, safeNormal);
                            placedPositions.Add(safePos);
                            spawned++;
                        }
                    }
                }
            }

            Debug.Log($"[TerrainGrassSpawner] Berhasil membuat {spawned} padang rumput lebat anti-nembus di sekitar {origin}!");
        }

        [ContextMenu("Clear All Grass")]
        public void ClearAllGrass()
        {
            #if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(gameObject, "Clear Grass");
            #endif

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child.gameObject);
                #else
                DestroyImmediate(child.gameObject);
                #endif
            }

            placedPositions.Clear();
        }

        public void SpawnGrassClump(Vector3 worldPos, Vector3 normal)
        {
            if (grassPrefab == null) return;

            GameObject grassObj = (GameObject)
            #if UNITY_EDITOR
                PrefabUtility.InstantiatePrefab(grassPrefab, Container);
            #else
                Instantiate(grassPrefab, Container);
            #endif

            if (grassObj == null) return;

            // Dudukkan sedikit di atas tanah agar tidak z-fighting dengan tekstur tanah
            grassObj.transform.position = worldPos + normal * 0.02f;

            float randomRotY = Random.Range(0f, 360f);
            if (alignToTerrainSlope && normal != Vector3.zero)
            {
                Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, normal);
                grassObj.transform.rotation = slopeRot * Quaternion.Euler(0f, randomRotY, 0f);
            }
            else
            {
                grassObj.transform.rotation = Quaternion.Euler(0f, randomRotY, 0f);
            }

            float s = Random.Range(scaleVariation.x, scaleVariation.y);
            grassObj.transform.localScale = Vector3.one * s;

            MeshRenderer mr = grassObj.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
        }

        public void EraseGrassAt(Vector3 worldPos, float radius)
        {
            float radiusSqr = radius * radius;
            List<GameObject> toDestroy = new List<GameObject>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Vector3 diff = child.position - worldPos;
                if ((diff.x * diff.x + diff.z * diff.z) <= radiusSqr)
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate(go);
                #else
                DestroyImmediate(go);
                #endif
            }

            RebuildPositionsList();
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(TerrainGrassSpawner))]
    public class TerrainGrassSpawnerEditor : Editor
    {
        private TerrainGrassSpawner spawner;
        private Vector3 lastMouseWorldPos;
        private double lastPaintTime = 0;

        private void OnEnable()
        {
            spawner = (TerrainGrassSpawner)target;
            if (spawner != null)
            {
                spawner.RebuildPositionsList();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("⚡ Padang Rumput Tsushima Actions", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🌾 Generate Tsushima Meadow", GUILayout.Height(38)))
            {
                spawner.GenerateTsushimaMeadow();
            }
            if (GUILayout.Button("🗑️ Clear All Grass", GUILayout.Height(38)))
            {
                if (EditorUtility.DisplayDialog("Clear Grass", "Hapus semua rumput lama untuk membuat padang baru?", "Ya, Hapus", "Batal"))
                {
                    spawner.ClearAllGrass();
                }
            }
            GUILayout.EndHorizontal();

            if (spawner.enableBrushMode)
            {
                EditorGUILayout.HelpBox("🖌️ MODE KUAS TSUSHIMA (ANTI-NEMBUS AKTIF)!\n- Rumput tidak akan pernah tertanam di tebing curam atau menggantung di udara.\n- Tahan & Geser Klik Kiri: Melukis padang rumput lebat yang menyatu.\n- Shift + Klik Kiri: Menghapus rumput di bawah kuas.", MessageType.Info);
            }
        }

        private void OnSceneGUI()
        {
            if (!spawner.enableBrushMode) return;

            Event e = Event.current;

            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.up;
            bool hitFound = false;

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, spawner.groundLayer))
            {
                if (spawner.IsValidGroundPosition(hit, out hitPoint, out hitNormal))
                {
                    hitFound = true;
                }
            }

            if (hitFound)
            {
                Handles.color = e.shift ? new Color(1f, 0.2f, 0.2f, 0.75f) : new Color(0.2f, 1f, 0.35f, 0.75f);
                Handles.DrawWireDisc(hitPoint, hitNormal, spawner.brushRadius);
                Handles.DrawSolidDisc(hitPoint, hitNormal, spawner.brushRadius * 0.08f);

                if (e.type == EventType.MouseMove)
                {
                    HandleUtility.Repaint();
                }

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    if (e.shift)
                    {
                        spawner.EraseGrassAt(hitPoint, spawner.brushRadius);
                    }
                    else
                    {
                        double now = EditorApplication.timeSinceStartup;
                        float spacing = Mathf.Max(0.45f, spawner.naturalSpacing);

                        if (now - lastPaintTime > 0.04 || (hitPoint - lastMouseWorldPos).sqrMagnitude > (spacing * spacing))
                        {
                            lastPaintTime = now;
                            lastMouseWorldPos = hitPoint;

                            Undo.SetCurrentGroupName("Paint Tsushima Meadow");
                            int group = Undo.GetCurrentGroup();

                            int density = Mathf.Clamp(spawner.brushDensity, 1, 5);
                            for (int i = 0; i < density; i++)
                            {
                                Vector2 randCircle = Random.insideUnitCircle * spawner.brushRadius;
                                Vector3 samplePos = hitPoint + new Vector3(randCircle.x, 20f, randCircle.y);

                                if (Physics.Raycast(samplePos, Vector3.down, out RaycastHit groundHit, 50f, spawner.groundLayer))
                                {
                                    if (spawner.IsValidGroundPosition(groundHit, out Vector3 safePos, out Vector3 safeNormal))
                                    {
                                        if (!spawner.IsSpotOccupied(safePos, spacing))
                                        {
                                            spawner.SpawnGrassClump(safePos, safeNormal);
                                            spawner.RebuildPositionsList();
                                        }
                                    }
                                }
                            }

                            Undo.CollapseUndoOperations(group);
                        }
                    }
                    e.Use();
                    HandleUtility.Repaint();
                }
            }
        }
    }
    #endif
}
