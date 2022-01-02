using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public class BlockyDefaultParentSetter : BlockyParentSetter
    {
        [SerializeField] private Transform parent;
        public override Transform GetParent(BlockyObject block) => parent;
    }
}