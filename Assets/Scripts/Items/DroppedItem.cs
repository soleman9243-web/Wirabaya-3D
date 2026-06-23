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

    [Header("Visuals")]
    [Tooltip("Titik pusat untuk model item. Biasakan buat child object kosong bernama 'Visuals' lalu taruh model 3D di dalamnya.")]
    public Transform visualsContainer;

    [Tooltip("Tandai true dari script Player saat membuang item dari inventori.")]
    [HideInInspector] public bool isPlayerDrop = false;

    [Header("Physics & Ground Detection")]
    public LayerMask groundLayer = ~0;

    private bool isBeingSucked = false;
    private bool canPickup = false;
    private Transform playerTarget;
    private PlayerItemController playerItemController;

    private float startYPos;
    private List<Transform> spawnedVisuals = new List<Transform>();

    private Vector3 currentVelocity;
    private bool hasLanded = false;
    private float landYPos;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        // Karena kita akan menggunakan raycast, Rigidbody bisa di-set ke kinematic
        // agar tidak dipengaruhi fisika bawaan Unity
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Buat SphereCollider otomatis khusus untuk mendeteksi area pickup
        SphereCollider triggerCol = gameObject.AddComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        if (itemData != null) triggerCol.radius = itemData.pickupRadius;

        // Ubah BoxCollider menjadi trigger sesuai permintaan agar tidak ada tabrakan sama sekali
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.isTrigger = true;
        }
    }

    private void Start()
    {

        if (itemData != null)
        {
            // Terapkan spawn offset (gunakan rotasi lokal agar tidak global)
            if (!isPlayerDrop) 
            {
                transform.position += transform.right * itemData.spawnOffset.x + transform.up * itemData.spawnOffset.y + transform.forward * itemData.spawnOffset.z;
            }

            // Beri sedikit dorongan ke atas saat pertama kali spawn (secara manual)
            if (itemData.autoBounceOnStart && !isBeingSucked)
            {
                float activeBounceForce = isPlayerDrop ? itemData.playerDropBounceForce : itemData.worldDropBounceForce;

                currentVelocity = Vector3.up * activeBounceForce;
                
                // Beri dorongan acak sedikit ke samping agar menyebar jika spawn banyak
                Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                currentVelocity += randomDir * (activeBounceForce * 0.5f);
            }
            
            Invoke(nameof(EnablePickup), itemData.pickupDelay);
        }

        startYPos = transform.position.y;
        SetupVisualStack();
    }

    private void EnablePickup()
    {
        canPickup = true;
    }

    private void Update()
    {
        if (isBeingSucked && playerTarget != null)
        {
            HandleSuckAnimation();
            return;
        }

        if (itemData == null || visualsContainer == null) return;

        // Fisika Manual & Raycast untuk mendeteksi tanah
        if (!hasLanded)
        {
            currentVelocity += Physics.gravity * Time.deltaTime;
            transform.position += currentVelocity * Time.deltaTime;

            // Jika sedang jatuh ke bawah, cek raycast
            if (currentVelocity.y <= 0f)
            {
                // Raycast ke bawah. Gunakan jarak pendek untuk mendeteksi tanah, dan batasi dengan groundLayer
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.6f, groundLayer))
                {
                    hasLanded = true;
                    landYPos = hit.point.y;
                    
                    // Posisikan tepat di tanah
                    transform.position = new Vector3(transform.position.x, landYPos, transform.position.z);
                    currentVelocity = Vector3.zero;
                }
            }
        }
        else
        {
            // Animasi Muter dan Naik Turun (Hanya berjalan jika sudah mendarat di tanah)
            visualsContainer.localEulerAngles = new Vector3(
                visualsContainer.localEulerAngles.x,
                visualsContainer.localEulerAngles.y + (itemData.rotationSpeed * Time.deltaTime),
                visualsContainer.localEulerAngles.z
            );

            float newY = Mathf.Sin(Time.time * itemData.floatSpeed) * itemData.floatAmplitude;
            visualsContainer.localPosition = new Vector3(visualsContainer.localPosition.x, newY + itemData.floatHeightOffset, visualsContainer.localPosition.z);
        }

        // --- Dynamic Visual Stacking Update ---
        // Jika kamu mengubah value di ScriptableObject secara realtime, posisi tumpukannya akan otomatis terupdate!
        if (itemData != null && itemData.enableVisualStacking && spawnedVisuals.Count > 1)
        {
            Transform baseVisual = spawnedVisuals[0];
            for (int i = 1; i < spawnedVisuals.Count; i++)
            {
                if (spawnedVisuals[i] != null)
                {
                    // Gunakan fixed offset agar pasti terlihat bertumpuk dan tidak tertutup sempurna
                    float offsetX = (i % 2 != 0 ? 1 : -1) * itemData.visualStackOffset.x;
                    float offsetZ = (i % 2 != 0 ? -1 : 1) * itemData.visualStackOffset.z;
                    float offsetY = i * itemData.visualStackOffset.y; // Bertumpuk ke atas 

                    spawnedVisuals[i].localPosition = baseVisual.localPosition + new Vector3(offsetX, offsetY, offsetZ);
                    spawnedVisuals[i].localRotation = baseVisual.localRotation;
                }
            }
        }
    }

    private void SetupVisualStack()
    {
        if (itemData == null || visualsContainer == null || visualsContainer.childCount == 0 || !itemData.enableVisualStacking) return;

        int visualCount = Mathf.Min(amount, itemData.maxVisualStack); 
        
        Debug.Log($"SetupVisualStack for {itemData.itemName}: Amount={amount}, VisualCount={visualCount}");

        if (visualCount <= 1) return;

        // BUNGKUS SEMUA CHILD KE DALAM SATU OBJEK BASE
        // Ini untuk mencegah masalah jika child pertama (index 0) ternyata bukan model 3D (misal efek partikel atau lampu).
        GameObject baseVisualObj = new GameObject("BaseVisualGroup");
        baseVisualObj.transform.SetParent(visualsContainer, false);
        baseVisualObj.transform.localPosition = Vector3.zero;
        baseVisualObj.transform.localRotation = Quaternion.identity;
        baseVisualObj.transform.localScale = Vector3.one;

        // Pindahkan semua child asli ke dalam BaseVisualGroup
        while (visualsContainer.childCount > 1)
        {
            Transform child = visualsContainer.GetChild(0);
            if (child == baseVisualObj.transform)
            {
                child = visualsContainer.GetChild(1);
            }
            child.SetParent(baseVisualObj.transform, false);
        }

        Transform baseVisual = baseVisualObj.transform;
        spawnedVisuals.Add(baseVisual);

        for (int i = 1; i < visualCount; i++)
        {
            Transform newVisual = Instantiate(baseVisual.gameObject, visualsContainer).transform;
            spawnedVisuals.Add(newVisual);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!canPickup || isBeingSucked) return;

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
        
        // Matikan semua collider (termasuk yang ada di child object) saat disedot agar tidak memblokir kamera/Cinemachine
        Collider[] colliders = GetComponentsInChildren<Collider>();
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Gambar lingkaran di Scene View agar radius pickup terlihat jelas
        Gizmos.color = Color.cyan;
        if (itemData != null)
        {
            Gizmos.DrawWireSphere(transform.position, itemData.pickupRadius);
        }
    }
#endif
}
