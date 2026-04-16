using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(AudioManagerComponent))]
    public static partial class AudioManagerComponentSystem
    {
        private const int DefaultSfxPoolSize = 8;
        private const float DefaultBgmVolume = 0.5f;
        private const float DefaultSfxVolume = 0.7f;
        private const float DefaultUiVolume  = 0.8f;
        private const float DefaultCrossfadeDuration = 1.5f;

        [EntitySystem]
        private static void Awake(this AudioManagerComponent self)
        {
            self.ClipCache = new Dictionary<string, AudioClip>();
            self.PlaceholderMode = false;
            self.BgmVolume = DefaultBgmVolume;
            self.SfxVolume = DefaultSfxVolume;
            self.UiVolume  = DefaultUiVolume;
            self.SfxPoolSize = DefaultSfxPoolSize;
            self.CrossfadeDuration = DefaultCrossfadeDuration;

            // 创建持久化 AudioRoot
            GameObject root = new GameObject("[AudioManager]");
            UnityEngine.Object.DontDestroyOnLoad(root);
            self.AudioRoot = root;

            // BGM 双 Source
            self.BgmSourceA = CreateSource(root, "BgmA", true);
            self.BgmSourceB = CreateSource(root, "BgmB", true);

            // SFX 池
            self.SfxSources = new AudioSource[self.SfxPoolSize];
            for (int i = 0; i < self.SfxPoolSize; i++)
            {
                self.SfxSources[i] = CreateSource(root, $"Sfx_{i}", false);
            }

            // UI 专用
            self.UiSource = CreateSource(root, "Ui", false);
            self.UiSource.priority = 0;
        }

        [EntitySystem]
        private static void Update(this AudioManagerComponent self)
        {
            if (!self.IsCrossfading) return;

            self.CrossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(self.CrossfadeTimer / self.CrossfadeDuration);

            float vol = self.IsMuted ? 0f : self.BgmVolume;
            self.BgmSourceA.volume = vol * (1f - t);
            self.BgmSourceB.volume = vol * t;

            if (t >= 1f)
            {
                // 交叉淡入完成：A 停止，B 变为 A
                self.BgmSourceA.Stop();
                (self.BgmSourceA, self.BgmSourceB) = (self.BgmSourceB, self.BgmSourceA);
                self.CurrentBgmKey = self.TargetBgmKey;
                self.TargetBgmKey = null;
                self.IsCrossfading = false;
            }
        }

        [EntitySystem]
        private static void Destroy(this AudioManagerComponent self)
        {
            if (self.AudioRoot != null)
            {
                UnityEngine.Object.Destroy(self.AudioRoot);
            }
            self.ClipCache?.Clear();
            self.SfxSources = null;
        }

        // ── Public API ──

        public static void PlayBgm(this AudioManagerComponent self, string key)
        {
            if (self.IsMuted) return;
            if (key == self.CurrentBgmKey && !self.IsCrossfading) return;

            AudioClip clip = self.GetOrCreateClip(key);
            if (clip == null) return;

            // 如果正在交叉淡入，立即完成当前淡入
            if (self.IsCrossfading)
            {
                self.BgmSourceA.Stop();
                (self.BgmSourceA, self.BgmSourceB) = (self.BgmSourceB, self.BgmSourceA);
                self.CurrentBgmKey = self.TargetBgmKey;
                self.IsCrossfading = false;
            }

            self.TargetBgmKey = key;
            self.BgmSourceB.clip = clip;
            self.BgmSourceB.volume = 0f;
            self.BgmSourceB.Play();
            self.CrossfadeTimer = 0f;
            self.IsCrossfading = true;
        }

        public static void StopBgm(this AudioManagerComponent self)
        {
            self.BgmSourceA.Stop();
            self.BgmSourceB.Stop();
            self.CurrentBgmKey = null;
            self.TargetBgmKey = null;
            self.IsCrossfading = false;
        }

        public static void PlaySfx(this AudioManagerComponent self, string key)
        {
            if (self.IsMuted) return;

            AudioClip clip = self.GetOrCreateClip(key);
            if (clip == null) return;

            AudioSource source = self.SfxSources[self.NextSfxIndex];
            source.PlayOneShot(clip, self.SfxVolume);
            self.NextSfxIndex = (self.NextSfxIndex + 1) % self.SfxPoolSize;
        }

        public static void PlayUi(this AudioManagerComponent self, string key)
        {
            if (self.IsMuted) return;

            AudioClip clip = self.GetOrCreateClip(key);
            if (clip == null) return;

            self.UiSource.PlayOneShot(clip, self.UiVolume);
        }

        public static void SetMuted(this AudioManagerComponent self, bool muted)
        {
            self.IsMuted = muted;
            if (muted)
            {
                self.BgmSourceA.volume = 0f;
                self.BgmSourceB.volume = 0f;
            }
            else if (!self.IsCrossfading)
            {
                self.BgmSourceA.volume = self.BgmVolume;
            }
        }

        // ── Internal ──

        private static AudioClip GetOrCreateClip(this AudioManagerComponent self, string key)
        {
            if (self.ClipCache.TryGetValue(key, out AudioClip cached))
            {
                return cached;
            }

            if (self.PlaceholderMode)
            {
                AudioClip clip = AudioPlaceholderGenerator.Generate(key);
                self.ClipCache[key] = clip;
                return clip;
            }

            // 真实资源模式：从 YooAssets 同步加载
            ResourcesLoaderComponent loader = self.Root().GetComponent<ResourcesLoaderComponent>();
            try
            {
                if (loader?.package != null)
                {
                    if (loader.package.CheckLocationValid(key))
                    {
                        AudioClip clip = loader.LoadAssetSync<AudioClip>(key);
                        if (clip != null)
                        {
                            self.ClipCache[key] = clip;
                            return clip;
                        }
                    }

                    Log.Warning($"[Audio] clip missing, fallback placeholder: {key}");
                    AudioClip fallbackClip = AudioPlaceholderGenerator.Generate(key);
                    self.ClipCache[key] = fallbackClip;
                    return fallbackClip;
                }
            }
            catch (System.Exception exception)
            {
                Log.Warning($"[Audio] clip load failed, fallback placeholder: {key}, exception: {exception.Message}");
            }

            Log.Warning($"[Audio] loader unavailable, fallback placeholder: {key}");
            AudioClip defaultClip = AudioPlaceholderGenerator.Generate(key);
            self.ClipCache[key] = defaultClip;
            return defaultClip;
        }

        private static AudioSource CreateSource(GameObject parent, string name, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D
            return source;
        }
    }
}
