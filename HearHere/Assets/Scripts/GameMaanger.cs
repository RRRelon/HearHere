using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UnityTTSSTTClient client;

    // void Start()
    // {
    //     // TTS 테스트
    //     ttsClient.ConvertTextToSpeech("안녕하세요!", 0.7f, 0.5f);
    //
    //     // 채팅 테스트
    //     ttsClient.SendChatMessage("안녕하세요!");
    //
    //     // 음성 채팅 테스트 (마이크 녹음)
    //     ttsClient.TestVoiceChat();
    // }

    // // 음성 녹음 후 STT 변환
    // public void ProcessRecordedAudio(AudioClip recordedClip)
    // {
    //     ttsClient.ConvertRecordedAudioToText(recordedClip);
    // }
    //
    // // 음성으로 AI와 대화
    // public void TalkToAI()
    // {
    //     ttsClient.StartVoiceRecording(0.8f, 0.3f); // 감정 강도 높게, 품질 조정
    // }
}