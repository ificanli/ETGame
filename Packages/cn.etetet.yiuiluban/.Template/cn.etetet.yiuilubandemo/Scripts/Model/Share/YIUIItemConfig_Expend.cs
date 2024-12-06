namespace ET.YIUITest
{
    //Config自定义扩展
    public partial class YIUIItemConfig
    {
        //自定义扩展需要的字段
        public int ItemConfigExpend;

        //对应初始化完毕后的调用
        public override void EndInit()
        {
            //可自定义进行操作
            //如自定义赋值等等
            //切记这个时候 其他表并未Init完毕 不可在这里进行读取其他表的操作
            ItemConfigExpend = 1;
            Log.Info($"YIUIItemConfig EndInit扩展完毕");
        }

        //所有表都初始化完毕 自身的ref完毕后调用
        partial void EndRef()
        {
            //切记这个时候 其他表并未Ref完毕 不可在这里进行读取其他表跟Ref相关的操作
            Log.Info($"YIUIItemConfig EndRef扩展完毕");
        }
    }
}
