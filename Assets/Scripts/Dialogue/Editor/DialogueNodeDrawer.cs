#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueNode))]
public class DialogueNodeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Fix untuk elemen branchNodes yang kosong akibat [SerializeReference]
        if (property.propertyType == SerializedPropertyType.ManagedReference && property.managedReferenceValue == null)
        {
            Rect btnRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(btnRect, $"Init Node (Kosong)"))
            {
                property.managedReferenceValue = new DialogueNode();
                property.serializedObject.ApplyModifiedProperties();
            }
            return;
        }

        // Ambil index dari path property (misal: "nodes.Array.data[5]" -> ambil angka 5)
        string path = property.propertyPath;
        int index = 0;
        int bracketIndex = path.LastIndexOf('[');
        if (bracketIndex >= 0)
        {
            string indexStr = path.Substring(bracketIndex + 1, path.Length - bracketIndex - 2);
            int.TryParse(indexStr, out index);
        }

        // Ambil nama pembicara jika ada
        SerializedProperty speakerProp = property.FindPropertyRelative("speakerName");
        string speakerName = speakerProp != null ? speakerProp.stringValue : "";

        // Ubah label bawaan menjadi "Node X - [Nama]"
        label.text = $"Node {index}" + (string.IsNullOrEmpty(speakerName) ? "" : $" - {speakerName}");

        // Gambar properti aslinya
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Pastikan tingginya sesuai dengan semua isinya
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif
