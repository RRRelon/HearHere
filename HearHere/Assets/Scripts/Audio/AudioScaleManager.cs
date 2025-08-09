using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioScaleManager", menuName = "Audio/AudioScaleManagerSO")]
public class AudioScaleManagerSO : ScriptableObject
{
    private float[] whiteKeyPitches = {
        // 낮은 옥타브 (Low Octave)
        0.500f, // 도(C)
        0.561f, // 레(D)
        0.630f, // 미(E)
        0.667f, // 파(F)
        0.749f, // 솔(G)
        0.841f, // 라(A)
        0.944f, // 시(B)

        // 기준 옥타브 (Reference Octave)
        1.000f, // 도(C)
        1.122f, // 레(D)
        1.260f, // 미(E)
        1.335f, // 파(F)
        1.498f, // 솔(G)
        1.682f, // 라(A)
        1.888f, // 시(B)

        // 높은 옥타브 (Higher Octave)
        2.000f, // 도(C)
        2.245f, // 레(D)
        2.520f, // 미(E)
        2.670f, // 파(F)
        2.997f, // 솔(G)
        3.364f, // 라(A)
        3.775f  // 시(B)
    };

    private int doe = 7;

    /// <summary>
    /// 여러 음계 시퀀스를 받아 하나의 멜로디 AudioClip으로 생성합니다.
    /// </summary>
    /// <param name="noteSequence">음계 인덱스의 리스트 (0은 '도')</param>
    /// <returns>생성된 멜로디 AudioClip</returns>
    public AudioClip CreateMelodyClip(List<int> noteSequence, AudioClip sourceNoteClip, float noteDuration)
    {
        if (sourceNoteClip == null || noteSequence.Count == 0)
        {
            Debug.LogError("원본 오디오 클립이 없거나, 음계 시퀀스가 비어있습니다.");
            return null;
        }

        // 1. 최종 멜로디 클립에 필요한 총 샘플 수를 계산합니다.
        int samplesPerNote = (int)(sourceNoteClip.frequency * noteDuration);
        int totalSamples = samplesPerNote * noteSequence.Count;
        float[] melodySamples = new float[totalSamples * sourceNoteClip.channels];

        // 2. 원본 클립에서 오디오 데이터(샘플)를 미리 추출합니다.
        float[] sourceSamples = new float[sourceNoteClip.samples * sourceNoteClip.channels];
        sourceNoteClip.GetData(sourceSamples, 0);

        // 3. 각 음계를 순회하며 멜로디 데이터를 만듭니다.
        for (int i = 0; i < noteSequence.Count; i++)
        {
            int pitchIndex = noteSequence[i] + doe;
            if (pitchIndex < 0 || pitchIndex >= whiteKeyPitches.Length) continue;

            float pitch = whiteKeyPitches[pitchIndex];
            
            // 4. 피치에 맞게 원본 샘플을 리샘플링(Resampling)하여 현재 음표의 데이터를 생성합니다.
            for (int sampleIndex = 0; sampleIndex < samplesPerNote; sampleIndex++)
            {
                for (int channel = 0; channel < sourceNoteClip.channels; channel++)
                {
                    float sourceSamplePosition = sampleIndex * pitch;
                    int readIndex = ((int)sourceSamplePosition * sourceNoteClip.channels + channel) % sourceSamples.Length;
                    int writeIndex = (i * samplesPerNote + sampleIndex) * sourceNoteClip.channels + channel;
                    melodySamples[writeIndex] = sourceSamples[readIndex];
                }
            }
        }

        // 5. 완성된 멜로디 데이터로 새로운 AudioClip을 생성합니다.
        AudioClip melodyClip = AudioClip.Create("GeneratedMelody", totalSamples, sourceNoteClip.channels, sourceNoteClip.frequency, false);
        melodyClip.SetData(melodySamples, 0);

        return melodyClip;
    }
}
