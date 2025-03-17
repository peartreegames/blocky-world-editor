using UnityEngine;

namespace PeartreeGames.Blocky.World
{
    public static class BlockyUtilities
    {
        public static readonly Vector3 GridOffset = new(0.5f, 0, 0.5f);
        public static float ClampAngle(float rotationY) => rotationY < 0 ? 360 - Mathf.Abs(rotationY) % 360 :
            rotationY > 360 ? rotationY % 360 : rotationY;

        public static Vector3Int SnapToGrid(Vector3 hit, int gridHeight, int gridSize = 1) =>
            new(Mathf.RoundToInt(hit.x / gridSize) * gridSize, gridHeight,
                Mathf.RoundToInt(hit.z / gridSize) * gridSize);
    }
}