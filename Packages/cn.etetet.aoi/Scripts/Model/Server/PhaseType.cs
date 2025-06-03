using System;

namespace ET.Server
{
    [Flags]
    public enum PhaseType: long
    {
        Normal = 1, // 普通阶段
        Phase1 = 2,
        Phase2 = 4,
    }
}