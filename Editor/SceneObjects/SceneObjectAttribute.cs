using System;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public class SceneObjectAttribute : PropertyAttribute
    {
        public readonly string backingPropertyName;

        public SceneObjectAttribute(string backingPropertyName)
        {
            this.backingPropertyName = backingPropertyName;
        }
    }
}