using System;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class TransformEditor : UnityEditor.Editor
    {
        private Transform _transform;
        private SerializedProperty _localPosition;
        private SerializedProperty _localScale;
        private SerializedProperty _localRotation;

        private void OnEnable()
        {
            _localPosition = serializedObject.FindProperty("m_LocalPosition");
            _localScale = serializedObject.FindProperty("m_LocalScale");
            _localRotation = serializedObject.FindProperty("m_LocalRotation");
        }

        public override void OnInspectorGUI()
        {
            _transform = (Transform) target;
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);
            var worldPosition = _transform.position;
            EditorGUILayout.Vector3Field("World Position", worldPosition);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(_localPosition, new GUIContent("Local Position"));
            var rotation = _localRotation.quaternionValue.eulerAngles;
            rotation = EditorGUILayout.Vector3Field("Rotation", rotation);
            _localRotation.quaternionValue = Quaternion.Euler(rotation);
            EditorGUILayout.PropertyField(_localScale, new GUIContent("Scale"));
            
            if (Mathf.Abs(worldPosition.x) > 100000 || Mathf.Abs(worldPosition.y) > 100000 || Mathf.Abs(worldPosition.z) > 100000)
                EditorGUILayout.HelpBox("Due to floating-point precision limitations, it is recommends to bring the world coordinates of the GameObject within a smaller range", MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}