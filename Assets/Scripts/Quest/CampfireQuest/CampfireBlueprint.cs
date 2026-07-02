using UnityEngine;

public class CampfireBlueprint : MonoBehaviour
{
    [Header("Quest Settings")]
    public string requireObjectiveId;
    public string completeObjectiveId = "build_campfire";

    [Header("Visuals")]
    public GameObject[] hologramPieces;
    public GameObject[] woodPieces;
    public GameObject finalCampfire;

    private int currentStep = 0;

    private void Start()
    {
        gameObject.SetActive(false);

        foreach (var wood in woodPieces) wood.SetActive(false);
        finalCampfire.SetActive(false);

        foreach (var holo in hologramPieces) holo.SetActive(true);

        QuestManager.Instance.onQuestUpdated.AddListener(CheckIfCanAppear);
    }

    private void CheckIfCanAppear(QuestData data)
    {
        if (gameObject.activeSelf) return;

        foreach (var obj in QuestManager.Instance.currentQuest.objectives)
        {
            if (obj.objectiveId == requireObjectiveId && obj.isCompleted)
            {
                gameObject.SetActive(true);
            }
        }
    }

    public void PlaceWoodVisual()
    {
        if (currentStep < woodPieces.Length && currentStep < hologramPieces.Length)
        {
            woodPieces[currentStep].SetActive(true);

            hologramPieces[currentStep].SetActive(false);

            currentStep++;

            if (currentStep >= woodPieces.Length)
            {
                FinishBuilding();
            }
        }
    }

    private void FinishBuilding()
    {
        foreach (var wood in woodPieces) wood.SetActive(false);

        finalCampfire.SetActive(true);

        GetComponent<Collider>().enabled = false;

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(completeObjectiveId) && QuestManager.Instance.IsObjectiveActive(completeObjectiveId))
        {
            QuestManager.Instance.AddProgress(completeObjectiveId, 1);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.onQuestUpdated.RemoveListener(CheckIfCanAppear);
        }
    }
}