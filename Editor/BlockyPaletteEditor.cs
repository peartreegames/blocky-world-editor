using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    [CustomEditor(typeof(BlockyPalette))]
    public class BlockyPaletteEditor : UnityEditor.Editor
    {
        private SerializedProperty blocksProperty;
        private void OnEnable()
        {
            blocksProperty = serializedObject.FindProperty("blocks");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var blocks = new PropertyField(blocksProperty);
            blocks.styleSheets.Add(Resources.Load<StyleSheet>("BlockyPalette"));
            blocks.AddToClassList("palette-list");
            root.Add(blocks);
            return root;
        }
    }
}