using System;
using System.Collections.Generic;
using System.Text;

namespace PEAK_AutoMic;

internal class PlayerVoiceInfo
{
    private float outputLevel;
    private readonly int processBuffer;
    private bool initialGate;
    private float avgLUFS;
    private ulong countLUFS;

    public readonly int voiceID;
    public readonly int sampleRateMS;

    private BiQuadFilter preFilter;
    private BiQuadFilter rlbFilter;

    private const int TERM_MS = 800;
    // These values were tweaked for best performance with a pristine audio stack
    //private const float TARGET_LUFS = -20.0f; 
   // private const float MAX_DYNAMIC_RANGE = 3.0f;

    // This is for whatever the fuck PEAK is doing, holy shit
    private const float TARGET_LUFS = -33.0f;
    private const float MAX_DYNAMIC_RANGE = 5.0f; // in LUFS
    private const float INITIAL_LUFS_GATE = -37.0f;

    // This is the approximate LUFS value recorded for a very loud noise at 0 dB from the microphone, serving as a maximum upper bound
    private const float LUFS_CEILING = -15.0f;
    
    private float[] squaredBuffer;
    //private float[] squaredDiff;
    private int bufferIndex = 0;
    private float runningSum = 0;
    //private float runningDiff = 0;
    private int sampleCount = 0;

    public PlayerVoiceInfo(int samplingRate, int voiceId = -1)
    {
        preFilter = BiQuadFilter.HighShelf((float)samplingRate, 1500.0f, 0.707f, 4.0f);
        rlbFilter = BiQuadFilter.HighPassFilter((float)samplingRate, 100.0f, 0.707f);
        MaxLUFS = -300.0f;
        outputLevel = 1.0f;
        voiceID = voiceId;
        sampleRateMS = samplingRate / 1000;
        processBuffer = TERM_MS * sampleRateMS;
        squaredBuffer = new float[processBuffer];
        //squaredDiff = new float[processBuffer];
        initialGate = false;
        avgLUFS = 0.0f;
        countLUFS = 0;
    }

    public void ProcessSamples(float[] samples)
    {
        foreach (float sample in samples)
        {
            float prefiltered = preFilter.Transform(sample);
            float filtered = rlbFilter.Transform(prefiltered);
            float squared = filtered * filtered;

            // Remove old sample from sum if buffer full
            if (sampleCount >= processBuffer)
            {
                runningSum -= squaredBuffer[bufferIndex];
                //runningDiff -= squaredDiff[bufferIndex];
            }

            // Add new squared sample
            squaredBuffer[bufferIndex] = squared;
            runningSum += squared;

            bufferIndex = (bufferIndex + 1) % processBuffer;
            if (sampleCount < processBuffer) sampleCount++;

            //float diff = sample - (runningSum / sampleCount);
            //float diffsq = diff * diff;
            //squaredDiff[bufferIndex] = diffsq;
            //runningDiff += diffsq;
        }
    }

    //public float GetVariance() { return runningDiff / sampleCount; }

    public float GetShortTermLUFS()
    {
        if (sampleCount < processBuffer) return -300.0f; // Not enough samples

        double mean = runningSum / processBuffer;
        if (mean <= 0.0) return -300.0f;

        //Plugin.Log.LogInfo($"VOICETEST RMS: {mean}");

        return (float)(10.0 * Math.Log10(mean) - 0.691);
    }

    public float MaxLUFS { get; set; }

    public void RecordLUFS(float level)
    {   
        // TODO: Potentially use a longer 3 second window to calculate MaxLUFS - so far this hasn't been necessary.
        MaxLUFS = Math.Max(level, MaxLUFS);

        // Gate 
        if (!initialGate)
        {
            if(MaxLUFS > INITIAL_LUFS_GATE)
            {
                initialGate = true;
            }
            else
            {
                return;
            }
        }

        // Record the current LUFS level but only if it's within MAX_DYNAMIC_RANGE of MaxLUFS
        // TODO: This doesn't work because the initial quiet frames that get sent drag the average down way too much.
        if(level > (MaxLUFS - MAX_DYNAMIC_RANGE))
        {
            countLUFS += 1;
            double delta = level - avgLUFS;
            avgLUFS += (float)(delta / (double)countLUFS);
        }

        // Clamp the maximum dynamic range to MAX_DYNAMIC_RANGE below the target or MAX_DYNAMIC_RANGE below the current detected
        // maximum volume, whatever is smaller, so we don't amplify background noise.
        //float clampLUFS = Math.Max(Math.Min(TARGET_LUFS, avgLUFS) - MAX_DYNAMIC_RANGE, level);
        float clampLUFS = Math.Max(MaxLUFS - MAX_DYNAMIC_RANGE, level);

        outputLevel = (float)Math.Pow(10.0, (TARGET_LUFS - clampLUFS) / 20.0);
    }

    public float GetOutputLevel() { return outputLevel; }
}
