using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PeartreeGames.Blocky.WorldEditor.Editor.BlockyVisualElements
{
    [CustomPropertyDrawer(typeof(SceneObject))]
    public class SceneObjectDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(
            SerializedProperty property)
        {
            var objProp = property.FindPropertyRelative("gameObject");
            var idProp = property.FindPropertyRelative("id");
            GlobalObjectId.TryParse(idProp.stringValue, out var id);
            var prop = new ObjectField(property.displayName)
            {
                allowSceneObjects = true,
                value = objProp.objectReferenceValue != null
                    ? objProp.objectReferenceValue
                    : GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id)
            };
            prop.RegisterValueChangedCallback(v =>
            {
                objProp.objectReferenceValue = v.newValue;
                idProp.stringValue = GlobalObjectId.GetGlobalObjectIdSlow(v.newValue).ToString();
                property.serializedObject.ApplyModifiedProperties();
            });
            return prop;
        }
    }
}