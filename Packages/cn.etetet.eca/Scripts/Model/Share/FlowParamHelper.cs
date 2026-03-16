using System;
using System.Collections.Generic;
using System.Globalization;

namespace ET
{
    public static class FlowParamHelper
    {
        public static bool TryGetIntParam(FlowNodeData node, string key, out int value)
        {
            return TryGetIntParam(node?.Params, key, out value);
        }

        public static bool TryGetIntParam(List<FlowParam> source, string key, out int value)
        {
            value = 0;
            return TryGetParamValue(source, key, out string rawValue) &&
                   int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryGetFloatParam(FlowNodeData node, string key, out float value)
        {
            return TryGetFloatParam(node?.Params, key, out value);
        }

        public static bool TryGetFloatParam(List<FlowParam> source, string key, out float value)
        {
            value = 0f;
            return TryGetParamValue(source, key, out string rawValue) &&
                   float.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryGetBoolParam(FlowNodeData node, string key, out bool value)
        {
            return TryGetBoolParam(node?.Params, key, out value);
        }

        public static bool TryGetBoolParam(List<FlowParam> source, string key, out bool value)
        {
            value = false;
            if (!TryGetParamValue(source, key, out string rawValue))
            {
                return false;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                value = intValue != 0;
                return true;
            }

            return bool.TryParse(rawValue, out value);
        }

        public static bool TryGetStringParam(FlowNodeData node, string key, out string value)
        {
            return TryGetStringParam(node?.Params, key, out value);
        }

        public static bool TryGetStringParam(List<FlowParam> source, string key, out string value)
        {
            value = GetParamValue(source, key);
            return !string.IsNullOrWhiteSpace(value);
        }

        public static string GetParamValue(List<FlowParam> source, string key)
        {
            if (!TryGetParamValue(source, key, out string value))
            {
                return null;
            }

            return value;
        }

        public static string GetStringParamOrDefault(List<FlowParam> source, string key, string defaultValue)
        {
            return TryGetStringParam(source, key, out string value) ? value : defaultValue;
        }

        public static bool TryGetParamValue(List<FlowParam> source, string key, out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(key) || source == null || source.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in source)
            {
                if (param == null || !string.Equals(param.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                value = param.Value;
                return true;
            }

            return false;
        }

        public static bool MatchAllParams(List<FlowParam> expectedParams, List<FlowParam> actualParams)
        {
            if (expectedParams == null || expectedParams.Count == 0)
            {
                return true;
            }

            if (actualParams == null || actualParams.Count == 0)
            {
                return false;
            }

            foreach (FlowParam expectedParam in expectedParams)
            {
                if (expectedParam == null)
                {
                    continue;
                }

                if (!TryGetParamValue(actualParams, expectedParam.Key, out string actualValue) ||
                    !string.Equals(actualValue, expectedParam.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
