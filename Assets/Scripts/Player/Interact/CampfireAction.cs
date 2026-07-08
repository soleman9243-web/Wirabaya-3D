using UnityEngine;
using UnityEngine.SceneManagement;

public class CampfireAction : MonoBehaviour
{
    [Header("Cooking Settings")]
    [Tooltip("Nama ItemData (itemName) yang wajib dipegang player. Contoh: 'Daging Babi'")]
    public string requiredItemName = "Daging Babi";
    
    [Tooltip("Nama Scene yang akan dimuat setelah berhasil memasak daging")]
    public string nextSceneName;

    [Tooltip("Centang ini jika item (daging) ingin dihilangkan dari tangan player setelah berhasil interact")]
    public bool consumeItemOnSuccess = true;

    /// <summary>
    /// Fungsi ini dipanggil dari event OnInteract() di script InteractObject
    /// </summary>
    public void TryCookMeat()
    {
        // Cari player di scene
        PlayerItemController playerItem = FindObjectOfType<PlayerItemController>();

        if (playerItem != null)
        {
            // Cek apakah player sedang memegang item dan namanya sesuai
            if (playerItem.currentItem != null && playerItem.currentItem.itemName == requiredItemName)
            {
                Debug.Log($"Berhasil memanggang {requiredItemName}! Pindah scene ke {nextSceneName}...");
                
                if (consumeItemOnSuccess)
                {
                    playerItem.ConsumeCurrentItem();
                }

                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    if (GameSceneManager.Instance != null)
                    {
                        GameSceneManager.Instance.ChangeScene(nextSceneName);
                    }
                    else
                    {
                        SceneManager.LoadScene(nextSceneName);
                    }
                }
                else
                {
                    Debug.LogWarning("Next Scene Name belum diisi di Inspector CampfireAction!");
                }
            }
            else
            {
                Debug.Log($"Gagal. Player harus memegang {requiredItemName} untuk memasak.");
                // Jika Anda punya script Notifikasi UI, bisa dipanggil di sini.
            }
        }
        else
        {
            Debug.LogError("PlayerItemController tidak ditemukan di scene!");
        }
    }
}
