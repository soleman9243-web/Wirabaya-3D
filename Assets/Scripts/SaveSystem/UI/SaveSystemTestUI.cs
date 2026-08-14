using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI Test sederhana untuk Save & Load System.
/// Menggunakan OnGUI (IMGUI) agar tidak perlu setup Canvas/Prefab.
/// Cukup pasang script ini di GameObject mana saja di test scene.
/// 
/// Fitur:
/// - Tombol Auto Save (simulasi trigger chapter)
/// - Tombol Manual Save (slot 0-4)
/// - Tombol Load dari semua save yang ada
/// - Tombol Delete save
/// - Display info save slots
/// - Tombol simulasi damage/heal untuk test state
/// </summary>
public class SaveSystemTestUI : MonoBehaviour
{
    [Header("Chapter Reference (Untuk Test)")]
    [Tooltip("Assign ChapterDefinition untuk testing auto-save")]
    public ChapterDefinition testChapter;

    [Header("UI Settings")]
    public bool showUI = true;
    public KeyCode toggleKey = KeyCode.F5;

    private Vector2 scrollPosition;
    private List<SaveSlotInfo> cachedSlots = new List<SaveSlotInfo>();
    private string statusMessage = "";
    private float statusMessageTimer = 0f;

    // Simpan state cursor sebelum UI dibuka agar bisa dikembalikan
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showUI = !showUI;

            if (showUI)
            {
                // Simpan state cursor saat ini, lalu unlock
                previousLockState = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Kembalikan cursor ke state semula
                Cursor.lockState = previousLockState;
                Cursor.visible = previousCursorVisible;
            }
        }

        // Paksa cursor tetap visible selama UI terbuka
        // (mencegah script lain me-lock ulang cursor)
        if (showUI)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (statusMessageTimer > 0)
        {
            statusMessageTimer -= Time.unscaledDeltaTime;
            if (statusMessageTimer <= 0)
                statusMessage = "";
        }
    }

    private void OnGUI()
    {
        if (!showUI) return;

        // Styling
        GUI.skin.button.fontSize = 14;
        GUI.skin.label.fontSize = 13;
        GUI.skin.box.fontSize = 13;

        float panelWidth = 420f;
        float panelX = Screen.width - panelWidth - 20f;
        float panelY = 20f;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, Screen.height - 40f));
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // === HEADER ===
        GUILayout.Box("═══ SAVE SYSTEM TEST ═══", GUILayout.ExpandWidth(true));
        GUILayout.Space(5);

        // Status Message
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUI.color = Color.yellow;
            GUILayout.Label(statusMessage);
            GUI.color = Color.white;
            GUILayout.Space(5);
        }

        // === SCENE INFO ===
        GUILayout.Box($"Scene: {SceneManager.GetActiveScene().name}");
        if (SaveManager.Instance != null)
        {
            // Toggle Save System On/Off
            bool isEnabled = SaveManager.Instance.saveSystemEnabled;
            GUI.color = isEnabled ? Color.green : Color.red;
            string toggleLabel = isEnabled ? "✅ Save System: ON" : "❌ Save System: OFF (Testing Mode)";
            if (GUILayout.Button(toggleLabel, GUILayout.Height(30)))
            {
                SaveManager.Instance.saveSystemEnabled = !isEnabled;
                ShowStatus(isEnabled ? "Save System DISABLED — testing mode" : "Save System ENABLED");
            }
            GUI.color = Color.white;

            GUILayout.Label($"Can Manual Save: {SaveManager.Instance.CanManualSave()}");
            GUILayout.Label($"Save Dir: {SaveManager.Instance.GetSaveDirectory()}");
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label("⚠ SaveManager.Instance = NULL! Pastikan sudah ada di scene.");
            GUI.color = Color.white;
        }
        GUILayout.Space(10);

        // === PLAYER STATUS ===
        GUILayout.Box("── Player Status ──", GUILayout.ExpandWidth(true));
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus ps = PlayerStatus.Instance;
            GUILayout.Label($"HP: {ps.health:F0} / {ps.maxHealth:F0}");
            GUILayout.Label($"Stamina: {ps.stamina:F0} / {ps.maxStamina:F0}");
            GUILayout.Label($"Mana: {ps.mana:F0} / {ps.maxMana:F0}");
            GUILayout.Label($"Position: {ps.transform.position}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take 25 Damage"))
            {
                ps.TakeDamage(25f);
                ShowStatus("Player took 25 damage!");
            }
            if (GUILayout.Button("Heal 50"))
            {
                ps.Heal(50f);
                ShowStatus("Player healed 50!");
            }
            if (GUILayout.Button("Use 30 Mana"))
            {
                ps.UseMana(30f);
                ShowStatus("Used 30 mana!");
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("PlayerStatus not found in scene");
        }
        GUILayout.Space(10);

        // === QUEST STATUS ===
        GUILayout.Box("── Quest Status ──", GUILayout.ExpandWidth(true));
        if (QuestManager.Instance != null)
        {
            QuestManager qm = QuestManager.Instance;
            if (qm.currentQuest != null && qm.currentQuest.data != null)
            {
                GUILayout.Label($"Quest: {qm.currentQuest.data.title} ({qm.currentQuest.data.questId})");
                foreach (var obj in qm.currentQuest.objectives)
                {
                    string check = obj.isCompleted ? "✓" : "○";
                    GUILayout.Label($"  {check} {obj.description} [{obj.currentAmount}/{obj.targetAmount}]");
                }
            }
            else
            {
                GUILayout.Label("No active quest");
            }
            GUILayout.Label($"Completed Objectives: {qm.allCompletedObjectives.Count}");
        }
        else
        {
            GUILayout.Label("QuestManager not found in scene");
        }
        GUILayout.Space(10);

        // === SAVE ACTIONS ===
        GUILayout.Box("── Save Actions ──", GUILayout.ExpandWidth(true));

        // Auto Save
        if (GUILayout.Button("🔄 AUTO SAVE (Chapter)", GUILayout.Height(35)))
        {
            if (SaveManager.Instance != null && testChapter != null)
            {
                SaveManager.Instance.AutoSave(testChapter);
                ShowStatus($"Auto-saved chapter: {testChapter.chapterTitle}");
                RefreshSlots();
            }
            else
            {
                ShowStatus("ERROR: SaveManager atau testChapter belum di-assign!");
            }
        }

        GUILayout.Space(5);

        // Manual Save Slots
        GUILayout.Label("Manual Save Slots:");
        int maxSlots = SaveManager.Instance != null ? SaveManager.Instance.maxManualSlots : 5;
        for (int i = 0; i < maxSlots; i++)
        {
            int slotIndex = i; // lambda capture
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button($"💾 Save Slot {i}", GUILayout.Height(28)))
            {
                if (SaveManager.Instance != null)
                {
                    bool success = SaveManager.Instance.ManualSave(slotIndex);
                    ShowStatus(success ? $"Saved to slot {slotIndex}!" : $"Failed to save slot {slotIndex}!");
                    RefreshSlots();
                }
            }

            // Quick load button
            string manualFile = SaveFileHandler.GetManualSaveFileName(slotIndex);
            if (SaveFileHandler.Exists(manualFile))
            {
                if (GUILayout.Button("Load", GUILayout.Width(50), GUILayout.Height(28)))
                {
                    SaveManager.Instance.LoadManualSave(slotIndex);
                }
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(28)))
                {
                    SaveManager.Instance.DeleteSave(manualFile);
                    ShowStatus($"Deleted slot {slotIndex}");
                    RefreshSlots();
                }
            }
            
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(10);

        // === LOAD SECTION ===
        GUILayout.Box("── All Save Files ──", GUILayout.ExpandWidth(true));

        if (GUILayout.Button("🔃 Refresh Save List"))
        {
            RefreshSlots();
        }

        if (cachedSlots.Count == 0)
        {
            GUILayout.Label("Tidak ada save file ditemukan.");
        }
        else
        {
            foreach (var slot in cachedSlots)
            {
                GUILayout.Space(5);
                GUI.color = slot.data.saveType == SaveType.AutoSave 
                    ? new Color(0.6f, 1f, 0.6f) 
                    : new Color(0.6f, 0.8f, 1f);
                
                GUILayout.Box(slot.data.GetDisplayText(), GUILayout.ExpandWidth(true));
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"  HP:{slot.data.playerHealth:F0} Pos:{slot.data.playerPosition.ToVector3():F1}", 
                    GUILayout.ExpandWidth(true));

                if (GUILayout.Button("📂 Load", GUILayout.Width(70), GUILayout.Height(25)))
                {
                    SaveManager.Instance.LoadGame(slot.fileName);
                }
                if (GUILayout.Button("🗑", GUILayout.Width(30), GUILayout.Height(25)))
                {
                    SaveManager.Instance.DeleteSave(slot.fileName);
                    ShowStatus($"Deleted: {slot.fileName}");
                    RefreshSlots();
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(10);

        // === SCENE NAVIGATION (Test) ===
        GUILayout.Box("── Scene Navigation (Test) ──", GUILayout.ExpandWidth(true));
        if (testChapter != null)
        {
            foreach (var scene in testChapter.scenes)
            {
                string label = scene.isCutscene ? $"🎬 {scene.sceneName}" : $"🎮 {scene.sceneName}";
                if (GUILayout.Button(label, GUILayout.Height(25)))
                {
                    SceneManager.LoadScene(scene.sceneName);
                }
            }
        }
        else
        {
            GUILayout.Label("Assign testChapter untuk navigasi scene");
        }

        GUILayout.Space(10);

        // Toggle
        GUI.color = Color.gray;
        GUILayout.Label($"Tekan [{toggleKey}] untuk hide/show panel ini");
        GUI.color = Color.white;

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void RefreshSlots()
    {
        if (SaveManager.Instance != null)
        {
            cachedSlots = SaveManager.Instance.GetAllSaveSlots();
        }
    }

    private void ShowStatus(string message)
    {
        statusMessage = message;
        statusMessageTimer = 3f;
        Debug.Log($"[SaveTestUI] {message}");
    }

    private void OnEnable()
    {
        RefreshSlots();
    }
}
