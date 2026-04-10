using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 运行时生成占位 AudioClip（正弦波），不同音频类别用不同频率和时长。
    /// 真实资源到位后设置 PlaceholderMode=false 即可跳过。
    /// </summary>
    public static class AudioPlaceholderGenerator
    {
        private const int SampleRate = 44100;

        public static AudioClip Generate(string key)
        {
            float frequency;
            float duration;

            if (key.Contains("bgm"))
            {
                frequency = 220f;
                duration  = 4f;
            }
            else if (key.Contains("shotgun"))
            {
                frequency = 600f;
                duration  = 0.15f;
            }
            else if (key.Contains("rifle1"))
            {
                frequency = 880f;
                duration  = 0.08f;
            }
            else if (key.Contains("rifle2"))
            {
                frequency = 1000f;
                duration  = 0.06f;
            }
            else if (key.Contains("rocket"))
            {
                frequency = 300f;
                duration  = 0.25f;
            }
            else if (key.Contains("fire"))
            {
                frequency = 800f;
                duration  = 0.1f;
            }
            else if (key.Contains("hit") || key.Contains("hurt"))
            {
                frequency = 1200f;
                duration  = 0.08f;
            }
            else if (key.Contains("reload"))
            {
                frequency = 400f;
                duration  = 0.5f;
            }
            else if (key.Contains("death") || key.Contains("fail"))
            {
                frequency = 150f;
                duration  = 1f;
            }
            else if (key.Contains("level_up") || key.Contains("success"))
            {
                frequency = 660f;
                duration  = 0.4f;
            }
            else if (key.Contains("choice") || key.Contains("reroll"))
            {
                frequency = 550f;
                duration  = 0.2f;
            }
            else if (key.Contains("click"))
            {
                frequency = 1000f;
                duration  = 0.04f;
            }
            else if (key.Contains("evac"))
            {
                frequency = 500f;
                duration  = 0.6f;
            }
            else if (key.Contains("container") || key.Contains("panel"))
            {
                frequency = 440f;
                duration  = 0.15f;
            }
            else if (key.Contains("pickup"))
            {
                frequency = 750f;
                duration  = 0.1f;
            }
            else
            {
                frequency = 440f;
                duration  = 0.2f;
            }

            int sampleCount = (int)(SampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create($"placeholder_{key}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
