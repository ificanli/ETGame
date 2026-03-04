using System.Globalization;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace ET.Client
{
    [EnableClass]
    public class ECAPointMarker : SerializedMonoBehaviour
    {
        [Header("基础配置")]
        public string ConfigId;
        [ValueDropdown(nameof(GetPointTypeOptions))]
        [OnValueChanged(nameof(OnTypeChanged))]
        public int Type = ECAPointType.EvacuationPoint;
        public List<FlowParam> Params = new();
        [HideInInspector]
        [FormerlySerializedAs("Config")]
        public ECAConfigAsset LegacyConfig;

        [Header("流程图")]
        public FlowGraphAsset FlowGraph;

        [Header("可视化")]
        public bool ShowRange = true;
        public Color RangeColor = Color.green;

        private void OnValidate()
        {
            this.MigrateLegacyConfigIfNeeded();
            this.ApplyTypeParamTemplate();
        }

        private void OnDrawGizmos()
        {
            if (ShowRange)
            {
                Gizmos.color = RangeColor;
                Gizmos.DrawWireSphere(transform.position, this.GetInteractRangeForGizmos());
            }
        }

        public ECAConfig GetConfig()
        {
            this.MigrateLegacyConfigIfNeeded();
            this.ApplyTypeParamTemplate();

            if (string.IsNullOrWhiteSpace(this.ConfigId))
            {
                Debug.LogError($"ECAPointMarker at {transform.position} has empty ConfigId!");
                return null;
            }

            Vector3 worldPos = this.transform.position;
            ECAConfig config = new()
            {
                ConfigId = this.ConfigId,
                Type = this.Type,
                PosX = worldPos.x,
                PosY = worldPos.y,
                PosZ = worldPos.z,
                Params = CopyParams(this.Params),
                FlowGraph = this.FlowGraph != null ? this.FlowGraph.Graph : null
            };

            config.SyncLegacyFieldsForCompatibility();
            return config;
        }

        private float GetInteractRangeForGizmos()
        {
            if (TryGetFloatParam(this.Params, ECAPointParamKey.InteractRange, out float range) && range > 0f)
            {
                return range;
            }

            return ECAConfig.DefaultInteractRange;
        }

        private static List<FlowParam> CopyParams(List<FlowParam> source)
        {
            List<FlowParam> copy = new();
            if (source == null || source.Count == 0)
            {
                return copy;
            }

            foreach (FlowParam param in source)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                copy.Add(new FlowParam
                {
                    Key = param.Key,
                    Value = param.Value
                });
            }

            return copy;
        }

        private static bool TryGetFloatParam(List<FlowParam> source, string key, out float value)
        {
            value = 0f;
            if (source == null || source.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in source)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                return float.TryParse(param.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }

            return false;
        }

        private void OnTypeChanged()
        {
            this.ApplyTypeParamTemplate();
        }

        private void ApplyTypeParamTemplate()
        {
            this.Params ??= new List<FlowParam>();

            Dictionary<string, string> templateValues = new();
            foreach (FlowParam param in this.Params)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Key))
                {
                    continue;
                }

                if (param.Key == ECAPointParamKey.InteractRange || param.Key == ECAPointParamKey.TeamId)
                {
                    templateValues[param.Key] = param.Value;
                }
            }

            this.RemoveTemplateParam(ECAPointParamKey.InteractRange);
            this.RemoveTemplateParam(ECAPointParamKey.TeamId);

            foreach (FlowParam templateParam in this.GetTypeParamTemplate(this.Type))
            {
                string value = templateParam.Value;
                if (templateValues.TryGetValue(templateParam.Key, out string oldValue) && !string.IsNullOrWhiteSpace(oldValue))
                {
                    value = oldValue;
                }

                this.SetOrAddParam(templateParam.Key, value);
            }
        }

        private List<FlowParam> GetTypeParamTemplate(int pointType)
        {
            List<FlowParam> template = new()
            {
                new FlowParam
                {
                    Key = ECAPointParamKey.InteractRange,
                    Value = ECAConfig.DefaultInteractRange.ToString(CultureInfo.InvariantCulture)
                }
            };

            if (pointType == ECAPointType.SpawnPoint)
            {
                template.Add(new FlowParam
                {
                    Key = ECAPointParamKey.TeamId,
                    Value = "0"
                });
            }

            return template;
        }

        private static IEnumerable<ValueDropdownItem<int>> GetPointTypeOptions()
        {
            return new List<ValueDropdownItem<int>>
            {
                new("EvacuationPoint(撤离点)", ECAPointType.EvacuationPoint),
                new("SpawnPoint(出生点)", ECAPointType.SpawnPoint),
                new("Container(容器)", ECAPointType.Container),
            };
        }

        private void MigrateLegacyConfigIfNeeded()
        {
            if (this.LegacyConfig == null)
            {
                return;
            }

            bool isNewConfigEmpty = string.IsNullOrWhiteSpace(this.ConfigId) &&
                                    (this.Params == null || this.Params.Count == 0);
            if (!isNewConfigEmpty)
            {
                return;
            }

            this.ConfigId = this.LegacyConfig.ConfigId;
            this.Type = this.LegacyConfig.PointType;

            this.Params ??= new List<FlowParam>();
            this.SetOrAddParam(ECAPointParamKey.InteractRange, this.LegacyConfig.InteractRange.ToString(CultureInfo.InvariantCulture));
            if (this.LegacyConfig.TeamId != 0)
            {
                this.SetOrAddParam(ECAPointParamKey.TeamId, this.LegacyConfig.TeamId.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void SetOrAddParam(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            foreach (FlowParam param in this.Params)
            {
                if (param == null || param.Key != key)
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

        private void RemoveTemplateParam(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || this.Params == null || this.Params.Count == 0)
            {
                return;
            }

            this.Params.RemoveAll(param => param != null && param.Key == key);
        }
    }
}
