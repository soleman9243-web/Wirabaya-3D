using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class BossTrigger : MonoBehaviour
{
    [Header("Quest Integration")]
    [Tooltip("ID Objective yang harus aktif untuk bisa melawan boss ini")]
    public string requiredObjectiveId;

    [Header("Scene Transition")]
    [Tooltip("Nama scene bos yang akan diload")]
    public string bossSceneName = "BearBossScene";

    [Header("Cutscene Cameras")]
    [Tooltip("Virtual Camera yang akan aktif untuk menyorot arena/boss sebelum loading screen")]
    public CinemachineVirtualCamera cutsceneCamera;
    
    [Tooltip("Durasi cutscene menyorot arena (dalam detik)")]
    public float cutsceneDuration = 3f;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            // Cek apakah quest untuk boss ini sedang aktif
            if (QuestManager.Instance != null && !string.IsNullOrEmpty(requiredObjectiveId))
            {
                if (!QuestManager.Instance.IsObjectiveActive(requiredObjectiveId))
                {
                    // Quest belum aktif, jangan mulai boss fight
                    return;
                }
            }

            hasTriggered = true;
            StartCoroutine(StartBossCutsceneSequence(other.gameObject));
        }
    }

    private IEnumerator StartBossCutsceneSequence(GameObject player)
    {
        // 1. Matikan kontrol player agar tidak bisa bergerak selama cutscene
        MonoBehaviour starterController = player.GetComponent("FirstPersonController") as MonoBehaviour;
        if (starterController == null)
        {
            starterController = player.GetComponent("ThirdPersonController") as MonoBehaviour;
        }

        if (starterController != null)
        {
            starterController.enabled = false;
        }

        // 2. Aktifkan Camera Cutscene (jika ada)
        if (cutsceneCamera != null)
        {
            cutsceneCamera.Priority = 100; // Pastikan jadi kamera utama
            yield return new WaitForSeconds(cutsceneDuration);
        }

        // 3. Load Scene Boss (disarankan pakai Loading Screen UI di sini jika punya)
        Debug.Log("Loading Boss Scene: " + bossSceneName);
        SceneManager.LoadScene(bossSceneName);
    }
}
