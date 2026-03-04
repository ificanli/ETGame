using System;
using SimpleJSON;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(WeaponVisualConfigComponent))]
    public static partial class WeaponVisualConfigComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WeaponVisualConfigComponent self)
        {
            self.Configs.Clear();

            TextAsset textAsset = Resources.Load<TextAsset>("WeaponVisualConfigCategory");
            if (textAsset == null)
            {
                Log.Warning("WeaponVisualConfig: WeaponVisualConfigCategory.json not found in Resources.");
                return;
            }

            JSONNode rootNode = JSON.Parse(textAsset.text);
            if (rootNode == null)
            {
                Log.Warning("WeaponVisualConfig: parse json failed.");
                return;
            }

            JSONArray items = rootNode["Items"]?.AsArray;
            if (items == null || items.Count == 0)
            {
                Log.Warning("WeaponVisualConfig: config file is empty.");
                return;
            }

            foreach (JSONNode itemNode in items)
            {
                int weaponId = itemNode["WeaponId"].AsInt;
                if (weaponId <= 0)
                {
                    continue;
                }

                WeaponVisualConfig item = new WeaponVisualConfig
                {
                    WeaponId = weaponId,
                    ProjectileEffect = itemNode["ProjectileEffect"],
                    HitEffect = itemNode["HitEffect"],
                    CasterBindPointId = itemNode["CasterBindPointId"].AsInt,
                    TargetBindPointId = itemNode["TargetBindPointId"].AsInt,
                    ProjectileSpeed = itemNode["ProjectileSpeed"].AsFloat,
                    ProjectileDurationMs = itemNode["ProjectileDurationMs"].AsInt,
                    HitEffectDurationMs = itemNode["HitEffectDurationMs"].AsInt
                };

                self.Configs[weaponId] = item;
            }

            Log.Info($"WeaponVisualConfig: loaded {self.Configs.Count} entries.");
        }

        [EntitySystem]
        private static void Destroy(this WeaponVisualConfigComponent self)
        {
            self.Configs.Clear();
        }

        public static bool TryGetConfig(this WeaponVisualConfigComponent self, int weaponId, out WeaponVisualConfig config)
        {
            return self.Configs.TryGetValue(weaponId, out config);
        }

        public static BindPoint ToBindPoint(this WeaponVisualConfig _, int bindPointId, BindPoint fallback)
        {
            return Enum.IsDefined(typeof(BindPoint), bindPointId) ? (BindPoint)bindPointId : fallback;
        }
    }
}
