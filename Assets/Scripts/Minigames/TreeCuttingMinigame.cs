using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

public class TreeCuttingMinigame : MonoBehaviour
{
    // Singleton agar mudah dipanggil dari script mana saja (misal script Player) tanpa perlu ditaruh di tiap pohon
    public static TreeCuttingMinigame Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Objek Canvas atau Panel yang membungkus UI minigame.")]
    [SerializeField] private GameObject minigameUI;
    [Tooltip("Background Bar tempat target dan garis berada.")]
    [SerializeField] private RectTransform minigameBar;
    [Tooltip("Kotak target yang harus ditepatkan.")]
    [SerializeField] private RectTransform targetArea;
    [Tooltip("Garis yang bergerak bolak-balik.")]
    [SerializeField] private RectTransform movingLine;

    [Header("Cinematics & Effects")]
    [Tooltip("Kamera (Virtual Camera Cinemachine) yang dibawa player.")]
    [SerializeField] private CinemachineVirtualCamera treeCamera;
    [Tooltip("Sumber getaran kamera saat kapak mengenai pohon.")]
    [SerializeField] private CinemachineImpulseSource shakeImpulse;

    [Header("Settings")]
    [Tooltip("Centang ini jika UI Bar Anda berbentuk vertikal (berdiri), hilangkan centang jika horizontal (mendatar).")]
    [SerializeField] private bool isVertical = true;
    [SerializeField] private float baseLineSpeed = 500f;
    [Tooltip("Kecepatan dikalikan dengan nilai ini setiap kali berhasil memukul target.")]
    [SerializeField] private float speedMultiplierPerHit = 1.25f;
    [SerializeField] private int maxHits = 3;

    [Header("Difficulty & Penalties")]
    [Tooltip("Daftar ukuran target area dari tebangan pertama hingga terakhir. (Misal: 100, 75, 50)")]
    [SerializeField] private float[] targetWidths = { 100f, 75f, 50f };
    [Tooltip("Kecepatan tambahan saat pemain gagal memukul (dikali per jumlah gagal).")]
    [SerializeField] private float speedMultiplierPerFail = 1.2f;
    [Tooltip("Batas maksimal akumulasi gagal yang mempercepat garis. Jika diset 3, maka gagal ke-4 tidak akan membuat garis bertambah cepat lagi.")]
    [SerializeField] private int maxFailPenaltyCount = 3;
    [Tooltip("Waktu delay pinalti (detik) tambahan garis berhenti saat pemain gagal memukul.")]
    [SerializeField] private float failDelay = 1f;
    [Tooltip("Waktu tunggu (detik) dari saat diklik hingga kapak menyentuh pohon (sesuaikan dengan animasi ayunan agar efek getar sinkron).")]
    [SerializeField] private float strikeHitDelay = 0.4f;

    [Header("Player Integration")]
    [Tooltip("Posisi Transform pemain (untuk menghitung arah jatuh pohon & pergerakan).")]
    public Transform playerTransform;
    [Tooltip("Komponen Animator pemain untuk memutar animasi tebang.")]
    public Animator playerAnimator;
    [Tooltip("Jarak dari titik tengah pohon saat karakter menebang (karakter akan otomatis maju sedekat angka ini).")]
    public float choppingDistance = 1.2f;
    [Tooltip("Offset rotasi karakter (derajat). Ubah jika animasi ayunan karakter melenceng ke kiri/kanan pohon.")]
    public float playerRotationOffset = 0f;

    [Header("Animation Triggers (Opsional)")]
    public string startChoppingTrigger = "StartChopping";
    public string chopStrikeTrigger = "ChopStrike";
    public string stopChoppingTrigger = "StopChopping";

    [Header("Events")]
    public UnityEvent OnMinigameComplete;
    public UnityEvent OnMinigameFailedRound;

    private CuttableTree currentTree;
    private float currentLineSpeed;
    private bool isPlaying = false;
    private bool movingRight = true;
    private MonoBehaviour playerMovement;
    private bool isStriking = false;
    private Coroutine strikeCoroutine;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (minigameUI != null)
        {
            minigameUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        // Keluar dari minigame jika memencet ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitMinigame();
            return;
        }

        // Garis hanya bergerak dan menerima klik jika tidak sedang dalam proses mengayun
        if (!isStriking)
        {
            MoveLine();

            // Menggunakan klik kiri untuk memukul
            if (Input.GetMouseButtonDown(0))
            {
                CheckHit();
            }
        }
    }

    /// <summary>
    /// Panggil fungsi ini dari script Player saat mendeteksi/memotong pohon.
    /// Contoh: TreeCuttingMinigame.Instance.StartMinigame(pohonYangDitebang);
    /// </summary>
    public void StartMinigame(CuttableTree targetTree)
    {
        if (targetTree == null) return;
        
        currentTree = targetTree;

        if (currentTree.currentHits >= maxHits) 
        {
            Debug.LogWarning("Minigame di pohon ini sudah selesai ditebang!");
            return;
        }

        // SYARAT: Cek apakah pedang/kapak sudah di-equip (ditarik dari sarung)
        if (playerTransform != null)
        {
            MonoBehaviour pc = playerTransform.GetComponent("PlayerControl") as MonoBehaviour;
            if (pc != null)
            {
                System.Reflection.FieldInfo sheathedField = pc.GetType().GetField("isSwordSheathed");
                if (sheathedField != null)
                {
                    bool isSheathed = (bool)sheathedField.GetValue(pc);
                    if (isSheathed)
                    {
                        Debug.LogWarning("Anda harus menarik pedang/kapak terlebih dahulu (Tekan R)!");
                        return; // Batal mulai minigame
                    }
                }
            }
        }

        // Pindah posisi mendekati pohon tanpa mengubah sudut asal pemain
        if (playerTransform != null)
        {
            // 1. Hitung arah dari pohon ke pemain
            Vector3 dirToPlayer = playerTransform.position - currentTree.transform.position;
            dirToPlayer.y = 0; // Abaikan perbedaan tinggi
            dirToPlayer.Normalize();

            // 2. Hitung posisi baru (sejauh 'choppingDistance' dari tengah pohon ke arah pemain)
            Vector3 newPos = currentTree.transform.position + (dirToPlayer * choppingDistance);
            newPos.y = playerTransform.position.y; // Pertahankan tinggi asli pijakan pemain
            
            playerTransform.position = newPos;

            // 4. Putar badan pemain menghadap persis ke tengah pohon
            Vector3 lookPos = currentTree.transform.position;
            lookPos.y = playerTransform.position.y;
            playerTransform.LookAt(lookPos);

            // Tambahkan offset rotasi jika animasi melenceng
            if (playerRotationOffset != 0f)
            {
                playerTransform.Rotate(0, playerRotationOffset, 0);
            }

            // Kunci pergerakan pemain (Cari script ThirdPersonController dari StarterAssets)
            playerMovement = playerTransform.GetComponent("ThirdPersonController") as MonoBehaviour;
            if (playerMovement == null) playerMovement = playerTransform.GetComponent("StarterAssets.ThirdPersonController") as MonoBehaviour;
            
            if (playerMovement != null) playerMovement.enabled = false;

            // 4. Putar pohon agar bagian depannya (tempat luka tebangan 3D) selalu menghadap pemain
            if (dirToPlayer != Vector3.zero)
            {
                // Karena posisinya terbalik, kita memutar rotasinya 180 derajat (atau menggunakan -dirToPlayer)
                // agar bagian luka yang ada di belakang berputar ke depan menghadap player.
                currentTree.transform.rotation = Quaternion.LookRotation(-dirToPlayer);
            }
        }

        // Reset state
        isStriking = false;
        if (strikeCoroutine != null) StopCoroutine(strikeCoroutine);

        // Mainkan animasi bersiap tebang
        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(stopChoppingTrigger)) playerAnimator.ResetTrigger(stopChoppingTrigger);
            if (!string.IsNullOrEmpty(chopStrikeTrigger)) playerAnimator.ResetTrigger(chopStrikeTrigger);
            if (!string.IsNullOrEmpty(startChoppingTrigger)) playerAnimator.SetTrigger(startChoppingTrigger);
        }

        // Tampilkan UI
        if (minigameUI != null) minigameUI.SetActive(true);
        isPlaying = true;

        // Arahkan kamera player ke pohon ini dan nyalakan kamera
        if (treeCamera != null)
        {
            treeCamera.LookAt = currentTree.GetFocusPoint();
            treeCamera.gameObject.SetActive(true);
        }
        
        UpdateTargetSize();
        
        // Kalkulasi kecepatan saat ini berdasarkan hit dan fail (dengan batas fail)
        int countedFails = Mathf.Min(currentTree.currentFails, maxFailPenaltyCount);
        currentLineSpeed = baseLineSpeed * Mathf.Pow(speedMultiplierPerHit, currentTree.currentHits) * Mathf.Pow(speedMultiplierPerFail, countedFails);
        
        RandomizeTargetPosition();
        UpdateTreeVisuals();
        
        // Mulai garis dari tengah
        movingLine.anchoredPosition = new Vector2(0, movingLine.anchoredPosition.y);
    }

    private void UpdateTargetSize()
    {
        if (targetWidths != null && targetWidths.Length > 0)
        {
            int index = Mathf.Clamp(currentTree.currentHits, 0, targetWidths.Length - 1);
            float newSize = targetWidths[index];
            if (isVertical)
            {
                targetArea.sizeDelta = new Vector2(targetArea.sizeDelta.x, newSize);
            }
            else
            {
                targetArea.sizeDelta = new Vector2(newSize, targetArea.sizeDelta.y);
            }
        }
    }

    private void MoveLine()
    {
        float maxLimit = isVertical ? 
            (minigameBar.rect.height / 2f) - (movingLine.rect.height / 2f) : 
            (minigameBar.rect.width / 2f) - (movingLine.rect.width / 2f);
        float minLimit = -maxLimit;

        float step = currentLineSpeed * Time.deltaTime;

        if (movingRight) // Jika vertical, ini berarti bergerak ke ATAS
        {
            if (isVertical)
            {
                movingLine.anchoredPosition += new Vector2(0, step);
                if (movingLine.anchoredPosition.y >= maxLimit)
                {
                    movingLine.anchoredPosition = new Vector2(movingLine.anchoredPosition.x, maxLimit);
                    movingRight = false;
                }
            }
            else
            {
                movingLine.anchoredPosition += new Vector2(step, 0);
                if (movingLine.anchoredPosition.x >= maxLimit)
                {
                    movingLine.anchoredPosition = new Vector2(maxLimit, movingLine.anchoredPosition.y);
                    movingRight = false;
                }
            }
        }
        else // Jika vertical, ini berarti bergerak ke BAWAH
        {
            if (isVertical)
            {
                movingLine.anchoredPosition -= new Vector2(0, step);
                if (movingLine.anchoredPosition.y <= minLimit)
                {
                    movingLine.anchoredPosition = new Vector2(movingLine.anchoredPosition.x, minLimit);
                    movingRight = true;
                }
            }
            else
            {
                movingLine.anchoredPosition -= new Vector2(step, 0);
                if (movingLine.anchoredPosition.x <= minLimit)
                {
                    movingLine.anchoredPosition = new Vector2(minLimit, movingLine.anchoredPosition.y);
                    movingRight = true;
                }
            }
        }
    }

    private void CheckHit()
    {
        if (isStriking) return;

        float targetMin = isVertical ? 
            targetArea.anchoredPosition.y - (targetArea.rect.height / 2f) : 
            targetArea.anchoredPosition.x - (targetArea.rect.width / 2f);
            
        float targetMax = isVertical ? 
            targetArea.anchoredPosition.y + (targetArea.rect.height / 2f) : 
            targetArea.anchoredPosition.x + (targetArea.rect.width / 2f);
            
        float linePos = isVertical ? movingLine.anchoredPosition.y : movingLine.anchoredPosition.x;

        bool isHit = (linePos >= targetMin && linePos <= targetMax);

        if (strikeCoroutine != null) StopCoroutine(strikeCoroutine);
        strikeCoroutine = StartCoroutine(StrikeRoutine(isHit));
    }

    private System.Collections.IEnumerator StrikeRoutine(bool isHit)
    {
        isStriking = true;

        if (isHit)
        {
            // SUKSES MENEBANG
            // Panggil trigger animasi ayun kapak HANYA saat sukses
            if (playerAnimator != null)
            {
                if (!string.IsNullOrEmpty(chopStrikeTrigger)) playerAnimator.ResetTrigger(chopStrikeTrigger);
                if (!string.IsNullOrEmpty(chopStrikeTrigger)) playerAnimator.SetTrigger(chopStrikeTrigger);
            }

            // Tunggu ayunan kapak sampai menyentuh pohon
            yield return new WaitForSeconds(strikeHitDelay);

            currentTree.currentHits++;
            UpdateTreeVisuals();

            // Efek partikel dan getar (terjadi pas kapak nyentuh pohon)
            if (currentTree != null && currentTree.hitParticle != null) 
            {
                // Cek apakah partikel ini berupa Prefab (belum ada di Scene)
                if (!currentTree.hitParticle.gameObject.scene.IsValid())
                {
                    // Spawn partikel baru di posisi pohon (ketinggian dada)
                    ParticleSystem spawnedParticle = Instantiate(currentTree.hitParticle, currentTree.transform.position + Vector3.up * 1.2f, Quaternion.identity);
                    spawnedParticle.Play();
                    Destroy(spawnedParticle.gameObject, spawnedParticle.main.duration + 1f); // Bersihkan setelah selesai
                }
                else
                {
                    // Partikel sudah ada di dalam Scene
                    currentTree.hitParticle.gameObject.SetActive(true);
                    currentTree.hitParticle.Play();
                }
            }
            if (shakeImpulse != null) shakeImpulse.GenerateImpulse();

            if (currentTree.currentHits >= maxHits)
            {
                MinigameWin();
            }
            else
            {
                // Lanjut ke tebasan berikutnya
                UpdateTargetSize();
                int countedFails = Mathf.Min(currentTree.currentFails, maxFailPenaltyCount);
                currentLineSpeed = baseLineSpeed * Mathf.Pow(speedMultiplierPerHit, currentTree.currentHits) * Mathf.Pow(speedMultiplierPerFail, countedFails);
                RandomizeTargetPosition();
                
                isStriking = false;
            }
        }
        else
        {
            // GAGAL MENEBANG
            if (currentTree != null)
            {
                currentTree.currentFails++;
            }
            
            OnMinigameFailedRound?.Invoke();

            // Animasi UI Bergetar saat gagal
            RectTransform uiRect = minigameUI.GetComponent<RectTransform>();
            Vector2 originalPos = Vector2.zero;
            if (uiRect != null) originalPos = uiRect.anchoredPosition;

            float shakeTimer = 0f;
            while (shakeTimer < failDelay)
            {
                shakeTimer += Time.deltaTime;
                if (uiRect != null)
                {
                    // Getar acak ke kiri-kanan dan atas-bawah dengan intensitas 10 pixel
                    float offsetX = UnityEngine.Random.Range(-10f, 10f);
                    float offsetY = UnityEngine.Random.Range(-5f, 5f);
                    uiRect.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
                }
                yield return null;
            }

            // Kembalikan UI ke posisi semula setelah selesai getar
            if (uiRect != null) uiRect.anchoredPosition = originalPos;

            // Perbarui kecepatan akibat pinalti gagal
            int countedFails = Mathf.Min(currentTree.currentFails, maxFailPenaltyCount);
            currentLineSpeed = baseLineSpeed * Mathf.Pow(speedMultiplierPerHit, currentTree.currentHits) * Mathf.Pow(speedMultiplierPerFail, countedFails);
            
            isStriking = false;
        }
    }

    private void MinigameWin()
    {
        isPlaying = false;
        isStriking = false;
        if (minigameUI != null) minigameUI.SetActive(false);
        if (treeCamera != null) treeCamera.gameObject.SetActive(false);
        
        // Panggil animasi berhenti dan bersihkan sisa trigger
        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(chopStrikeTrigger)) playerAnimator.ResetTrigger(chopStrikeTrigger);
            if (!string.IsNullOrEmpty(startChoppingTrigger)) playerAnimator.ResetTrigger(startChoppingTrigger);
            if (!string.IsNullOrEmpty(stopChoppingTrigger)) playerAnimator.SetTrigger(stopChoppingTrigger);
        }

        // Buka kunci pergerakan
        if (playerMovement != null) playerMovement.enabled = true;

        // Picu animasi jatuh dan dissolve
        if (currentTree != null)
        {
            currentTree.TriggerFallAndDissolve(playerTransform);
            currentTree.DisableInteraction(); // Matikan interaksi agar teks tidak muncul lagi
        }

        // Reset target tree agar bisa dipakai pohon lain
        currentTree = null;
        
        OnMinigameComplete?.Invoke();
    }

    private void QuitMinigame()
    {
        isPlaying = false;
        isStriking = false;
        if (strikeCoroutine != null) StopCoroutine(strikeCoroutine);

        if (minigameUI != null) minigameUI.SetActive(false);
        if (treeCamera != null) treeCamera.gameObject.SetActive(false);
        
        // Bersihkan sisa trigger
        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(chopStrikeTrigger)) playerAnimator.ResetTrigger(chopStrikeTrigger);
            if (!string.IsNullOrEmpty(startChoppingTrigger)) playerAnimator.ResetTrigger(startChoppingTrigger);
            if (!string.IsNullOrEmpty(stopChoppingTrigger)) playerAnimator.SetTrigger(stopChoppingTrigger);
        }

        if (playerMovement != null) playerMovement.enabled = true;
        currentTree = null;
    }

    private void RandomizeTargetPosition()
    {
        float maxLimit = isVertical ? 
            (minigameBar.rect.height / 2f) - (targetArea.rect.height / 2f) : 
            (minigameBar.rect.width / 2f) - (targetArea.rect.width / 2f);
        float minLimit = -maxLimit;
        
        float randomPos = Random.Range(minLimit, maxLimit);
        
        if (isVertical)
            targetArea.anchoredPosition = new Vector2(targetArea.anchoredPosition.x, randomPos);
        else
            targetArea.anchoredPosition = new Vector2(randomPos, targetArea.anchoredPosition.y);
    }

    private void UpdateTreeVisuals()
    {
        if (currentTree == null || currentTree.treeStages == null || currentTree.treeStages.Length == 0) return;

        // Nonaktifkan semua model di pohon yang sedang dipotong
        for (int i = 0; i < currentTree.treeStages.Length; i++)
        {
            if (currentTree.treeStages[i] != null)
                currentTree.treeStages[i].SetActive(false);
        }

        // Aktifkan model yang sesuai dengan jumlah hit
        int stageIndex = Mathf.Clamp(currentTree.currentHits, 0, currentTree.treeStages.Length - 1);
        if (currentTree.treeStages[stageIndex] != null)
        {
            currentTree.treeStages[stageIndex].SetActive(true);
        }
    }
}
