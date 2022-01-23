using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    [CustomPropertyDrawer(typeof(SceneObjectAttribute))]
    public class SceneObjectDrawer : PropertyDrawer
    {
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var elem = new ObjectField(property.name) { allowSceneObjects = true, objectType = typeof(GameObject) };
            var sceneObject = attribute as SceneObjectAttribute;
            elem.RegisterValueChangedCallback(changed =>
            {
                var type = property.serializedObject.targetObject.GetType();
                type.GetProperty(sceneObject.backingPropertyName)?.SetValue(property.serializedObject.targetObject, changed.newValue);
            });
            return elem;
        }
    }
}