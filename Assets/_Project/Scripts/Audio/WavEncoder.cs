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

        /// <summary>
        /// Reads a 16-bit PCM mono or stereo WAV (any chunk layout) into float samples, downmixing stereo to mono.
        /// </summary>
        public static float[] DecodePcm16(byte[] wav, out int sampleRate)
        {
            if (wav == null || wav.Length < 44 || Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wav, 8, 4) != "WAVE")
                throw new FormatException("Not a RIFF/WAVE file");

            int channels = 1, bits = 16;
            sampleRate = 0;
            int pos = 12;
            while (pos + 8 <= wav.Length)
            {
                string id = Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                int body = pos + 8;
                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(wav, body + 2);
                    sampleRate = BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToInt16(wav, body + 14);
                }
                else if (id == "data")
                {
                    if (bits != 16) throw new FormatException($"Only 16-bit PCM is supported (got {bits})");
                    int frames = Math.Min(size, wav.Length - body) / (2 * channels);
                    var samples = new float[frames];
                    for (int f = 0; f < frames; f++)
                    {
                        float sum = 0f;
                        for (int c = 0; c < channels; c++)
                            sum += BitConverter.ToInt16(wav, body + (f * channels + c) * 2) / 32768f;
                        samples[f] = sum / channels;
                    }
                    return samples;
                }
                pos = body + size + (size & 1);
            }
            throw new FormatException("No data chunk");
        }

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
