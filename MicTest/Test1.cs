using Microsoft.CodeCoverage.Core.Reports.Coverage;
using PEAK_AutoMic;
using UnityEngine.UIElements;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace MicTest
{
    [TestClass]
    public sealed class Test1
    {
        [TestInitialize]
        public void TestInit()
        {
            // This method is called before each test method.
        }

        [TestMethod]
        public void TestExternal()
        {
            float[] samples;
            byte[] header;

            {
                using var fs = new FileStream("D:/mic_awful24.wav", FileMode.Open);
                using var reader = new BinaryReader(fs);

                // Skip RIFF header
                header = reader.ReadBytes(44);

                int sampleCount = (int)((fs.Length - 44) / sizeof(Int16));
                samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    //samples[i] = reader.ReadSingle();
                    samples[i] = reader.ReadInt16() / 32676.0f;
                }
            }

            var info = new PlayerVoiceInfo(24000);
            const int FRAME_COUNT = 4096;

            for(int i = 0; i < samples.Length; i += FRAME_COUNT)
            {
                info.ProcessSamples(samples[i..Math.Min(i + FRAME_COUNT, samples.Length)]);
                
                info.RecordLUFS(info.GetShortTermLUFS());
                var scale = info.GetOutputLevel();
                //Console.WriteLine(scale);

                for (int j = i; j < Math.Min(i + FRAME_COUNT, samples.Length); ++j)
                {
                    float s = samples[j] * scale;
                    samples[j] = s;
                }
            }

            {
                using var fs = new FileStream("D:/mic_awful2.wav", FileMode.Create);
                using var w = new BinaryWriter(fs);
                w.Write(header);
                foreach (var s in samples)
                {
                    short i = (short)Math.Clamp(s * 32676.0f, -32676.0f, 32676.0f);
                    w.Write(BitConverter.GetBytes(i));
                }
            }

            Console.WriteLine(info.MaxLUFS);
        }
    }
}
