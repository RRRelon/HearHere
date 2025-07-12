using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private AssetReference sceneToLoad;

    [Header("Listening to")]
    [SerializeField] private LoadEventChannelSO loadScene;
    [SerializeField] private LoadEventChannelSO coldStartup;
    
    private AsyncOperationHandle<SceneInstance> loadingOperationHandle;
    private AssetReference currentlyLoadedScene;

    private bool isLoading = false; // 씬을 중복 로딩하지 않게 하는 flag

    private void OnEnable()
    {
        loadScene.OnLoadingRequested += LoadScene   ;
#if UNITY_EDITOR
        coldStartup.OnLoadingRequested += ColdStartup;
#endif
    }

    private void OnDisable()
    {
        loadScene.OnLoadingRequested -= LoadScene;
#if UNITY_EDITOR
        coldStartup.OnLoadingRequested -= ColdStartup;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// This special loading function is only used in the editor, when the developer presses Play in a Location scene, without passing by Initialisation.
    /// </summary>
    private void ColdStartup(AssetReference currentlyOpenedScene)
    {
        currentlyLoadedScene = currentlyOpenedScene;
    }
#endif
    
    public void LoadScene(AssetReference homeToLoad)
    {
        if (isLoading)
            return;

        sceneToLoad = homeToLoad;

        isLoading = true;
        StartCoroutine(UnloadPreviousScene());
    }

    /// <summary>
    /// 새로운 씬을 로드하기 전, 현재 씬을 언로드
    /// </summary>
    private IEnumerator UnloadPreviousScene()
    {
        yield return new WaitForSeconds(0.2f);

        if (currentlyLoadedScene != null)
        {
            if (currentlyLoadedScene.IsValid())
            {
                var unloadHandle = currentlyLoadedScene.UnLoadScene();
                yield return unloadHandle;    
            }
#if UNITY_EDITOR
            else
            {
                var unloadOperation = SceneManager.UnloadSceneAsync(currentlyLoadedScene.editorAsset.name);
                yield return unloadOperation;
            }
#endif
        }
        
        // 현재 Scene Unload 종료 시 실행
        LoadNewScene();
    }
    
    /// <summary>
    /// 로딩 핸들러를 통해 새로운 씬을 로딩
    /// </summary>
    private void LoadNewScene()
    {
        loadingOperationHandle = sceneToLoad.LoadSceneAsync(LoadSceneMode.Additive, true, 0);
        loadingOperationHandle.Completed += OnNewSceneLoad;
    }

    /// <summary>
    /// 로딩이 끝난 씬을 실제 게임에 적용
    /// </summary>
    private void OnNewSceneLoad(AsyncOperationHandle<SceneInstance> obj)
    {
        currentlyLoadedScene = sceneToLoad;

        Scene s = obj.Result.Scene;
        SceneManager.SetActiveScene(s);

        isLoading = false;
    }
}
