using UnityEngine;
using System.Collections;

public class CuttableTree : MonoBehaviour
{
    [Header("Tree Data")]
    [Tooltip("Daftar GameObject pohon dari utuh sampai tumbang. Index 0 = Utuh, Index 1 = Hit 1, dst.")]
    public GameObject[] treeStages;

    [Tooltip("Particle effect cipratan kayu khusus untuk pohon ini. Bebas mau ditaruh sebagai child objek ini.")]
    public ParticleSystem hitParticle;

    [Tooltip("Titik yang akan difokuskan oleh kamera (Opsional). Jika kosong, kamera akan melihat ke tengah transform pohon.")]
    public Transform cameraFocusPoint;

    [Header("Drop Settings")]
    [Tooltip("Prefab item yang akan di-drop saat pohon tumbang (misalnya WoodDrop)")]
    public GameObject dropPrefab;
    [Tooltip("Jumlah item yang akan di-drop")]
    public int dropAmount = 3;
    [Tooltip("Titik spawn drop. Jika kosong, akan spawn agak di atas posisi pohon.")]
    public Transform dropSpawnPoint;

    // Menyimpan progres pukulan pohon ini saja (jadi tiap pohon punya progres masing-masing)
    [HideInInspector] public int currentHits = 0;
    [HideInInspector] public int currentFails = 0;

    [Header("Fall & Dissolve Animation")]
    [Tooltip("Objek batang pohon (Trunk) yang akan jatuh dan menghilang. Pastikan objek ini adalah bagian dari Stage terakhir (misal child dari Tunggul).")]
    public Transform fallingTrunk;
    [Tooltip("Material yang menggunakan shader Custom/URP_SimpleDissolve (Sama seperti musuh)")]
    public Material dissolveMaterial;
    [Tooltip("Waktu yang dibutuhkan pohon untuk jatuh (detik).")]
    public float fallDuration = 1.5f;
    [Tooltip("Waktu yang dibutuhkan untuk efek dissolve sampai hilang (detik).")]
    public float dissolveDuration = 2f;
    [Tooltip("Sumbu rotasi jatuhnya pohon. Sesuaikan dengan orientasi 3D Anda (misal X, Y, atau Z).")]
    public Vector3 fallRotationAxis = Vector3.right;
    [Tooltip("Sudut jatuhnya pohon (derajat).")]
    public float fallAngle = 90f;

    private void Start()
    {
        // Saat game baru mulai, matikan semua model kecuali model yang sesuai dengan progress saat ini (Utuh)
        if (treeStages == null) return;
        
        for (int i = 0; i < treeStages.Length; i++)
        {
            if (treeStages[i] != null)
            {
                // Aktifkan hanya model ke-0 (karena currentHits awalnya 0)
                treeStages[i].SetActive(i == currentHits);
            }
        }
    }

    /// <summary>
    /// Mendapatkan titik untuk disorot kamera.
    /// </summary>
    public Transform GetFocusPoint()
    {
        return cameraFocusPoint != null ? cameraFocusPoint : transform;
    }

    /// <summary>
    /// Panggil fungsi ini (misal dari Unity Event OnInteract) untuk memulai minigame.
    /// </summary>
    public void InteractWithTree()
    {
        if (TreeCuttingMinigame.Instance != null)
        {
            TreeCuttingMinigame.Instance.StartMinigame(this);
        }
        else
        {
            Debug.LogError("TreeCuttingMinigame Manager tidak ditemukan di Scene!");
        }
    }

    /// <summary>
    /// Memanggil Coroutine untuk menganimasikan batang jatuh lalu menghilang.
    /// Arah jatuh akan menjauhi posisi pemain.
    /// </summary>
    public void TriggerFallAndDissolve(Transform playerTransform)
    {
        if (fallingTrunk != null && fallingTrunk.gameObject.activeInHierarchy)
        {
            StartCoroutine(FallAndDissolveRoutine(playerTransform));
            StartCoroutine(SpawnDropsRoutine());
        }
    }

    private IEnumerator SpawnDropsRoutine()
    {
        if (dropPrefab == null) yield break;

        // Gunakan titik spawn custom, atau secara default 1 meter di atas tanah agar tidak nyangkut
        Vector3 spawnPos = dropSpawnPoint != null ? dropSpawnPoint.position : transform.position + Vector3.up * 1f;

        for (int i = 0; i < dropAmount; i++)
        {
            Instantiate(dropPrefab, spawnPos, transform.rotation);
            // Beri jeda sedikit agar tidak saling bertumpuk dan meledak
            yield return new WaitForSeconds(0.1f); 
        }
    }

    /// <summary>
    /// Menonaktifkan interaksi sementara (cooldown) setelah gagal memotong.
    /// </summary>
    public void StartCooldown(float delay)
    {
        StartCoroutine(CooldownRoutine(delay));
    }

    private IEnumerator CooldownRoutine(float delay)
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        yield return new WaitForSeconds(delay);
        
        if (col != null) col.enabled = true;
    }

    /// <summary>
    /// Mematikan collider agar tidak memunculkan teks "Potong Pohon" lagi.
    /// </summary>
    public void DisableInteraction()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Jika Anda menggunakan script InteractObject khusus, matikan juga (opsional)
        MonoBehaviour interactObj = GetComponent("InteractObject") as MonoBehaviour;
        if (interactObj != null)
        {
            interactObj.enabled = false;
        }
    }

    private IEnumerator FallAndDissolveRoutine(Transform playerTransform)
    {
        // --- 1. Animasi Jatuh ---
        // BUG FIX: Gunakan World Rotation agar kalkulasi arah jatuhnya akurat dari mana pun arah pohon menghadap
        Quaternion startRot = fallingTrunk.rotation;
        
        // Menghitung arah jatuh (menjauhi pemain)
        Vector3 dynamicAxis = fallRotationAxis;
        if (playerTransform != null)
        {
            Vector3 dirAwayFromPlayer = (fallingTrunk.position - playerTransform.position).normalized;
            dirAwayFromPlayer.y = 0; // Bikin rata tanah
            dirAwayFromPlayer.Normalize();
            // Sumbu rotasi adalah tegak lurus (cross product) dari sumbu Y (atas) dan arah jatuh
            dynamicAxis = Vector3.Cross(Vector3.up, dirAwayFromPlayer).normalized;
        }

        // Rotasi target = Rotasi tambahan (berdasarkan sumbu dinamis) dikali Rotasi awal
        Quaternion endRot = Quaternion.AngleAxis(fallAngle, dynamicAxis) * startRot;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            // Ease-in effect: awal jatuh lambat, makin lama makin cepat (seperti gravitasi)
            float easeT = t * t; 
            fallingTrunk.rotation = Quaternion.Slerp(startRot, endRot, easeT);
            yield return null;
        }
        fallingTrunk.rotation = endRot;

        // --- 2. Animasi Dissolve (Meniru EnemyDissolveController) ---
        Renderer[] renderers = fallingTrunk.GetComponentsInChildren<Renderer>();
        System.Collections.Generic.List<Material> activeDissolveMaterials = new System.Collections.Generic.List<Material>();

        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                Material[] originalMats = r.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];

                for (int i = 0; i < originalMats.Length; i++)
                {
                    if (dissolveMaterial != null)
                    {
                        // Buat instance material dissolve baru untuk setiap slot material
                        Material newDissolveMat = new Material(dissolveMaterial);
                        activeDissolveMaterials.Add(newDissolveMat);

                        if (originalMats[i] != null)
                        {
                            // Salin tekstur JIKA ADA
                            if (originalMats[i].HasProperty("_BaseMap") && originalMats[i].GetTexture("_BaseMap") != null)
                                newDissolveMat.SetTexture("_BaseMap", originalMats[i].GetTexture("_BaseMap"));
                            else if (originalMats[i].HasProperty("_MainTex") && originalMats[i].GetTexture("_MainTex") != null)
                                newDissolveMat.SetTexture("_BaseMap", originalMats[i].GetTexture("_MainTex"));

                            // Salin WARNA (Base Color) agar jika tidak punya tekstur, warnanya tetap sama!
                            if (originalMats[i].HasProperty("_BaseColor"))
                                newDissolveMat.SetColor("_BaseColor", originalMats[i].GetColor("_BaseColor"));
                            else if (originalMats[i].HasProperty("_Color"))
                                newDissolveMat.SetColor("_BaseColor", originalMats[i].GetColor("_Color"));
                        }

                        newMats[i] = newDissolveMat;
                    }
                    else
                    {
                        // Fallback jika belum diisi di Inspector, gunakan material bawaan
                        newMats[i] = originalMats[i];
                        activeDissolveMaterials.Add(originalMats[i]);
                    }
                }

                r.sharedMaterials = newMats;
            }
        }

        int dissolveAmountProp = Shader.PropertyToID("_DissolveAmount");
        elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);
            
            foreach (Material mat in activeDissolveMaterials)
            {
                if (mat != null && mat.HasProperty(dissolveAmountProp))
                {
                    mat.SetFloat(dissolveAmountProp, t);
                }
            }
            yield return null;
        }

        // Hancurkan objek batang yang jatuh setelah dissolve selesai
        Destroy(fallingTrunk.gameObject);
    }
}
