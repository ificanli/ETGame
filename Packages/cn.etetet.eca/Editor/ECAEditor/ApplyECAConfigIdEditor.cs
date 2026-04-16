using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET.Client
{
    public static class ApplyECAConfigIdEditor
    {
        [MenuItem("ET/ECA/Fill Missing ConfigIds")]
        public static void FillMissingConfigIds()
        {
            UnityScene activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.name))
            {
                Debug.LogError("[ApplyECAConfigId] No active scene!");
                return;
            }

            List<ECAPointMarker> markers = CollectSceneMarkers(activeScene);
            if (markers.Count == 0)
            {
                Debug.LogWarning($"[ApplyECAConfigId] No ECAPointMarker found in scene '{activeScene.name}'");
                return;
            }

            HashSet<string> duplicateConfigIds = new(StringComparer.Ordinal);
            HashSet<string> usedConfigIds = new(StringComparer.Ordinal);
            List<ECAPointMarker> markersMissingConfigId = new List<ECAPointMarker>();
            List<ECAPointMarker> markersWithDuplicateConfigId = new List<ECAPointMarker>();

            foreach (ECAPointMarker marker in markers)
            {
                if (marker == null)
                {
                    continue;
                }

                string configId = NormalizeConfigId(marker.ConfigId);
                if (string.IsNullOrEmpty(configId))
                {
                    markersMissingConfigId.Add(marker);
                    continue;
                }

                if (!usedConfigIds.Add(configId))
                {
                    duplicateConfigIds.Add(configId);
                    markersWithDuplicateConfigId.Add(marker);
                }
            }

            int filledCount = 0;
            int repairedDuplicateCount = 0;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            foreach (ECAPointMarker marker in markersMissingConfigId)
            {
                string prefix = BuildConfigIdPrefix(activeScene.name, marker.Type);
                string newConfigId = CreateUniqueConfigId(prefix, usedConfigIds);

                Undo.RecordObject(marker, "Fill Missing ECA ConfigIds");
                marker.ConfigId = newConfigId;
                EditorUtility.SetDirty(marker);
                filledCount++;
            }

            foreach (ECAPointMarker marker in markersWithDuplicateConfigId)
            {
                string prefix = BuildConfigIdPrefix(activeScene.name, marker.Type);
                string newConfigId = CreateUniqueConfigId(prefix, usedConfigIds);

                Undo.RecordObject(marker, "Fix Duplicate ECA ConfigIds");
                marker.ConfigId = newConfigId;
                EditorUtility.SetDirty(marker);
                repairedDuplicateCount++;
            }

            if (filledCount > 0 || repairedDuplicateCount > 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Scene: {activeScene.name}");
            messageBuilder.AppendLine($"Markers: {markers.Count}");
            messageBuilder.AppendLine($"Filled missing: {filledCount}");
            messageBuilder.AppendLine($"Repaired duplicates: {repairedDuplicateCount}");

            if (duplicateConfigIds.Count > 0)
            {
                List<string> duplicates = new List<string>(duplicateConfigIds);
                duplicates.Sort(StringComparer.Ordinal);
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("Detected duplicate ConfigIds (kept first, reassigned later duplicates):");
                messageBuilder.AppendLine(string.Join("\n", duplicates));
            }

            string message = messageBuilder.ToString().TrimEnd();
            Debug.Log($"[ApplyECAConfigId] {message.Replace("\n", " | ")}");
            EditorUtility.DisplayDialog("Fill Missing ConfigIds", message, "知道了");
        }

        private static List<ECAPointMarker> CollectSceneMarkers(UnityScene scene)
        {
            List<ECAPointMarker> markers = new List<ECAPointMarker>();
            foreach (ECAPointMarker marker in Resources.FindObjectsOfTypeAll<ECAPointMarker>())
            {
                if (marker == null || EditorUtility.IsPersistent(marker))
                {
                    continue;
                }

                if (marker.gameObject.scene != scene)
                {
                    continue;
                }

                markers.Add(marker);
            }

            markers.Sort((a, b) => string.CompareOrdinal(GetHierarchyPath(a.transform), GetHierarchyPath(b.transform)));
            return markers;
        }

        private static string CreateUniqueConfigId(string prefix, HashSet<string> usedConfigIds)
        {
            for (int index = 1; ; ++index)
            {
                string candidate = $"{prefix}_{index:D4}";
                if (usedConfigIds.Add(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string BuildConfigIdPrefix(string sceneName, int pointType)
        {
            string sanitizedSceneName = SanitizeToken(sceneName);
            string pointTypeToken = GetPointTypeToken(pointType);
            return $"eca_{sanitizedSceneName}_{pointTypeToken}";
        }

        private static string NormalizeConfigId(string configId)
        {
            return string.IsNullOrWhiteSpace(configId) ? string.Empty : configId.Trim();
        }

        private static string GetPointTypeToken(int pointType)
        {
            return pointType switch
            {
                ECAPointType.EvacuationPoint => "evacuation",
                ECAPointType.SpawnPoint => "spawn",
                ECAPointType.Container => "container",
                ECAPointType.MonsterSpawnPoint => "monster_spawn",
                ECAPointType.RangeTrigger => "range_trigger",
                _ => "point"
            };
        }

        private static string SanitizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "scene";
            }

            StringBuilder builder = new StringBuilder(value.Length);
            bool previousUnderscore = false;
            foreach (char c in value)
            {
                char normalized = char.ToLowerInvariant(c);
                if ((normalized >= 'a' && normalized <= 'z') || (normalized >= '0' && normalized <= '9'))
                {
                    builder.Append(normalized);
                    previousUnderscore = false;
                    continue;
                }

                if (previousUnderscore)
                {
                    continue;
                }

                builder.Append('_');
                previousUnderscore = true;
            }

            string result = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "scene" : result;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            Stack<string> names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", names);
        }
    }
}
