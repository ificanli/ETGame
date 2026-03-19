namespace ET
{
    /// <summary>
    /// SearchPanel 的打开模式。
    /// </summary>
    public enum SearchPanelOpenMode
    {
        Unknown = 0,
        ContainerSearch = 1,
        BackpackInspect = 2,
        CorpseLoot = 3,
    }

    /// <summary>
    /// 尸体来源子类型。当前先覆盖玩家与怪物，后续保留扩展值。
    /// </summary>
    public enum SearchPanelCorpseSubType
    {
        Unknown = 0,
        Player = 1,
        Monster = 2,
        Boss = 3,
        Npc = 4,
    }

    /// <summary>
    /// 容器打开时的模式解析结果。
    /// </summary>
    public struct SearchPanelModeResolveResult
    {
        public SearchPanelOpenMode OpenMode;
        public SearchPanelCorpseSubType CorpseSubType;
    }
}
