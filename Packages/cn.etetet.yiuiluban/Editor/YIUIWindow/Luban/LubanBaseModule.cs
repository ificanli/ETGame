
namespace YIUILuban.Editor
{
    public class LubanBaseModule : LubanModuleBase
    {
        protected override ELubanEditorShowType EditorShowType => ELubanEditorShowType.Base;

        public override void Initialize()
        {
            base.Initialize();
            GetAllData($"{this.Data.ConfigPath}/Base");
        }
    }
}