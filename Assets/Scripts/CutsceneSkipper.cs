using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct SkipInputData
{
    [Tooltip("Tombol untuk men-skip")]
    public KeyCode skipKey;
    [Tooltip("GameObject (Image/Text) icon tombol yang akan muncul di tengah lingkaran saat tombol ini ditekan")]
    public GameObject buttonIconObject;
}

public class CutsceneSkipper : MonoBehaviour
{
    [Header("Skip Settings")]
    [Tooltip("Daftar tombol yang bisa digunakan untuk skip dan Icon-nya masing-masing")]
    public SkipInputData[] skipInputs;
    
    [Tooltip("Berapa detik tombol harus ditahan sampai cutscene di-skip")]
    public float holdDuration = 1.5f; 

    [Header("UI References")]
    [Tooltip("Komponen Image dengan tipe Filled -> Radial 360")]
    public Image progressCircle; 
    
    [Tooltip("CanvasGroup (opsional) agar UI bisa muncul/hilang perlahan secara halus")]
    public CanvasGroup skipUI; 

    [Header("Event Saat Skip Penuh")]
    [Tooltip("Isi nama scene jika ingin pindah scene langsung dari script ini (Kosongkan jika pakai event di bawah)")]
    public string nextSceneName; 
    
    [Tooltip("Event yang akan dipanggil saat progress penuh (Bisa memanggil fungsi LoadScene dari TimelineSceneLoader)")]
    public UnityEvent onSkipComplete; 

    private float currentHoldTime = 0f;
    private bool hasSkipped = false;
    private KeyCode currentLockedKey = KeyCode.None;

    void Update()
    {
        if (hasSkipped) return;

        bool isHolding = false;

        // 1. Jika kita sedang men-lock sebuah tombol
        if (currentLockedKey != KeyCode.None)
        {
            // Cek apakah tombol yang di-lock MASIH ditekan
            if (Input.GetKey(currentLockedKey))
            {
                isHolding = true;
            }
            else
            {
                // Jika tombol dilepas, lepaskan kuncian
                currentLockedKey = KeyCode.None;
            }
        }

        // 2. Jika tidak ada tombol yang sedang dikunci, cari input baru
        if (currentLockedKey == KeyCode.None)
        {
            foreach (var input in skipInputs)
            {
                if (Input.GetKey(input.skipKey))
                {
                    isHolding = true;
                    currentLockedKey = input.skipKey;
                    break; // Langsung lock ke tombol ini, abaikan tombol lain
                }
            }
        }

        if (isHolding)
        {
            currentHoldTime += Time.deltaTime; // Tambah waktu hold

            // Aktifkan GameObject (Icon) HANYA untuk tombol yang sedang dikunci
            foreach (var input in skipInputs)
            {
                if (input.buttonIconObject != null)
                {
                    input.buttonIconObject.SetActive(input.skipKey == currentLockedKey);
                }
            }

            // Memunculkan UI secara perlahan (jika memakai CanvasGroup)
            if (skipUI != null)
            {
                skipUI.alpha = Mathf.MoveTowards(skipUI.alpha, 1f, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Jika tombol dilepas, progress turun dengan cepat (me-reset)
            currentHoldTime -= Time.deltaTime * 2f;
            if (currentHoldTime < 0) currentHoldTime = 0;

            // Menghilangkan UI jika progress sudah 0 (opsional)
            if (currentHoldTime <= 0) 
            {
                if (skipUI != null)
                {
                    skipUI.alpha = Mathf.MoveTowards(skipUI.alpha, 0f, Time.deltaTime * 3f);
                }

                // Matikan semua icon jika UI sudah hilang
                foreach (var input in skipInputs)
                {
                    if (input.buttonIconObject != null)
                    {
                        input.buttonIconObject.SetActive(false);
                    }
                }
            }
        }

        // Update visual lingkaran (Fill Amount dari 0.0 ke 1.0)
        if (progressCircle != null)
        {
            progressCircle.fillAmount = currentHoldTime / holdDuration;
        }

        // Jika waktu tahan sudah mencapai batas waktu yang ditentukan -> Eksekusi Skip
        if (currentHoldTime >= holdDuration)
        {
            ExecuteSkip();
        }
    }

    private void ExecuteSkip()
    {
        hasSkipped = true;
        Debug.Log("Cutscene berhasil di-skip!");

        // Jalankan event OnSkipComplete
        onSkipComplete?.Invoke();

        // Jika nama scene diisi di Inspector, langsung lakukan pemindahan scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneFader fader = FindFirstObjectByType<SceneFader>();
            if (fader != null)
            {
                fader.LoadScene(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
