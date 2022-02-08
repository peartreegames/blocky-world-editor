using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public static class BlockyEditorUtilities
    {
        public static void SetTargetVisualization(Vector3Int target, BlockyEditMode mode, int brushSize,
            bool isSquareDragging, List<Vector3Int> squareDraggingList, Vector3Int squareDragStart)
        {
            var originalColor = Handles.color;
            Handles.color = Event.current.control ? Color.red : Color.cyan;
            var gridSize = Vector3.one;
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

        private static List<Vector3Int> SetDraggingList(Vector3Int target, List<Vector3Int> squareDraggingList,
            Vector3Int squareDragStart)
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
            targetHit = BlockyUtilities.SnapToGrid(hit, gridHeight);
            return true;
        }

        public static void SetPlacementVisualization(Vector3Int target, Vector3 rotation, BlockyEditMode mode, IBlockyPiece current,
            Shader shader, ref GameObject placementObject)
        {
            if (current == null || mode != BlockyEditMode.Paint) return;
            if (placementObject == null || placementObject.name != $"PLACEMENT_{current.Name}")
            {
                Object.DestroyImmediate(placementObject);
                placementObject = Object.Instantiate(current.GetPlacement());
                placementObject.name = $"PLACEMENT_{current.Name}";
                var rends = placementObject.GetComponentsInChildren<MeshRenderer>();
                if (rends.Length > 0)
                {
                    foreach (var rend in rends)
                    {
                        var tempMat = new Material(rend.sharedMaterial) {shader = shader};
                        var col = tempMat.color;
                        col.a = 0.4f;
                        tempMat.color = col;
                        rend.material = tempMat;
                    }
                }

            }
            placementObject.transform.position = target;
            placementObject.transform.rotation = Quaternion.Euler(rotation);
        }
    }
}