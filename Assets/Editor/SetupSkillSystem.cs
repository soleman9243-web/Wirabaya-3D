using UnityEngine;
using UnityEditor;

public class SetupSkillSystem : EditorWindow
{
    [MenuItem("Wirabaya/Setup Skill System (Base)")]
    static void SetupSkills()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Setup Skill System", "Pilih Player GameObject di Hierarchy terlebih dahulu!", "OK");
            return;
        }

        PlayerControl playerControl = selected.GetComponent<PlayerControl>();
        if (playerControl == null)
        {
            EditorUtility.DisplayDialog("Setup Skill System", "GameObject yang dipilih tidak memiliki komponen PlayerControl!", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Setup Skill System (Base)");
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Tambahkan SkillManager
        SkillManager manager = selected.GetComponent<SkillManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<SkillManager>(selected);
            manager.canUseSkills = playerControl.canUseSkills;
            Debug.Log("[Setup] SkillManager ditambahkan.");
        }

        // 2. Tambahkan SkillAwakening
        SkillAwakening awakening = selected.GetComponent<SkillAwakening>();
        if (awakening == null)
        {
            awakening = Undo.AddComponent<SkillAwakening>(selected);
            Debug.Log("[Setup] SkillAwakening ditambahkan.");
        }

        // Auto-assign dependensi Awakening
        awakening.playerControl = playerControl;
        awakening.skillName = "Awakening";
        awakening.keyBind = KeyCode.V;
        awakening.manaCost = 100f; // Sesuai dengan PlayerControl sebelumnya

        // 3. Tambahkan SkillSpawnBuaya
        SkillSpawnBuaya spawnBuaya = selected.GetComponent<SkillSpawnBuaya>();
        if (spawnBuaya == null)
        {
            spawnBuaya = Undo.AddComponent<SkillSpawnBuaya>(selected);
            Debug.Log("[Setup] SkillSpawnBuaya ditambahkan.");
        }

        // Auto-assign dependensi Spawn Buaya
        spawnBuaya.skillName = "Spawn Mulut Buaya";
        spawnBuaya.keyBind = KeyCode.B;
        spawnBuaya.manaCost = 50f;

        // 4. Register skill ke SkillManager
        manager.skills.Clear();
        manager.skills.Add(awakening);
        manager.skills.Add(spawnBuaya);

        // Mark dirty agar tersimpan
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(awakening);
        EditorUtility.SetDirty(spawnBuaya);
        EditorUtility.SetDirty(selected);

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Setup Skill System",
            "Sistem Skill (Base) berhasil di-setup pada Player!\n\n" +
            "Komponen yang ditambahkan:\n" +
            "1. SkillManager\n" +
            "2. SkillAwakening (Key: V)\n" +
            "3. SkillSpawnBuaya (Key: B)\n\n" +
            "Jangan lupa assign 'Buaya Prefab' dan 'Spawn Point' di komponen SkillSpawnBuaya melalui Inspector.", "OK");
    }
}
