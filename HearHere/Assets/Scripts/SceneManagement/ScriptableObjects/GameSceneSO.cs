using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "Game Scene", menuName = "Scene Data/GameSceneSO")]
public class GameSceneSO : DescriptionBaseSO
{
    public GameSceneType SceneType;
    public AssetReference SceneReference;
}

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
