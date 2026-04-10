using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class AudioManagerComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // ── BGM (双 Source 交叉淡入) ──
        public AudioSource BgmSourceA;
        public AudioSource BgmSourceB;
        public string CurrentBgmKey;
        public string TargetBgmKey;
        public float CrossfadeDuration;
        public float CrossfadeTimer;
        public bool IsCrossfading;

        // ── SFX 对象池 ──
        public AudioSource[] SfxSources;
        public int SfxPoolSize;
        public int NextSfxIndex;

        // ── UI 专用 Source ──
        public AudioSource UiSource;

        // ── 音量设置 ──
        public float BgmVolume;
        public float SfxVolume;
        public float UiVolume;
        public bool IsMuted;

        // ── 音效缓存 ──
        public Dictionary<string, AudioClip> ClipCache;
        public bool PlaceholderMode;

        // ── 宿主 GameObject ──
        public GameObject AudioRoot;
    }
}
