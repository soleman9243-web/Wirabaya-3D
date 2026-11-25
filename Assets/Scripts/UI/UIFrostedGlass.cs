using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wirabaya.UI
{
    /// <summary>
    /// Komponen untuk memberikan efek kaca buram (Frosted Glass) pada Image/RawImage UI
    /// dengan tingkat keburaman yang bisa diatur langsung di Inspector (tanpa merusak asset material bersama).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIFrostedGlass : MonoBehaviour, IMaterialModifier
    {
        private const string FrostedShaderName = "Wirabaya/UI/FrostedGlass";
        private const string DefaultMaterialPath = "Assets/Materials/UI_FrostedGlass.mat";

        private static readonly int BlurAmountId = Shader.PropertyToID("_BlurAmount");
        private static readonly int BlurSpreadId = Shader.PropertyToID("_BlurSpread");
        private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        private static readonly int BrightnessId = Shader.PropertyToID("_BackgroundBrightness");
        private static readonly int SaturationId = Shader.PropertyToID("_BackgroundSaturation");
        private static readonly int FrostGrainId = Shader.PropertyToID("_FrostGrain");
        private static readonly int FlipYId = Shader.PropertyToID("_FlipBackgroundY");

        [Header("=== KEBURAMAN KACA (INSPECTOR CONTROL) ===")]
        [Range(0f, 1f)]
        [Tooltip("Tingkat keburaman kaca (0 = latar jernih, 0.5 = blur sedang, 1 = buram pekat).")]
        [SerializeField] private float blurAmount = 0.8f;

        [Range(0f, 12f)]
        [Tooltip("Sebaran tambahan di shader agar kaca terlihat semakin buram dan lembut.")]
        [SerializeField] private float extraBlurSpread = 4.0f;

        [Header("=== WARNA & TEKSTUR KACA ===")]
        [Range(0f, 1f)]
        [Tooltip("Kepekatan warna Image di atas kaca buram (0 = kaca bening, 1 = warna Image solid).")]
        [SerializeField] private float tintOpacity = 0.55f;

        [Range(0.2f, 1.5f)]
        [Tooltip("Kecerahan pemandangan 3D di balik kaca buram.")]
        [SerializeField] private float backgroundBrightness = 0.85f;

        [Range(0f, 1.5f)]
        [Tooltip("Saturasi warna pemandangan di balik kaca (nilai < 1 memberi kesan kaca buram modern).")]
        [SerializeField] private float backgroundSaturation = 0.85f;

        [Range(0f, 0.03f)]
        [Tooltip("Tekstur butiran kristal halus khas kaca buram (frosted glass).")]
        [SerializeField] private float frostGrain = 0.004f;

        [Tooltip("Aktifkan hanya jika latar di balik kaca terlihat terbalik secara vertikal.")]
        [SerializeField] private bool flipBackgroundVertically = false;

        private Graphic cachedGraphic;
        private Material runtimeMaterial;
        private Shader cachedShader;

        public float BlurAmount
        {
            get => blurAmount;
            set
            {
                blurAmount = Mathf.Clamp01(value);
                MarkMaterialDirty();
            }
        }

        public float ExtraBlurSpread
        {
            get => extraBlurSpread;
            set
            {
                extraBlurSpread = Mathf.Clamp(value, 0f, 12f);
                MarkMaterialDirty();
            }
        }

        public float TintOpacity
        {
            get => tintOpacity;
            set
            {
                tintOpacity = Mathf.Clamp01(value);
                MarkMaterialDirty();
            }
        }

        private Graphic TargetGraphic
        {
            get
            {
                if (cachedGraphic == null)
                    cachedGraphic = GetComponent<Graphic>();
                return cachedGraphic;
            }
        }

        private void Reset()
        {
            EnsureFrostedBaseMaterial();
            MarkMaterialDirty();
        }

        private void OnEnable()
        {
            EnsureFrostedBaseMaterial();
            MarkMaterialDirty();
        }

        private void OnDisable()
        {
            MarkMaterialDirty();
            CleanupRuntimeMaterial();
        }

        private void OnDestroy()
        {
            CleanupRuntimeMaterial();
        }

        private void OnValidate()
        {
            EnsureFrostedBaseMaterial();
            MarkMaterialDirty();
        }

        public void MarkMaterialDirty()
        {
            if (TargetGraphic != null)
            {
                TargetGraphic.SetMaterialDirty();
            }
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled)
                return baseMaterial;

            Shader targetShader = ResolveFrostedShader(baseMaterial);
            if (targetShader == null)
                return baseMaterial;

            if (runtimeMaterial == null || runtimeMaterial.shader != targetShader)
            {
                CleanupRuntimeMaterial();
                runtimeMaterial = baseMaterial != null && baseMaterial.shader == targetShader
                    ? new Material(baseMaterial)
                    : new Material(targetShader);
                runtimeMaterial.name = $"{gameObject.name}_FrostedGlassInstance";
                runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            else if (baseMaterial != null && baseMaterial.shader == targetShader)
            {
                runtimeMaterial.CopyPropertiesFromMaterial(baseMaterial);
            }

            runtimeMaterial.SetFloat(BlurAmountId, blurAmount);
            runtimeMaterial.SetFloat(BlurSpreadId, extraBlurSpread);
            runtimeMaterial.SetFloat(TintStrengthId, tintOpacity);
            runtimeMaterial.SetFloat(BrightnessId, backgroundBrightness);
            runtimeMaterial.SetFloat(SaturationId, backgroundSaturation);
            runtimeMaterial.SetFloat(FrostGrainId, frostGrain);
            runtimeMaterial.SetFloat(FlipYId, flipBackgroundVertically ? 1f : 0f);

            return runtimeMaterial;
        }

        private Shader ResolveFrostedShader(Material baseMaterial)
        {
            if (baseMaterial != null && baseMaterial.shader != null && baseMaterial.shader.name == FrostedShaderName)
                return baseMaterial.shader;

            if (cachedShader == null)
                cachedShader = Shader.Find(FrostedShaderName);

            return cachedShader;
        }

        private void EnsureFrostedBaseMaterial()
        {
            Graphic g = TargetGraphic;
            if (g == null) return;

            if (g.material != null && g.material.shader != null && g.material.shader.name == FrostedShaderName)
                return;

#if UNITY_EDITOR
            Material assetMat = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            if (assetMat != null)
            {
                g.material = assetMat;
                return;
            }
#endif
        }

        private void CleanupRuntimeMaterial()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(runtimeMaterial);
                else
                    DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }
        }
    }
}
