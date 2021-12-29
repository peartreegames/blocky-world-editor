using System.Collections.Generic;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor
{
    [CreateAssetMenu(fileName = "bPalette_", menuName = "Blocky/Palette", order = 0)]
    public class BlockyPalette : ScriptableObject
    {
        public List<Object> blocks;

        public List<IBlockyPiece> Blocks
        {
            get
            {
                var result = new List<IBlockyPiece>();
                foreach (var block in blocks)
                {
                    var iBlock = block switch
                    {
                        GameObject go => go.GetComponent<IBlockyPiece>(),
                        ScriptableObject so => so as IBlockyPiece,
                        _ => null
                    };
                    if (iBlock != null) result.Add(iBlock);
                }
                return result;
            }
        }
    }
}