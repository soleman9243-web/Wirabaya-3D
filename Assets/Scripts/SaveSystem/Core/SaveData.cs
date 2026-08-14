using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Serializable Types — Wrapper untuk Unity types agar bisa JSON
// ============================================================

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);

    public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v);
    public static implicit operator Vector3(SerializableVector3 sv) => sv.ToVector3();
}

[Serializable]
public struct SerializableQuaternion
{
    public float x, y, z, w;

    public SerializableQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }

    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);

    public static implicit operator SerializableQuaternion(Quaternion q) => new SerializableQuaternion(q);
    public static implicit operator Quaternion(SerializableQuaternion sq) => sq.ToQuaternion();
}

// ============================================================
// Save Data — Container utama semua state yang disimpan
// ============================================================

public enum SaveType
{
    AutoSave,
    ManualSave
}

[Serializable]
public class ObjectiveSaveData
{
    public string objectiveId;
    public string description;
    public int targetAmount;
    public int currentAmount;
    public bool isCompleted;
}

[Serializable]
public class SaveData
{
    // === META INFO ===
    public string saveId;
    public SaveType saveType;
    public string timestamp;           // Waktu save (display di UI)
    public float playTime;             // Total waktu bermain dalam detik

    // === PROGRESS INFO ===
    public int chapterId;              // Chapter ke berapa (Act)
    public string sceneName;           // Nama scene Unity saat save
    public string chapterTitle;        // "Act 1: Kebangkitan" (untuk display)
    public bool isCutscene;            // Apakah scene ini cutscene

    // === QUEST STATE ===
    public string currentQuestId;
    public List<ObjectiveSaveData> objectiveProgress = new List<ObjectiveSaveData>();
    public List<string> completedObjectives = new List<string>();
    public int questDatabaseIndex;

    // === PLAYER STATE ===
    public float playerHealth;
    public float playerMaxHealth;
    public float playerStamina;
    public float playerMaxStamina;
    public float playerMana;
    public float playerMaxMana;
    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;

    // === SKILL STATE ===
    public bool canUseSkills = true;

    // === HELD ITEM STATE ===
    public string heldItemName;        // Nama ScriptableObject ItemData (untuk lookup via Resources)
    public int heldItemAmount;

    // === GAME FLAGS ===
    // Flag generik untuk story events, door unlocked, dll
    public List<string> gameFlagKeys = new List<string>();
    public List<bool> gameFlagValues = new List<bool>();

    // === Helper Methods ===

    /// <summary>
    /// Menghasilkan display string untuk UI (misal: "Auto Save — Act 1 | 14 Aug 2026 10:30")
    /// </summary>
    public string GetDisplayText()
    {
        string typeLabel = saveType == SaveType.AutoSave ? "Auto Save" : "Manual Save";
        return $"{typeLabel} — {chapterTitle} | {sceneName}\n{timestamp}";
    }

    /// <summary>
    /// Set game flag
    /// </summary>
    public void SetFlag(string key, bool value)
    {
        int index = gameFlagKeys.IndexOf(key);
        if (index >= 0)
        {
            gameFlagValues[index] = value;
        }
        else
        {
            gameFlagKeys.Add(key);
            gameFlagValues.Add(value);
        }
    }

    /// <summary>
    /// Get game flag (default: false)
    /// </summary>
    public bool GetFlag(string key)
    {
        int index = gameFlagKeys.IndexOf(key);
        return index >= 0 && gameFlagValues[index];
    }
}
