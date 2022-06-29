using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    [EditorToolbarElement("World Position", typeof(SceneView))]
    public class WorldPositionField : EditorToolbarButton
    {
        public const string id = "World Position";
        
        public WorldPositionField()
        {
            var field = new Vector3Field
            {
                value = Selection.activeGameObject == null ? Vector3.zero : Selection.activeGameObject.transform.position
            };
            field.Query<VisualElement>("unity-text-input").Build().ForEach(f => f.style.width = 50);
            field.SetEnabled(false);
            void UpdateField(SceneView _)
            {
                
                field.value = Selection.activeGameObject == null || Selection.count > 1 ? Vector3.zero : Selection.activeGameObject.transform.position;
                field.MarkDirtyRepaint();
            }

            SceneView.duringSceneGui += UpdateField;
            Add(field);
        }
    }
    
    [Overlay(typeof(SceneView), "World Position", true)]
    public class WorldPositionViewer : ToolbarOverlay
    {
        public WorldPositionViewer() : base(WorldPositionField.id) {}
    }
}