namespace ET.Client
{
    /// <summary>
    /// 音频播放静态 Helper，一行代码即可从任意 Handler/System 触发音效。
    /// 首次调用时懒初始化 AudioManagerComponent。
    /// </summary>
    public static class AudioHelper
    {
        public static void PlayBgm(Scene root, string key)
        {
            GetOrAdd(root)?.PlayBgm(key);
        }

        public static void StopBgm(Scene root)
        {
            GetOrAdd(root)?.StopBgm();
        }

        public static void PlaySfx(Scene root, string key)
        {
            GetOrAdd(root)?.PlaySfx(key);
        }

        public static void PlayUi(Scene root, string key)
        {
            GetOrAdd(root)?.PlayUi(key);
        }

        public static void PlayWeaponFire(Scene root, int weaponTypeId)
        {
            string key = AudioEventId.GetFireSoundByWeaponType(weaponTypeId);
            PlaySfx(root, key);
        }

        private static AudioManagerComponent GetOrAdd(Scene root)
        {
            if (root == null || root.IsDisposed) return null;
            AudioManagerComponent mgr = root.GetComponent<AudioManagerComponent>();
            if (mgr == null)
            {
                mgr = root.AddComponent<AudioManagerComponent>();
            }
            return mgr;
        }
    }
}
