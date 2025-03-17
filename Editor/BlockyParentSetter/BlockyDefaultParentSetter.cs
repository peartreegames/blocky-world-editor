using UnityEditor;
using UnityEngine;

namespace PeartreeGames.Blocky.World.Editor.BlockyParentSetter
{
    public class BlockyDefaultParentSetter : BlockyParentSetter
    {
        [SerializeField] public SceneObject parent;
        public override Transform GetParent(BlockyObject block)
        {
            if (parent == null) return null;
            if (parent.gameObject != null) return parent.gameObject.transform;
            if (string.IsNullOrEmpty(parent.id)) return null;
            GlobalObjectId.TryParse(parent.id, out var id);
            parent.gameObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
            if (parent.gameObject != null) return parent.gameObject.transform;
            return null;
        }
    }
}