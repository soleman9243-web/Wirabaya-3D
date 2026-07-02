using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Masukkan SkinnedMeshRenderer karakter utama (baju, celana, badan, dll)")]
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;
    [Tooltip("Material khusus untuk bayangan (Gunakan shader URP/Unlit transparan atau Distortion)")]
    [SerializeField] private Material ghostMaterial;

    [Header("Settings")]
    [Tooltip("Jeda waktu antar spawn bayangan (semakin kecil semakin rapat)")]
    [SerializeField] private float spawnRate = 0.05f;
    [Tooltip("Berapa lama bayangan bertahan sebelum hilang")]
    [SerializeField] private float destroyTime = 0.3f;
    [Tooltip("Apakah warna alpha akan di-fade out perlahan? (Material harus punya properti _BaseColor)")]
    [SerializeField] private bool fadeOut = true;

    private bool isSpawning = false;
    private Coroutine spawnRoutine;

    public void StartTrail()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            spawnRoutine = StartCoroutine(SpawnGhostRoutine());
        }
    }

    public void StopTrail()
    {
        isSpawning = false;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnGhostRoutine()
    {
        while (isSpawning)
        {
            SpawnGhost();
            yield return new WaitForSeconds(spawnRate);
        }
    }

    private void SpawnGhost()
    {
        if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0) return;

        foreach (var smr in skinnedMeshRenderers)
        {
            if (smr == null) continue;

            // Buat objek kosong untuk menampung mesh
            GameObject ghostObj = new GameObject("Ghost_" + smr.gameObject.name);
            
            // Posisikan tepat di posisi mesh asli saat ini
            ghostObj.transform.position = smr.transform.position;
            ghostObj.transform.rotation = smr.transform.rotation;
            
            // Tambahkan komponen yang dibutuhkan
            MeshRenderer mr = ghostObj.AddComponent<MeshRenderer>();
            MeshFilter mf = ghostObj.AddComponent<MeshFilter>();

            // Bake (cetak) pose animasi saat ini ke dalam sebuah mesh statis
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);
            mf.mesh = bakedMesh;

            // Set material
            mr.material = ghostMaterial;

            // Hancurkan objek setelah batas waktu
            Destroy(ghostObj, destroyTime);

            // Jalankan efek fade out jika diaktifkan
            if (fadeOut)
            {
                StartCoroutine(FadeOutRoutine(mr.material));
            }
        }
    }

    private IEnumerator FadeOutRoutine(Material mat)
    {
        if (!mat.HasProperty("_BaseColor")) yield break;

        Color startColor = mat.GetColor("_BaseColor");
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float time = 0f;
        while (time < destroyTime)
        {
            time += Time.deltaTime;
            float t = time / destroyTime;
            mat.SetColor("_BaseColor", Color.Lerp(startColor, targetColor, t));
            yield return null;
        }
    }
}
