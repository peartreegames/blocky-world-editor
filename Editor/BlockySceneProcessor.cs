using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeartreeGames.Blocky.WorldEditor.Editor
{
    public class BlockySceneProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 1000;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var blocks = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<BlockyObject>());
            foreach(var block in blocks) Object.DestroyImmediate(block);
        }
    }
}