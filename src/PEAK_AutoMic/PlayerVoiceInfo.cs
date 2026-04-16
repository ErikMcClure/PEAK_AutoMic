using System;
using System.Collections.Generic;
using System.Text;

namespace PEAK_AutoMic;

internal struct BiquadFilter
{
    private double a1, a2, b0, b1, b2;
    private double x1, x2, y1, y2;

    public BiquadFilter(double b0, double b1, double b2, double a1, double a2)
    {
        this.b0 = b0;
        this.b1 = b1;
        this.b2 = b2;
        this.a1 = a1;
        this.a2 = a2;
        x1 = x2 = y1 = y2 = 0;
    }

    public double Process(double input)
    {
        double output = b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
        x2 = x1;
        x1 = input;
        y2 = y1;
        y1 = output;
        return output;
    }
}

internal class PlayerVoiceInfo
{
    private float maxLUFS;
    private float avgLUFS;
    private float outputLevel;

    public float[] sampleCache;

    private BiquadFilter preFilter = new BiquadFilter(1.53512485958697, -2.69169618940638, 1.19839281085285, -1.69065929318241, 0.73248077421585);
    private BiquadFilter rlFilter = new BiquadFilter(1.0, -1.99004745483398, 0.99007225036621, -1.53512485958697, -2.69169618940638);

    private const int SAMPLE_RATE_MS = 44;
    private const int TERM_MS = 400;
    private const float TARGET_LUFS = -8.0f;
    private readonly int SHORT_TERM_SAMPLES = SAMPLE_RATE_MS * TERM_MS;

    private float[] filteredSquaredBuffer;
    private int bufferIndex = 0;
    private double runningSum = 0;
    private int sampleCount = 0;

    public PlayerVoiceInfo(int sampleCount)
    {
        maxLUFS = -300.0f;
        avgLUFS = -300.0f;
        outputLevel = 1.0f;
        sampleCache = new float[sampleCount];
        filteredSquaredBuffer = new float[SHORT_TERM_SAMPLES];
    }

    public void ProcessSamples(float[] samples)
    {
        foreach (float sample in samples)
        {
            double filtered = preFilter.Process(sample);
            filtered = rlFilter.Process(filtered);
            double squared = filtered * filtered;

            // Remove old sample from sum if buffer full
            if (sampleCount >= SHORT_TERM_SAMPLES)
            {
                runningSum -= filteredSquaredBuffer[bufferIndex];
            }

            // Add new squared sample
            filteredSquaredBuffer[bufferIndex] = (float)squared;
            runningSum += squared;

            bufferIndex = (bufferIndex + 1) % SHORT_TERM_SAMPLES;
            if (sampleCount < SHORT_TERM_SAMPLES) sampleCount++;
        }
    }

    public float GetShortTermLUFS()
    {
        if (sampleCount < SHORT_TERM_SAMPLES) return -300.0f; // Not enough samples

        double mean = runningSum / SHORT_TERM_SAMPLES;
        if (mean <= 0.0) return -300.0f;

        Plugin.Log.LogInfo($"VOICETEST RMS: {mean}");

        return (float)(10.0 * Math.Log10(mean) - 0.691);
    }

    public void RecordLUFS(float level)
    {
        maxLUFS = Math.Max(level, maxLUFS);
        avgLUFS = (maxLUFS + level) * 0.5f; // Anchor the LUFS value to the maximum value so it doesn't magnify background noise.
        outputLevel = (float)Math.Pow(10.0, (TARGET_LUFS - avgLUFS) / 20.0);
    }

    public float GetOutputLevel() { return outputLevel; }
}
