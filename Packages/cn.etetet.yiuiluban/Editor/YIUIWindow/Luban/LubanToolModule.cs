using Sirenix.OdinInspector;
using YIUIFramework.Editor;

namespace YIUILuban.Editor
{
    public class LubanToolModule : BaseYIUIToolModule
    {
        [ShowInInspector]
        [HideLabel]
        [HideReferenceObjectPicker]
        public LubanToolRoot ToolRoot;

        private void RefreshAllLuban()
        {
            ToolRoot = new LubanToolRoot();
            ToolRoot.RefreshAllLuban(this.AutoTool, this.Tree);
        }

        public override void Initialize()
        {
            this.RefreshAllLuban();
        }

        public override void OnDestroy()
        {
        }
    }
}