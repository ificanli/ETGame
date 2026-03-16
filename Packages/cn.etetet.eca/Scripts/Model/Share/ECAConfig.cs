using System;
using System.Collections.Generic;
using System.Globalization;

namespace ET
{
    [Serializable]
    [EnableClass]
    public class ECAConfig
    {
        public const float DefaultInteractRange = 3f;
        public const int DefaultCheckRangeIntervalMs = 200;
        public const int DefaultEvacuationDurationMs = 10000;
        public const string DefaultLobbyMapName = "Home";
        public const float DefaultNavBlockHalfExtentsX = 1.25f;
        public const float DefaultNavBlockHalfExtentsY = 2.5f;
        public const float DefaultNavBlockHalfExtentsZ = 1.25f;

        public string ConfigId;
        public int Type;
        public List<FlowParam> Params = new();
        public float PosX;
        public float PosY;
        public float PosZ;
        public FlowGraphData FlowGraph;

        // 兼容旧导出格式（PointType/TeamId/InteractRange）
        public int PointType;
        public int TeamId;
        public float InteractRange = DefaultInteractRange;

        public int GetPointType()
        {
            return this.Type != 0 ? this.Type : this.PointType;
        }

        public int GetTeamId()
        {
            if (this.TryGetIntParam(ECAPointParamKey.TeamId, out int teamId))
            {
                return teamId;
            }

            return this.TeamId;
        }

        public float GetInteractRange()
        {
            if (this.TryGetFloatParam(ECAPointParamKey.InteractRange, out float range))
            {
                return range;
            }

            return this.InteractRange > 0f ? this.InteractRange : DefaultInteractRange;
        }

        public bool TryGetIntParam(string key, out int value)
        {
            return FlowParamHelper.TryGetIntParam(this.Params, key, out value);
        }

        public bool TryGetFloatParam(string key, out float value)
        {
            return FlowParamHelper.TryGetFloatParam(this.Params, key, out value);
        }

        public string GetParamValue(string key)
        {
            return FlowParamHelper.GetParamValue(this.Params, key);
        }

        public void SetParam(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            this.Params ??= new List<FlowParam>();

            foreach (FlowParam param in this.Params)
            {
                if (param == null || !string.Equals(param.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                param.Value = value;
                return;
            }

            this.Params.Add(new FlowParam
            {
                Key = key,
                Value = value
            });
        }

        public void SyncLegacyFieldsForCompatibility()
        {
            this.PointType = this.GetPointType();
            this.TeamId = this.GetTeamId();
            this.InteractRange = this.GetInteractRange();
        }

        public int GetCheckRangeIntervalMs()
        {
            return this.TryGetIntParam(ECAPointParamKey.CheckRangeIntervalMs, out int intervalMs) && intervalMs > 0
                ? intervalMs
                : DefaultCheckRangeIntervalMs;
        }

        public int GetEvacuationDurationMs()
        {
            return this.TryGetIntParam(ECAPointParamKey.EvacuationDurationMs, out int durationMs) && durationMs > 0
                ? durationMs
                : DefaultEvacuationDurationMs;
        }

        public string GetLobbyMapName()
        {
            string mapName = this.GetParamValue(ECAPointParamKey.LobbyMapName);
            return string.IsNullOrWhiteSpace(mapName) ? DefaultLobbyMapName : mapName;
        }
    }
}
