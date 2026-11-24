using System;
using System.IO;
using System.Text;

namespace Jupiter.Audio
{
    /// <summary>
    /// Encodes float samples (-1..1) as a 16-bit PCM RIFF/WAVE file.
    /// Used by the editor tool that generates the project's original procedural sounds.
    /// </summary>
    public static class WavEncoder
    {
        public const int HeaderSize = 44;

        public static byte[] Encode(float[] samples, int sampleRate, int channels = 1)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));

            const short bitsPerSample = 16;
            int blockAlign = channels * bitsPerSample / 8;
            int dataSize = samples.Length * 2;

            using (var ms = new MemoryStream(HeaderSize + dataSize))
            using (var w = new BinaryWriter(ms, Encoding.ASCII))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataSize);                 // chunk size
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);                            // fmt chunk size (PCM)
                w.Write((short)1);                      // audio format: PCM
                w.Write((short)channels);
                w.Write(sampleRate);
                w.Write(sampleRate * blockAlign);       // byte rate
                w.Write((short)blockAlign);
                w.Write(bitsPerSample);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataSize);

                foreach (float s in samples)
                {
                    float c = s < -1f ? -1f : (s > 1f ? 1f : s);
                    w.Write((short)Math.Round(c * short.MaxValue));
                }
                return ms.ToArray();
            }
        }
    }
}
