using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database referensi semua ItemData ScriptableObject.
/// Digunakan oleh ItemSaveProvider untuk mencari item berdasarkan nama saat load.
/// Cara pakai: pasang di GameManager, lalu drag semua ItemData ke list.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Tooltip("Drag semua ItemData ScriptableObject ke sini")]
    public List<ItemData> allItems;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Cari ItemData berdasarkan nama ScriptableObject.
    /// </summary>
    public ItemData GetItem(string itemName)
    {
        if (allItems == null) return null;

        foreach (var item in allItems)
        {
            if (item != null && item.name == itemName)
                return item;
        }
        return null;
    }
}
