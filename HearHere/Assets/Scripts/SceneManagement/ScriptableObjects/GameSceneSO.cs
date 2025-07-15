using UnityEngine.AddressableAssets;

public class GameSceneSO : DescriptionBaseSO
{
    public GameSceneType SceneType;
    public AssetReference SceneReference;

    public enum GameSceneType
    {
        // Playerable
        Location,
        Menu,

        // Special Scenes
        Initialisation,
        PersistentManager,
        GamePlay
    }
}
