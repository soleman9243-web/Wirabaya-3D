using UnityEngine;
using System.Collections.Generic;

public class DashTelegraphLight : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashLength = 15f; 
    public float dashWidth = 3f;   
    
    [Tooltip("Pilih material berwarna merah transparan. Jika kosong, script akan mencoba membuat otomatis.")]
    public Material customMaterial;

    public Color outlineColor = new Color(1f, 0f, 0f, 0.2f);
    public Color fillColor = new Color(1f, 0.2f, 0.2f, 0.8f);

    [Header("Tracking")]
    public float yOffset = 0.5f;
    public float terrainRayHeight = 10f;
    public float terrainRayDistance = 20f;

    private Transform trackTarget;
    
    private GameObject outlineObj;
    private Mesh outlineMesh;
    
    private GameObject fillObj;
    private Mesh fillMesh;

    private bool isFilling = false;
    private float fillDuration;
    private float fillElapsed;

    public void Initialize(Transform target = null)
    {
        this.trackTarget = target;
        
        outlineObj = CreateMeshObject("OutlineMesh", outlineColor, out outlineMesh);
        fillObj = CreateMeshObject("FillMesh", fillColor, out fillMesh);
        
        BuildRectMesh(outlineMesh, dashLength, 0f);
        BuildRectMesh(fillMesh, 0.1f, 0.02f); // Fill awal
        
        Debug.Log("Dash Telegraph Mesh Initialized!");
    }

    private GameObject CreateMeshObject(string objName, Color color, out Mesh mesh)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        mf.mesh = mesh;

        if (customMaterial != null)
        {
            mr.material = customMaterial;
            mr.material.color = color;
            if (mr.material.HasProperty("_BaseColor")) mr.material.SetColor("_BaseColor", color);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit"); 
            if (shader == null) shader = Shader.Find("Unlit/Color"); 

            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mr.material = mat;
        }

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return obj;
    }

    public void StartFill(float duration)
    {
        this.fillDuration = duration;
        this.fillElapsed = 0f;
        this.isFilling = true;
    }

    private void Update()
    {
        if (trackTarget != null && !isFilling)
        {
            Vector3 targetPos = trackTarget.position;
            Vector3 euler = trackTarget.eulerAngles;
            transform.position = targetPos;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        if (isFilling)
        {
            fillElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(fillElapsed / fillDuration);
            
            float currentLength = Mathf.Lerp(0.1f, dashLength, t);
            
            BuildRectMesh(fillMesh, currentLength, 0.02f);
            BuildRectMesh(outlineMesh, dashLength, 0f); // Update contour

            if (fillElapsed >= fillDuration)
            {
                isFilling = false;
            }
        }
        else if (trackTarget != null)
        {
            BuildRectMesh(outlineMesh, dashLength, 0f);
        }
    }

    private void BuildRectMesh(Mesh mesh, float length, float extraYOffset)
    {
        if (mesh == null) return;

        int segments = Mathf.CeilToInt(length); 
        if (segments < 1) segments = 1;
        
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float z = (i / (float)segments) * length;
            
            Vector3 leftLocal = new Vector3(-dashWidth / 2f, 0, z);
            Vector3 rightLocal = new Vector3(dashWidth / 2f, 0, z);

            float yLeft = GetLocalTerrainHeight(leftLocal) + extraYOffset;
            float yRight = GetLocalTerrainHeight(rightLocal) + extraYOffset;

            vertices[i * 2] = new Vector3(leftLocal.x, yLeft, z);      // Kiri
            vertices[i * 2 + 1] = new Vector3(rightLocal.x, yRight, z); // Kanan

            if (i < segments)
            {
                int v = i * 2;
                int t = i * 6;

                // Segitiga menghadap atas
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private float GetLocalTerrainHeight(Vector3 localPos)
    {
        Vector3 worldPos = transform.TransformPoint(localPos);
        return GetWorldTerrainHeight(worldPos) - transform.position.y + yOffset;
    }

    private float GetWorldTerrainHeight(Vector3 worldPos)
    {
        worldPos.y += terrainRayHeight; 
        RaycastHit[] hits = Physics.RaycastAll(worldPos, Vector3.down, terrainRayDistance);
        float highest = -9999f;
        
        foreach (var h in hits)
        {
            if (h.collider.isTrigger) continue;
            
            int layer = h.collider.gameObject.layer;
            // Blokir total layer Player, Enemy, Boss agar tidak memanjat karakter
            if (layer == LayerMask.NameToLayer("Player") || 
                layer == LayerMask.NameToLayer("Enemy") || 
                layer == LayerMask.NameToLayer("Boss")) 
            {
                continue;
            }
            
            if (h.point.y > highest) highest = h.point.y;
        }
        
        if (highest > -9999f) return highest;
        
        if (trackTarget != null) return trackTarget.position.y;
        return worldPos.y - terrainRayHeight;
    }
}
