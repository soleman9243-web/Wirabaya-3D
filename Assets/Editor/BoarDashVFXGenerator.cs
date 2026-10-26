using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generator otomatis untuk Particle System saat Boss Boar Dash (chargeVfx).
/// Menyediakan tombol di menu Unity: Wirabaya > Boss Boar > Generate Dash Particle (chargeVfx)
/// </summary>
public class BoarDashVFXGenerator : EditorWindow
{
    private const string PREFAB_DIR = "Assets/Prefabs/Boar Boss";
    private const string PREFAB_PATH = "Assets/Prefabs/Boar Boss/Boar_DashTrail_VFX.prefab";
    private const string MATERIAL_PATH = "Assets/Materials/BoarDashDust_Mat.mat";
    private const string TEXTURE_PATH = "Assets/Materials/T_BoarSmokeDust.png";

    [MenuItem("Wirabaya/Boss Boar/Generate Dash Particle (chargeVfx)")]
    public static void GenerateDashVFX()
    {
        // 1. Pastikan folder tujuan ada
        if (!Directory.Exists(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
        }
        if (!Directory.Exists("Assets/Materials"))
        {
            Directory.CreateDirectory("Assets/Materials");
        }

        // 2. Dapatkan atau buat Tekstur Soft Circle & Material URP Particle Transparan
        Texture2D smokeTex = GetOrCreateSoftSmokeTexture();
        Material dustMat = GetOrCreateDustMaterial(smokeTex);

        // 3. Bangun Root GameObject VFX
        GameObject rootVfx = new GameObject("Boar_DashTrail_VFX");
        rootVfx.transform.position = Vector3.zero;
        rootVfx.transform.rotation = Quaternion.identity;

        // Sub-emitter 1: Kepulan Debu Tanah Utama (World Space)
        GameObject dustObj = new GameObject("DustTrail_Main");
        dustObj.transform.SetParent(rootVfx.transform);
        dustObj.transform.localPosition = new Vector3(0f, 0.15f, -0.3f);
        dustObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetupDustTrail(dustObj, dustMat);

        // Sub-emitter 2: Kerikil Terlempar ke Belakang (Pebble Debris)
        GameObject pebblesObj = new GameObject("GroundPebbles_Debris");
        pebblesObj.transform.SetParent(rootVfx.transform);
        pebblesObj.transform.localPosition = new Vector3(0f, 0.1f, -0.4f);
        pebblesObj.transform.localRotation = Quaternion.Euler(-15f, 180f, 0f);
        SetupGroundPebbles(pebblesObj, dustMat);

        // Sub-emitter 3: Garis Kecepatan Angin (Speed Lines / Wind Streaks)
        GameObject speedLinesObj = new GameObject("Wind_SpeedLines");
        speedLinesObj.transform.SetParent(rootVfx.transform);
        speedLinesObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        speedLinesObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetupSpeedLines(speedLinesObj, dustMat);

        // 4. Simpan sebagai Prefab di Project Assets
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootVfx, PREFAB_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[BoarDashVFX]</color> Prefab berhasil diperbarui di: {PREFAB_PATH}");

        // 5. Cek apakah ada BoarBossAI yang sedang dipilih di Hierarchy atau di Scene
        GameObject selected = Selection.activeGameObject;
        BoarBossAI bossAI = null;
        if (selected != null)
        {
            bossAI = selected.GetComponent<BoarBossAI>();
            if (bossAI == null)
            {
                bossAI = selected.GetComponentInChildren<BoarBossAI>();
            }
        }

        if (bossAI == null)
        {
            bossAI = Object.FindFirstObjectByType<BoarBossAI>();
        }

        if (bossAI != null)
        {
            // Jika bossAI sudah punya child lama "Boar_DashTrail_VFX", hapus yang lama agar bersih
            Transform oldVfx = bossAI.transform.Find("Boar_DashTrail_VFX");
            if (oldVfx != null)
            {
                Undo.DestroyObjectImmediate(oldVfx.gameObject);
            }

            // Pasang rootVfx baru sebagai child dari bossAI
            Undo.RegisterCreatedObjectUndo(rootVfx, "Attach Dash VFX to Boar Boss");
            rootVfx.transform.SetParent(bossAI.transform);
            rootVfx.transform.localPosition = new Vector3(0f, 0f, -0.3f);
            rootVfx.transform.localRotation = Quaternion.identity;

            // Sambungkan ke field chargeVfx di BoarBossAI
            Undo.RecordObject(bossAI, "Assign chargeVfx");
            bossAI.chargeVfx = rootVfx;
            EditorUtility.SetDirty(bossAI);

            // Set inactive di awal sesuai mekanisme BoarBossAI
            rootVfx.SetActive(false);

            EditorUtility.DisplayDialog(
                "Berhasil Diperbaiki!",
                $"Particle Dash (chargeVfx) telah diperbarui dengan Material Transparan & Soft Texture!\n\n" +
                $"Boss Target: '{bossAI.gameObject.name}'\n" +
                $"Lokasi Prefab: {PREFAB_PATH}\n\n" +
                "Silakan coba Play Mode dan lihat efek dash-nya sekarang!",
                "Mantap"
            );
        }
        else
        {
            DestroyImmediate(rootVfx);

            EditorUtility.DisplayDialog(
                "Prefab Berhasil Diperbarui!",
                $"Prefab Particle Dash (chargeVfx) berhasil diperbarui di:\n{PREFAB_PATH}\n\n" +
                "Material telah diperbaiki menjadi Transparent dengan Soft Smoke Texture.",
                "OK"
            );
        }

        EditorGUIUtility.PingObject(prefabAsset);
    }

    private static Texture2D GetOrCreateSoftSmokeTexture()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
        if (tex != null) return tex;

        int size = 128;
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / maxRadius);

                // Smoothstep radial falloff: 1 di tengah, 0 di tepi luar
                float alpha = Mathf.SmoothStep(1f, 0f, norm);
                alpha = Mathf.Pow(alpha, 1.4f);

                // RGB murni putih (1,1,1), variasi hanya di channel Alpha
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(TEXTURE_PATH, bytes);
        AssetDatabase.ImportAsset(TEXTURE_PATH, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
    }

    private static Material GetOrCreateDustMaterial(Texture2D smokeTex)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MATERIAL_PATH);
        }

        mat.name = "BoarDashDust_Mat";

        // Konfigurasi KUNCI URP Transparent
        mat.SetFloat("_Surface", 1f); // 1 = Transparent
        mat.SetFloat("_Blend", 0f);   // 0 = Alpha Blend
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);     // Jangan timpa Z-buffer
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Keywords URP
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.EnableKeyword("_BLENDMODE_ALPHA");
        mat.EnableKeyword("_SOFTPARTICLES_ON");

        // Warna debu pasir/tanah kartun lembut
        Color dustColor = new Color(0.85f, 0.78f, 0.65f, 0.85f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", dustColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", dustColor);

        // Pasang tekstur partikel soft
        if (smokeTex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", smokeTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", smokeTex);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SetupDustTrail(GameObject go, Material mat)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        // Main Module
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // KUNCI: Debu tertinggal di tanah
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.maxParticles = 250;

        // Warna debu pasir/tanah kartun
        main.startColor = new Color(0.82f, 0.75f, 0.62f, 0.75f);

        // Emission Module
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 15f;
        emission.rateOverDistance = 12f; // Semakin cepat lari, semakin padat debu

        // Shape Module (Box tipis di area kaki belakang babi)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.2f, 0.2f, 0.6f);

        // Size over Lifetime (Membesar saat membubung lalu pudar)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(0.35f, 1.0f);
        sizeCurve.AddKey(1f, 1.5f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over Lifetime (Pudar halus dari 0.75 ke 0 alpha)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.85f, 0.78f, 0.65f), 0f),
                new GradientColorKey(new Color(0.72f, 0.64f, 0.52f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.75f, 0f),
                new GradientAlphaKey(0.55f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        // Rotation over Lifetime (Berputar acak dinamis)
        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);
    }

    private static void SetupGroundPebbles(GameObject go, Material mat)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.6f; // Kerikil jatuh ke tanah
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.48f, 0.40f, 0.32f, 0.9f);
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 8f;
        emission.rateOverDistance = 8f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.3f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.48f, 0.40f, 0.32f), 0f),
                new GradientColorKey(new Color(0.38f, 0.30f, 0.24f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;
    }

    private static void SetupSpeedLines(GameObject go, Material mat)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch; // Memanjang mengikuti arah kecepatan
        renderer.lengthScale = 3f;
        renderer.velocityScale = 0.05f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // Mengikuti boss untuk efek aura
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor = new Color(1f, 1f, 1f, 0.25f);
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 20f;
        emission.rateOverDistance = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.6f, 0.8f, 0.5f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.25f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;
    }
}
