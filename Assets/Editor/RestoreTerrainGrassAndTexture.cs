#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Wirabaya.EditorTools
{
    public static class RestoreTerrainGrassAndTexture
    {
        [MenuItem("Wirabaya/🌿 Pulihkan Terrain (Rumput & Tekstur Otomatis)")]
        public static void Execute()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                terrain = Object.FindFirstObjectByType<Terrain>();
            }

            if (terrain == null || terrain.terrainData == null)
            {
                EditorUtility.DisplayDialog("Error", "Terrain tidak ditemukan di scene aktif!", "OK");
                return;
            }

            TerrainData td = terrain.terrainData;
            Undo.RecordObject(td, "Pulihkan Terrain Tekstur dan Rumput");

            // 1. Pulihkan Terrain Layers (Tekstur Rumput, Tanah, Batu, Pasir)
            List<TerrainLayer> layerList = new List<TerrainLayer>();
            string[] layerPaths = new string[]
            {
                "Assets/Terrain/Materials/Blockout_Grass_A.terrainlayer",
                "Assets/Terrain/Materials/Blockout_Dirt_A.terrainlayer",
                "Assets/Terrain/Materials/Blockout_Rock_A.terrainlayer",
                "Assets/Terrain/Materials/Blockout_Sand_A.terrainlayer"
            };

            foreach (var path in layerPaths)
            {
                var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (l != null) layerList.Add(l);
            }

            if (layerList.Count > 0)
            {
                td.terrainLayers = layerList.ToArray();
            }

            // 2. Pulihkan Detail Prototype (Rumput Kartun GrassPrefabb)
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/8-12-2026/GrassBissmillah/GrassPrefabb.prefab");
            if (grassPrefab != null)
            {
                DetailPrototype proto = new DetailPrototype
                {
                    prototype = grassPrefab,
                    renderMode = DetailRenderMode.Grass,
                    usePrototypeMesh = true,
                    minWidth = 0.8f,
                    maxWidth = 1.3f,
                    minHeight = 0.7f,
                    maxHeight = 1.2f,
                    noiseSpread = 0.1f,
                    alignToGround = 1.0f
                };

                td.detailPrototypes = new DetailPrototype[] { proto };

                // 3. Tanam Rumput di Sekitar Pemain & Rumah Nenek
                int res = td.detailResolution;
                int[,] map = td.GetDetailLayer(0, 0, res, res, 0);

                Vector3 tPos = terrain.transform.position;
                Vector3 tSize = td.size;

                Transform player = GameObject.Find("PlayerArmature")?.transform ?? GameObject.FindGameObjectWithTag("Player")?.transform;
                Vector3 center = player != null ? player.position : new Vector3(tPos.x + tSize.x * 0.5f, 0, tPos.z + tSize.z * 0.5f);

                float normX = (center.x - tPos.x) / tSize.x;
                float normZ = (center.z - tPos.z) / tSize.z;

                int cX = Mathf.Clamp(Mathf.RoundToInt(normX * res), 0, res - 1);
                int cZ = Mathf.Clamp(Mathf.RoundToInt(normZ * res), 0, res - 1);
                int radius = Mathf.RoundToInt((35f / tSize.x) * res);

                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        float dist = Mathf.Sqrt((x - cX) * (x - cX) + (y - cZ) * (y - cZ));
                        if (dist < radius)
                        {
                            float falloff = 1f - (dist / radius);
                            map[y, x] = Mathf.Max(map[y, x], Mathf.RoundToInt(falloff * 8f));
                        }
                    }
                }

                td.SetDetailLayer(0, 0, 0, map);
            }

            // 4. Pastikan Detail Object Distance cukup agar rumput terlihat
            terrain.detailObjectDistance = Mathf.Max(terrain.detailObjectDistance, 120f);
            terrain.detailObjectDensity = 1.0f;

            EditorUtility.SetDirty(td);
            EditorUtility.SetDirty(terrain);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Sukses!", "🌿 Tekstur tanah dan rumput berhasil dipulihkan dan ditanam kembali di sekitar karakter!", "Mantap");
        }
    }
}
#endif
