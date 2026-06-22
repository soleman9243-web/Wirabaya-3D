using UnityEngine;
using UnityEngine.UI;

public class BossUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bossUIPanel;
    public Text bossNameText;
    public Image healthFill;
    public Image healthEaseFill;

    [Header("Settings")]
    public float easeSpeed = 2f;

    private float targetFillAmount = 1f;

    private void Start()
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
        if (healthFill != null) healthFill.fillAmount = 1f;
        if (healthEaseFill != null) healthEaseFill.fillAmount = 1f;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        targetFillAmount = currentHealth / maxHealth;
        
        if (healthFill != null)
        {
            healthFill.fillAmount = targetFillAmount;
        }
    }

    private void Update()
    {
        if (healthEaseFill != null && healthFill != null)
        {
            if (healthEaseFill.fillAmount != healthFill.fillAmount)
            {
                healthEaseFill.fillAmount = Mathf.Lerp(healthEaseFill.fillAmount, healthFill.fillAmount, Time.deltaTime * easeSpeed);
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
