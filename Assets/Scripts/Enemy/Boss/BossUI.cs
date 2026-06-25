using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bossUIPanel;
    public TextMeshProUGUI bossNameText;
    public Image healthFill;
    public Image healthEaseFill;

    [Header("Settings")]
    public float easeSpeed = 2f;

    private float targetFillAmount = 1f;

    private void Awake()
    {
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(false); // Hide awalnya
        }
    }

    public void InitializeBossUI(string bossName)
    {
        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }

        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(true);
        }

        targetFillAmount = 1f;
        if (healthFill != null) healthFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
        if (healthEaseFill != null) healthEaseFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        targetFillAmount = currentHealth / maxHealth;
        
        if (healthFill != null)
        {
            healthFill.rectTransform.localScale = new Vector3(targetFillAmount, 1f, 1f);
        }
    }

    private void Update()
    {
        if (healthEaseFill != null && healthFill != null)
        {
            float targetScale = healthFill.rectTransform.localScale.x;
            float currentScale = healthEaseFill.rectTransform.localScale.x;

            if (currentScale != targetScale)
            {
                float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * easeSpeed);
                healthEaseFill.rectTransform.localScale = new Vector3(newScale, 1f, 1f);
            }
        }
    }

    public void HideUI()
    {
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(false);
        }
    }
}
