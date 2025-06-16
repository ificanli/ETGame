
namespace YIUILuban.Editor
{
    public class LubanDefinesModule : LubanModuleBase
    {
        protected override ELubanEditorShowType EditorShowType => ELubanEditorShowType.Defines;

        public override void Initialize()
        {
            base.Initialize();
            GetAllData($"{this.Data.ConfigPath}/Base/Defines");
        }
    }
}