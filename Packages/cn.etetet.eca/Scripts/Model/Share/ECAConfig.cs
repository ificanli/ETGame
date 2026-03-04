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
            value = 0;
            string paramValue = this.GetParamValue(key);
            return !string.IsNullOrWhiteSpace(paramValue) &&
                   int.TryParse(paramValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetFloatParam(string key, out float value)
        {
            value = 0f;
            string paramValue = this.GetParamValue(key);
            return !string.IsNullOrWhiteSpace(paramValue) &&
                   float.TryParse(paramValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public string GetParamValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || this.Params == null || this.Params.Count == 0)
            {
                return null;
            }

            foreach (FlowParam param in this.Params)
            {
                if (param == null || !string.Equals(param.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                return param.Value;
            }

            return null;
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
    }
}
