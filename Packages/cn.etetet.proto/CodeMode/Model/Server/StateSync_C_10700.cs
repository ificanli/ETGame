using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.RouterSync)]
    public partial class RouterSync : MessageObject
    {
        public static RouterSync Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RouterSync>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public uint ConnectId { get; set; }
        [MemoryPackOrder(1)]
        public string Address { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConnectId = default;
            this.Address = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_TestRequest)]
    [ResponseType(nameof(M2C_TestResponse))]
    public partial class C2M_TestRequest : MessageObject, ILocationRequest
    {
        public static C2M_TestRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string request { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.request = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_TestResponse)]
    public partial class M2C_TestResponse : MessageObject, IResponse
    {
        public static M2C_TestResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public string response { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.response = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_Reload)]
    [ResponseType(nameof(M2C_Reload))]
    public partial class C2M_Reload : MessageObject, ISessionRequest
    {
        public static C2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_Reload>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public string Password { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.Password = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_Reload)]
    public partial class M2C_Reload : MessageObject, ISessionResponse
    {
        public static M2C_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Reload>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_TestHotfixMessage)]
    public partial class G2C_TestHotfixMessage : MessageObject, ISessionMessage
    {
        public static G2C_TestHotfixMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_TestHotfixMessage>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string Info { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Info = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_Benchmark)]
    [ResponseType(nameof(G2C_Benchmark))]
    public partial class C2G_Benchmark : MessageObject, ISessionRequest
    {
        public static C2G_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Benchmark>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_Benchmark)]
    public partial class G2C_Benchmark : MessageObject, ISessionResponse
    {
        public static G2C_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Benchmark>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    // 摇杆移动消息
    [MemoryPackable]
    [Message(Opcode.C2M_JoystickInput)]
    public partial class C2M_JoystickInput : MessageObject, ILocationMessage
    {
        public static C2M_JoystickInput Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_JoystickInput>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public float DirX { get; set; }
        [MemoryPackOrder(2)]
        public float DirZ { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.DirX = default;
            this.DirZ = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_JoystickMove)]
    public partial class M2C_JoystickMove : MessageObject, IMessage
    {
        public static M2C_JoystickMove Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_JoystickMove>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public float PosX { get; set; }
        [MemoryPackOrder(2)]
        public float PosY { get; set; }
        [MemoryPackOrder(3)]
        public float PosZ { get; set; }
        [MemoryPackOrder(4)]
        public float RotX { get; set; }
        [MemoryPackOrder(5)]
        public float RotY { get; set; }
        [MemoryPackOrder(6)]
        public float RotZ { get; set; }
        [MemoryPackOrder(7)]
        public float RotW { get; set; }
        [MemoryPackOrder(8)]
        public float DirX { get; set; }
        [MemoryPackOrder(9)]
        public float DirZ { get; set; }
        [MemoryPackOrder(10)]
        public float Speed { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.PosX = default;
            this.PosY = default;
            this.PosZ = default;
            this.RotX = default;
            this.RotY = default;
            this.RotZ = default;
            this.RotW = default;
            this.DirX = default;
            this.DirZ = default;
            this.Speed = default;

            ObjectPool.Recycle(this);
        }
    }

    // 武器切换消息
    [MemoryPackable]
    [Message(Opcode.C2M_SwitchWeapon)]
    public partial class C2M_SwitchWeapon : MessageObject, ILocationMessage
    {
        public static C2M_SwitchWeapon Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_SwitchWeapon>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        /// <summary>
        /// 切换到的槽位（1 或 2）
        /// </summary>
        [MemoryPackOrder(1)]
        public int SlotIndex { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SlotIndex = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_SwitchWeapon)]
    public partial class M2C_SwitchWeapon : MessageObject, IMessage
    {
        public static M2C_SwitchWeapon Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_SwitchWeapon>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        /// <summary>
        /// 切换到的槽位
        /// </summary>
        [MemoryPackOrder(1)]
        public int SlotIndex { get; set; }
        /// <summary>
        /// 武器配置ID
        /// </summary>
        [MemoryPackOrder(2)]
        public int WeaponId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.SlotIndex = default;
            this.WeaponId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_WeaponAmmoState)]
    public partial class M2C_WeaponAmmoState : MessageObject, IMessage
    {
        public static M2C_WeaponAmmoState Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_WeaponAmmoState>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public int Slot1Ammo { get; set; }
        [MemoryPackOrder(2)]
        public int Slot2Ammo { get; set; }
        [MemoryPackOrder(3)]
        public bool Slot1Reloading { get; set; }
        [MemoryPackOrder(4)]
        public bool Slot2Reloading { get; set; }
        [MemoryPackOrder(5)]
        public int Slot1MagazineSize { get; set; }
        [MemoryPackOrder(6)]
        public int Slot2MagazineSize { get; set; }
        [MemoryPackOrder(7)]
        public float Slot1AttackRange { get; set; }
        [MemoryPackOrder(8)]
        public float Slot2AttackRange { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.Slot1Ammo = default;
            this.Slot2Ammo = default;
            this.Slot1Reloading = default;
            this.Slot2Reloading = default;
            this.Slot1MagazineSize = default;
            this.Slot2MagazineSize = default;
            this.Slot1AttackRange = default;
            this.Slot2AttackRange = default;

            ObjectPool.Recycle(this);
        }
    }

    // 武器开火表现消息（服务端权威触发，客户端仅做表现）
    [MemoryPackable]
    [Message(Opcode.M2C_WeaponFire)]
    public partial class M2C_WeaponFire : MessageObject, IMessage
    {
        public static M2C_WeaponFire Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_WeaponFire>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long CasterUnitId { get; set; }
        [MemoryPackOrder(1)]
        public long TargetUnitId { get; set; }
        [MemoryPackOrder(2)]
        public int WeaponId { get; set; }
        [MemoryPackOrder(3)]
        public int SlotIndex { get; set; }
        [MemoryPackOrder(4)]
        public int BulletCount { get; set; }
        [MemoryPackOrder(5)]
        public int FireLockTypeId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.CasterUnitId = default;
            this.TargetUnitId = default;
            this.WeaponId = default;
            this.SlotIndex = default;
            this.BulletCount = default;
            this.FireLockTypeId = default;

            ObjectPool.Recycle(this);
        }
    }

    // 武器命中特效消息（服务端权威命中后广播）
    [MemoryPackable]
    [Message(Opcode.M2C_WeaponHit)]
    public partial class M2C_WeaponHit : MessageObject, IMessage
    {
        public static M2C_WeaponHit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_WeaponHit>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long CasterUnitId { get; set; }
        [MemoryPackOrder(1)]
        public long TargetUnitId { get; set; }
        [MemoryPackOrder(2)]
        public int WeaponId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.CasterUnitId = default;
            this.TargetUnitId = default;
            this.WeaponId = default;

            ObjectPool.Recycle(this);
        }
    }

    // ========================= Rogue =========================
    [MemoryPackable]
    [Message(Opcode.RogueNumericDelta)]
    public partial class RogueNumericDelta : MessageObject
    {
        public static RogueNumericDelta Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RogueNumericDelta>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int NumericType { get; set; }
        [MemoryPackOrder(1)]
        public long Value { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.NumericType = default;
            this.Value = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.RogueOptionData)]
    public partial class RogueOptionData : MessageObject
    {
        public static RogueOptionData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RogueOptionData>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int OptionId { get; set; }
        [MemoryPackOrder(1)]
        public int BuffConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int NameTextId { get; set; }
        [MemoryPackOrder(3)]
        public int DescTextId { get; set; }
        [MemoryPackOrder(4)]
        public string Icon { get; set; }
        [MemoryPackOrder(5)]
        public int RerollCount { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.OptionId = default;
            this.BuffConfigId = default;
            this.NameTextId = default;
            this.DescTextId = default;
            this.Icon = default;
            this.RerollCount = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_RogueExpSync)]
    public partial class M2C_RogueExpSync : MessageObject, IMessage
    {
        public static M2C_RogueExpSync Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RogueExpSync>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Level { get; set; }
        [MemoryPackOrder(1)]
        public int CurrentExp { get; set; }
        [MemoryPackOrder(2)]
        public int NeedExp { get; set; }
        [MemoryPackOrder(3)]
        public int CurrentGold { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Level = default;
            this.CurrentExp = default;
            this.NeedExp = default;
            this.CurrentGold = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_RogueLevelUp)]
    public partial class M2C_RogueLevelUp : MessageObject, IMessage
    {
        public static M2C_RogueLevelUp Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RogueLevelUp>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int OldLevel { get; set; }
        [MemoryPackOrder(1)]
        public int NewLevel { get; set; }
        [MemoryPackOrder(2)]
        public List<RogueNumericDelta> GainedNumerics { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.OldLevel = default;
            this.NewLevel = default;
            this.GainedNumerics.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_RogueChoicePopup)]
    public partial class M2C_RogueChoicePopup : MessageObject, IMessage
    {
        public static M2C_RogueChoicePopup Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RogueChoicePopup>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long ChoiceSerial { get; set; }
        [MemoryPackOrder(1)]
        public List<RogueOptionData> Options { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ChoiceSerial = default;
            this.Options.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_RogueChooseOption)]
    [ResponseType(nameof(M2C_RogueChoiceResult))]
    public partial class C2M_RogueChooseOption : MessageObject, ILocationRequest
    {
        public static C2M_RogueChooseOption Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_RogueChooseOption>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long ChoiceSerial { get; set; }
        [MemoryPackOrder(2)]
        public int OptionId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ChoiceSerial = default;
            this.OptionId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_RogueChoiceResult)]
    public partial class M2C_RogueChoiceResult : MessageObject, ILocationResponse
    {
        public static M2C_RogueChoiceResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RogueChoiceResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long ChoiceSerial { get; set; }
        [MemoryPackOrder(4)]
        public int OptionId { get; set; }
        [MemoryPackOrder(5)]
        public int BuffConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ChoiceSerial = default;
            this.OptionId = default;
            this.BuffConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_RogueRerollOption)]
    [ResponseType(nameof(M2C_RogueRerollOptionResult))]
    public partial class C2M_RogueRerollOption : MessageObject, ILocationRequest
    {
        public static C2M_RogueRerollOption Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_RogueRerollOption>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long ChoiceSerial { get; set; }
        [MemoryPackOrder(2)]
        public int OptionIndex { get; set; }
        [MemoryPackOrder(3)]
        public int CurrentOptionId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ChoiceSerial = default;
            this.OptionIndex = default;
            this.CurrentOptionId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_RogueRerollOptionResult)]
    public partial class M2C_RogueRerollOptionResult : MessageObject, ILocationResponse
    {
        public static M2C_RogueRerollOptionResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RogueRerollOptionResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long ChoiceSerial { get; set; }
        [MemoryPackOrder(4)]
        public int OptionIndex { get; set; }
        [MemoryPackOrder(5)]
        public int OldOptionId { get; set; }
        [MemoryPackOrder(6)]
        public RogueOptionData Option { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ChoiceSerial = default;
            this.OptionIndex = default;
            this.OldOptionId = default;
            this.Option = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort RouterSync = 10701;
        public const ushort C2M_TestRequest = 10702;
        public const ushort M2C_TestResponse = 10703;
        public const ushort C2M_Reload = 10704;
        public const ushort M2C_Reload = 10705;
        public const ushort G2C_TestHotfixMessage = 10706;
        public const ushort C2G_Benchmark = 10707;
        public const ushort G2C_Benchmark = 10708;
        public const ushort C2M_JoystickInput = 10709;
        public const ushort M2C_JoystickMove = 10710;
        public const ushort C2M_SwitchWeapon = 10711;
        public const ushort M2C_SwitchWeapon = 10712;
        public const ushort M2C_WeaponAmmoState = 10713;
        public const ushort M2C_WeaponFire = 10714;
        public const ushort M2C_WeaponHit = 10715;
        public const ushort RogueNumericDelta = 10716;
        public const ushort RogueOptionData = 10717;
        public const ushort M2C_RogueExpSync = 10718;
        public const ushort M2C_RogueLevelUp = 10719;
        public const ushort M2C_RogueChoicePopup = 10720;
        public const ushort C2M_RogueChooseOption = 10721;
        public const ushort M2C_RogueChoiceResult = 10722;
        public const ushort C2M_RogueRerollOption = 10723;
        public const ushort M2C_RogueRerollOptionResult = 10724;
    }
}