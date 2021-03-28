namespace DefenseShields
{
    using System;
    using System.ComponentModel;
    using ProtoBuf;
    using VRageMath;

    [ProtoContract]
    public class DefenseShieldsEnforcement
    {
        [ProtoMember(1), DefaultValue(-1)] public float HeatScaler = -1f;
        [ProtoMember(2), DefaultValue(-1)] public int BaseScaler = -1;
        [ProtoMember(3), DefaultValue(-1)] public float Unused = -1f;
        [ProtoMember(4), DefaultValue(-1)] public int StationRatio = -1;
        [ProtoMember(5), DefaultValue(-1)] public int LargeShipRatio = -1;
        [ProtoMember(6), DefaultValue(-1)] public int SmallShipRatio = -1;
        [ProtoMember(7), DefaultValue(-1)] public int DisableVoxelSupport = -1;
        [ProtoMember(8), DefaultValue(-1)] public int DisableEntityBarrier = -1;
        [ProtoMember(9), DefaultValue(-1)] public int Debug = -1;
        [ProtoMember(10), DefaultValue(-1)] public int SuperWeapons = 1;
        [ProtoMember(11), DefaultValue(-1)] public int Version = -1;
        [ProtoMember(12)] public ulong SenderId = 0;
        [ProtoMember(13), DefaultValue(-1)] public float CapScaler = -1f;
        [ProtoMember(14), DefaultValue(-1)] public float HpsEfficiency = -1f;
        [ProtoMember(15), DefaultValue(-1)] public float MaintenanceCost = -1f;
        [ProtoMember(16), DefaultValue(-1)] public int DisableBlockDamage = -1;
        [ProtoMember(17), DefaultValue(-1)] public int DisableLineOfSight = -1;
    }

    [ProtoContract]
    public class ControllerStateValues
    {
        [ProtoMember(1), DefaultValue(-1)] public float Charge;
        [ProtoMember(2), DefaultValue(-1)] public double IncreaseO2ByFPercent = 0f;
        [ProtoMember(3), DefaultValue(1f)] public float ModulateEnergy = 1f;
        [ProtoMember(4), DefaultValue(1f)] public float ModulateKinetic = 1f;
        [ProtoMember(5), DefaultValue(-1)] public int EnhancerPowerMulti = 1;
        [ProtoMember(6), DefaultValue(-1)] public int EnhancerProtMulti = 1;
        [ProtoMember(7)] public bool Online = false;
        [ProtoMember(8)] public bool Overload = false;
        [ProtoMember(9)] public bool Remodulate = false;
        [ProtoMember(10)] public bool Lowered = false;
        [ProtoMember(11)] public bool Sleeping = false;
        [ProtoMember(12)] public bool Suspended = false;
        [ProtoMember(13)] public bool Waking = false;
        [ProtoMember(14)] public bool FieldBlocked = false;
        [ProtoMember(15)] public bool InFaction = false;
        [ProtoMember(16)] public bool IsOwner = false;
        [ProtoMember(17)] public bool ControllerGridAccess = true;
        [ProtoMember(18)] public bool NoPower = false;
        [ProtoMember(19)] public bool Enhancer = false;
        [ProtoMember(20), DefaultValue(-1)] public double EllipsoidAdjust = Math.Sqrt(2);
        [ProtoMember(21)] public Vector3D GridHalfExtents;
        [ProtoMember(22), DefaultValue(-1)] public int Mode = -1;
        [ProtoMember(23)] public bool EmitterLos;
        [ProtoMember(24)] public float ShieldFudge;
        [ProtoMember(25)] public bool Message;
        [ProtoMember(26)] public int Heat;
        [ProtoMember(27), DefaultValue(-1)] public float ShieldPercent;
        [ProtoMember(28)] public bool EmpOverLoad = false;
        [ProtoMember(29)] public bool EmpProtection = false;
        [ProtoMember(30)] public float GridIntegrity;
        [ProtoMember(31)] public bool ReInforce = false;
        [ProtoMember(32)] public long ActiveEmitterId;
        [ProtoMember(33)] public uint MId;
        [ProtoMember(34)] public int MessageCount;
    }

    [ProtoContract]
    public class ControllerSettingsValues
    {
        [ProtoMember(1), DefaultValue(true)] public bool RefreshAnimation = true;
        [ProtoMember(2), DefaultValue(-1)] public float Width = 30f;
        [ProtoMember(3), DefaultValue(-1)] public float Height = 30f;
        [ProtoMember(4), DefaultValue(-1)] public float Depth = 30f;
        [ProtoMember(5)] public bool NoWarningSounds = false;
        [ProtoMember(6)] public bool ActiveInvisible = false;
        [ProtoMember(7), DefaultValue(-1)] public float Rate = 50f;
        [ProtoMember(8)] public bool SideRedirect = false;
        [ProtoMember(9)] public bool SphereFit = false;
        [ProtoMember(10)] public bool FortifyShield = false;
        [ProtoMember(11), DefaultValue(true)] public bool SendToHud = true;
        [ProtoMember(12), DefaultValue(true)] public bool UseBatteries = true;
        [ProtoMember(13), DefaultValue(true)] public bool DimShieldHits = true;
        [ProtoMember(14), DefaultValue(true)] public bool RaiseShield = true;
        [ProtoMember(15)] public long ShieldShell = 2;
        [ProtoMember(16), DefaultValue(false)] public bool HitWaveAnimation = false;
        [ProtoMember(17)] public long Visible = 2;
        [ProtoMember(18)] public Vector3I ShieldOffset = Vector3I.Zero;
        [ProtoMember(19)] public uint MId;
        [ProtoMember(20)] public long PowerScale = 0;
        [ProtoMember(21), DefaultValue(-1)] public int PowerWatts = 999;
        [ProtoMember(22), DefaultValue(2)] public int Fit = 15;
        [ProtoMember(23)] public Vector3I ShieldRedirects = Vector3I.Zero;
        [ProtoMember(24)] public bool ShowRedirect;
    }

    [ProtoContract]
    public class EmitterStateValues
    {
        [ProtoMember(1)] public bool UnusedWasOnline;
        [ProtoMember(2), DefaultValue(true)] public bool Los = true;
        [ProtoMember(3)] public bool Link;
        [ProtoMember(4)] public bool Suspend;
        [ProtoMember(5)] public bool Backup;
        [ProtoMember(6)] public bool Compatible;
        [ProtoMember(7), DefaultValue(-1)] public int Mode;
        [ProtoMember(8), DefaultValue(-1)] public double BoundingRange;
        [ProtoMember(9)] public bool UnusedWasCompact;
        [ProtoMember(10)] public long ActiveEmitterId;
        [ProtoMember(11)] public uint MId;
    }

    [ProtoContract]
    public class ModulatorStateValues
    {
        [ProtoMember(1)] public bool Online;
        [ProtoMember(2), DefaultValue(1f)] public float ModulateEnergy = 1f;
        [ProtoMember(3), DefaultValue(1f)] public float ModulateKinetic = 1f;
        [ProtoMember(4), DefaultValue(100)] public int ModulateDamage = 100;
        [ProtoMember(5)] public bool Backup;
        [ProtoMember(6)] public bool Link;
        [ProtoMember(7)] public uint MId;
    }

    [ProtoContract]
    public class ModulatorSettingsValues
    {
        [ProtoMember(1)] public bool EmpEnabled = false;
        [ProtoMember(2), DefaultValue(true)] public bool ModulateVoxels = true;
        [ProtoMember(3)] public bool ModulateGrids = false;
        [ProtoMember(4), DefaultValue(-1)] public int ModulateDamage = 100;
        [ProtoMember(5)] public bool ReInforceEnabled = false;
        [ProtoMember(6)] public uint MId;
        [ProtoMember(7)] public bool AllowAllies = false;
    }

    [ProtoContract]
    public class PlanetShieldStateValues
    {
        [ProtoMember(1)] public bool Online;
        [ProtoMember(2)] public bool Backup;
        [ProtoMember(3)] public uint MId;
    }

    [ProtoContract]
    public class PlanetShieldSettingsValues
    {
        [ProtoMember(1)] public bool ShieldActive = false;
        [ProtoMember(2)] public long ShieldShell = 0;
        [ProtoMember(3)] public uint MId;
    }

    [ProtoContract]
    public class O2GeneratorStateValues
    {
        [ProtoMember(1)] public bool Pressurized = false;
        [ProtoMember(2), DefaultValue(-1)] public float DefaultO2 = 0;
        [ProtoMember(3), DefaultValue(-1)] public double ShieldVolume = 0;
        [ProtoMember(4), DefaultValue(-1)] public double VolFilled = 0;
        [ProtoMember(5), DefaultValue(-1)] public double O2Level = 0;
        [ProtoMember(6)] public bool Backup = false;
        [ProtoMember(7)] public uint MId;
    }

    [ProtoContract]
    public class O2GeneratorSettingsValues
    {
        [ProtoMember(1)] public bool FixRoomPressure;
        [ProtoMember(2), DefaultValue(true)] public bool Unused2 = true;
        [ProtoMember(3)] public bool Unused3 = false;
        [ProtoMember(4), DefaultValue(-1)] public int Unused4 = 100;
        [ProtoMember(5)] public uint MId;
    }

    [ProtoContract]
    public class EnhancerStateValues
    {
        [ProtoMember(1)] public bool Online;
        [ProtoMember(2)] public bool Backup;
        [ProtoMember(3)] public uint MId;
    }

    [ProtoContract]
    public class EnhancerSettingsValues
    {
        [ProtoMember(1)] public bool Unused;
        [ProtoMember(2), DefaultValue(true)] public bool ModulateVoxels = true;
        [ProtoMember(3)] public bool ModulateGrids = false;
        [ProtoMember(4), DefaultValue(-1)] public int ModulateDamage = 100;
        [ProtoMember(5)] public uint MId;
    }

    [ProtoContract]
    public class ShieldHitValues
    {
        [ProtoMember(1)] public long AttackerId;
        [ProtoMember(2)] public float Amount;
        [ProtoMember(3)] public string DamageType;
        [ProtoMember(4)] public Vector3D HitPos;
    }
}
