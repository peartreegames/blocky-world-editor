using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PeartreeGames.Blocky.WorldEditor.Editor.BlockyVisualElements
{
    [CustomPropertyDrawer(typeof(BlockyRule))]
    public class BlockyRuleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var blockProperty = property.FindPropertyRelative("block");
            var transformProperty = property.FindPropertyRelative("transform");
            var gridProperty = property.FindPropertyRelative("grid");
            var ruleEditor = new GroupBox();
            ruleEditor.AddToClassList("rule-box");
            var texture = GetTexture(blockProperty.objectReferenceValue);
            var image = new Image() {image = texture};
            image.AddToClassList("preview-rule");

            var settings = new VisualElement() {style = {flexDirection = FlexDirection.Column, flexGrow = 1}};
            var block = new ObjectField("Block")
            {
                allowSceneObjects = false,
                objectType = typeof(GameObject)
            };
            block.BindProperty(blockProperty);
            block.Bind(property.serializedObject); // probably not needed?
            block.RegisterValueChangedCallback(c =>
            {
                image.image = GetTexture(c.newValue);
                image.MarkDirtyRepaint();
            });

            if (gridProperty.arraySize != 9)
            {
                gridProperty.ClearArray();
                for(var i = 0; i < 9; i++) gridProperty.InsertArrayElementAtIndex(i);
            }

            var transformField = new PropertyField(transformProperty);
            transformField.Bind(property.serializedObject);
            var grid = new GroupBox();
            grid.AddToClassList("grid");
            
            void OnClick(ClickEvent evt, int index)
            {
                if (index == 4) return;
                var currentValue = gridProperty.GetArrayElementAtIndex(index).enumValueIndex;
                currentValue++;
                if (currentValue > 2) currentValue = 0;
                gridProperty.GetArrayElementAtIndex(index).enumValueIndex = currentValue;
                ((Button) evt.target).text = GetCellText((BlockyCellRule) currentValue);
                property.serializedObject.ApplyModifiedProperties();
            }
            
            for (var i = 0; i < 3; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("row");
                for (var j = 0; j < 3; j++)
                {
                    var cell = i == 1 && j == 1 ? new VisualElement() : new Button()
                    {
                        name = $"Cell_{i}_{j}",
                        text = GetCellText((BlockyCellRule) gridProperty.GetArrayElementAtIndex(i * 3 + j)
                            .enumValueIndex)
                    };
                    cell.AddToClassList("cell");
                    cell.RegisterCallback<ClickEvent, int>(OnClick, i * 3 + j);
                    row.Add(cell);
                }
                grid.Add(row);
            }

            settings.Add(block);
            settings.Add(transformField);
            ruleEditor.Add(grid);
            ruleEditor.Add(image);
            ruleEditor.Add(settings);
            return ruleEditor;
        }

        private static string GetCellText(BlockyCellRule value) => value switch
        {
            BlockyCellRule.None => "",
            BlockyCellRule.Diff => "✘",
            BlockyCellRule.Same => "✔",
            _ => ""
        };

        private static Texture GetTexture(Object obj) => obj switch
        {
            GameObject go => go.GetComponent<IBlockyPiece>()?.GetTexture(),
            ScriptableObject so => ((IBlockyPiece) so).GetTexture(),
            _ => Texture2D.grayTexture
        };
    }
}