using UnityEngine.SceneManagement;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public interface IBlockyScenePreprocessor
    {
        public void ProcessScene(BlockyEditorWindow window, Scene scene);
        public void RevertScene(BlockyEditorWindow window, Scene scene);
    }
}