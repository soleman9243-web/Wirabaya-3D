using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;

namespace Wirabaya.EditorTools
{
    /// <summary>
    /// ToonLightingSetup: Otomasi konfigurasi pencahayaan Toon Anime / Ghibli Style.
    /// - Menyesuaikan sudut rotasi Directional Light (matahari) agar menyinari karakter dan lanskap dari atas-depan (X: 42, Y: 135, Z: 0).
    /// - Mengatur Ambient Lighting ke mode Gradient (Trilight) dengan warna Sky (Biru Langit), Equator (Warm Peach), Ground (Warm Meadow).
    /// - Menyediakan menu 1-klik untuk menerapkan dan mengembalikan (revert) ke nilai semula kapan saja.
    /// </summary>
    public static class ToonLightingSetup
    {

        [MenuItem("Tools/Wirabaya/1. Terapkan Lighting Toon Anime")]
        public static void ApplyToonLightingMenu()
        {
            ApplyToonLighting(true);
        }

        public static void ApplyToonLighting(bool showNotification)
        {
            // 1. Sesuaikan Directional Light (Matahari)
            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }

            if (sun != null)
            {
                Undo.RecordObject(sun.transform, "Adjust Sun Angle Toon");
                sun.transform.rotation = Quaternion.Euler(42f, 135f, 0f);
                EditorUtility.SetDirty(sun.gameObject);
                Debug.Log("[ToonLighting] Directional Light disesuaikan ke rotasi Euler (42, 135, 0).");
            }
            else
            {
                Debug.LogWarning("[ToonLighting] Directional Light tidak ditemukan di active scene!");
            }

            // 2. RenderSettings Ambient Gradient (Trilight)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.627451f, 0.8235294f, 1.0f, 1.0f);     // #A0D2FF (Biru Langit Sejuk)
            RenderSettings.ambientEquatorColor = new Color(1.0f, 0.88235295f, 0.74509805f, 1.0f); // #FFE1BE (Warm Peach Anime Glow)
            RenderSettings.ambientGroundColor = new Color(0.43137255f, 0.5294118f, 0.3137255f, 1.0f); // #6E8750 (Fresh Meadow Bounce)
            RenderSettings.subtractiveShadowColor = new Color(0.65f, 0.62f, 0.60f, 1.0f);

            // 3. Mark Scene Dirty & Save
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("<color=#55FF55><b>[ToonLighting] SUKSES!</b> Pencahayaan Toon Anime & Ambient Gradient berhasil diterapkan ke scene!</color>");

            if (showNotification)
            {
                EditorUtility.DisplayDialog("Toon Lighting Setup", "Pencahayaan Toon Anime berhasil diterapkan!\n\n- Rotasi Matahari: Euler (42, 135, 0)\n- Ambient Mode: Gradient (Trilight)\n- Sky Color: Soft Blue\n- Equator Color: Warm Peach\n- Ground Color: Warm Meadow", "OK");
            }
        }

        [MenuItem("Tools/Wirabaya/2. Kembalikan Lighting Semula (Backup)")]
        public static void RevertToonLighting()
        {
            // Revert Directional Light
            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }

            if (sun != null)
            {
                Undo.RecordObject(sun.transform, "Revert Sun Angle");
                sun.transform.rotation = Quaternion.Euler(8f, -68f, -17f);
                EditorUtility.SetDirty(sun.gameObject);
            }

            // Revert RenderSettings
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f, 1f);
            RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f, 1f);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[ToonLighting] Pencahayaan berhasil dikembalikan ke nilai semula (Backup).");
            EditorUtility.DisplayDialog("Toon Lighting Setup", "Pencahayaan berhasil dikembalikan ke nilai semula!", "OK");
        }
    }
}
