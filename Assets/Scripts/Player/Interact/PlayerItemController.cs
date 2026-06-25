using UnityEngine;

public class PlayerItemController : MonoBehaviour
{
    [Header("Current Item")]
    public ItemData currentItem;
    public int currentAmount = 0;

    [Header("Hand Rig")]
    [Tooltip("Transform atau objek di rig tangan Mixamo tempat berkumpulnya semua model 3D item.")]
    public Transform rightHandItemContainer;

    [Header("Controls")]
    public KeyCode dropKey = KeyCode.Q;
    public Transform dropSpawnPoint;

    [Header("Animator (Opsional)")]
    public Animator playerAnimator;

    private PlayerControl playerControl;
    private System.Collections.Generic.List<GameObject> spawnedHandVisuals = new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        playerControl = GetComponent<PlayerControl>();
    }

    private void Start()
    {
        UpdateHandVisual();
    }

    private void Update()
    {
        // Fitur Drop Item
        if (Input.GetKeyDown(dropKey) && currentItem != null && currentAmount > 0)
        {
            DropItems();
        }

        // Sinkronisasi status memegang item ke PlayerControl
        if (playerControl != null)
        {
            playerControl.isHoldingItem = (currentItem != null && currentAmount > 0);
        }

#if UNITY_EDITOR
        // Fitur Live Update: Agar kamu bisa melihat perubahan posisi kayu secara langsung (Realtime) saat mengubah nilai di ScriptableObject
        if (currentItem != null && currentAmount > 1 && spawnedHandVisuals.Count > 0)
        {
            if (!string.IsNullOrEmpty(currentItem.heldModelName))
            {
                Transform heldModel = rightHandItemContainer.Find(currentItem.heldModelName);
                if (heldModel != null)
                {
                    for (int i = 0; i < spawnedHandVisuals.Count; i++)
                    {
                        if (spawnedHandVisuals[i] != null)
                        {
                            int stackIndex = i + 1;
                            Vector3 localOffset = new Vector3(
                                stackIndex * currentItem.handStackOffset.x,
                                stackIndex * currentItem.handStackOffset.y,
                                stackIndex * currentItem.handStackOffset.z
                            );
                            spawnedHandVisuals[i].transform.position = heldModel.position + (heldModel.rotation * localOffset);
                        }
                    }
                }
            }
        }
#endif
    }

    public bool CanPickup(ItemData item, int amount)
    {
        // Jika item adalah senjata, pastikan belum punya senjata
        if (item.itemType == ItemType.Weapon)
        {
            return (playerControl != null && !playerControl.hasSwordEquipped);
        }

        // Jika tangan kosong, bisa ambil
        if (currentItem == null || currentAmount == 0) return true;

        // Jika tangan memegang item yang sama dan masih ada slot stack, bisa ambil
        if (currentItem == item && currentAmount + amount <= currentItem.maxStackSize) return true;

        return false;
    }

    public void PickupItem(ItemData item, int amount)
    {
        if (item.itemType == ItemType.Weapon)
        {
            if (playerControl != null)
            {
                playerControl.UnlockWeapon();
                Debug.Log($"Weapon picked up and unlocked: {item.itemName}");
            }
            return;
        }

        if (currentItem == null || currentAmount == 0)
        {
            currentItem = item;
            currentAmount = amount;
        }
        else if (currentItem == item)
        {
            currentAmount += amount;
            currentAmount = Mathf.Min(currentAmount, currentItem.maxStackSize);
        }

        Debug.Log($"Picked up {amount} {item.itemName}. Total: {currentAmount}");
        UpdateHandVisual();
    }

    private void DropItems()
    {
        if (currentItem == null || currentItem.droppedPrefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 1f;
        if (dropSpawnPoint != null) spawnPos = dropSpawnPoint.position;

        // Munculkan item yang jatuh
        GameObject droppedObj = Instantiate(currentItem.droppedPrefab, spawnPos, transform.rotation);
        DroppedItem droppedItem = droppedObj.GetComponent<DroppedItem>();
        if (droppedItem != null)
        {
            droppedItem.isPlayerDrop = true;
            droppedItem.itemData = currentItem;
            droppedItem.amount = currentAmount;
        }

        Debug.Log($"Dropped {currentAmount} {currentItem.itemName}.");

        // Matikan parameter animasi di Animator (jika ada) sebelum mengosongkan item
        if (playerAnimator != null && !string.IsNullOrEmpty(currentItem.holdingAnimatorParameter))
        {
            playerAnimator.SetBool(currentItem.holdingAnimatorParameter, false);
        }

        // Kosongkan tangan
        currentItem = null;
        currentAmount = 0;
        
        UpdateHandVisual();
    }

    private void UpdateHandVisual()
    {
        if (rightHandItemContainer == null) return;

        // Bersihkan tumpukan visual sebelumnya (jika ada)
        foreach (GameObject spawned in spawnedHandVisuals)
        {
            if (spawned != null) Destroy(spawned);
        }
        spawnedHandVisuals.Clear();

        // Matikan semua child base model di dalam container tangan terlebih dahulu
        foreach (Transform child in rightHandItemContainer)
        {
            child.gameObject.SetActive(false);
        }

        if (currentItem != null && currentAmount > 0)
        {
            // Update Animator jika ada parameternya
            if (playerAnimator != null && !string.IsNullOrEmpty(currentItem.holdingAnimatorParameter))
            {
                playerAnimator.SetBool(currentItem.holdingAnimatorParameter, true);
            }

            // Cari model dasar
            if (!string.IsNullOrEmpty(currentItem.heldModelName))
            {
                Transform heldModel = rightHandItemContainer.Find(currentItem.heldModelName);
                if (heldModel != null)
                {
                    heldModel.gameObject.SetActive(true);

                    // Buat tumpukan visual jika amount > 1 dan fitur diaktifkan
                    if (currentItem.enableVisualStacking && currentAmount > 1)
                    {
                        int visualCount = Mathf.Min(currentAmount, currentItem.maxVisualStack);
                        for (int i = 1; i < visualCount; i++)
                        {
                            GameObject newVisual = Instantiate(heldModel.gameObject, rightHandItemContainer);
                            newVisual.SetActive(true);
                            
                            // Gunakan offset khusus tangan yang menumpuk lurus (tanpa zig-zag)
                            Vector3 localOffset = new Vector3(
                                i * currentItem.handStackOffset.x,
                                i * currentItem.handStackOffset.y,
                                i * currentItem.handStackOffset.z
                            );

                            // Mengalikan rotasi dengan offset agar posisinya mengikuti arah kayu,
                            // NAMUN tidak terpengaruh oleh skala kayu yang mengecil (misal scale 0.01).
                            // Ini menjamin kayunya pasti bergeser sejauh nilai di Inspector (dalam hitungan meter).
                            newVisual.transform.position = heldModel.position + (heldModel.rotation * localOffset);
                            newVisual.transform.localRotation = heldModel.localRotation;
                            newVisual.transform.localScale = heldModel.localScale;

                            spawnedHandVisuals.Add(newVisual);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Tidak menemukan model dengan nama '{currentItem.heldModelName}' di dalam {rightHandItemContainer.name}!");
                }
            }
        }
    }
}
