using System.Collections.Generic;

namespace ET.Server
{
    public static class HomeMuseumHelper
    {
        public static HomeMuseumDisplayData GetDisplay(PlayerHomeComponent homeComp, long displayId)
        {
            if (homeComp?.MuseumDisplays == null || displayId <= 0)
            {
                return null;
            }

            for (int i = 0; i < homeComp.MuseumDisplays.Count; ++i)
            {
                HomeMuseumDisplayData display = homeComp.MuseumDisplays[i];
                if (display != null && display.DisplayId == displayId)
                {
                    return display;
                }
            }

            return null;
        }

        public static int CountDisplays(PlayerHomeComponent homeComp, long buildingId)
        {
            int count = 0;
            if (homeComp?.MuseumDisplays == null)
            {
                return count;
            }

            for (int i = 0; i < homeComp.MuseumDisplays.Count; ++i)
            {
                HomeMuseumDisplayData display = homeComp.MuseumDisplays[i];
                if (display == null)
                {
                    continue;
                }

                if (buildingId > 0 && display.BuildingId != buildingId)
                {
                    continue;
                }

                ++count;
            }

            return count;
        }

        public static int GetMuseumCapacity(HomeBuilding building)
        {
            if (building == null)
            {
                return 0;
            }

            HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level);
            return levelConfig?.CapacityValue1 ?? 0;
        }

        public static int PlaceDisplay(
            PlayerHomeComponent homeComp,
            long buildingId,
            int itemConfigId,
            out HomeMuseumDisplayData display,
            out string message)
        {
            display = null;
            message = string.Empty;
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            HomeBuilding building = homeComp.GetChild<HomeBuilding>(buildingId);
            if (!IsMuseumBuilding(building))
            {
                message = "目标建筑不是大红收藏馆。";
                return ErrorCode.ERR_HomeBuildingNotFound;
            }

            int capacity = GetMuseumCapacity(building);
            if (capacity <= 0)
            {
                message = "当前收藏馆没有可用展示位。";
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            int slotIndex = GetFirstEmptySlotIndex(homeComp, buildingId, capacity);
            if (slotIndex <= 0)
            {
                message = "当前收藏馆展示位已满，请先取下部分展示单位。";
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            display = new HomeMuseumDisplayData
            {
                DisplayId = IdGenerater.Instance.GenerateId(),
                BuildingId = buildingId,
                SlotIndex = slotIndex,
                ItemConfigId = itemConfigId,
            };
            homeComp.MuseumDisplays ??= new List<HomeMuseumDisplayData>();
            homeComp.MuseumDisplays.Add(display);
            return ErrorCode.ERR_Success;
        }

        public static int TakeDownDisplay(
            PlayerHomeComponent homeComp,
            long displayId,
            out HomeMuseumDisplayData display,
            out string message)
        {
            display = null;
            message = string.Empty;
            if (homeComp?.MuseumDisplays == null || displayId <= 0)
            {
                return ErrorCode.ERR_HomeBuildingNotFound;
            }

            for (int i = 0; i < homeComp.MuseumDisplays.Count; ++i)
            {
                HomeMuseumDisplayData current = homeComp.MuseumDisplays[i];
                if (current == null || current.DisplayId != displayId)
                {
                    continue;
                }

                display = current;
                homeComp.MuseumDisplays.RemoveAt(i);
                return ErrorCode.ERR_Success;
            }

            message = "没有找到要取下的展示单位。";
            return ErrorCode.ERR_HomeBuildingNotFound;
        }

        private static bool IsMuseumBuilding(HomeBuilding building)
        {
            HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building?.ConfigId ?? 0);
            return config != null && config.BuildingType == HomeBuildingType.Museum;
        }

        private static int GetFirstEmptySlotIndex(PlayerHomeComponent homeComp, long buildingId, int capacity)
        {
            bool[] used = new bool[capacity + 1];
            if (homeComp?.MuseumDisplays != null)
            {
                for (int i = 0; i < homeComp.MuseumDisplays.Count; ++i)
                {
                    HomeMuseumDisplayData display = homeComp.MuseumDisplays[i];
                    if (display == null || display.BuildingId != buildingId)
                    {
                        continue;
                    }

                    if (display.SlotIndex > 0 && display.SlotIndex <= capacity)
                    {
                        used[display.SlotIndex] = true;
                    }
                }
            }

            for (int i = 1; i <= capacity; ++i)
            {
                if (!used[i])
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
