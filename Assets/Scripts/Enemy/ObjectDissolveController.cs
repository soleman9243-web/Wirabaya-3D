using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDissolveController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Tooltip("Material yang menggunakan shader Custom/URP_SimpleDissolve (Abaikan jika objek menggunakan CelShaderV3)")]
    public Material dissolveMaterial;
    [Tooltip("Berapa lama waktu yang dibutuhkan objek untuk hancur sepenuhnya (dalam detik)")]
    public float dissolveDuration = 2f;
    [Tooltip("Mesh renderer dari objek ini (bisa SkinnedMeshRenderer atau MeshRenderer)")]
    public Renderer[] objectRenderers;

    [Tooltip("Paksa gunakan Dissolve Material di atas meskipun objek menggunakan CelShader")]
    public bool forceCustomDissolve = true;

    private void Start()
    {
        // Jika renderers belum di-assign di Inspector, coba cari secara otomatis
        if (objectRenderers == null || objectRenderers.Length == 0)
        {
            objectRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    /// <summary>
    /// Panggil fungsi ini saat objek mati atau hancur
    /// </summary>
    public void TriggerDissolveAndDestroy()
    {
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        List<Material> activeDissolveMaterials = new List<Material>();
        List<Material> celShaderMaterials = new List<Material>();

        foreach (Renderer r in objectRenderers)
        {
            if (r != null)
            {
                Material[] originalMats = r.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];
                bool materialSwapped = false;

                for (int i = 0; i < originalMats.Length; i++)
                {
                    if (originalMats[i] != null)
                    {
                        // CEK JIKA MATERIAL MENGGUNAKAN CEL SHADER (Punya property _dissolve_edge)
                        if (!forceCustomDissolve && originalMats[i].HasProperty("_dissolve_edge"))
                        {
                            // Gunakan material instance agar tidak merubah aset asli
                            Material celMat = new Material(originalMats[i]);
                            celMat.EnableKeyword("_USE_DISSOLVE_ON");
                            celMat.SetFloat("_use_dissolve", 1f);
                            
                            celShaderMaterials.Add(celMat);
                            newMats[i] = celMat;
                            materialSwapped = true;
                        }
                        else if (dissolveMaterial != null)
                        {
                            // JIKA TIDAK, GUNAKAN URP_SimpleDissolve (SWAP)
                            Material newDissolveMat = new Material(dissolveMaterial);
                            newDissolveMat.SetFloat("_DissolveAmount", 0f); // Cegah hilang seketika jika aset diatur ke 1
                            activeDissolveMaterials.Add(newDissolveMat);

                            if (originalMats[i].HasProperty("_BaseMap") && originalMats[i].GetTexture("_BaseMap") != null)
                                newDissolveMat.SetTexture("_BaseMap", originalMats[i].GetTexture("_BaseMap"));
                            else if (originalMats[i].HasProperty("_MainTex") && originalMats[i].GetTexture("_MainTex") != null)
                                newDissolveMat.SetTexture("_BaseMap", originalMats[i].GetTexture("_MainTex"));

                            if (originalMats[i].HasProperty("_BaseColor"))
                                newDissolveMat.SetColor("_BaseColor", originalMats[i].GetColor("_BaseColor"));
                            else if (originalMats[i].HasProperty("_Color"))
                                newDissolveMat.SetColor("_BaseColor", originalMats[i].GetColor("_Color"));

                            newMats[i] = newDissolveMat;
                            materialSwapped = true;
                        }
                        else
                        {
                            newMats[i] = originalMats[i]; // Fallback
                        }
                    }
                }

                if (materialSwapped)
                {
                    r.sharedMaterials = newMats;
                }
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / dissolveDuration);
            
            // Animasi material URP_SimpleDissolve (0 ke 1)
            foreach (Material mat in activeDissolveMaterials)
            {
                if (mat != null) mat.SetFloat("_DissolveAmount", t);
            }
            
            // Animasi material CelShaderV3 (1 ke 0)
            foreach (Material mat in celShaderMaterials)
            {
                if (mat != null) mat.SetFloat("_dissolve_edge", 1f - t);
            }
            
            yield return null;
        }

        Destroy(gameObject);
    }
}
