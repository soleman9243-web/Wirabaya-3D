using UnityEngine;

/// <summary>
/// Indikator telegraph lingkaran menggunakan SPOT LIGHT.
/// Sepenuhnya tanpa objek 3D (mesh), hanya memanfaatkan pantulan cahaya di atas tanah.
/// </summary>
public class TelegraphIndicator : MonoBehaviour
{
    [Header("Spotlight Settings")]
    [Tooltip("Ketinggian lampu dari tanah. Makin tinggi makin luas area yang bisa disorot.")]
    public float lightHeight = 15f; 
    public Color outlineColor = new Color(1f, 0f, 0f, 0.4f);
    public Color fillColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float lightIntensity = 25f;

    [Tooltip("Layer mana saja yang akan disinari. (Otomatis akan membuang layer Player jika dibiarkan Default)")]
    public LayerMask lightCullingMask = ~0; // Default Everything

    [Header("Tracking")]
    public float yOffset = 0f;

    // Internal
    private float targetRadius;
    private Transform trackTarget;
    private float lockTimeBeforeImpact;
    
    private Light bgLight;
    private Light fillLight;

    // State
    private bool isFilling = false;
    private float fillDuration;
    private float fillElapsed;
    private bool isLocked = false;

    private void Awake()
    {
        // OTOMATIS membuang layer "Player" dan "Enemy" dari culling mask agar cahaya menembus tubuh karakter
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        
        if (playerLayer != -1) lightCullingMask &= ~(1 << playerLayer);
        if (enemyLayer != -1) lightCullingMask &= ~(1 << enemyLayer);
    }

    public void Initialize(float radius, Transform target = null, float lockTime = 0.3f)
    {
        this.targetRadius = radius;
        this.trackTarget = target;
        this.lockTimeBeforeImpact = lockTime;
        this.isLocked = false;

        // Background Spotlight (Batas Luar)
        bgLight = CreateSpotLight("BgLight", outlineColor, radius);
        
        // Fill Spotlight (Mengembang dari dalam)
        fillLight = CreateSpotLight("FillLight", fillColor, 0.01f);
    }

    private Light CreateSpotLight(string name, Color color, float rad)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(transform);
        
        // Taruh di atas menghadap lurus ke bawah
        lightObj.transform.localPosition = new Vector3(0f, lightHeight, 0f);
        lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Spot; // KEMBALI KE SPOTLIGHT
        l.color = color;
        l.intensity = lightIntensity;
        l.range = lightHeight + 10f; // Jangkauan cahaya sampai menembus tanah
        l.spotAngle = RadiusToSpotAngle(rad);
        
        l.cullingMask = lightCullingMask;
        
        // Supaya ringan dan pinggirannya tajam (URP/HDRP support)
        l.shadows = LightShadows.None;
        l.innerSpotAngle = l.spotAngle * 0.95f; 

        return l;
    }

    private float RadiusToSpotAngle(float r)
    {
        // Rumus Trigonometri: Mencari sudut cone berdasarkan radius alas dan tinggi
        return 2f * Mathf.Atan(r / lightHeight) * Mathf.Rad2Deg;
    }

    public void StartFill(float duration)
    {
        this.fillDuration = duration;
        fillElapsed = 0f;
        isFilling = true;
        isLocked = false;
    }

    public Vector3 GetTargetPosition()
    {
        // Karena parent-nya selalu di tanah, ini tetap akurat untuk target jatuhnya batu
        return transform.position;
    }

    private void Update()
    {
        // 1. TRACKING
        if (trackTarget != null && !isLocked)
        {
            Vector3 targetPos = trackTarget.position;
            
            // 1 Raycast untuk menempelkan 'root' ke tanah (supaya radius angle lampu akurat)
            RaycastHit[] hits = Physics.RaycastAll(targetPos + Vector3.up * 2f, Vector3.down, 5f);
            float highestGround = -9999f;
            foreach (var h in hits)
            {
                if (h.collider.CompareTag("Player") || h.collider.isTrigger) continue;
                if (h.point.y > highestGround) highestGround = h.point.y;
            }
            if (highestGround > -9999f) targetPos.y = highestGround;
            
            targetPos.y += yOffset;
            
            transform.position = targetPos;
            transform.rotation = Quaternion.identity; // Harus selalu rata
        }

        // 2. FILLING (Animasi membesar)
        if (!isFilling) return;

        fillElapsed += Time.deltaTime;
        float remaining = fillDuration - fillElapsed;

        // Kunci posisi di detik-detik akhir sebelum batu jatuh
        if (!isLocked && remaining <= lockTimeBeforeImpact)
        {
            isLocked = true;
        }

        // Hitung radius saat ini (ease-in curve)
        float t = Mathf.Clamp01(fillElapsed / fillDuration);
        float easedT = t * t; 
        float currentRadius = Mathf.Lerp(0f, targetRadius, easedT);

        // Update spotlight
        if (fillLight != null)
        {
            fillLight.spotAngle = RadiusToSpotAngle(currentRadius);
            fillLight.innerSpotAngle = fillLight.spotAngle * 0.95f; // Pertahankan ketajaman tepi
        }

        if (fillElapsed >= fillDuration)
        {
            isFilling = false;
        }
    }
}
