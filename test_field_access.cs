using Unity.Mathematics;

namespace ET.Server
{
    public class TestFieldAccess
    {
        public void TestMethod()
        {
            // 这应该触发字段访问错误
            var change = new ChangePosition();
            float3 oldPos = change.OldPos;  // 这里应该报错
        }
    }
}