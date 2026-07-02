using UnityEngine;
using StarterAssets;

public class StealthFailHandler : MonoBehaviour
{
    [Header("Reset Settings")]
    public Transform resetPoint;
    public ThirdPersonController playerController;
    public EnemyAI enemyAI;

    private void Start()
    {
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerController = player.GetComponent<ThirdPersonController>();
        }

        if (enemyAI != null)
        {
            enemyAI.onPlayerDetected.AddListener(ResetPlayerPosition);
        }
    }

    private void ResetPlayerPosition()
    {
        if (playerController != null && resetPoint != null)
        {
            Debug.Log("Stealth Failed! Resetting position...");
            // Nonaktifkan controller sejenak saat teleport
            CharacterController cc = playerController.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerController.transform.position = resetPoint.position;
            playerController.transform.rotation = resetPoint.rotation;

            if (cc != null) cc.enabled = true;

            // Reset status alert musuh
            enemyAI.isAlerted = false;
        }
    }

    private void OnDestroy()
    {
        if (enemyAI != null)
        {
            enemyAI.onPlayerDetected.RemoveListener(ResetPlayerPosition);
        }
    }
}
