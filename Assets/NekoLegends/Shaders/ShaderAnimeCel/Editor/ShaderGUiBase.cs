#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace NekoLegends
{
    public class ShaderGUIBase : ShaderGUI
    {
        protected MaterialEditor materialEditor;
        protected MaterialProperty[] materialProperties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.materialEditor = materialEditor;
            this.materialProperties = properties;
        }

        protected MaterialProperty FindProperty(string propertyName)
        {
            if (materialProperties == null)
                return null;

            return ShaderGUI.FindProperty(propertyName, materialProperties, false);
        }

        protected void DrawProperty(MaterialProperty property, string label = null)
        {
            if (property == null || materialEditor == null)
                return;

            if (string.IsNullOrEmpty(label))
                materialEditor.ShaderProperty(property, property.displayName);
            else
                materialEditor.ShaderProperty(property, label);
        }

        protected void ShowLogo()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Neko Legends Cel Shader", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
        }
    }
}

#endif