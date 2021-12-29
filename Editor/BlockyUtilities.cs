using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public static class BlockyUtilities
    {
        public static float ClampAngle(float rotationY) => rotationY < 0 ? 360 - Mathf.Abs(rotationY) % 360 :
            rotationY > 360 ? rotationY % 360 : rotationY;

        public static void SetBoundsVisualization(Vector3Int target, int gridHeight)
        {
            var originalColor = Handles.color;
            Handles.color = Color.cyan * 0.5f;
            var center = GetSectionPosition(target, gridHeight);
            Handles.DrawWireCube(center, new Vector3(100, 0, 100));
            var quad = GetQuadPosition(target, gridHeight);
            Handles.color = Color.cyan * 0.35f;
            Handles.DrawWireCube(quad, new Vector3(50, 0, 50));
            Handles.color = originalColor;
        }

        public static Vector3Int SnapToGrid(Vector3 hit, int gridHeight, int gridSize = 1) =>
            new(Mathf.RoundToInt(hit.x / gridSize) * gridSize, gridHeight,
                Mathf.RoundToInt(hit.z / gridSize) * gridSize);

        public static Vector3 GetSectionPosition(Vector3Int target, int gridHeight) =>
            SnapToGrid(target - BlockyEditorWindow.GridOffset, gridHeight, 100) + BlockyEditorWindow.GridOffset;

        public static Vector3 GetQuadPosition(Vector3Int target, int gridHeight) =>
            SnapToGrid(target - new Vector3(25.5f, 0, 25.5f), gridHeight, 50) + new Vector3(25.5f, 0, 25.5f);

        public static void SetTargetVisualization(Vector3Int target, EditMode mode, int brushSize, bool isSquareDragging, List<Vector3Int> squareDraggingList, Vector3Int squareDragStart)
        {
            var originalColor = Handles.color;
            Handles.color = Event.current.control ? Color.red : Color.cyan;
            var gridSize = Vector3.one;
            if (mode == EditMode.Paint)
            {
                // render placement prefab
            }
                

            var offset = new Vector3(0, -0.5f, 0);

            if (isSquareDragging)
            {
                squareDraggingList = SetDraggingList(target, squareDraggingList, squareDragStart);
                foreach (var pos in squareDraggingList)
                {
                    Handles.DrawWireCube(pos + offset, gridSize);
                }

                Handles.color = originalColor;
                return;
            }
            
            
            Handles.DrawWireCube(target + offset, gridSize);
            for (var i = -brushSize; i <= brushSize; i++)
            {
                for (var j = -brushSize; j <= brushSize; j++)
                {
                    if (i == 0 && j == 0) continue;
                    Handles.DrawWireCube(target + new Vector3Int(i, 0, j) + offset, gridSize);
                }
            }
            Handles.color = originalColor;
        }

        private static List<Vector3Int> SetDraggingList(Vector3Int target, List<Vector3Int> squareDraggingList, Vector3Int squareDragStart)
        {
            squareDraggingList.Clear();
            var xIntervals = Mathf.Abs(squareDragStart.x - target.x);
            var zIntervals = Mathf.Abs(squareDragStart.z - target.z);
            for (var i = 0; i <= xIntervals; i++)
            {
                for (var j = 0; j <= zIntervals; j++)
                {
                    var x = squareDragStart.x + (squareDragStart.x > target.x ? -i : i);
                    var z = squareDragStart.z + (squareDragStart.z > target.z ? -j : j);
                    var pos = new Vector3Int(x, squareDragStart.y, z);
                    squareDraggingList.Add(pos);
                }
            }

            return squareDraggingList;
        }

        public static bool TryGetTargetPoint(Vector2 currentMousePosition, int gridHeight, out Vector3Int targetHit)
        {
            targetHit = Vector3Int.zero;
            var cam = Camera.current;
            if (cam == null) return false;
            var ray = HandleUtility.GUIPointToWorldRay(currentMousePosition);
            var hPlane = new Plane(Vector3.up, Vector3.up * gridHeight);
            if (!hPlane.Raycast(ray, out var enter)) return false;
            var hit = ray.GetPoint(enter);
            targetHit = SnapToGrid(hit, gridHeight);
            return true;
        }
    }
}