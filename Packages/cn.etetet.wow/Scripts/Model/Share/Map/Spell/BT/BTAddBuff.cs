namespace ET
{
    public class BTAddBuff: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public BuffConfig BuffConfig;
    }
}