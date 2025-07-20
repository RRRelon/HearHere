using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GPTManager : MonoBehaviour
{
    public TMP_InputField userInputField;
    public TextMeshProUGUI responseText;

    private readonly string openAIApiURL = "https://api.openai.com/v1/chat/completions";
    private readonly string apiKey = "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";

    public void OnSubmit()
    {
        string userInput = userInputField.text;
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            StartCoroutine(SendRequestToChatGPT(userInput));
        }
    }

    private IEnumerator SendRequestToChatGPT(string userInput)
    {
        var request = new UnityWebRequest(openAIApiURL, "POST");
        string requestData = "{\"model\":\"gpt-3.5-turbo\",\"messages\":[{\"role\":\"system\",\"content\":\"You are a helpful assistant.\"},{\"role\":\"user\",\"content\":\"" + userInput + "\"}]}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
            responseText.text = "Error: " + request.error;
        }
        else
        {
            string response = request.downloadHandler.text;
            Debug.Log("Response: " + response);
            ChatGPTResponse chatGPTResponse = JsonUtility.FromJson<ChatGPTResponse>(response);
            string gptResponse = chatGPTResponse.choices[0].message.content;
            responseText.text = gptResponse; //ChatGPT 응답을 UI에 표시
        }
    }
}

[System.Serializable]
public class ChatGPTResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string content;
}