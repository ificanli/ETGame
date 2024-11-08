namespace ET
{
    public static class TextHelper
    {
        // 不允许直接传入int，应该传入TextConst的常量
        public static string GetText(int key)
        {
            return TextConfigCategory.Instance.Get(key).CN;
        }

        public static void OutputText(int key, object args1)
        {
            Log.Debug(string.Format(GetText(key), args1));
        }
        
        public static void OutputText(int key, object args1, object args2)
        {
            Log.Debug(string.Format(GetText(key), args1, args2));
        }
        
        public static void OutputText(int key, object args1, object args2, object args3)
        {
            Log.Debug(string.Format(GetText(key), args1, args2, args3));
        }
        
        public static void OutputText(int key, params object[] args)
        {
            Log.Debug(string.Format(GetText(key), args));
        }
    }
}