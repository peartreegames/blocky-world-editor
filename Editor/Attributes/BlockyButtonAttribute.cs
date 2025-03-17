using System;

namespace PeartreeGames.Blocky.World.Editor.Attributes
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