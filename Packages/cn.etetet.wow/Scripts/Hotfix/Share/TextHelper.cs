namespace ET
{
    public static class TextHelper
    {
        // 不允许直接传入int，应该传入TextConst的常量
        private static string GetText(int key)
        {
            return TextConfigCategory.Instance.Get(key).CN;
        }

        public static void OutputText(int key)
        {
            Log.Error(GetText(key));
        }

        public static void OutputText(int key, object args1)
        {
            Log.Error(string.Format(GetText(key), args1));
        }
        
        public static void OutputText(int key, object args1, object args2)
        {
            Log.Error(string.Format(GetText(key), args1, args2));
        }
        
        public static void OutputText(int key, object args1, object args2, object args3)
        {
            Log.Error(string.Format(GetText(key), args1, args2, args3));
        }
        
        public static void OutputText(int key, params object[] args)
        {
            Log.Error(string.Format(GetText(key), args));
        }
        
        public static void OutputText(int key, params string[] args)
        {
            Log.Error(string.Format(GetText(key), args));
        }
    }
}