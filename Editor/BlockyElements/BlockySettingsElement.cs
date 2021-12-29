using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    
    public sealed class BlockySettingsElement : VisualElement
    {
        public BlockySettingsElement(BlockyEditorWindow window, BlockyEditorSettings settings)
        {
            var palettes = GetPalettes();
            var paletteDropdown = new DropdownField()
            {
                choices = palettes.Select(palette => palette.name).ToList(),
                value = settings.palette == null
                    ? ""
                    : palettes.Find(palette => palette.name == settings.palette.name).name,
                label = "Palette"
            };

            paletteDropdown.RegisterValueChangedCallback(changed =>
            {
                settings.palette = palettes.Find(palette => palette.name == changed.newValue);
                window.RefreshPalette();
            });

            var placement = new GroupBox();
            var serializedSettings = new SerializedObject(settings);
            var targetPosition = new PropertyField(serializedSettings.FindProperty("target"));
            targetPosition.Bind(serializedSettings);
            targetPosition.SetEnabled(false);

            var gridHeight = new PropertyField(serializedSettings.FindProperty("gridHeight"));
            gridHeight.Bind(serializedSettings);

            var brushSize = new PropertyField(serializedSettings.FindProperty("brushSize"));
            brushSize.Bind(serializedSettings);

            var parentOverride = new PropertyField(serializedSettings.FindProperty("parentOverride"));
            parentOverride.Bind(serializedSettings);
            parentOverride.RegisterValueChangeCallback(go =>
            {
                if (go.changedProperty.objectReferenceValue == null) parentOverride.RemoveFromClassList("parent-override");
                else parentOverride.AddToClassList("parent-override");
            });
            
            placement.Add(targetPosition);
            placement.Add(gridHeight);
            placement.Add(brushSize);
            placement.Add(parentOverride);

            var modes = new GroupBox();
            var paintButton = new ToolbarToggle() {text = "Paint"};
            var selectButton = new ToolbarToggle() {text = "Select"};
            paintButton.RegisterValueChangedCallback(c =>
            {
                settings.editMode = c.newValue ? EditMode.Paint : EditMode.None;
                selectButton.SetValueWithoutNotify(false);
            });
            selectButton.RegisterValueChangedCallback(c =>
            {
                settings.editMode = c.newValue ? EditMode.Select : EditMode.None;
                paintButton.SetValueWithoutNotify(false);
            });
            modes.Add(paintButton);
            modes.Add(selectButton);


            var settingsView = new GroupBox();
            settingsView.Add(paletteDropdown);
            settingsView.Add(placement);
            settingsView.Add(modes);
            contentContainer.Add(settingsView);
        }
        
        private static List<BlockyPalette> GetPalettes() => AssetDatabase.FindAssets($"t:{nameof(BlockyPalette)}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<BlockyPalette>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
    }
}