using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class DroppedItem : MonoBehaviour
{
    public ItemData itemData;
    public int amount = 1;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;

    [Header("Pickup Settings")]
    [Tooltip("Radius jarak maksimal player bisa mengambil item ini")]
    public float pickupRadius = 2.5f;
    
    [Header("Visuals")]
    [Tooltip("Titik pusat untuk model item. Biasakan buat child object kosong bernama 'Visuals' lalu taruh model 3D di dalamnya.")]
    public Transform visualsContainer;

    private bool isBeingSucked = false;
    private Transform playerTarget;
    private PlayerItemController playerItemController;

    private float startYPos;
    private List<Transform> spawnedVisuals = new List<Transform>();

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        // Buat SphereCollider otomatis khusus untuk mendeteksi area pickup
        SphereCollider triggerCol = gameObject.AddComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius = pickupRadius;

        // BoxCollider bawaan JANGAN di-set isTrigger=true, 
        // biarkan berfungsi murni sebagai collider fisik agar item tidak tembus tanah.
    }

    private void Start()
    {
        // Beri sedikit dorongan ke atas saat pertama kali spawn (seperti Minecraft)
        if (itemData != null && !isBeingSucked)
        {
            rb.AddForce(Vector3.up * itemData.bounceForce, ForceMode.Impulse);
            
            // Beri dorongan acak sedikit ke samping agar menyebar jika spawn banyak
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            rb.AddForce(randomDir * (itemData.bounceForce * 0.5f), ForceMode.Impulse);
        }

        startYPos = transform.position.y;
        SetupVisualStack();
    }

    private void Update()
    {
        if (isBeingSucked && playerTarget != null)
        {
            HandleSuckAnimation();
            return;
        }

        if (itemData == null || visualsContainer == null) return;

        // Animasi Muter dan Naik Turun (Minecraft style)
        // Kita memutar dan menggerakkan container visualnya, BUKAN physics object utamanya
        visualsContainer.Rotate(Vector3.up, itemData.rotationSpeed * Time.deltaTime, Space.World);

        // Hanya float jika benda sudah menyentuh tanah (kecepatan y mendekati 0)
        if (Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        {
            float newY = Mathf.Sin(Time.time * itemData.floatSpeed) * itemData.floatAmplitude;
            visualsContainer.localPosition = new Vector3(visualsContainer.localPosition.x, newY + 0.5f, visualsContainer.localPosition.z);
        }
    }

    private void SetupVisualStack()
    {
        if (visualsContainer == null || visualsContainer.childCount == 0) return;

        // Ambil visual utama (model pertama yang ada di dalam container)
        Transform baseVisual = visualsContainer.GetChild(0);
        spawnedVisuals.Add(baseVisual);

        // Jika jumlahnya > 1, kita akan menduplikasi visualnya agar kelihatan bertumpuk
        int visualCount = Mathf.Min(amount, 5); // Maksimal 5 tumpukan visual agar tidak berat
        
        for (int i = 1; i < visualCount; i++)
        {
            Transform newVisual = Instantiate(baseVisual.gameObject, visualsContainer).transform;
            
            // Berikan offset posisi dan rotasi sedikit
            float offsetX = Random.Range(-0.15f, 0.15f);
            float offsetZ = Random.Range(-0.15f, 0.15f);
            float offsetRotY = Random.Range(0, 360f);

            newVisual.localPosition = baseVisual.localPosition + new Vector3(offsetX, i * 0.05f, offsetZ);
            newVisual.localRotation = Quaternion.Euler(baseVisual.localEulerAngles.x, offsetRotY, baseVisual.localEulerAngles.z);
            
            spawnedVisuals.Add(newVisual);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isBeingSucked) return;

        if (other.CompareTag("Player"))
        {
            PlayerControl playerControl = other.GetComponent<PlayerControl>();
            
            // Syarat ambil item: Pedang harus disarungkan (sheathed)
            if (playerControl != null && !playerControl.isSwordSheathed)
            {
                return; // Gagal mengambil karena masih pegang pedang
            }

            PlayerItemController itemController = other.GetComponent<PlayerItemController>();
            if (itemController != null)
            {
                // Cek apakah pemain bisa mengambil item ini
                if (itemController.CanPickup(itemData, amount))
                {
                    StartSuckAnimation(other.transform, itemController);
                }
            }
        }
    }

    private void StartSuckAnimation(Transform player, PlayerItemController controller)
    {
        isBeingSucked = true;
        playerTarget = player;
        playerItemController = controller;

        rb.isKinematic = true; // Matikan physics saat disedot
        
        // Matikan semua collider di object ini saat disedot agar tidak mentok-mentok
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void HandleSuckAnimation()
    {
        // Gerak menuju pemain
        transform.position = Vector3.Lerp(transform.position, playerTarget.position + Vector3.up * 1f, 10f * Time.deltaTime);

        // Mengecil perlahan
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 10f * Time.deltaTime);

        // Jika sudah sangat dekat dengan pemain, ambil itemnya
        if (Vector3.Distance(transform.position, playerTarget.position + Vector3.up * 1f) < 0.2f)
        {
            playerItemController.PickupItem(itemData, amount);
            Destroy(gameObject);
        }
    }
}
