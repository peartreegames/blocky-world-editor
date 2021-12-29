using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public static class BlockyEditorSceneManager
    {
        private static Dictionary<string, GameObject> _mapParents;

        public static void Init()
        {
            _mapParents = new Dictionary<string, GameObject>();
        }
        public static Transform GetParent(BlockyObject block)
        {
            var pos = Vector3Int.RoundToInt(block.transform.position);
            var center = BlockyUtilities.GetSectionPosition(pos, pos.y);
            var sceneName = $"world_{Mathf.RoundToInt(center.x / 100)}_{Mathf.RoundToInt(center.z / 100)}";
            if (!_mapParents.TryGetValue(sceneName, out var mapParent))
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                var foundScene = sceneGuids.Any(sceneGuid =>
                    SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(sceneGuid)).name == sceneName);
                Scene scene;
                if (!foundScene)
                {
                    var baseSettings = AddressableAssetSettingsDefaultObject.Settings;
                    var worldGroup = baseSettings.FindGroup("World");
                    if (worldGroup == null)
                        worldGroup = baseSettings.CreateGroup("World", false, false, false,
                            baseSettings.DefaultGroup.Schemas);
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    scene.name = sceneName;
                    EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
                    var guid = AssetDatabase.AssetPathToGUID(scene.path);
                    var entry = baseSettings.CreateOrMoveEntry(guid, worldGroup);
                    entry.address = sceneName;
                    baseSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    scene = SceneManager.GetSceneByName(sceneName);
                    if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(scene.path);
                }

                var roots = scene.GetRootGameObjects().ToList();
                mapParent = roots.Find(root => root.name == "Map");

                if (mapParent == null)
                {
                    mapParent = new GameObject
                    {
                        name = "Map",
                        transform =
                        {
                            position = center
                        }
                    };
                    SceneManager.MoveGameObjectToScene(mapParent, scene);
                }

                _mapParents.Add(sceneName, mapParent);
            }

            var quadPos = BlockyUtilities.GetQuadPosition(pos, pos.y);
            var x = Mathf.RoundToInt(quadPos.x / 50f);
            var z = Mathf.RoundToInt(quadPos.z / 50f);
            var quad = mapParent.transform.Find($"{x},{z}")?.gameObject;
            if (quad != null) return quad.transform;
            quad = new GameObject {name = $"{x},{z}", transform = {position = quadPos}};
            quad.transform.SetParent(mapParent.transform);
            return quad.transform;
        }
    }
}