using System.Collections.Generic;
using ClusterVR.CreatorKit.Media.Implements;
using UnityEditor;
using UnityEngine;

namespace ClusterVR.CreatorKit.Editor.Custom
{
    [CustomPropertyDrawer(typeof(MediaPlayerRenderer))]
    public sealed class MediaPlayerRendererEditor : PropertyDrawer
    {
        static readonly GUIContent GuiTextTextureProperty = new("Texture Property");
        static readonly float RowHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        readonly List<string> textureNames = new();
        readonly List<GUIContent> materialTextureProperties = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propRenderer = property.FindPropertyRelative("renderer");
            var propTexturePropertyName = property.FindPropertyRelative("texturePropertyName");

            textureNames.Clear();
            materialTextureProperties.Clear();

            var texturePropertyIndex = 0;

            if (propRenderer.objectReferenceValue != null)
            {
                var renderer = (Renderer) (propRenderer.objectReferenceValue);

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }
                    var matProps = MaterialEditor.GetMaterialProperties(new Object[] { material });
                    foreach (var matProp in matProps)
                    {
                        if (matProp.type != MaterialProperty.PropType.Texture)
                        {
                            continue;
                        }
                        if (textureNames.Contains(matProp.name))
                        {
                            continue;
                        }
                        textureNames.Add(matProp.name);

                        if (matProp.name == propTexturePropertyName.stringValue)
                        {
                            texturePropertyIndex = materialTextureProperties.Count;
                        }
                        materialTextureProperties.Add(new GUIContent(matProp.name));
                    }
                }
            }

            var fieldRect = position;
            fieldRect.height = RowHeight;

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                property.isExpanded = EditorGUI.Foldout(new Rect(fieldRect), property.isExpanded, label);
                if (!property.isExpanded)
                {
                    return;
                }
                using (new EditorGUI.IndentLevelScope())
                {
                    fieldRect.y += RowHeight;
                    EditorGUI.PropertyField(new Rect(fieldRect), propRenderer);

                    fieldRect.y += RowHeight;
                    var newTexturePropertyIndex = EditorGUI.Popup(
                        new Rect(fieldRect),
                        GuiTextTextureProperty,
                        texturePropertyIndex,
                        materialTextureProperties.ToArray());
                    if (newTexturePropertyIndex >= 0 && newTexturePropertyIndex < materialTextureProperties.Count)
                    {
                        propTexturePropertyName.stringValue = materialTextureProperties[newTexturePropertyIndex].text;
                    }
                }
            }
        }
    }
}
