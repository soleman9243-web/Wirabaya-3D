using UnityEngine;
using UnityEngine.Events;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance;

    [Header("UI")]
    public GameObject minigameUI;

    private InteractObject currentSource;
    private bool isActive;

    [Header("Events")]
    public UnityEvent onMinigameSuccess;
    public UnityEvent onMinigameFail;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartMinigame(InteractObject source)
    {
        if (isActive)
        {
            return;
        }

        if (source == null)
        {
            Debug.LogWarning("Minigame source is NULL!");
            return;
        }

        isActive = true;
        currentSource = source;

        if (minigameUI != null)
        {
            minigameUI.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void CompleteMinigame(bool success)
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;

        if (minigameUI != null)
        {
            minigameUI.SetActive(false);
        }

        Time.timeScale = 1f;

        if (success)
        {
            if (currentSource != null && !string.IsNullOrEmpty(currentSource.objectiveId))
            {
                QuestManager.Instance.AddProgress(currentSource.objectiveId, currentSource.questAmount);
            }

            onMinigameSuccess?.Invoke();
        }
        else
        {
            onMinigameFail?.Invoke();
        }

        currentSource = null;
    }
}