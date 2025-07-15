using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Allows a "cold start" in the editor, when pressing Play and not passing from the Initialisation scene.
/// </summary> 
public class EditorColdStartup : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private GameSceneSO thisSceneSO;
    [SerializeField] private GameSceneSO persistentManagersSO;
    [SerializeField] private AssetReference notifyColdStartupChannel;

    private bool isColdStart = false;
    private void Awake()
    {
        if (!SceneManager.GetSceneByName(persistentManagersSO.SceneReference.editorAsset.name).isLoaded)
        {
            isColdStart = true;
        }
        // TODO: 세이브 로드 기능 추가해야 함
        //CreateSaveFileIfNotPresent();
    }

    private void Start()
    {
        if (isColdStart)
        {
            persistentManagersSO.SceneReference.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += LoadEventChannel;
        }
        // TODO: 세이브 로드 기능 추가해야 함
        // CreateSaveFileIfNotPresent();
    }

    // TODO: 세이브 로드 기능 추가해야 함
    // private void CreateSaveFileIfNotPresent()
    // {
    //     if (_saveLoadSystem != null && !_saveLoadSystem.LoadSaveDataFromDisk())
    //     {
    //         Debug.LogWarning("There is no save Fiels");
    //         _saveLoadSystem.SetNewGameData();
    //     }
    // }

    private void LoadEventChannel(AsyncOperationHandle<SceneInstance> obj)
    {
        notifyColdStartupChannel.LoadAssetAsync<LoadEventChannelSO>().Completed += OnNotifyChannelLoaded;
    }

    private void OnNotifyChannelLoaded(AsyncOperationHandle<LoadEventChannelSO> obj)
    {
        if (thisSceneSO != null)
        {
            obj.Result.RaiseEvent(thisSceneSO);
        }
    }
#endif
}
