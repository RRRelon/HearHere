using UnityEngine;
using System;

public class MicRecorder : MonoBehaviour
{
    public int maxRecordingTime = 10;
    public int sampleRate = 44100;
    
    private AudioClip recordedClip;
    private string microphoneName;
    private bool isRecording = false;
    
    public event Action<byte[]> OnRecordingComplete;
    public event Action OnRecordingStart;
    public event Action OnRecordingStop;
    
    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneName = Microphone.devices[0];
            Debug.Log($"마이크 발견: {microphoneName}");
        }
        else
        {
            Debug.LogError("마이크를 찾을 수 없습니다!");
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return;
        
        isRecording = true;
        recordedClip = Microphone.Start(microphoneName, false, maxRecordingTime, sampleRate);
        OnRecordingStart?.Invoke();
        Debug.Log("녹음 시작!");
    }
    
    public void StopRecording()
    {
        if (!isRecording) return;
        
        isRecording = false;
        Microphone.End(microphoneName);
        OnRecordingStop?.Invoke();
        
        if (recordedClip != null)
        {
            byte[] audioData = ConvertToWav(recordedClip);
            OnRecordingComplete?.Invoke(audioData);
        }
        
        Debug.Log("녹음 중지!");
    }
    
    public bool IsRecording() => isRecording;
    
    private byte[] ConvertToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        
        byte[] data = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short value = (short)(samples[i] * 32767);
            byte[] bytes = BitConverter.GetBytes(value);
            data[i * 2] = bytes[0];
            data[i * 2 + 1] = bytes[1];
        }
        
        byte[] header = CreateWavHeader(clip.frequency, clip.channels, data.Length);
        byte[] wavFile = new byte[header.Length + data.Length];
        System.Buffer.BlockCopy(header, 0, wavFile, 0, header.Length);
        System.Buffer.BlockCopy(data, 0, wavFile, header.Length, data.Length);
        
        return wavFile;
    }
    
    private byte[] CreateWavHeader(int sampleRate, int channels, int dataLength)
    {
        byte[] header = new byte[44];
        
        // RIFF header
        header[0] = 0x52; header[1] = 0x49; header[2] = 0x46; header[3] = 0x46;
        
        // File size
        int fileSize = dataLength + 36;
        header[4] = (byte)(fileSize & 0xFF);
        header[5] = (byte)((fileSize >> 8) & 0xFF);
        header[6] = (byte)((fileSize >> 16) & 0xFF);
        header[7] = (byte)((fileSize >> 24) & 0xFF);
        
        // WAVE header
        header[8] = 0x57; header[9] = 0x41; header[10] = 0x56; header[11] = 0x45;
        
        // fmt chunk
        header[12] = 0x66; header[13] = 0x6D; header[14] = 0x74; header[15] = 0x20;
        header[16] = 16; header[17] = 0; header[18] = 0; header[19] = 0;
        header[20] = 1; header[21] = 0;
        header[22] = (byte)channels; header[23] = 0;
        
        // Sample rate
        header[24] = (byte)(sampleRate & 0xFF);
        header[25] = (byte)((sampleRate >> 8) & 0xFF);
        header[26] = (byte)((sampleRate >> 16) & 0xFF);
        header[27] = (byte)((sampleRate >> 24) & 0xFF);
        
        // Byte rate
        int byteRate = sampleRate * channels * 2;
        header[28] = (byte)(byteRate & 0xFF);
        header[29] = (byte)((byteRate >> 8) & 0xFF);
        header[30] = (byte)((byteRate >> 16) & 0xFF);
        header[31] = (byte)((byteRate >> 24) & 0xFF);
        
        // Block align
        int blockAlign = channels * 2;
        header[32] = (byte)blockAlign; header[33] = 0;
        header[34] = 16; header[35] = 0;
        
        // Data chunk
        header[36] = 0x64; header[37] = 0x61; header[38] = 0x74; header[39] = 0x61;
        header[40] = (byte)(dataLength & 0xFF);
        header[41] = (byte)((dataLength >> 8) & 0xFF);
        header[42] = (byte)((dataLength >> 16) & 0xFF);
        header[43] = (byte)((dataLength >> 24) & 0xFF);
        
        return header;
    }
}