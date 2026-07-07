using UnityEngine;

/// <summary>
/// Script opsional untuk ditaruh di objek 3D (seperti pintu atau portal) sebagai Trigger fisik.
/// Script ini HANYA akan memanggil GameSceneManager yang sudah tersentralisasi.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneChangeTrigger : MonoBehaviour
{
    [Tooltip("Nama scene yang akan dituju saat player menyentuh area ini")]
    public string targetSceneName;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Hanya jalan jika yang menyentuh adalah player
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // Memanggil sistem Manager pusat untuk pindah scene (akan otomatis fade out jika disetting)
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ChangeScene(targetSceneName);
            }
            else
            {
                Debug.LogError("GameSceneManager tidak ditemukan di scene! Pastikan ada objek GameManager/UI yang memakai script GameSceneManager.");
            }
        }
    }
}
