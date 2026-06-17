using System.Collections;   
using UnityEngine;
using StarterAssets;
using Unity.VisualScripting;

public class PlayerTakedown : MonoBehaviour
{
    [Header("Quest")]
    public string takedownObjectiveId = "perform_takedown";

    private EnemyAI currentTarget;

    private ThirdPersonController controller;
    private CharacterController characterController;
    private Animator playerAnimator;
    private void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        playerAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (controller.IsInFinisher)
        {
            return;
        }

        FindTarget();

        if (currentTarget == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartTakedown();
        }
    }
    private void StartTakedown()
    {
        currentTarget.ShowTakedownUI(false);

        // Sembunyikan UI persis seperti dialog
        if (DialogueManager.Instance != null && DialogueManager.Instance.uiElementsToHide != null)
        {
            foreach (var uiObj in DialogueManager.Instance.uiElementsToHide)
            {
                if (uiObj != null) uiObj.SetActive(false);
            }
        }

        controller.IsInFinisher = true;

        characterController.enabled = false;

        transform.position =
            currentTarget.TakedownPoint.position;

        transform.rotation =
            currentTarget.TakedownPoint.rotation;

        characterController.enabled = true;

        currentTarget.TakedownCamera.gameObject.SetActive(true);

        playerAnimator.SetTrigger("Takedown");

        currentTarget.OnTakedownStart();
    }

    private void FindTarget()
    {
        EnemyAI previousTarget = currentTarget;

        currentTarget = null;

        EnemyAI[] enemies =
            FindObjectsByType<EnemyAI>(
                FindObjectsSortMode.None
            );

        float closestDistance = Mathf.Infinity;

        foreach (EnemyAI enemy in enemies)
        {
            enemy.ShowTakedownUI(false);

            if (!enemy.IsPlayerInTakedownArea)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    enemy.transform.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = enemy;
            }
        }

        if (currentTarget != null)
        {
            currentTarget.ShowTakedownUI(true);
        }
    }
    public void OnTakedownFinished()
    {
        //StartCoroutine(FinishTakedownRoutine());
        TakedownTest();
    }
    private IEnumerator FinishTakedownRoutine()
    {
        yield return ScreenFader.Instance.FadeOut();

        if (currentTarget != null)
        {
            currentTarget.TakedownCamera.gameObject.SetActive(false);

            // Destroy(currentTarget.gameObject);
        }

        controller.IsInFinisher = false;

        yield return ScreenFader.Instance.FadeIn();
    }

    public void TakedownTest()
    {
        if (currentTarget != null)
        {
            currentTarget.TakedownCamera.gameObject.SetActive(false);

            if (QuestManager.Instance != null && !string.IsNullOrEmpty(takedownObjectiveId) && QuestManager.Instance.IsObjectiveActive(takedownObjectiveId))
            {
                QuestManager.Instance.AddProgress(takedownObjectiveId, 1);
            }

            // Panggil Die() agar musuh mati, kehitung quest, dan badannya hilang setelah beberapa detik
            currentTarget.Die();
        }

        // Kembalikan UI
        if (DialogueManager.Instance != null && DialogueManager.Instance.uiElementsToHide != null)
        {
            foreach (var uiObj in DialogueManager.Instance.uiElementsToHide)
            {
                if (uiObj != null) uiObj.SetActive(true);
            }
        }

        controller.IsInFinisher = false;
    }
}