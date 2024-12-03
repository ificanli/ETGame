using System;

namespace ET
{
    [Flags]
    public enum UnitType
    {
        Player = 1,
        Monster = 1 << 1,
        NPC = 1 << 2,
        FakeBullet = 1 << 3
    }
}