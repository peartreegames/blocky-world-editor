using System.Collections.Generic;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public class BlockyDefaultParentSetter : BlockyParentSetter
    {
        [SerializeField]
        [SceneObject("Parent")] 
        private GameObject parent;

        public GameObject Parent { get; set; }

        public override Transform GetParent(BlockyObject block) => Parent == null ? null : Parent.transform;
    }
}