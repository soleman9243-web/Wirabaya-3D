using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossArenaManager : MonoBehaviour
{
    [Header("References")]
    public BossAI bossAI;
    public BossUI bossUI;

    [Header("Post-Boss Transition")]
    [Tooltip("Nama scene yang akan dimuat setelah bos berhasil dikalahkan")]
    public string nextSceneName = "WorldScene";
    
    [Tooltip("Waktu jeda (detik) setelah bos mati sebelum pindah scene")]
    public float delayBeforeNextScene = 5f;

    [Header("Quest Completion")]
    [Tooltip("ID Objective Quest yang selesai ketika bos mati")]
    public string objectiveToComplete = "defeat_bear_boss";

    private void Start()
    {
        if (bossAI == null)
        {
            bossAI = FindObjectOfType<BossAI>();
        }

        if (bossUI == null)
        {
            bossUI = FindObjectOfType<BossUI>();
        }

        if (bossAI != null && bossUI != null)
        {
            // Set up Boss UI
            bossUI.InitializeBossUI(bossAI.bossData != null ? bossAI.bossData.bossName : "Boss");

            // Subscribe ke event
            bossAI.OnBossHealthChanged.AddListener(bossUI.UpdateHealth);
            bossAI.OnBossDied.AddListener(HandleBossDeath);
        }
    }

    private void HandleBossDeath()
    {
        Debug.Log("Boss Defeated!");

        if (bossUI != null)
        {
            bossUI.HideUI();
        }

        // Selesaikan Quest
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(objectiveToComplete))
        {
            if (QuestManager.Instance.IsObjectiveActive(objectiveToComplete))
            {
                QuestManager.Instance.AddProgress(objectiveToComplete, 1);
            }
        }

        StartCoroutine(TransitionToNextScene());
    }

    private IEnumerator TransitionToNextScene()
    {
        // Tunggu animasi mati boss atau selebrasi player
        yield return new WaitForSeconds(delayBeforeNextScene);

        // Load scene berikutnya
        Debug.Log("Loading Next Scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        if (bossAI != null)
        {
            bossAI.OnBossHealthChanged.RemoveListener(bossUI.UpdateHealth);
            bossAI.OnBossDied.RemoveListener(HandleBossDeath);
        }
    }
}
