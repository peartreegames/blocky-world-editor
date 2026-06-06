using System.Collections.Generic;
using PeartreeGames.Blocky.World.BlockyMap;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.Blocky.World.Editor
{
    public static class BlockyEditorUtilities
    {
        public static void SetTargetVisualization(Vector3Int target, BlockyEditMode mode,
            int brushSize, bool placeAtTop,
            bool isSquareDragging, List<Vector3Int> squareDraggingList, Vector3Int squareDragStart,
            BlockyObjectHeightMap heightMap, BlockyLayer layer, int topAdj)
        {
            var originalColor = Handles.color;
            Handles.color = Event.current.control ? Color.red : Color.cyan;
            var gridSize = Vector3.one;
            var offset = new Vector3(0, -0.5f, 0);

            if (isSquareDragging)
            {
                squareDraggingList = SetDraggingList(target, placeAtTop, squareDraggingList, squareDragStart, heightMap, layer, topAdj);
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

                    var brushTarget = target + new Vector3Int(i, 0, j);
                    if (placeAtTop)
                    {
                        brushTarget.y = heightMap.TryGetValue(
                            new BlockyObjectHeightKey(brushTarget, layer),
                            out var h) ? h + topAdj : 0;
                    }

                    Handles.DrawWireCube(brushTarget + offset, gridSize);
                }
            }

            Handles.color = originalColor;
        }

        private static List<Vector3Int> SetDraggingList(Vector3Int target,
            bool placeAtTop,
            List<Vector3Int> squareDraggingList,
            Vector3Int squareDragStart,
            BlockyObjectHeightMap heightMap,
            BlockyLayer layer,
            int topAdj)
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
                    
                    if (placeAtTop)
                    {
                        pos.y = heightMap.TryGetValue(
                            new BlockyObjectHeightKey(pos, layer),
                            out var h)
                            ? h + topAdj
                            : 0;
                    }
                    squareDraggingList.Add(pos);
                }
            }

            return squareDraggingList;
        }

        public static bool TryGetTargetPoint(Vector2 currentMousePosition, int gridHeight, bool raycastHeight,
            out Vector3Int targetHit)
        {
            targetHit = Vector3Int.zero;
            var cam = Camera.current;
            if (cam == null) return false;
            var ray = HandleUtility.GUIPointToWorldRay(currentMousePosition);
            if (raycastHeight)
            {
                if (Physics.Raycast(ray, out var info))
                {
                    targetHit = BlockyUtilities.SnapToGrid(info.point, Mathf.RoundToInt(info.point.y));
                    return true;
                }
            }
            var hPlane = new Plane(Vector3.up, Vector3.up * gridHeight);
            if (!hPlane.Raycast(ray, out var enter)) return false;
            var hit = ray.GetPoint(enter);
            targetHit = BlockyUtilities.SnapToGrid(hit, gridHeight);
            return true;
        }

        public static void SetPlacementVisualization(Vector3Int target, Vector3 rotation,
            BlockyEditMode mode, IBlockyPiece current,
            Shader shader, ref GameObject placementObject)
        {
            if (current == null || mode != BlockyEditMode.Paint) return;
            if (placementObject == null || placementObject.name != $"PLACEMENT_{current.Name}")
            {
                DestroyPlacementObject(ref placementObject);
                placementObject = Object.Instantiate(current.GetPlacement());
                placementObject.name = $"PLACEMENT_{current.Name}";
                placementObject.layer = LayerMask.NameToLayer(IgnoreRaycastLayer);
                placementObject.hideFlags = HideFlags.HideAndDontSave;
                Object.DestroyImmediate(placementObject.GetComponent<BlockyObject>());
                var rends = placementObject.GetComponentsInChildren<MeshRenderer>();
                if (rends.Length > 0)
                {
                    foreach (var rend in rends)
                    {
                        var tempMat = new Material(rend.sharedMaterial) { shader = shader };
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

        private const string IgnoreRaycastLayer = "Ignore Raycast";

        public static void DestroyPlacementObject(ref GameObject placementObject)
        {
            if (placementObject == null) return;
            var seen = new HashSet<Material>();
            foreach (var rend in placementObject.GetComponentsInChildren<MeshRenderer>())
            {
                var mat = rend.sharedMaterial;
                if (mat != null && seen.Add(mat)) Object.DestroyImmediate(mat);
            }
            Object.DestroyImmediate(placementObject);
            placementObject = null;
        }
    }
}