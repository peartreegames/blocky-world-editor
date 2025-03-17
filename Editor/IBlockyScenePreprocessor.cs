using UnityEngine.SceneManagement;

namespace PeartreeGames.Blocky.WorldEditor.Editor
{
    public interface IBlockyScenePreprocessor
    {
        public int Order { get; }
        public void ProcessScene(BlockyEditorWindow window, Scene scene);
        public void RevertScene(BlockyEditorWindow window, Scene scene);
    }
}