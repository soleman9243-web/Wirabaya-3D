using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject yang mendefinisikan sebuah Chapter (Act).
/// Berisi info tentang urutan scene dalam chapter tersebut.
/// 
/// Konvensi penamaan scene Wirabaya:
/// - Normal scene: "{urutan}ProtAct{actNumber}" (contoh: 2ProtAct1, 4ProtAct1)
/// - Cutscene:     "{urutan}ProtSceneAct{actNumber}" (contoh: 1ProtSceneAct1, 3ProtSceneAct1)
/// </summary>
[CreateAssetMenu(fileName = "New Chapter", menuName = "Save System/Chapter Definition")]
public class ChapterDefinition : ScriptableObject
{
    [Header("Chapter Info")]
    [Tooltip("ID numerik chapter (Act). Contoh: 1 untuk Act 1")]
    public int chapterId;

    [Tooltip("Judul chapter untuk ditampilkan di UI. Contoh: 'Act 1: Kebangkitan'")]
    public string chapterTitle;

    [Header("Scene List (Urut)")]
    [Tooltip("Daftar nama scene dalam chapter ini, HARUS urut sesuai alur cerita.")]
    public List<SceneEntry> scenes = new List<SceneEntry>();

    [System.Serializable]
    public class SceneEntry
    {
        [Tooltip("Nama scene persis seperti di Build Settings")]
        public string sceneName;

        [Tooltip("Apakah scene ini cutscene? (nama mengandung 'Scene')")]
        public bool isCutscene;

        [Tooltip("Deskripsi singkat scene ini (opsional, untuk UI)")]
        public string description;
    }

    /// <summary>
    /// Cek apakah scene name termasuk dalam chapter ini.
    /// </summary>
    public bool ContainsScene(string sceneName)
    {
        foreach (var entry in scenes)
        {
            if (entry.sceneName == sceneName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Mendapatkan scene pertama di chapter ini (untuk auto-save / load chapter).
    /// </summary>
    public string GetFirstSceneName()
    {
        if (scenes.Count > 0)
            return scenes[0].sceneName;
        return "";
    }

    /// <summary>
    /// Cek apakah scene tertentu adalah cutscene.
    /// </summary>
    public bool IsSceneCutscene(string sceneName)
    {
        foreach (var entry in scenes)
        {
            if (entry.sceneName == sceneName)
                return entry.isCutscene;
        }
        return false;
    }

    /// <summary>
    /// Mendapatkan index scene dalam chapter. -1 jika tidak ditemukan.
    /// </summary>
    public int GetSceneIndex(string sceneName)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneName == sceneName)
                return i;
        }
        return -1;
    }
}
