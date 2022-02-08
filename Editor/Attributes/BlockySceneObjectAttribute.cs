using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public class BlockySceneObjectAttribute : PropertyAttribute
    {
        public readonly string backingPropertyName;

        public BlockySceneObjectAttribute(string backingPropertyName)
        {
            this.backingPropertyName = backingPropertyName;
        }
    }
}