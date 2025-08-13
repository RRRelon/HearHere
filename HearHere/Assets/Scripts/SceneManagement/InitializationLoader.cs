using System;
using System.Collections;
using HH;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class InitializationLoader : MonoBehaviour
{
    [SerializeField] private GameSceneSO managersScene;
    [SerializeField] private GameSceneSO menuToLoad;
    [SerializeField] private AIConversationManagerSO manager;
    
    [Header("Broadcasting on")]
    [SerializeField] private AssetReference menuLoadChannel;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        RequestTTS("For the best experience, please use headphones.");
    }
    
    private async void RequestTTS(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            Debug.Log($"TTS Request: {text}");
            AudioClip textToSpeech = await manager.RequestTextToSpeech(text);
            StartCoroutine(PlayAudioAndLoadManagersScene(textToSpeech));
        }
    }
    
    private IEnumerator PlayAudioAndLoadManagersScene(AudioClip TTSClip)
    {
        audioSource.clip = TTSClip; // unity의 audioSource 컴포넌트에 mp3 연결
        audioSource.Play();         // mp3 실제로 재생
        Debug.Log(audioSource.clip.length);
        yield return new WaitWhile(() => audioSource.isPlaying);
        yield return new WaitForSeconds(2.0f);

        LoadManagersScene();
    }
    
    private void LoadManagersScene()
    {
        managersScene.SceneReference.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += LoadEventChannel;
    }

    private void LoadEventChannel(AsyncOperationHandle<SceneInstance> obj)
    {
        menuLoadChannel.LoadAssetAsync<LoadEventChannelSO>().Completed += LoadHome;
    }

    private void LoadHome(AsyncOperationHandle<LoadEventChannelSO> obj)
    {
        obj.Result.RaiseEvent(menuToLoad);

        SceneManager.UnloadSceneAsync(0); // Initialization Scene is the only scene in Build setting
    }
}
