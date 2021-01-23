using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
namespace DefenseShields
{
    internal class PbApiWrapper
    {
        private IMyTerminalBlock _block;

        private readonly Func<IMyTerminalBlock, RayD, Vector3D?> _rayIntersectShield;
        private readonly Func<IMyTerminalBlock, LineD, Vector3D?> _lineIntersectShield;
        private readonly Func<IMyTerminalBlock, Vector3D, bool> _pointInShield;
        private readonly Func<IMyTerminalBlock, float> _getShieldPercent;
        private readonly Func<IMyTerminalBlock, int> _getShieldHeat;
        private readonly Func<IMyTerminalBlock, float> _getChargeRate;
        private readonly Func<IMyTerminalBlock, int> _hpToChargeRatio;
        private readonly Func<IMyTerminalBlock, float> _getMaxCharge;
        private readonly Func<IMyTerminalBlock, float> _getCharge;
        private readonly Func<IMyTerminalBlock, float> _getPowerUsed;
        private readonly Func<IMyTerminalBlock, float> _getPowerCap;
        private readonly Func<IMyTerminalBlock, float> _getMaxHpCap;
        private readonly Func<IMyTerminalBlock, bool> _isShieldUp;
        private readonly Func<IMyTerminalBlock, string> _shieldStatus;
        private readonly Func<IMyTerminalBlock, IMyEntity, bool, bool> _entityBypass;
        private readonly Func<IMyTerminalBlock, long, bool, bool> _entityBypassPb;
        // Fields below do not require SetActiveShield to be defined first.
        private readonly Func<IMyCubeGrid, bool> _gridHasShield;
        private readonly Func<IMyCubeGrid, bool> _gridShieldOnline;
        private readonly Func<IMyEntity, bool> _protectedByShield;
        private readonly Func<IMyEntity, IMyTerminalBlock> _getShieldBlock;
        private readonly Func<IMyTerminalBlock, bool> _isShieldBlock;
        private readonly Func<Vector3D, IMyTerminalBlock> _getClosestShield;
        private readonly Func<IMyTerminalBlock, Vector3D, double> _getDistanceToShield;
        private readonly Func<IMyTerminalBlock, Vector3D, Vector3D?> _getClosestShieldPoint;

        public void SetActiveShield(IMyTerminalBlock block) => _block = block; // AutoSet to TapiFrontend(block) if shield exists on grid.

        public PbApiWrapper(IMyTerminalBlock block)
        {
            _block = block;
            var delegates = _block.GetProperty("DefenseSystemsPbAPI")?.As<Dictionary<string, Delegate>>().GetValue(_block);
            if (delegates == null) return;

            _rayIntersectShield = (Func<IMyTerminalBlock, RayD, Vector3D?>)delegates["RayIntersectShield"];
            _lineIntersectShield = (Func<IMyTerminalBlock, LineD, Vector3D?>)delegates["LineIntersectShield"];
            _pointInShield = (Func<IMyTerminalBlock, Vector3D, bool>)delegates["PointInShield"];
            _getShieldPercent = (Func<IMyTerminalBlock, float>)delegates["GetShieldPercent"];
            _getShieldHeat = (Func<IMyTerminalBlock, int>)delegates["GetShieldHeat"];
            _getChargeRate = (Func<IMyTerminalBlock, float>)delegates["GetChargeRate"];
            _hpToChargeRatio = (Func<IMyTerminalBlock, int>)delegates["HpToChargeRatio"];
            _getMaxCharge = (Func<IMyTerminalBlock, float>)delegates["GetMaxCharge"];
            _getCharge = (Func<IMyTerminalBlock, float>)delegates["GetCharge"];
            _getPowerUsed = (Func<IMyTerminalBlock, float>)delegates["GetPowerUsed"];
            _getPowerCap = (Func<IMyTerminalBlock, float>)delegates["GetPowerCap"];
            _getMaxHpCap = (Func<IMyTerminalBlock, float>)delegates["GetMaxHpCap"];
            _isShieldUp = (Func<IMyTerminalBlock, bool>)delegates["IsShieldUp"];
            _shieldStatus = (Func<IMyTerminalBlock, string>)delegates["ShieldStatus"];
            _entityBypass = (Func<IMyTerminalBlock, IMyEntity, bool, bool>)delegates["EntityBypass"];
            _entityBypassPb = (Func<IMyTerminalBlock, long, bool, bool>)delegates["EntityBypassPb"];
            _gridHasShield = (Func<IMyCubeGrid, bool>)delegates["GridHasShield"];
            _gridShieldOnline = (Func<IMyCubeGrid, bool>)delegates["GridShieldOnline"];
            _protectedByShield = (Func<IMyEntity, bool>)delegates["ProtectedByShield"];
            _getShieldBlock = (Func<IMyEntity, IMyTerminalBlock>)delegates["GetShieldBlock"];
            _isShieldBlock = (Func<IMyTerminalBlock, bool>)delegates["IsShieldBlock"];
            _getClosestShield = (Func<Vector3D, IMyTerminalBlock>)delegates["GetClosestShield"];
            _getDistanceToShield = (Func<IMyTerminalBlock, Vector3D, double>)delegates["GetDistanceToShield"];
            _getClosestShieldPoint = (Func<IMyTerminalBlock, Vector3D, Vector3D?>)delegates["GetClosestShieldPoint"];

            if (!IsShieldBlock()) _block = GetShieldBlock(_block.CubeGrid) ?? _block;
        }
        public Vector3D? RayIntersectShield(RayD ray) => _rayIntersectShield?.Invoke(_block, ray) ?? null;
        public Vector3D? LineIntersectShield(LineD line) => _lineIntersectShield?.Invoke(_block, line) ?? null;
        public bool PointInShield(Vector3D pos) => _pointInShield?.Invoke(_block, pos) ?? false;
        public float GetShieldPercent() => _getShieldPercent?.Invoke(_block) ?? -1;
        public int GetShieldHeat() => _getShieldHeat?.Invoke(_block) ?? -1;
        public float GetChargeRate() => _getChargeRate?.Invoke(_block) ?? -1;
        public float HpToChargeRatio() => _hpToChargeRatio?.Invoke(_block) ?? -1;
        public float GetMaxCharge() => _getMaxCharge?.Invoke(_block) ?? -1;
        public float GetCharge() => _getCharge?.Invoke(_block) ?? -1;
        public float GetPowerUsed() => _getPowerUsed?.Invoke(_block) ?? -1;
        public float GetPowerCap() => _getPowerCap?.Invoke(_block) ?? -1;
        public float GetMaxHpCap() => _getMaxHpCap?.Invoke(_block) ?? -1;
        public bool IsShieldUp() => _isShieldUp?.Invoke(_block) ?? false;
        public string ShieldStatus() => _shieldStatus?.Invoke(_block) ?? string.Empty;
        public bool EntityBypass(IMyEntity entity, bool remove = false) => _entityBypass?.Invoke(_block, entity, remove) ?? false;
        public bool EntityBypassPb(long entity, bool remove = false) => _entityBypassPb?.Invoke(_block, entity, remove) ?? false;
        public bool GridHasShield(IMyCubeGrid grid) => _gridHasShield?.Invoke(grid) ?? false;
        public bool GridShieldOnline(IMyCubeGrid grid) => _gridShieldOnline?.Invoke(grid) ?? false;
        public bool ProtectedByShield(IMyEntity entity) => _protectedByShield?.Invoke(entity) ?? false;
        public IMyTerminalBlock GetShieldBlock(IMyEntity entity) => _getShieldBlock?.Invoke(entity) ?? null;
        public bool IsShieldBlock() => _isShieldBlock?.Invoke(_block) ?? false;
        public IMyTerminalBlock GetClosestShield(Vector3D pos) => _getClosestShield?.Invoke(pos) ?? null;
        public double GetDistanceToShield(Vector3D pos) => _getDistanceToShield?.Invoke(_block, pos) ?? -1;
        public Vector3D? GetClosestShieldPoint(Vector3D pos) => _getClosestShieldPoint?.Invoke(_block, pos) ?? null;
    }
}
