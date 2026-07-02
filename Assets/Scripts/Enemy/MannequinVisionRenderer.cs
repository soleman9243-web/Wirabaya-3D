using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MannequinVisionRenderer : MonoBehaviour
{
    public EnemyAI enemyAI;
    public bool showVision = true;
    public int segments = 30; // Kualitas melingkar
    public Color visionColor = new Color(1f, 0f, 1f, 0.5f);
    public float yOffset = 0.05f; // Diangkat sedikit dari lantai

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Buat material unlit transparent (ungu)
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = visionColor;
        meshRenderer.material = mat;

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        if (enemyAI == null)
            enemyAI = GetComponentInParent<EnemyAI>();
    }

    private void Update()
    {
        bool shouldShow = showVision;
        if (enemyAI != null && (enemyAI.isAlerted || enemyAI.isDead || enemyAI.isBeingTakenDown))
        {
            shouldShow = false;
        }

        meshRenderer.enabled = shouldShow;
        if (shouldShow && enemyAI != null)
        {
            UpdateVisionMesh();
        }
    }

    private void UpdateVisionMesh()
    {
        float angle = enemyAI.visionAngle;
        float maxRadius = enemyAI.visionRange;

        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        // Titik pusat (kaki musuh)
        vertices[0] = new Vector3(0, yOffset, 0);

        float currentAngle = -angle / 2f;
        float angleIncrement = angle / segments;

        Vector3 eyePos = transform.position + Vector3.up * 1f;

        for (int i = 0; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * currentAngle;
            Vector3 localDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            Vector3 worldDir = transform.TransformDirection(localDir);

            float actualRadius = maxRadius;

            // 1. HORIZONTAL RAYCAST: Cek tembok
            if (Physics.Raycast(eyePos, worldDir, out RaycastHit obstacleHit, maxRadius, enemyAI.obstacleLayer))
            {
                actualRadius = obstacleHit.distance;
            }

            vertices[i + 1] = localDir * actualRadius;
            vertices[i + 1].y = yOffset; // Selalu datar
            
            currentAngle += angleIncrement;

            if (i < segments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
