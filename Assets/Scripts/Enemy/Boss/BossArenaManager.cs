using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossArenaManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Untuk boss generik (Bear, dll)")]
    public BossAI bossAI;
    [Tooltip("Untuk boss Babi")]
    public BoarBossAI boarBossAI;
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
        // Auto-find BoarBossAI jika belum di-set
        if (boarBossAI == null)
        {
            boarBossAI = FindFirstObjectByType<BoarBossAI>();
        }

        // Auto-find BossAI jika belum di-set (dan tidak ada BoarBossAI)
        if (bossAI == null && boarBossAI == null)
        {
            bossAI = FindFirstObjectByType<BossAI>();
        }

        if (bossUI == null)
        {
            bossUI = FindFirstObjectByType<BossUI>();
        }

        // ===== SETUP: BoarBossAI (prioritas) =====
        if (boarBossAI != null)
        {
            // BoarBossAI mengelola intro + health UI sendiri secara internal.
            // ArenaManager hanya subscribe ke event kematian untuk quest & scene transition.
            boarBossAI.OnBossDied.AddListener(HandleBossDeath);

            // Mulai boss fight jika belum auto-start
            if (!boarBossAI.autoStart)
            {
                boarBossAI.StartBossFight();
            }
        }
        // ===== SETUP: BossAI generik (fallback) =====
        else if (bossAI != null && bossUI != null)
        {
            bossUI.InitializeBossUI(bossAI.bossData != null ? bossAI.bossData.bossName : "Boss");

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

        // Fade out dulu
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }

        // Async load saat layar sudah gelap
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
    }

    private void OnDestroy()
    {
        if (boarBossAI != null)
        {
            boarBossAI.OnBossDied.RemoveListener(HandleBossDeath);
        }

        if (bossAI != null && bossUI != null)
        {
            bossAI.OnBossHealthChanged.RemoveListener(bossUI.UpdateHealth);
            bossAI.OnBossDied.RemoveListener(HandleBossDeath);
        }
    }
}

