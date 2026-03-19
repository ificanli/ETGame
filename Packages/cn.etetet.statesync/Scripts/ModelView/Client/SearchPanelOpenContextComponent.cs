using YIUIFramework;
using ET;

namespace ET.Client
{
    /// <summary>
    /// SearchPanel 打开前的临时上下文，由调用方写入，面板打开时消费。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class SearchPanelOpenContextComponent : Entity, IAwake
    {
        public SearchPanelOpenMode OpenMode;
        public SearchPanelCorpseSubType CorpseSubType;
        public string CurrentPointId;
        public string TitleText;
        public string SubTitleText;
    }
}
