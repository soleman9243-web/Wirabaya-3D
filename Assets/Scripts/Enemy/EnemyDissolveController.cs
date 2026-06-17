using System.Collections;
using UnityEngine;

public class EnemyDissolveController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Tooltip("Material yang menggunakan shader Custom/URP_SimpleDissolve")]
    public Material dissolveMaterial;
    [Tooltip("Berapa lama waktu yang dibutuhkan musuh untuk hancur sepenuhnya (dalam detik)")]
    public float dissolveDuration = 2f;
    [Tooltip("Mesh renderer dari musuh ini (bisa SkinnedMeshRenderer atau MeshRenderer)")]
    public Renderer[] enemyRenderers;

    private void Start()
    {
        // Jika renderers belum di-assign di Inspector, coba cari secara otomatis
        if (enemyRenderers == null || enemyRenderers.Length == 0)
        {
            enemyRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    /// <summary>
    /// Panggil fungsi ini saat musuh mati (misal di EnemyPatrol.Die())
    /// </summary>
    public void TriggerDissolveAndDestroy()
    {
        if (dissolveMaterial == null)
        {
            Debug.LogWarning("Dissolve Material belum dipasang di " + gameObject.name);
            Destroy(gameObject, dissolveDuration);
            return;
        }

        // Mulai Coroutine untuk merubah nilai dissolve dari 0 ke 1
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        // Menyimpan semua material dissolve yang baru dibuat agar bisa diubah nilai dissolve-nya
        System.Collections.Generic.List<Material> activeDissolveMaterials = new System.Collections.Generic.List<Material>();

        // Untuk setiap renderer (badan, senjata, dll), ganti materialnya dengan dissolve material
        foreach (Renderer r in enemyRenderers)
        {
            if (r != null)
            {
                Material[] originalMats = r.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];

                for (int i = 0; i < originalMats.Length; i++)
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

                r.sharedMaterials = newMats;
            }
        }

        float elapsedTime = 0f;

        // 2. Animasikan property _DissolveAmount dari 0 ke 1
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAmount = Mathf.Lerp(0f, 1f, elapsedTime / dissolveDuration);
            
            foreach (Material mat in activeDissolveMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat("_DissolveAmount", currentAmount);
                }
            }
            yield return null;
        }

        // 3. Setelah selesai dissolve, hancurkan GameObject musuh
        Destroy(gameObject);
    }
}
