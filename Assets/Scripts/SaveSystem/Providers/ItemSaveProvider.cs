using UnityEngine;

/// <summary>
/// Mengambil dan mengembalikan data item yang sedang dipegang dari PlayerItemController.
/// Pasang di GameObject yang sama dengan PlayerItemController (biasanya PlayerArmature).
/// Menggunakan ItemDatabase untuk lookup item — tidak perlu folder Resources.
/// </summary>
public class ItemSaveProvider : MonoBehaviour, ISaveDataProvider
{
    public void PopulateSaveData(SaveData data)
    {
        PlayerItemController pic = FindFirstObjectByType<PlayerItemController>();
        if (pic == null)
        {
            Debug.LogWarning("[ItemSaveProvider] PlayerItemController tidak ditemukan, skip populate.");
            return;
        }

        if (pic.currentItem != null && pic.currentAmount > 0)
        {
            data.heldItemName = pic.currentItem.name; // Nama ScriptableObject asset
            data.heldItemAmount = pic.currentAmount;
        }
        else
        {
            data.heldItemName = "";
            data.heldItemAmount = 0;
        }
    }

    public void RestoreFromSaveData(SaveData data)
    {
        PlayerItemController pic = FindFirstObjectByType<PlayerItemController>();
        if (pic == null)
        {
            Debug.LogWarning("[ItemSaveProvider] PlayerItemController tidak ditemukan, skip restore.");
            return;
        }

        // Jika save file tidak punya data item, jangan ganggu — biarkan startingItem jalan normal
        if (string.IsNullOrEmpty(data.heldItemName))
        {
            Debug.Log("[ItemSaveProvider] Tidak ada item data di save, biarkan startingItem.");
            return;
        }

        // Jika save file tidak punya data item, jangan ganggu — biarkan startingItem jalan normal
        if (string.IsNullOrEmpty(data.heldItemName))
        {
            Debug.Log("[ItemSaveProvider] Tidak ada item data di save, biarkan startingItem.");
            return;
        }

        // Ada data item di save → kosongkan dulu, lalu restore
        pic.ConsumeCurrentItem();

        if (data.heldItemAmount > 0)
        {
            if (ItemDatabase.Instance == null)
            {
                Debug.LogWarning("[ItemSaveProvider] ItemDatabase.Instance belum ada! Pastikan ItemDatabase sudah dipasang di scene.");
                return;
            }

            ItemData item = ItemDatabase.Instance.GetItem(data.heldItemName);

            if (item != null)
            {
                pic.PickupItem(item, data.heldItemAmount);
                Debug.Log($"[ItemSaveProvider] Restored item: {data.heldItemName} x{data.heldItemAmount}");
            }
            else
            {
                Debug.LogWarning($"[ItemSaveProvider] ItemData '{data.heldItemName}' tidak ditemukan di ItemDatabase!");
            }
        }
    }
}
