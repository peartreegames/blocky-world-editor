using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.Blocky.World.Editor
{
    [CustomEditor(typeof(BlockyRuleSet))]
    public class BlockyRuleSetEditor : UnityEditor.Editor
    {
        private SerializedProperty rulesProperty;
        private SerializedProperty defaultBlockProperty;
        private SerializedProperty layerProperty;
        private SerializedProperty thumbnailProperty;
        private SerializedProperty slopesProperty;
        private SerializedProperty slopeAscentProperty;

        private void OnEnable()
        {
            rulesProperty = serializedObject.FindProperty("rules");
            defaultBlockProperty = serializedObject.FindProperty("defaultBlock");
            layerProperty = serializedObject.FindProperty("layer");
            thumbnailProperty = serializedObject.FindProperty("thumbnail");
            slopesProperty = serializedObject.FindProperty("slopes");
            slopeAscentProperty = serializedObject.FindProperty("slopeAscent");
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
            var thumbnailImage = new Image { scaleMode = ScaleMode.ScaleToFit, style = { width = 128, height = 128, alignSelf = Align.Center } };
            void ShowThumbnail(SerializedProperty property)
            {
                thumbnailImage.image = property.objectReferenceValue as Texture2D;
                thumbnailImage.style.display = thumbnailImage.image != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            ShowThumbnail(thumbnailProperty);
            thumbnailImage.TrackPropertyValue(thumbnailProperty, ShowThumbnail);
            elem.Add(thumbnailImage);
            elem.Add(new Button(GenerateThumbnail) { text = "Generate Thumbnail" });
            elem.Add(new Button(DeleteThumbnail) { text = "Delete Thumbnail" });
            elem.Add(defaultField);
            elem.Add(layerField);
            elem.Add(rulesField);
            foreach (var property in new[] { slopesProperty, slopeAscentProperty })
            {
                var field = new PropertyField(property);
                field.Bind(serializedObject);
                elem.Add(field);
            }
            return elem;
        }

        private void GenerateThumbnail()
        {
            var ruleSet = (BlockyRuleSet)target;
            DeleteThumbnail();
            var texture = ruleSet.RenderThumbnail();
            texture.name = "Thumbnail";
            AssetDatabase.AddObjectToAsset(texture, ruleSet);
            serializedObject.Update();
            thumbnailProperty.objectReferenceValue = texture;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(ruleSet);
        }

        private void DeleteThumbnail()
        {
            var children = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(target)).OfType<Texture2D>().ToArray();
            if (children.Length == 0) return;
            serializedObject.Update();
            if (children.Contains(thumbnailProperty.objectReferenceValue)) thumbnailProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            foreach (var child in children) DestroyImmediate(child, true);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }
}