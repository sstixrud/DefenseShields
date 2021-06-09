using VRageMath;
using System;
using System.Diagnostics.Eventing.Reader;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Utils;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        private bool PowerOnline()
        {
            UpdateGridPower();
            if (!_shieldPowered) return false;

            CalculatePowerCharge();

            if (!WarmedUp) return true;

            if (_isServer && _shieldConsumptionRate.Equals(0f) && DsState.State.Charge.Equals(0.01f))
                return false;

            _power = _shieldMaxChargeRate > 0 ? _shieldConsumptionRate + _shieldMaintaintPower : 0f;

            if (_power < ShieldCurrentPower && (_power - _shieldMaxChargeRate) >= 0.0001f) {
                
                _sink.Update();
            }
            else if (_count == 28 && (ShieldCurrentPower <= 0 || Math.Abs(_power - ShieldCurrentPower) >= 0.0001f)) {
               
                _sink.Update();
            }

            if (Absorb > 0) {

                _damageReadOut += Absorb;
                KineticAvg = KineticAverage.Add((int) KineticDamage);
                EnergyAvg = EnergyAverage.Add((int)EnergyDamage);

                _damageTypeBalance = KineticAvg - EnergyAvg;
                _lastDamageTick = _tick;

                EffectsCleanTick = _tick;
                DsState.State.Charge -= Absorb * ConvToWatts;
            }
            else if (Absorb < 0) 
                DsState.State.Charge += (Absorb * - 1) * ConvToWatts;

            if (_isServer && DsState.State.Charge < 0) {

                DsState.State.Charge = 0;
                if (!_empOverLoad) _overLoadLoop = 0;
                else _empOverLoadLoop = 0;
            }

            if (_tick - _lastDamageTick > 600 && (EnergyAvg > 0 || KineticAvg > 0))
                ClearDamageTypeInfo();
            
            Absorb = 0f;
            KineticDamage = 0;
            EnergyDamage = 0;

            return DsState.State.Charge > 0;
        }

        private void ClearDamageTypeInfo()
        {
            KineticAverage.Clear();
            EnergyAverage.Clear();
            KineticDamage = 0;
            EnergyDamage = 0;
            KineticAvg = 0;
            EnergyAvg = 0;
            _damageTypeBalance = 0;
        }

        private void CalculatePowerCharge()
        {
            var capScaler = Session.Enforced.CapScaler;
            var hpsEfficiency = Session.Enforced.HpsEfficiency;
            var baseScaler = Session.Enforced.BaseScaler;
            var maintenanceCost = Session.Enforced.MaintenanceCost;
            var fortify = DsSet.Settings.FortifyShield && DsState.State.Enhancer;
            var shieldTypeRatio = _shieldTypeRatio / 100f;
            var shieldMaintainPercent = maintenanceCost / 100;

            if (ShieldMode == ShieldType.Station && DsState.State.Enhancer)
                hpsEfficiency *= 3.5f;
            else if (fortify)
                hpsEfficiency *= 2f;

            var bufferMaxScaler = (baseScaler * shieldTypeRatio) / _sizeScaler;

            ShieldMaxHpBase = ShieldMaxPower * bufferMaxScaler;
            var bonus = 0f;

            if (DsState.State.CapModifier < 1) {
                var diff = 1 - DsState.State.CapModifier;
                if (ShieldMode == ShieldType.Station) {
                    bonus = 1 - (diff / 2) / 2;
                }
                else if (fortify)
                    bonus = (diff / 2);
            }

            var maxHpScaler = DsState.State.CapModifier + bonus;
            shieldMaintainPercent = shieldMaintainPercent * DsState.State.EnhancerPowerMulti * (DsState.State.ShieldPercent * ConvToDec);
            
            if (DsState.State.Lowered) 
                shieldMaintainPercent *= 0.25f;

            _shieldMaintaintPower = ShieldMaxPower * maxHpScaler * shieldMaintainPercent;

            ShieldMaxCharge = ShieldMaxHpBase * maxHpScaler;
            var powerForShield = PowerNeeded(hpsEfficiency);

            if (!WarmedUp) return;

            var overCharged = DsState.State.Charge > ShieldMaxCharge;
            if (overCharged && ++_overChargeCount >= 120) {
                DsState.State.Charge = ShieldMaxCharge;
                _overChargeCount = 0;
            }
            else if (!overCharged)
                _overChargeCount = 0;

            if (_isServer) {

                var powerLost = powerForShield <= 0 || _powerNeeded > ShieldMaxPower || MyUtils.IsZero(ShieldMaxPower - _powerNeeded);
                var serverNoPower = DsState.State.NoPower;

                if (powerLost && _pLossTimer++ > 60 || serverNoPower) {

                    if (PowerLoss(powerLost, serverNoPower)) {
                        _powerFail = true;
                        return;
                    }
                }
                else {

                    _pLossTimer = 0;

                    if (_capacitorLoop != 0 && _tick - _capacitorTick > CapacitorStableCount) 
                        _capacitorLoop = 0;

                    _powerFail = false;
                }
            }

            if (DsState.State.Heat != 0) 
                UpdateHeatRate();
            else 
                _expChargeReduction = 0;
            if (_count == 29 && DsState.State.Charge < ShieldMaxCharge) {
                DsState.State.Charge += ShieldChargeRate;
            }
            else if (DsState.State.Charge.Equals(ShieldMaxCharge))
            {
                ShieldChargeRate = 0f;
                _shieldConsumptionRate = 0f;
            }

            if (_isServer) {

                if (DsState.State.Charge < ShieldMaxCharge) {
                    DsState.State.ShieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;
                }
                else if (DsState.State.Charge < ShieldMaxCharge * 0.1) {
                    DsState.State.ShieldPercent = 0f;
                }
                else {
                    DsState.State.ShieldPercent = 100f;
                }
            }
        }

        private float PowerNeeded(float hpsEfficiency)
        {
            var cleanPower = ShieldAvailablePower + ShieldCurrentPower;
            _otherPower = ShieldMaxPower - cleanPower;
            var powerForShield = (cleanPower * 0.9f) - _shieldMaintaintPower;
            var rawMaxChargeRate = powerForShield > 0 ? powerForShield : 0f;
            _shieldMaxChargeRate = rawMaxChargeRate;
            _shieldPeakRate = (_shieldMaxChargeRate * hpsEfficiency);

            if (DsState.State.Charge + _shieldPeakRate < ShieldMaxCharge) {
                ShieldChargeRate = _shieldPeakRate;
                _shieldConsumptionRate = _shieldMaxChargeRate;
            }
            else {

                if (_shieldPeakRate > 0) {

                    var remaining = MathHelper.Clamp(ShieldMaxCharge - DsState.State.Charge, 0, ShieldMaxCharge);
                    var remainingScaled = remaining / _shieldPeakRate;
                    _shieldConsumptionRate = remainingScaled * _shieldMaxChargeRate;
                    ShieldChargeRate = _shieldPeakRate * remainingScaled;
                }
                else {
                    _shieldConsumptionRate = 0;
                    ShieldChargeRate = 0;
                }
            }

            _powerNeeded = _shieldMaintaintPower + _shieldConsumptionRate + _otherPower;

            return powerForShield;
        }

        private bool PowerLoss(bool powerLost, bool serverNoPower)
        {
            if (powerLost) {

                if (!DsState.State.Online) {

                    DsState.State.Charge = 0.01f;
                    ShieldChargeRate = 0f;
                    _shieldConsumptionRate = 0f;
                    return true;
                }

                _capacitorTick = _tick;
                _capacitorLoop++;
                if (_capacitorLoop > CapacitorDrainCount) {

                    if (_isServer && !DsState.State.NoPower) {

                        DsState.State.NoPower = true;
                        _sendMessage = true;
                        ShieldChangeState();
                    }

                    var shieldLoss = ShieldMaxCharge * 0.0016667f;
                    DsState.State.Charge -= shieldLoss;
                    if (DsState.State.Charge < 0.01f) DsState.State.Charge = 0.01f;

                    if (_isServer) {
                        if (DsState.State.Charge < ShieldMaxCharge) DsState.State.ShieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;
                        else if (DsState.State.Charge < ShieldMaxCharge * 0.1) DsState.State.ShieldPercent = 0f;
                        else DsState.State.ShieldPercent = 100f;
                    }

                    ShieldChargeRate = 0f;
                    _shieldConsumptionRate = 0f;
                    return true;
                }
            }

            if (serverNoPower) {

                _powerNoticeLoop++;
                if (_powerNoticeLoop >= PowerNoticeCount) {

                    DsState.State.NoPower = false;
                    _powerNoticeLoop = 0;
                    ShieldChangeState();
                }
            }
            return false;
        }


        private void UpdateGridPower()
        {
            GridAvailablePower = 0;
            GridMaxPower = 0;
            GridCurrentPower = 0;
            _batteryMaxPower = 0;
            _batteryCurrentOutput = 0;
            _batteryCurrentInput = 0;

            if (MyResourceDist == null || MyResourceDist.SourcesEnabled == MyMultipleEnabledEnum.NoObjects)
                ResetDistributor();

            GridMaxPower = MyResourceDist.MaxAvailableResourceByType(GId);
            GridCurrentPower = MyResourceDist.TotalRequiredInputByType(GId);
            if (!DsSet.Settings.UseBatteries) CalculateBatteryInput();

            GridAvailablePower = GridMaxPower - GridCurrentPower;

            if (!DsSet.Settings.UseBatteries)
            {
                GridCurrentPower += _batteryCurrentInput;
                GridAvailablePower -= _batteryCurrentInput;
            }

            var powerScale = Session.Instance.GameLoaded ? DsSet.Settings.PowerScale : 0;
            var reserveScaler = ReserveScaler[powerScale];
            var userPowerCap = DsSet.Settings.PowerWatts * reserveScaler;
            var shieldMax = GridMaxPower > userPowerCap && reserveScaler > 0 ? userPowerCap : GridMaxPower;
            ShieldMaxPower = shieldMax;
            ShieldAvailablePower = ShieldMaxPower - GridCurrentPower;

            _shieldPowered = ShieldMaxPower > 0;
        }

        private void CalculateBatteryInput()
        {
            foreach (var sub in ShieldComp.LinkedGrids.Keys)
            {
                ConcurrentCachingList<MyBatteryBlock> batteries;
                if (Session.Instance.GridBatteryMap.TryGetValue(sub, out batteries))
                {
                    for (int i = 0; i < batteries.Count; i++)
                    {

                        var battery = batteries[i];
                        if (!battery.IsWorking) continue;
                        var currentInput = battery.CurrentInput;
                        var currentOutput = battery.CurrentOutput;
                        var maxOutput = battery.MaxOutput;

                        if (currentInput > 0)
                        {

                            _batteryCurrentInput += currentInput;
                            if (battery.IsCharging) _batteryCurrentOutput -= currentInput;
                            else _batteryCurrentOutput -= currentInput;
                        }

                        _batteryMaxPower += maxOutput;
                        _batteryCurrentOutput += currentOutput;
                    }
                }
            }
        }
    }
}