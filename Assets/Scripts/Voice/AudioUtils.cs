using System;
using System.IO;
using UnityEngine;

public static class AudioUtils
{
    public static byte[] EncodeToWAV(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                var samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                int sampleRate = clip.frequency;
                int channels = clip.channels;
                short bitsPerSample = 16;
                int byteRate = sampleRate * channels * (bitsPerSample / 8);
                int blockAlign = channels * (bitsPerSample / 8);

                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)blockAlign);
                writer.Write(bitsPerSample);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * 32767));
                }
            }
            return stream.ToArray();
        }
    }
}