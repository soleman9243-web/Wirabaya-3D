using UnityEngine;
using UnityEditor;
using Cinemachine;

#if UNITY_EDITOR
public class SetupSnapTrap : Editor
{
    [MenuItem("Tools/Setup Crocodile Snap Trap Skill")]
    public static void CreateSnapTrap()
    {
        string vfxPath = "Assets/Prefabs/SnapTrap_VFX.prefab";
        string playerPath = "Assets/Prefabs/PlayerArmature.prefab";

        // --- 1. Buat Material ---
        if (!System.IO.Directory.Exists("Assets/Materials"))
        {
            System.IO.Directory.CreateDirectory("Assets/Materials");
        }
        
        Material jawMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/JawMaterial.mat");
        if (jawMat == null)
        {
            jawMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            jawMat.color = new Color(0.1f, 0.3f, 0.1f); // Hijau buaya gelap
            AssetDatabase.CreateAsset(jawMat, "Assets/Materials/JawMaterial.mat");
        }
        
        Material toothMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ToothMaterial.mat");
        if (toothMat == null)
        {
            toothMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            toothMat.color = new Color(0.9f, 0.9f, 0.8f); // Putih tulang
            AssetDatabase.CreateAsset(toothMat, "Assets/Materials/ToothMaterial.mat");
        }

        // --- 2. Bangun 3D Model Rahang dari nol ---
        GameObject root = new GameObject("SnapTrap_VFX");
        CrocodileTrap trapScript = root.AddComponent<CrocodileTrap>();
        trapScript.damage = 50f;
        trapScript.stunDuration = 2f;
        trapScript.trapRadius = 2.5f;

        // PIVOT KIRI
        GameObject pivotLeft = new GameObject("Pivot_Left");
        pivotLeft.transform.SetParent(root.transform);
        pivotLeft.transform.localPosition = new Vector3(-1f, 0, 0);
        
        GameObject leftJawBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftJawBase.name = "Jaw_Base";
        leftJawBase.transform.SetParent(pivotLeft.transform);
        leftJawBase.transform.localPosition = new Vector3(0.5f, 1f, 0); // Di atas pivot
        leftJawBase.transform.localScale = new Vector3(0.5f, 2f, 2f);
        leftJawBase.GetComponent<MeshRenderer>().sharedMaterial = jawMat;
        DestroyImmediate(leftJawBase.GetComponent<BoxCollider>()); // Matikan collision bawaan

        // Gigi kiri
        for(int i = 0; i < 3; i++)
        {
            GameObject tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tooth.name = "Tooth_" + i;
            tooth.transform.SetParent(leftJawBase.transform);
            float zPos = -0.6f + (i * 0.6f);
            tooth.transform.localPosition = new Vector3(0.5f, 0.5f, zPos); // Nancap di ujung rahang
            tooth.transform.localScale = new Vector3(1f, 0.4f, 0.2f);
            // Rotasi agar ujung gigi runcing ke dalam
            tooth.transform.localRotation = Quaternion.Euler(0, 0, -45f);
            tooth.GetComponent<MeshRenderer>().sharedMaterial = toothMat;
            DestroyImmediate(tooth.GetComponent<BoxCollider>());
        }

        // PIVOT KANAN
        GameObject pivotRight = new GameObject("Pivot_Right");
        pivotRight.transform.SetParent(root.transform);
        pivotRight.transform.localPosition = new Vector3(1f, 0, 0);
        
        GameObject rightJawBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightJawBase.name = "Jaw_Base";
        rightJawBase.transform.SetParent(pivotRight.transform);
        rightJawBase.transform.localPosition = new Vector3(-0.5f, 1f, 0); 
        rightJawBase.transform.localScale = new Vector3(0.5f, 2f, 2f);
        rightJawBase.GetComponent<MeshRenderer>().sharedMaterial = jawMat;
        DestroyImmediate(rightJawBase.GetComponent<BoxCollider>());

        // Gigi kanan
        for(int i = 0; i < 3; i++)
        {
            GameObject tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tooth.name = "Tooth_" + i;
            tooth.transform.SetParent(rightJawBase.transform);
            float zPos = -0.6f + (i * 0.6f);
            tooth.transform.localPosition = new Vector3(-0.5f, 0.5f, zPos);
            tooth.transform.localScale = new Vector3(1f, 0.4f, 0.2f);
            tooth.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            tooth.GetComponent<MeshRenderer>().sharedMaterial = toothMat;
            DestroyImmediate(tooth.GetComponent<BoxCollider>());
        }

        // Wiring trapScript
        trapScript.leftJaw = pivotLeft.transform;
        trapScript.rightJaw = pivotRight.transform;

        // Simpan sebagai Prefab
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
        }
        
        GameObject savedVfx = PrefabUtility.SaveAsPrefabAsset(root, vfxPath);
        DestroyImmediate(root);
        Debug.Log("✅ SnapTrap_VFX Prefab berhasil dibuat (beserta 3D model rahangnya)!");

        // --- 3. Inject ke Player ---
        GameObject playerPrefab = PrefabUtility.LoadPrefabContents(playerPath);
        if (playerPrefab != null)
        {
            SnapTrapSkill skill = playerPrefab.GetComponent<SnapTrapSkill>();
            if (skill == null)
            {
                skill = playerPrefab.AddComponent<SnapTrapSkill>();
                skill.animator = playerPrefab.GetComponent<Animator>();
                skill.playerControl = playerPrefab.GetComponentInChildren<PlayerControl>();
                skill.impulseSource = playerPrefab.GetComponentInChildren<CinemachineImpulseSource>();
                skill.trapPrefab = savedVfx;
            }
            else
            {
                skill.trapPrefab = savedVfx;
            }
            
            PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPath);
            PrefabUtility.UnloadPrefabContents(playerPrefab);
            Debug.Log("✅ PlayerArmature Prefab berhasil diupdate dengan SnapTrapSkill!");
        }
        else
        {
            Debug.LogError("❌ Gagal menemukan PlayerArmature.prefab!");
        }

        Debug.Log("🎉 SETUP CROCODILE SNAP TRAP SELESAI!");
    }
}
#endif
