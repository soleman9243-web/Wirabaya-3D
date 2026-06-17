using TMPro;
using UnityEngine;
using System.Collections;

public class QuestUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text objectiveText;

    private int currentDisplayIndex = 0;
    private Coroutine transitionCoroutine;
    private Vector3 originalLocalPos;
    private Color originalColor;
    private bool isInitialized = false;

    private void InitIfNecessary()
    {
        if (!isInitialized && objectiveText != null)
        {
            RectTransform rt = objectiveText.GetComponent<RectTransform>();
            originalLocalPos = rt.localPosition;
            originalColor = objectiveText.color;
            isInitialized = true;
        }
    }

    private void Start()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.currentQuest == null)
        {
            gameObject.SetActive(false);
        }
    }

    public void ShowQuest(QuestData data)
    {
        // Pengecekan penting untuk menghindari NullReferenceException
        if (QuestManager.Instance.currentQuest == null) 
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);

        InitIfNecessary();

        titleText.text = data.title;
        
        int activeIndex = GetActiveObjectiveIndex();
        
        // Deteksi jika quest baru (indeks lebih kecil dari display saat ini)
        if (activeIndex < currentDisplayIndex)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }
            ResetAnimationState();
            currentDisplayIndex = activeIndex;
            UpdateObjectiveText(currentDisplayIndex);
        }
        // Jika ada progres ke objektif berikutnya dan kita tidak sedang transisi
        else if (activeIndex > currentDisplayIndex)
        {
            if (transitionCoroutine == null)
            {
                transitionCoroutine = StartCoroutine(TransitionToNextObjective(currentDisplayIndex, activeIndex));
            }
        }
        // Jika tidak ada pergantian objektif (hanya update progress)
        else if (transitionCoroutine == null)
        {
            currentDisplayIndex = activeIndex;
            UpdateObjectiveText(currentDisplayIndex);
        }
    }

    private int GetActiveObjectiveIndex()
    {
        if (QuestManager.Instance.currentQuest == null) return 0;
        var objectives = QuestManager.Instance.currentQuest.objectives;
        for (int i = 0; i < objectives.Count; i++)
        {
            if (!objectives[i].isCompleted)
                return i;
        }
        return Mathf.Max(0, objectives.Count - 1);
    }

    private void UpdateObjectiveText(int index)
    {
        if (QuestManager.Instance.currentQuest == null) return;
        var objectives = QuestManager.Instance.currentQuest.objectives;
        if (index < 0 || index >= objectives.Count) return;

        var obj = objectives[index];
        string status = obj.isCompleted ? "[V]" : "[ ]";
        string displayDesc = string.IsNullOrEmpty(obj.description) ? obj.objectiveId : obj.description;
        objectiveText.text = $"{status} {displayDesc} ({obj.currentAmount}/{obj.targetAmount})";
    }

    private IEnumerator TransitionToNextObjective(int fromIndex, int toIndex)
    {
        // 1. Update text ke status complete dari objektif sebelumnya
        UpdateObjectiveText(fromIndex);
        
        // 2. Tunggu sebentar agar player bisa membaca bahwa objektif selesai (misal: 3/3)
        yield return new WaitForSeconds(1.5f);
        
        RectTransform rt = objectiveText.GetComponent<RectTransform>();
        
        // 3. Animasi slide ke kiri dan fade out
        float timer = 0;
        float duration = 0.5f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            rt.localPosition = originalLocalPos + Vector3.left * 50f * t; // Geser ke kiri
            objectiveText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - t);
            yield return null;
        }
        
        // 4. Ganti teks ke objektif berikutnya
        currentDisplayIndex = toIndex;
        UpdateObjectiveText(currentDisplayIndex);
        
        // 5. Animasi slide masuk dari kanan dan fade in
        timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            rt.localPosition = originalLocalPos + Vector3.right * 50f * (1f - t); // Dari kanan ke tengah
            objectiveText.color = new Color(originalColor.r, originalColor.g, originalColor.b, t);
            yield return null;
        }
        
        // Pastikan kembali ke posisi dan warna semula
        ResetAnimationState();
        
        transitionCoroutine = null;
        
        // Cek jika saat animasi berlangsung ada objektif lain yang juga selesai/update
        int latestActiveIndex = GetActiveObjectiveIndex();
        if (latestActiveIndex > currentDisplayIndex)
        {
            transitionCoroutine = StartCoroutine(TransitionToNextObjective(currentDisplayIndex, latestActiveIndex));
        }
        else
        {
            UpdateObjectiveText(currentDisplayIndex);
        }
    }

    private void ResetAnimationState()
    {
        if (isInitialized && objectiveText != null)
        {
            RectTransform rt = objectiveText.GetComponent<RectTransform>();
            rt.localPosition = originalLocalPos;
            objectiveText.color = originalColor;
        }
    }

    public void Clear()
    {
        titleText.text = "";
        objectiveText.text = "";
        currentDisplayIndex = 0;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        ResetAnimationState();
        gameObject.SetActive(false);
    }
}