using UnityEngine;
using UnityEngine.AddressableAssets;

public class SceneController : MonoBehaviour
{
    [SerializeField] private GameSceneSO sceneToLoad;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO sceneLoadChannel;

    /// <summary>
    /// 버튼 액션 바인딩
    /// </summary>
    public void SceneLoad()
    {
        sceneLoadChannel.OnLoadingRequested(sceneToLoad);
    }
}
