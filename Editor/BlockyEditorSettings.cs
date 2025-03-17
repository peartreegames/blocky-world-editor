using System.Collections.Generic;
using PeartreeGames.Blocky.World.Editor.BlockyParentSetter;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.Blocky.World.Editor
{
    public enum BlockyEditMode
    {
        None,
        Paint,
        Select
    }

    public class BlockyEditorSettings : ScriptableObject
    {
        public Vector3Int target;
        public BlockyEditMode editMode;
        public int gridHeight;
        public Vector3 rotation;
        public bool randomRotation;
        [Range(0, 3)] public int brushSize;


        public BlockyPalette palette;
        public IBlockyPiece Selected;
        [SerializeField] public BlockyParentSetter.BlockyParentSetter parentSetter;
        [SerializeField] public bool useUndo;

        public static BlockyEditorSettings GetOrCreateSettings()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(BlockyEditorSettings)}");
            BlockyEditorSettings settings;
            if (guids.Length == 0)
            {
                settings = CreateInstance<BlockyEditorSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/BlockyEditorSettings.asset");
                var defaultParent = CreateInstance<BlockyDefaultParentSetter>();
                defaultParent.name = defaultParent.GetType().Name;
                AssetDatabase.AddObjectToAsset(defaultParent, settings);
                settings.parentSetter = defaultParent;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(settings));
            }
            else
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<BlockyEditorSettings>(path);
            }

            return settings;
        }

        public static SerializedObject GetSerializedSettings() => new(GetOrCreateSettings());
    }

    public class BlockyEditorSettingsProvider : SettingsProvider
    {
        private SerializedObject _settings;

        public BlockyEditorSettingsProvider(string path, SettingsScope scopes,
            IEnumerable<string> keywords = null) :
            base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = BlockyEditorSettings.GetSerializedSettings();
            var title = new Label
            {
                text = "Blocky Editor",
                style = {fontSize = 22, unityFontStyleAndWeight = FontStyle.Bold}
            };
            rootElement.Add(title);
            var undo = new PropertyField(_settings.FindProperty("useUndo"));
            undo.Bind(_settings);
            rootElement.Add(undo);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettings()
        {
            return new BlockyEditorSettingsProvider("Preferences/Blocky",
                SettingsScope.User);
        }
    }
}