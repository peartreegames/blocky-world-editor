using System;
using System.Collections.Generic;
using System.Linq;
using PeartreeGames.Blocky.WorldEditor.Editor.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.Blocky.WorldEditor.Editor.BlockyVisualElements
{
    public sealed class BlockySettingsElement : VisualElement
    {
        public BlockySettingsElement(BlockyEditorWindow window, SerializedObject serializedSettings)
        {
            var settings = (BlockyEditorSettings) serializedSettings.targetObject;
            var palettes = GetPalettes();
            var paletteDropdown = new DropdownField
            {
                choices = palettes.Select(palette => palette.name).ToList(),
                value = settings.palette == null
                    ? ""
                    : palettes.Find(palette => palette.name == settings.palette.name).name,
                label = "Palette"
            };
            paletteDropdown.RegisterCallback<FocusEvent>(_ =>
            {
                palettes = GetPalettes();
                paletteDropdown.choices = palettes.Select(palette => palette.name).ToList();
                MarkDirtyRepaint();
            });

            paletteDropdown.RegisterValueChangedCallback(changed =>
            {
                settings.palette = palettes.Find(palette => palette.name == changed.newValue);
                window.RefreshPalette();
            });

            var serializedParentSetterProperty = serializedSettings.FindProperty("parentSetter");
            var parentSetters = GetParentSetters(serializedSettings, window);
            var parentSetterDropdown = new DropdownField
            {
                choices = parentSetters.Select(p => p.GetType().Name).ToList(),
                value = parentSetters.Find(p =>
                        p.GetType().Name == serializedParentSetterProperty.objectReferenceValue.GetType().Name)
                    .GetType()
                    .Name,
                label = "Parent Setter"
            };

            var placement = new GroupBox();

            var parentSetterBox = CreateParentSetterBox(serializedParentSetterProperty);
            parentSetterBox.Insert(0, parentSetterDropdown);
            placement.Insert(0, parentSetterBox);
            parentSetterDropdown.RegisterCallback<FocusEvent>(_ =>
            {
                parentSetters = GetParentSetters(serializedSettings, window);
                parentSetterDropdown.choices = parentSetters.Select(p => p.GetType().Name).ToList();
                MarkDirtyRepaint();
            });

            parentSetterDropdown.RegisterValueChangedCallback(change =>
            {
                serializedParentSetterProperty.objectReferenceValue =
                    parentSetters.Find(p => p.GetType().Name == change.newValue);
                serializedSettings.ApplyModifiedProperties();
                serializedParentSetterProperty = serializedSettings.FindProperty("parentSetter");
                var prev = placement.Q("ParentSetter");
                prev?.parent.Remove(prev);
                parentSetterBox = CreateParentSetterBox(serializedParentSetterProperty);
                parentSetterBox.Insert(0, parentSetterDropdown);
                placement.Insert(0, parentSetterBox);
            });
            
            var targetPosition = new PropertyField(serializedSettings.FindProperty("target"));
            targetPosition.Bind(serializedSettings);
            targetPosition.SetEnabled(false);

            var targetRotation = new PropertyField(serializedSettings.FindProperty("rotation"));
            targetRotation.Bind(serializedSettings);
            targetRotation.SetEnabled(false);

            var randomRotation = new PropertyField(serializedSettings.FindProperty("randomRotation"));
            randomRotation.Bind(serializedSettings);

            var gridHeight = new PropertyField(serializedSettings.FindProperty("gridHeight"));
            gridHeight.Bind(serializedSettings);

            var brushSize = new PropertyField(serializedSettings.FindProperty("brushSize"));
            brushSize.Bind(serializedSettings);
            
            placement.Add(targetPosition);
            placement.Add(targetRotation);
            placement.Add(randomRotation);
            placement.Add(gridHeight);
            placement.Add(brushSize);

            var modes = new GroupBox();
            var paintButton = new ToolbarToggle {text = "Paint"};
            var selectButton = new ToolbarToggle {text = "Select"};
            paintButton.RegisterValueChangedCallback(c =>
            {
                settings.editMode = c.newValue ? BlockyEditMode.Paint : BlockyEditMode.None;
                window.OnBlockyModeChange(settings.editMode);
                selectButton.SetValueWithoutNotify(false);
                window.PopulateMap();
            });
            selectButton.RegisterValueChangedCallback(c =>
            {
                settings.editMode = c.newValue ? BlockyEditMode.Select : BlockyEditMode.None;
                window.OnBlockyModeChange(settings.editMode);
                paintButton.SetValueWithoutNotify(false);
                window.PopulateMap();
            });
            modes.Add(paintButton);
            modes.Add(selectButton);


            var settingsView = new GroupBox();
            settingsView.Add(placement);
            settingsView.Add(modes);
            settingsView.Add(paletteDropdown);
            settingsView.Add(new Button(window.RefreshPalette){ text = "Refresh" });
            contentContainer.Add(settingsView);
        }

        private static GroupBox CreateParentSetterBox(SerializedProperty prop)
        {
            var box = new GroupBox {name = "ParentSetter"};
            if (prop.objectReferenceValue == null) return box;
            
            var obj = new SerializedObject(prop.objectReferenceValue);
            box.AddToClassList("parent-setter");
            
            var itr = obj.GetIterator();
            if (itr.NextVisible(true))
            {
                do
                {
                    if (itr.name == "m_Script") continue;
                    var field = new PropertyField(itr);
                    field.Bind(obj);
                    box.Add(field);
                } while (itr.NextVisible(false));
            }

            var methods = prop.objectReferenceValue.GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(BlockyButtonAttribute), true).Length > 0).ToList();
            foreach (var method in methods)
            {
                if (method.GetParameters().Length > 0) continue;
                var label =
                    ((BlockyButtonAttribute) method.GetCustomAttributes(typeof(BlockyButtonAttribute), false)[0]).label;
                var button = new Button(() =>
                {
                    method.Invoke(prop.objectReferenceValue, null);
                })
                {
                    text = label ?? method.Name
                };

                box.Add(button);
            }

            return box;
        }
        

        private static List<BlockyParentSetter.BlockyParentSetter> GetParentSetters(SerializedObject obj, BlockyEditorWindow window)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var parentSetterClasses = new List<BlockyParentSetter.BlockyParentSetter>();
            foreach (var assembly in assemblies)
            {
                var parentSetters = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(BlockyParentSetter.BlockyParentSetter))).Select(
                        t => ScriptableObject.CreateInstance(t.Name) as BlockyParentSetter.BlockyParentSetter).ToList();
                parentSetterClasses.AddRange(parentSetters);
            }

            var path = AssetDatabase.GetAssetPath(obj.targetObject);
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var result in parentSetterClasses)
            {
                if (Array.Exists(subAssets, sub => sub.GetType().Name == result.GetType().Name)) continue;
                result.name = result.GetType().Name;
                AssetDatabase.AddObjectToAsset(result, obj.targetObject);
                AssetDatabase.ImportAsset(path);
            }
            
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlockyParentSetter.BlockyParentSetter>().ToList();
        }

        private static List<BlockyPalette> GetPalettes() => AssetDatabase.FindAssets($"t:{nameof(BlockyPalette)}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<BlockyPalette>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
    }


}