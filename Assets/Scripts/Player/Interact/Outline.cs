using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimized Outline System (High Performance):
/// - Zero-Cost saat Disabled (Tidak membebani CPU/GPU sama sekali saat tidak disorot).
/// - Menggunakan Shared Material Caching (Mencegah kebocoran ribuan material instance).
/// - Restorasi Material Bersih (Mengembalikan fungsi GPU Batching & Instancing).
/// </summary>
[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    public enum Mode
    {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    public Mode OutlineMode
    {
        get { return outlineMode; }
        set
        {
            outlineMode = value;
            needsUpdate = true;
        }
    }

    public Color OutlineColor
    {
        get { return outlineColor; }
        set
        {
            outlineColor = value;
            needsUpdate = true;
        }
    }

    public float OutlineWidth
    {
        get { return outlineWidth; }
        set
        {
            outlineWidth = value;
            needsUpdate = true;
        }
    }

    [SerializeField]
    private Mode outlineMode = Mode.OutlineVisible;

    [SerializeField]
    private Color outlineColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 2f;

    private static Material sharedMaskMaterial;
    private static Material sharedFillMaterial;

    private Renderer[] cachedRenderers;
    private Dictionary<Renderer, Material[]> originalSharedMaterials = new Dictionary<Renderer, Material[]>();

    private Material instanceMaskMaterial;
    private Material instanceFillMaterial;
    private bool isInitialized = false;
    private bool needsUpdate = false;

    private void Awake()
    {
        // Cache renderers saja tanpa kalkulasi berat
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        InitializeMaterials();
        ApplyOutline();
        UpdateMaterialProperties();
    }

    private void OnDisable()
    {
        RemoveOutline();
    }

    private void OnDestroy()
    {
        RemoveOutline();

        if (instanceMaskMaterial != null) Destroy(instanceMaskMaterial);
        if (instanceFillMaterial != null) Destroy(instanceFillMaterial);
    }

    private void Update()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    private void InitializeMaterials()
    {
        if (isInitialized) return;

        if (sharedMaskMaterial == null)
        {
            var loadedMask = Resources.Load<Material>(@"Materials/OutlineMask");
            if (loadedMask != null) sharedMaskMaterial = loadedMask;
        }

        if (sharedFillMaterial == null)
        {
            var loadedFill = Resources.Load<Material>(@"Materials/OutlineFill");
            if (loadedFill != null) sharedFillMaterial = loadedFill;
        }

        if (sharedMaskMaterial != null && instanceMaskMaterial == null)
        {
            instanceMaskMaterial = Instantiate(sharedMaskMaterial);
            instanceMaskMaterial.hideFlags = HideFlags.DontSave;
        }

        if (sharedFillMaterial != null && instanceFillMaterial == null)
        {
            instanceFillMaterial = Instantiate(sharedFillMaterial);
            instanceFillMaterial.hideFlags = HideFlags.DontSave;
        }

        isInitialized = true;
    }

    private void ApplyOutline()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (instanceMaskMaterial == null || instanceFillMaterial == null) return;

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;

            // Simpan referensi shared material asli untuk restorasi bersih
            if (!originalSharedMaterials.ContainsKey(r))
            {
                originalSharedMaterials[r] = r.sharedMaterials;
            }

            var origMats = originalSharedMaterials[r];
            var newMats = new Material[origMats.Length + 2];
            for (int i = 0; i < origMats.Length; i++)
            {
                newMats[i] = origMats[i];
            }
            newMats[origMats.Length] = instanceMaskMaterial;
            newMats[origMats.Length + 1] = instanceFillMaterial;

            r.sharedMaterials = newMats;
        }
    }

    private void RemoveOutline()
    {
        if (cachedRenderers == null) return;

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;

            if (originalSharedMaterials.TryGetValue(r, out var origMats))
            {
                // Kembalikan ke sharedMaterials asli agar GPU Batching pulih
                r.sharedMaterials = origMats;
            }
        }
    }

    private void UpdateMaterialProperties()
    {
        if (instanceFillMaterial == null || instanceMaskMaterial == null) return;

        instanceFillMaterial.SetColor("_OutlineColor", outlineColor);
        instanceFillMaterial.SetFloat("_OutlineWidth", outlineWidth);

        switch (outlineMode)
        {
            case Mode.OutlineAll:
                instanceMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                instanceFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                break;

            case Mode.OutlineVisible:
                instanceMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                instanceFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                break;

            case Mode.OutlineHidden:
                instanceMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                instanceFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                break;

            case Mode.OutlineAndSilhouette:
                instanceMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                instanceFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                break;

            case Mode.SilhouetteOnly:
                instanceMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                instanceFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                instanceFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }
}
