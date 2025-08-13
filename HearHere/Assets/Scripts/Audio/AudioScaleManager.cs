using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioScaleManager", menuName = "Audio/AudioScaleManagerSO")]
public class AudioScaleManagerSO : ScriptableObject
{
    private float[] whiteKeyPitches = {
        // 낮은 옥타브 (Low Octave)
        0.500f, // 도(C)
        0.630f, // 미(E)
        0.749f, // 솔(G)

        // 기준 옥타브 (Reference Octave)
        1.000f, // 도(C)
        1.260f, // 미(E)
        1.498f, // 솔(G)

        // 높은 옥타브 (Higher Octave)
        2.000f, // 도(C)
        2.520f, // 미(E)
        2.997f, // 솔(G)
    };

    private int doe = 3;

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

        // 1. (변경 없음)
        int samplesPerNote = (int)(sourceNoteClip.frequency * noteDuration);
        int totalSamples = samplesPerNote * noteSequence.Count;
        float[] melodySamples = new float[totalSamples * sourceNoteClip.channels];

        // 2. (변경 없음)
        float[] sourceSamples = new float[sourceNoteClip.samples * sourceNoteClip.channels];
        sourceNoteClip.GetData(sourceSamples, 0);

        // 3. (변경 없음)
        for (int i = 0; i < noteSequence.Count; i++)
        {
            int pitchIndex = noteSequence[i] + doe;
            if (pitchIndex < 0 || pitchIndex >= whiteKeyPitches.Length)
            {
                // 음계 범위를 벗어나면 해당 노트는 무음으로 처리합니다. (선택 사항)
                continue;
            }
            float pitch = whiteKeyPitches[pitchIndex];

            // =======================================================================
            // 4. 피치에 맞게 원본 샘플을 리샘플링하는 수정된 로직
            // =======================================================================
            for (int sampleIndex = 0; sampleIndex < samplesPerNote; sampleIndex++)
            {
                // 현재 음표 내에서 원본 오디오의 어느 위치(프레임)를 읽을지 계산합니다.
                float sourceFramePosition = sampleIndex * pitch;

                // 프레임 인덱스로 변환하고, 원본 클립의 총 프레임 수를 넘지 않도록 루프시킵니다.
                // 이렇게 하면 noteDuration이 원본 클립보다 길어도 자연스럽게 루프됩니다.
                int sourceFrameIndex = (int)sourceFramePosition % sourceNoteClip.samples;

                for (int channel = 0; channel < sourceNoteClip.channels; channel++)
                {
                    // 정확한 읽기/쓰기 위치를 계산합니다.
                    int readIndex = sourceFrameIndex * sourceNoteClip.channels + channel;
                    int writeIndex = (i * samplesPerNote + sampleIndex) * sourceNoteClip.channels + channel;

                    melodySamples[writeIndex] = sourceSamples[readIndex];
                }
            }
        }

        // 5. (변경 없음)
        AudioClip melodyClip = AudioClip.Create("GeneratedMelody", totalSamples, sourceNoteClip.channels, sourceNoteClip.frequency, false);
        melodyClip.SetData(melodySamples, 0);

        return melodyClip;
    }
    }
