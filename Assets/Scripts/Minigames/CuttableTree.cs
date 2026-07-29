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
    
    [Tooltip("Objek sisa tebangan (Stump/Tunggul) yang akan otomatis diaktifkan setelah pohon ditebang (Opsional).")]
    public GameObject cutStump;

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
        
        // BUG FIX: Matikan sisa tunggul di awal agar tidak terlihat sebelum ditebang
        if (cutStump != null)
        {
            cutStump.SetActive(false);
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
            // Aktifkan sisa tunggul (stump) secara langsung saat pohon mulai tumbang
            if (cutStump != null)
            {
                cutStump.SetActive(true);
            }

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

        // --- 2. Animasi Menghilang (Dissolve Shader) ---
        float dissolveElapsed = 0f;
        Renderer[] renderers = fallingTrunk.GetComponentsInChildren<Renderer>();
        
        // Ganti material ke material dissolve (jika dislot di Inspector)
        if (dissolveMaterial != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    Material[] oldMats = r.materials;
                    Material[] newMats = new Material[oldMats.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = new Material(dissolveMaterial); // Buat instance baru
                        
                        // Coba copy tekstur lama agar tidak polos
                        Texture tex = null;
                        if (oldMats[i].HasProperty("_BaseMap")) tex = oldMats[i].GetTexture("_BaseMap");
                        else if (oldMats[i].HasProperty("_MainTex")) tex = oldMats[i].GetTexture("_MainTex");
                        
                        if (tex != null && newMats[i].HasProperty("_BaseMap"))
                        {
                            newMats[i].SetTexture("_BaseMap", tex);
                        }
                    }
                    r.materials = newMats;
                }
            }
        }
        else
        {
            // Jika tidak ada material dissolve pengganti, cukup buat salinan (instance) material saat ini
            foreach (Renderer r in renderers)
            {
                if (r != null) r.materials = r.materials; 
            }
        }

        int dissolveAmountID = Shader.PropertyToID("_DissolveAmount");

        while (dissolveElapsed < dissolveDuration)
        {
            dissolveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(dissolveElapsed / dissolveDuration);
            
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    foreach (Material mat in r.materials)
                    {
                        // Animasikan properti DissolveAmount di Shader Graph kita
                        mat.SetFloat(dissolveAmountID, t);

                        // (Opsional) Tetap panggil alpha fade untuk berjaga-jaga jika ada material non-dissolve
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = Mathf.Lerp(1f, 0f, t);
                            mat.SetColor("_BaseColor", c);
                        }
                    }
                }
            }
            
            yield return null;
        }

        Destroy(fallingTrunk.gameObject);
    }
}
