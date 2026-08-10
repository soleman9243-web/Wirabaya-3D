using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor untuk AnimationSoundPlayer.
/// Menampilkan panduan cara pakai langsung di Inspector agar tidak perlu buka dokumentasi.
/// </summary>
[CustomEditor(typeof(AnimationSoundPlayer))]
public class AnimationSoundPlayerEditor : Editor
{
    private bool showGuide = true;

    public override void OnInspectorGUI()
    {
        // Gambar default Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        showGuide = EditorGUILayout.Foldout(showGuide, "📖 Info Animation Event", true, EditorStyles.foldoutHeader);

        if (showGuide)
        {
            EditorGUILayout.HelpBox(
                "Tambahkan Animation Event di klip animasi, lalu panggil salah satu fungsi ini:\n\n" +
                "1. PlayRandomFromGroup (isi parameter String dengan nama Grup, misal: 'Step')\n" +
                "2. PlaySoundByName (isi parameter String dengan nama spesifik file suaranya)",
                MessageType.Info);
        }
    }
}
