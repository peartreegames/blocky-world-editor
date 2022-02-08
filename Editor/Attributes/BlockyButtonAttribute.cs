using System;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BlockyButtonAttribute : Attribute
    {
        public readonly string label;

        public BlockyButtonAttribute() {}

        public BlockyButtonAttribute(string label)
        {
            this.label = label;
        }
    }
}