// 这是一个示例文件，展示 CoroutineLockAnalyzer 如何捕获违规使用
// CoroutineLockAnalyzer 规则：
// 1. ET0034: 禁止使用 using 语句或 using 声明获取 CoroutineLock
// 2. ET0035: 禁止手动调用 CoroutineLock.Dispose()
//            应该让 CoroutineLock 自动管理其生命周期，框架层通过 EntityRef 间接管理

using ET;

namespace TestCoroutineLockAnalyzer
{
    public class ExampleClass
    {
        // ❌ 示例 1: 禁止使用 using 语句
        // 触发诊断：ET0034
        public async ETTask BadExample1()
        {
            // 不允许使用 using 语句获取 CoroutineLock
            using (var lockEntity = await SomeMethodReturningLock())
            {
                // 这里会触发 ET0034 诊断错误
                // 原因：CoroutineLock 可能在 using 块结束前已经超时释放，
                //       导致 using 块末尾调用 Dispose 时发生重复释放
            }
        }

        // ❌ 示例 2: 禁止使用 using 声明（C# 8.0+）
        // 触发诊断：ET0034
        public async ETTask BadExample2()
        {
            // 不允许使用 using 声明语法
            using CoroutineLock coroutineLock = await SomeMethodReturningLock();

            // 这里会触发 ET0034 诊断错误
            // 原因同上：CoroutineLock 可能在作用域结束前已经超时释放
        }

        // ❌ 示例 3: 禁止手动调用 Dispose
        // 触发诊断：ET0035
        public async ETTask BadExample3()
        {
            var lockEntity = await SomeMethodReturningLock();

            try
            {
                // 使用锁...
            }
            finally
            {
                // 不允许手动调用 Dispose
                lockEntity.Dispose();  // 这里会触发 ET0035 诊断错误
                // 原因：CoroutineLock 可能已经在超时时自动释放，
                //       手动调用 Dispose 会导致重复释放异常
            }
        }

        // ✅ 示例 4: 正确的使用方式
        public async ETTask GoodExample()
        {
            var lockEntity = await SomeMethodReturningLock();

            try
            {
                // 使用 CoroutineLock
                // 不要在 finally 中手动释放
            }
            finally
            {
                // CoroutineLock 会自动管理其生命周期
                // 框架会在超时或正常释放时自动调用 Dispose
            }
        }

        // 假设返回的是 CoroutineLock
        private async ETTask<CoroutineLock> SomeMethodReturningLock()
        {
            // 返回逻辑...
            return null;
        }
    }

    // ✅ 框架内部代码：使用 EntityRef 间接管理
    // 这段代码在 cn.etetet.core/Scripts/Core/CoroutineLockSystem.cs 中
    // 框架通过 EntityRef 来间接管理 CoroutineLock 的生命周期
    // 示例（伪代码）：
    /*
    [EntitySystemOf(typeof(CoroutineLock))]
    public static partial class CoroutineLockSystem
    {
        [EntitySystem]
        private static void Destroy(this CoroutineLock self)
        {
            // ✅ 框架通过 EntityRef 间接调用 Dispose
            // 这样避免了直接调用 Dispose 的问题
            EntityRef<CoroutineLock> selfRef = self;
            // 通过 EntityRef 来管理生命周期，而不是直接调用 Dispose
            // selfRef.Dispose();  // 不直接调用，而是通过框架层间接管理
        }
    }
    */
}
