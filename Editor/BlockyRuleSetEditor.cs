using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    [CustomEditor(typeof(BlockyRuleSet))]
    public class BlockyRuleSetEditor : UnityEditor.Editor
    {
        private SerializedProperty rulesProperty;
        private SerializedProperty defaultBlockProperty;
        private SerializedProperty layerProperty;
        private SerializedProperty thumbnailProperty;

        private void OnEnable()
        {
            rulesProperty = serializedObject.FindProperty("rules");
            defaultBlockProperty = serializedObject.FindProperty("defaultBlock");
            layerProperty = serializedObject.FindProperty("layer");
            thumbnailProperty = serializedObject.FindProperty("thumbnail");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var elem = new VisualElement();
            elem.styleSheets.Add(Resources.Load<StyleSheet>("BlockyEditor"));
            var thumbnailField = new PropertyField(thumbnailProperty);
            thumbnailField.Bind(serializedObject);
            var defaultField = new PropertyField(defaultBlockProperty);
            defaultField.Bind(serializedObject);
            var layerField = new PropertyField(layerProperty);
            layerField.Bind(serializedObject);
            var rulesField = new PropertyField(rulesProperty);
            rulesField.Bind(serializedObject);
            
            elem.Add(thumbnailField);
            elem.Add(defaultField);
            elem.Add(layerField);
            elem.Add(rulesField);
            return elem;
        }
    }
}