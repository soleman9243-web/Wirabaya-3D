using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float damage = 10f;
    private Rigidbody rb;
    private Collider col;
    private bool isStuck = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        // Buat massa panah menjadi sangat ringan (nyaris nol) 
        // agar tidak mementalkan Player saat terjadi tabrakan fisik.
        if (rb != null) rb.mass = 0.001f; 
    }

    void Update()
    {
        // Membuat ujung panah selalu menukik ke bawah mengikuti gravitasi
        if (!isStuck && rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck) return; 

        // 1. Kena Player
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerStatus playerStatus = collision.gameObject.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                playerStatus.TakeDamage(damage);
            }
            
            // Panah langsung hilang kalau kena player
            Destroy(gameObject);
        }
        // 2. Kena Tanah/Tembok (Meleset)
        else 
        {
            StickToSurface(collision.transform);
        }
    }

    void StickToSurface(Transform surface)
    {
        isStuck = true;
        
        // Hentikan fisika agar panah nancap dan tidak jatuh lagi
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Matikan collider agar player tidak tersandung panah
        if (col != null)
        {
            col.enabled = false;
        }

        // Parent ke objek yang ditabrak
        transform.SetParent(surface);
    }

    // Fungsi Unity: Dipanggil saat panah TIDAK LAGI disorot oleh kamera manapun
    void OnBecameInvisible()
    {
        // Kalau panahnya lagi nancap dan player/kamera memalingkan pandangan, hapus!
        if (isStuck)
        {
            Destroy(gameObject);
        }
    }
}
