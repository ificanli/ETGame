namespace ET
{
    public static class TextHelper
    {
        // 不允许直接传入int，应该传入TextConst的常量
        public static string GetText(int key)
        {
            return TextConfigCategory.Instance.Get(key).CN;
        }
    }
}