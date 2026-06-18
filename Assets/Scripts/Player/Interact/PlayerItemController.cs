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

    private PlayerControl playerControl;

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
    }

    public bool CanPickup(ItemData item, int amount)
    {
        // Jika tangan kosong, bisa ambil
        if (currentItem == null || currentAmount == 0) return true;

        // Jika tangan memegang item yang sama dan masih ada slot stack, bisa ambil
        if (currentItem == item && currentAmount + amount <= currentItem.maxStackSize) return true;

        return false;
    }

    public void PickupItem(ItemData item, int amount)
    {
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
        GameObject droppedObj = Instantiate(currentItem.droppedPrefab, spawnPos, Quaternion.identity);
        DroppedItem droppedItem = droppedObj.GetComponent<DroppedItem>();
        if (droppedItem != null)
        {
            droppedItem.itemData = currentItem;
            droppedItem.amount = currentAmount;
        }

        Debug.Log($"Dropped {currentAmount} {currentItem.itemName}.");

        // Kosongkan tangan
        currentItem = null;
        currentAmount = 0;
        
        UpdateHandVisual();
    }

    private void UpdateHandVisual()
    {
        if (rightHandItemContainer == null) return;

        // Matikan semua child di dalam container tangan terlebih dahulu
        foreach (Transform child in rightHandItemContainer)
        {
            child.gameObject.SetActive(false);
        }

        // Jika sedang memegang item, cari child dengan nama yang sesuai di ItemData.heldModelName
        if (currentItem != null && currentAmount > 0 && !string.IsNullOrEmpty(currentItem.heldModelName))
        {
            Transform heldModel = rightHandItemContainer.Find(currentItem.heldModelName);
            if (heldModel != null)
            {
                heldModel.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Tidak menemukan model dengan nama '{currentItem.heldModelName}' di dalam {rightHandItemContainer.name}!");
            }
        }
    }
}
