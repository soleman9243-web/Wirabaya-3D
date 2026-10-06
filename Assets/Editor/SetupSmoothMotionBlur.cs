#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wirabaya.Rendering;

namespace Wirabaya.Editor
{
    [InitializeOnLoad]
    public static class SetupSmoothMotionBlur
    {
        [MenuItem("Wirabaya/🎥 Pasang Smooth Motion Blur (Karakter Bebas Blur)", priority = 200)]
        public static void InstallFeature()
        {
            // Ambil Desktop Renderer
            string rendererPath = "Assets/URP/Settings/Desktop Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);

            if (rendererData == null)
            {
                Debug.LogError($"[SmoothMotionBlur] Tidak menemukan {rendererPath}");
                return;
            }

            // Cek apakah sudah terpasang
            for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                if (rendererData.rendererFeatures[i] is SmoothMotionBlurFeature)
                {
                    Debug.Log("[SmoothMotionBlur] Smooth Motion Blur Feature SUDAH terpasang di Desktop Renderer!");
                    EditorUtility.DisplayDialog("Smooth Motion Blur", "Smooth Motion Blur Feature sudah aktif di Desktop Renderer!", "OK");
                    return;
                }
            }

            // Buat instance feature baru sebagai sub-asset di rendererData
            SmoothMotionBlurFeature feature = ScriptableObject.CreateInstance<SmoothMotionBlurFeature>();
            feature.name = "SmoothMotionBlurFeature";
            feature.settings.enabled = true;
            feature.settings.shutter = 0.6f;
            feature.settings.maxBlur = 0.035f;
            feature.settings.samples = 24;

            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
            rendererData.SetDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[SmoothMotionBlur] BERHASIL menambahkan SmoothMotionBlurFeature ke Desktop Renderer!</color>");
            EditorUtility.DisplayDialog("Smooth Motion Blur Berhasil Dipasang", 
                "Smooth Motion Blur Feature berhasil ditambahkan ke Desktop Renderer!\n\n" +
                "Karakter (layer Player) sekarang 100% bebas dari blur, dan latar belakang bergerak halus ala game AAA.", 
                "Mantap!");
        }

        [MenuItem("Wirabaya/🎥 Hapus Smooth Motion Blur (Kembalikan Semula)", priority = 201)]
        public static void RemoveFeature()
        {
            string rendererPath = "Assets/URP/Settings/Desktop Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);

            if (rendererData == null) return;

            bool found = false;
            for (int i = rendererData.rendererFeatures.Count - 1; i >= 0; i--)
            {
                if (rendererData.rendererFeatures[i] is SmoothMotionBlurFeature)
                {
                    var f = rendererData.rendererFeatures[i];
                    rendererData.rendererFeatures.RemoveAt(i);
                    Object.DestroyImmediate(f, true);
                    found = true;
                }
            }

            if (found)
            {
                rendererData.SetDirty();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=yellow>[SmoothMotionBlur] Fitur Smooth Motion Blur telah dilepas dari Desktop Renderer.</color>");
                EditorUtility.DisplayDialog("Smooth Motion Blur", "Smooth Motion Blur berhasil dinonaktifkan dan dihapus dari Desktop Renderer.", "OK");
            }
            else
            {
                Debug.Log("[SmoothMotionBlur] Tidak ada Smooth Motion Blur Feature di Desktop Renderer.");
            }
        }
    }
}
#endif
