using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现帮助类，提供公共的索引查询逻辑
    /// </summary>
    public static class ServiceDiscoveryHelper
    {
        /// <summary>
        /// 通用的服务查询方法，支持索引优化和多值查询
        /// </summary>
        /// <param name="services">服务字典</param>
        /// <param name="servicesIndexs">服务索引字典</param>
        /// <param name="filterMetadata">过滤条件</param>
        /// <returns>匹配的服务列表</returns>
        public static List<ServiceInfo> GetServiceInfoByFilter(
            Dictionary<string, EntityRef<ServiceInfo>> services,
            Dictionary<string, MultiMapSet<string, string>> servicesIndexs,
            Dictionary<string, string> filterMetadata)
        {
            List<ServiceInfo> serviceInfos = new();

            if (filterMetadata == null || filterMetadata.Count == 0)
            {
                // 没有过滤条件，返回所有服务
                foreach ((_, EntityRef<ServiceInfo> serviceRef) in services)
                {
                    ServiceInfo sInfo = serviceRef;
                    if (sInfo != null)
                    {
                        serviceInfos.Add(sInfo);
                    }
                }
                return serviceInfos;
            }

            // 优先使用索引查询
            HashSet<string> candidateSceneNames = null;
            bool firstIndexedFilter = true;

            // 遍历过滤条件，优先使用有索引的字段
            foreach ((string key, string value) in filterMetadata)
            {
                // 检查是否有索引
                if (servicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index))
                {
                    HashSet<string> matchedSceneNames = new HashSet<string>();

                    // 只有包含逗号时才进行分割，优化性能
                    if (value.Contains(','))
                    {
                        // 支持多值查询，用逗号分割
                        string[] values = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string singleValue in values)
                        {
                            string trimmedValue = singleValue.Trim();
                            HashSet<string> singleMatchedNames = index[trimmedValue];
                            matchedSceneNames.UnionWith(singleMatchedNames);
                        }
                    }
                    else
                    {
                        // 单值查询，直接获取索引结果
                        HashSet<string> singleMatchedNames = index[value];
                        matchedSceneNames.UnionWith(singleMatchedNames);
                    }

                    if (matchedSceneNames.Count == 0)
                    {
                        // 如果某个索引字段没有匹配结果，直接返回空列表
                        return serviceInfos;
                    }

                    if (firstIndexedFilter)
                    {
                        // 第一个索引过滤条件，直接使用结果集
                        candidateSceneNames = new HashSet<string>(matchedSceneNames);
                        firstIndexedFilter = false;
                    }
                    else
                    {
                        // 后续索引过滤条件，取交集
                        candidateSceneNames.IntersectWith(matchedSceneNames);
                        if (candidateSceneNames.Count == 0)
                        {
                            // 交集为空，直接返回
                            return serviceInfos;
                        }
                    }
                }
            }

            // 如果使用了索引查询
            if (candidateSceneNames != null)
            {
                // 从候选集合中筛选最终结果
                foreach (string sceneName in candidateSceneNames)
                {
                    if (services.TryGetValue(sceneName, out var serviceInfoRef))
                    {
                        ServiceInfo sInfo = serviceInfoRef;
                        if (sInfo != null)
                        {
                            // 对于有索引的字段已经匹配了，只需要检查没有索引的字段
                            if (sInfo.MatchesFilter(filterMetadata))
                            {
                                serviceInfos.Add(sInfo);
                            }
                        }
                    }
                }
            }
            else
            {
                // 如果没有任何索引字段，回退到原来的全遍历方式
                foreach ((_, EntityRef<ServiceInfo> serviceRef) in services)
                {
                    ServiceInfo sInfo = serviceRef;
                    if (sInfo != null && sInfo.MatchesFilter(filterMetadata))
                    {
                        serviceInfos.Add(sInfo);
                    }
                }
            }

            return serviceInfos;
        }
    }
}