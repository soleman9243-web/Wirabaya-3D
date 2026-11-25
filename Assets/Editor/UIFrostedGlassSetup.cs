using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Wirabaya.Rendering;
using Wirabaya.UI;

namespace Wirabaya.EditorTools
{
    public static class UIFrostedGlassSetup
    {
        private const string FrostedShaderName = "Wirabaya/UI/FrostedGlass";
        private const string BackgroundBlurShaderName = "Hidden/Wirabaya/UIBackgroundBlur";
        private const string MaterialAssetPath = "Assets/Materials/UI_FrostedGlass.mat";

        private static readonly string[] TargetRendererPaths =
        {
            "Assets/URP/Settings/Desktop Renderer.asset",
            "Assets/URP/Settings/Desktop Low Renderer.asset",
            "Assets/URP/Settings/Mobile High Renderer.asset",
            "Assets/URP/Settings/Mobile Medium Renderer.asset",
            "Assets/URP/Settings/Mobile Low Renderer.asset"
        };

        [MenuItem("Wirabaya/UI/Setup Frosted Glass Blur")]
        public static void RunMenuSetup()
        {
            string report = ApplySetup();
            Debug.Log("[UIFrostedGlassSetup] " + report);
        }

        public static string ApplySetup()
        {
            Shader uiShader = Shader.Find(FrostedShaderName);
            Shader blurShader = Shader.Find(BackgroundBlurShaderName);

            var errors = new List<string>();
            CheckShaderErrors(uiShader, FrostedShaderName, errors);
            CheckShaderErrors(blurShader, BackgroundBlurShaderName, errors);

            if (uiShader == null || blurShader == null)
            {
                return BuildJson(false, null, 0, null, errors);
            }

            // 1. Pastikan Assets/Materials/UI_FrostedGlass.mat tersedia
            if (!Directory.Exists("Assets/Materials"))
            {
                Directory.CreateDirectory("Assets/Materials");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            if (mat == null)
            {
                mat = new Material(uiShader) { name = "UI_FrostedGlass" };
                AssetDatabase.CreateAsset(mat, MaterialAssetPath);
            }
            else if (mat.shader != uiShader)
            {
                mat.shader = uiShader;
                EditorUtility.SetDirty(mat);
            }

            // 2. Pasang UIFrostedGlassFeature ke URP Renderer asset aktif
            int addedFeatureCount = 0;
            foreach (string rendererPath in TargetRendererPaths)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (rendererData == null) continue;

                UIFrostedGlassFeature existing = null;
                foreach (var f in rendererData.rendererFeatures)
                {
                    if (f is UIFrostedGlassFeature fg)
                    {
                        existing = fg;
                        break;
                    }
                }

                if (existing == null)
                {
                    var feature = ScriptableObject.CreateInstance<UIFrostedGlassFeature>();
                    feature.name = "UIFrostedGlassFeature";
                    feature.settings.enabled = true;
                    feature.settings.blurShader = blurShader;
                    feature.settings.passEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    feature.settings.globalBlurSpread = 2.0f;
                    feature.settings.iterations = 3;

                    AssetDatabase.AddObjectToAsset(feature, rendererData);
                    rendererData.rendererFeatures.Add(feature);
                    rendererData.SetDirty();
                    EditorUtility.SetDirty(rendererData);
                    feature.Create();
                    addedFeatureCount++;
                }
                else
                {
                    if (existing.settings.blurShader != blurShader)
                    {
                        existing.settings.blurShader = blurShader;
                        EditorUtility.SetDirty(existing);
                    }
                    existing.SetActive(true);
                    existing.settings.enabled = true;
                    existing.Create();
                }
            }

            // 3. Pasang komponen UIFrostedGlass pada backdrop Settings UI (sETTINGS -> Image)
            string targetApplied = "none";
            var settingsCtrl = Object.FindFirstObjectByType<SettingsMenuController>(FindObjectsInactive.Include);
            if (settingsCtrl != null)
            {
                Transform backdropTr = settingsCtrl.transform.Find("Image");
                if (backdropTr != null)
                {
                    Image img = backdropTr.GetComponent<Image>();
                    if (img != null)
                    {
                        Undo.RecordObject(img, "Apply Frosted Glass Material");
                        img.material = mat;

                        // Pastikan alpha Image = 1 agar CanvasGroup.alpha mengatur fade,
                        // dan tidak terjadi double-vision dengan background tajam di belakang kaca.
                        Color c = img.color;
                        if (c.a < 0.99f)
                        {
                            c.a = 1f;
                            img.color = c;
                        }
                        EditorUtility.SetDirty(img);

                        UIFrostedGlass frosted = backdropTr.GetComponent<UIFrostedGlass>();
                        if (frosted == null)
                        {
                            frosted = Undo.AddComponent<UIFrostedGlass>(backdropTr.gameObject);
                        }
                        frosted.enabled = true;
                        frosted.MarkMaterialDirty();
                        EditorUtility.SetDirty(frosted);
                        EditorSceneManager.MarkSceneDirty(settingsCtrl.gameObject.scene);
                        targetApplied = backdropTr.parent.name + "/" + backdropTr.name;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return BuildJson(errors.Count == 0, MaterialAssetPath, addedFeatureCount, targetApplied, errors);
        }

        private static void CheckShaderErrors(Shader shader, string name, List<string> errors)
        {
            if (shader == null)
            {
                errors.Add($"Shader '{name}' tidak ditemukan.");
                return;
            }

            var msgs = ShaderUtil.GetShaderMessages(shader);
            if (msgs != null)
            {
                foreach (var msg in msgs)
                {
                    if (msg.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    {
                        errors.Add($"{name} line {msg.line}: {msg.message}");
                    }
                }
            }
        }

        private static string BuildJson(bool ok, string matPath, int featuresAdded, string target, List<string> errors)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"status\":\"{(ok ? "ok" : "error")}\",");
            sb.Append($"\"material\":\"{Escape(matPath ?? "")}\",");
            sb.Append($"\"rendererFeaturesAdded\":{featuresAdded},");
            sb.Append($"\"appliedTarget\":\"{Escape(target ?? "none")}\",");
            sb.Append("\"errors\":[");
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"{Escape(errors[i])}\"");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string Escape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
