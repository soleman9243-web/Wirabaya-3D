using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniversalDissolveController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Tooltip("Material fallback dengan shader dissolve (misal Custom/URP_SimpleDissolve). Dipakai jika material asli objek tidak punya properti dissolve bawaan.")]
    public Material fallbackDissolveMaterial;
    
    [Tooltip("Waktu untuk hancur sepenuhnya (detik)")]
    public float dissolveDuration = 2f;
    
    [Tooltip("Otomatis hancurkan GameObject setelah dissolve selesai?")]
    public bool destroyOnComplete = true;

    private Renderer[] renderers;
    
    // Struct untuk menyimpan data material yang akan dianimasikan secara dinamis
    private class DissolveData
    {
        public Material materialInstance;
        public string propertyName;
        public bool reverseAnimation; // True jika nilai awal 1 lalu menuju 0. False jika 0 menuju 1.
    }
    
    private List<DissolveData> activeDissolveMaterials = new List<DissolveData>();

    private void Awake()
    {
        // Ambil semua renderer di objek ini dan anaknya
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        // Kosongkan atau gunakan untuk inisialisasi lain jika perlu
    }

    /// <summary>
    /// Panggil fungsi ini untuk memicu efek dissolve.
    /// </summary>
    public void TriggerDissolve()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
        
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        Debug.Log($"[Dissolve] Mulai efek dissolve. Duration: {dissolveDuration}");
        activeDissolveMaterials.Clear();

        // 1. PERSIAPAN MATERIAL: Cek setiap material dan setup data animasinya
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            Material[] originalMats = r.sharedMaterials;
            Material[] newMats = new Material[originalMats.Length];
            bool needsApply = false;

            for (int i = 0; i < originalMats.Length; i++)
            {
                Material origMat = originalMats[i];
                if (origMat == null) continue;

                if (origMat.HasProperty("_dissolve_edge")) // Khusus CelShaderV3 milikmu
                {
                    Material inst = new Material(origMat);
                    inst.EnableKeyword("_USE_DISSOLVE_ON");
                    inst.SetFloat("_use_dissolve", 1f);
                    activeDissolveMaterials.Add(new DissolveData { materialInstance = inst, propertyName = "_dissolve_edge", reverseAnimation = true });
                    newMats[i] = inst;
                    needsApply = true;
                }
                else if (fallbackDissolveMaterial != null)
                {
                    // SWAP ke material fallback (DissolveWood)
                    // LANGSUNG gunakan material tanpa di-clone! (Untuk mengatasi bug invisible)
                    Material inst = fallbackDissolveMaterial;

                    // Kita tidak bisa mengganti warna/tekstur material asli secara permanen, 
                    // jadi kita biarkan tekstur aslinya (DissolveEmission)
                    
                    if (inst.HasProperty("_DissolveAmount"))
                    {
                        // Reset ke 0
                        inst.SetFloat("_DissolveAmount", 0f);
                        activeDissolveMaterials.Add(new DissolveData { materialInstance = inst, propertyName = "_DissolveAmount", reverseAnimation = false });
                    }

                    newMats[i] = inst;
                    needsApply = true;
                    Debug.Log($"[Dissolve] Direct Swap {origMat.name} to {inst.name}");
                }
                else
                {
                    // Tidak ada dukungan dissolve dan tidak ada fallback
                    newMats[i] = origMat;
                    Debug.Log($"[Dissolve] No swap for {origMat.name}");
                }
            }

            // Terapkan material instancing yang baru agar aset asli tidak berubah
            if (needsApply)
            {
                r.sharedMaterials = newMats;
            }
        }

        // 2. PROSES ANIMASI DISSOLVE
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);

            // Animasikan seluruh material secara dinamis berdasarkan propertinya masing-masing
            foreach (var data in activeDissolveMaterials)
            {
                if (data.materialInstance != null)
                {
                    float value = data.reverseAnimation ? (1f - t) : t; // CelShaderV3 bergerak dari 1 ke 0. Shader lain dari 0 ke 1.
                    data.materialInstance.SetFloat(data.propertyName, value);
                }
            }

            yield return null;
        }

        // 3. SELESAI
        Debug.Log($"[Dissolve] Selesai efek dissolve. Menghancurkan object: {gameObject.name}");
        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }
}
