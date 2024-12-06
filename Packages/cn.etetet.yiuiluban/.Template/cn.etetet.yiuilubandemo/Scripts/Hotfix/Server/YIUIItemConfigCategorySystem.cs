using ET.YIUITest;

namespace ET
{
    [EntitySystemOf(typeof(YIUIItemConfigCategory))]
    public static partial class YIUIItemConfigCategorySystem
    {
        [EntitySystem]
        private static void LubanConfig(this YIUIItemConfigCategory self)
        {
            //解决为了在hotfix扩展
            //如果你要扩展一个 自定义的结构
            //但是这个结构的字段的实现方法在hotfix
            //将如何自动化的实现 初始化
            //而不是手动的调用
            //ConfigCategory 可用
            self.ItemCategoryExpend = 2; //假设这个赋值方法只有在hotfix有实现
            Log.Info($"YIUIItemConfigCategory LubanConfig扩展完毕");
        }
    }
}
