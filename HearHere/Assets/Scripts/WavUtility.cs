using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static void SaveClipAsWav(AudioClip clip, string path)
    {
        byte[] wavData = ConvertToWav(clip);
        File.WriteAllBytes(path, wavData);
    }

    public static byte[] ConvertToWav(AudioClip clip)
    {
        int samples = clip.samples;
        int channels = clip.channels;
        int frequency = clip.frequency;
        
        float[] data = new float[samples * channels];
        clip.GetData(data, 0);

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int byteRate = frequency * channels * 2; // 16-bit PCM

            // RIFF header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples * channels * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt subchunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16); // bits per sample

            // data subchunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(samples * channels * 2);

            // Write PCM samples
            for (int i = 0; i < data.Length; i++)
            {
                short val = (short)(Mathf.Clamp(data[i], -1.0f, 1.0f) * 32767f);
                writer.Write(val);
            }

            return stream.ToArray();
        }
    }
}
