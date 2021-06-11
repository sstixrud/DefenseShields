
namespace DefenseShields
{
    using Support;
    using VRageMath;

    public partial class DefenseShields
    {
        private bool EntityAlive()
        {
            _tick = Session.Instance.Tick;
            _tick20 = Session.Instance.Tick20;
            _tick30 = Session.Instance.Tick30;
            _tick60 = Session.Instance.Tick60;
            _tick180 = Session.Instance.Tick180;
            _tick300 = Session.Instance.Tick300;
            _tick600 = Session.Instance.Tick600;
            _tick1800 = Session.Instance.Tick1800;
            if (_count++ == 59) _count = 0;

            if (WasPaused && (PlayerByShield || MoverByShield || NewEntByShield || LostPings > 59)) UnPauseLogic();
            LostPings = 0;

            var wait = _isServer && !_tick60 && DsState.State.Suspended;

            MyGrid = MyCube.CubeGrid;
            if (MyGrid?.Physics == null) return false;

            if (_resetEntity) 
                ResetEntity();

            if (!_firstSync && _readyToSync) 
                SaveAndSendAll();

            if (!_isDedicated && _count == 29 && InControlPanel && InThisTerminal) 
                TerminalRefresh();

            if (wait || (!_allInited && !PostInit())) return false;

            if (_tick1800 && Session.Enforced.Debug > 0) 
                Debug();

            IsStatic = MyGrid.IsStatic;

            if (!Warming) 
                WarmUpSequence();

            if (_subUpdate && !DsState.State.Suspended) 
                UpdateSubGrids();

            if (_blockEvent && _tick >= _funcTick) 
                BlockChanged(true);

            if (_blockChanged) 
                BlockMonitor();
            
            if (_isServer && (_updateCap || _tick >= _delayedCapTick ))
                ComputeCap();

            if (ClientUiUpdate || SettingsUpdated) 
                UpdateSettings();

            if (_mpActive)
            {
                if (_isServer)
                {
                    if (_tick - 1 > _lastSendDamageTick) ShieldHitReset(ShieldHit.Amount > 0 && ShieldHit.HitPos != Vector3D.Zero);
                    if (ShieldHitsToSend.Count != 0) SendShieldHits();
                    if (!_isDedicated && ShieldHits.Count != 0) AbsorbClientShieldHits();
                }
                else if (ShieldHits.Count != 0) AbsorbClientShieldHits();
            }
            return true;
        }

        private State ShieldOn()
        {
            if (_isServer)
            {
                if (Suspended()) return State.Init;
                if (ShieldSleeping()) return State.Sleep;
                if (ShieldWaking()) return State.Wake;
                
                UpdateSides();
                
                if (UpdateDimensions) RefreshDimensions();

                if (_tick60)
                {
                    GetModulationInfo();
                    GetEnhancernInfo();
                }

                if (_tick >= LosCheckTick) LosCheck();
                if (ShieldComp.EmitterEvent) EmitterEventDetected();
                if (_shapeEvent || FitChanged) CheckExtents();
                if (_adjustShape) AdjustShape(true);
                if (!ShieldServerStatusUp())
                {
                    if (DsState.State.Lowered) return State.Lowered;
                    if (_overLoadLoop > -1 || _reModulationLoop > -1 || _empOverLoadLoop > -1) FailureDurations();
                    return State.Failure;
                }
            }
            else
            {
                if (ClientOfflineStates()) return State.Failure;

                UpdateSides();

                if (UpdateDimensions) RefreshDimensions();

                if (!GridIsMobile && !DsState.State.IncreaseO2ByFPercent.Equals(_ellipsoidOxyProvider.O2Level))
                    _ellipsoidOxyProvider.UpdateOxygenProvider(DetectMatrixOutsideInv, DsState.State.IncreaseO2ByFPercent);

                PowerOnline();
                StepDamageState();

                if (!ClientShieldShieldRaised()) return State.Lowered;
            }

            return State.Active;
        }

        private bool ShieldServerStatusUp()
        {
            var notFailing = _overLoadLoop == -1 && _empOverLoadLoop == -1 && _reModulationLoop == -1;
            ShieldActive = ShieldRaised() && DsState.State.EmitterLos && notFailing && PowerOnline();
            StepDamageState();
            if (!ShieldActive)
                return false;

            var prevOnline = DsState.State.Online;
            if (!prevOnline && GridIsMobile && FieldShapeBlocked())
                return false;

            _comingOnline = !prevOnline || _firstLoop;

            DsState.State.Online = true;

            if (!GridIsMobile && (_comingOnline || ShieldComp.O2Updated))
            {
                _ellipsoidOxyProvider.UpdateOxygenProvider(DetectMatrixOutsideInv, DsState.State.IncreaseO2ByFPercent);
                ShieldComp.O2Updated = false;
            }

            return true;
        }

        private void ComingOnlineSetup()
        {
            if (!_isDedicated) ShellVisibility();
            ShieldEnt.Render.Visible = true;
            _updateRender = true;
            _comingOnline = false;
            _firstLoop = false;
            _shapeEvent = true;
            LastWokenTick = _tick;
            NotFailed = true;
            WarmedUp = true;
            
            if (_isServer)
            {
                _updateCap = true;
                CleanWebEnts();
                ShieldChangeState();
                if (Session.Enforced.Debug == 3) Log.Line($"StateUpdate: ComingOnlineSetup - ShieldId [{Shield.EntityId}]");
            }
            else
            {
                CheckBlocksAndNewShape(false);
                TerminalRefresh();
                if (Session.Enforced.Debug == 3) Log.Line($"StateUpdate: ComingOnlineSetup - ShieldId [{Shield.EntityId}]");
            }
            Session.Instance.ActiveShields[this] = byte.MaxValue;
            UpdateSubGrids();
        }

        private void OfflineShield(bool clear, bool resetShape, bool keepCharge = false)
        {
            DefaultShieldState(clear, keepCharge, resetShape);

            if (_isServer) ShieldChangeState();
            else TerminalRefresh();

            if (!_isDedicated) ShellVisibility(true);

            if (Session.Enforced.Debug >= 2) Log.Line($"[ShieldOff] clear:{clear} - resetShape:{resetShape} - keepCharge:{keepCharge} - ShieldId [{Shield.EntityId}]");
        }

        private void DefaultShieldState(bool clear, bool keepCharge, bool resetShape = true)
        {
            NotFailed = false;
            var clearHeat = !DsState.State.Lowered && !MyCube.IsWorking && Shield.Enabled;
            if (clear)
            {
                _power = 0.001f;
                _sink.Update();

                if (_isServer && !keepCharge)
                {
                    DsState.State.Charge = 0f;
                    DsState.State.ShieldPercent = 0f;
                }
                if (resetShape)
                {
                    ResetShape(true, true);
                    _shapeEvent = true;
                }
            }

            if (_isServer)
            {
                DsState.State.IncreaseO2ByFPercent = 0f;
                if (clearHeat)
                    DsState.State.Heat = 0;
                DsState.State.Online = false;
            }

            if (clearHeat)
            {
                _currentHeatStep = 0;
                _accumulatedHeat = 0;
                _heatCycle = -1;
            }

            Absorb = 0f;
            ClearDamageTypeInfo();

            EnergyHit = HitType.Kinetic;
            WorldImpactPosition = Vector3D.NegativeInfinity;
            ShieldEnt.Render.Visible = false;

            TerminalRefresh(false);
            CleanWebEnts();
            byte ignore;
            Session.Instance.ActiveShields.TryRemove(this, out ignore);
        }

        private bool ShieldRaised()
        {
            if (!DsSet.Settings.RaiseShield)
            {
                if (!DsState.State.Lowered)
                {
                    if (!GridIsMobile) _ellipsoidOxyProvider.UpdateOxygenProvider(MatrixD.Zero, 0);

                    DsState.State.IncreaseO2ByFPercent = 0f;
                    if (!_isDedicated) 
                        ShellVisibility(true);
                    DsState.State.Lowered = true;
                    if (_isServer)
                        ShieldChangeState();
                }
                PowerOnline();
                return false;
            }
            if (DsState.State.Lowered)
            {
                if (!GridIsMobile)
                {
                    _ellipsoidOxyProvider.UpdateOxygenProvider(DetectMatrixOutsideInv, DsState.State.IncreaseO2ByFPercent);
                    ShieldComp.O2Updated = false;
                }
                DsState.State.Lowered = false;

                _updateCap = true;

                if (!_isDedicated) 
                    ShellVisibility();
                if (_isServer)
                    ShieldChangeState();
            }

            return true;
        }

        private bool ShieldSleeping()
        {
            if (ShieldComp.EmittersSuspended || (_linkedGridCount > 1 || DsState.State.Sleeping) && SubGridSlaveControllerLink())
            {
                if (!DsState.State.Sleeping)
                {
                    if (!GridIsMobile) _ellipsoidOxyProvider.UpdateOxygenProvider(MatrixD.Zero, 0);

                    DsState.State.IncreaseO2ByFPercent = 0f;
                    if (!_isDedicated) ShellVisibility(true);
                    DsState.State.Sleeping = true;
                    TerminalRefresh(false);
                    if (Session.Enforced.Debug >= 3) Log.Line($"Sleep: controller detected sleeping emitter, shield mode: {ShieldMode} - ShieldId [{Shield.EntityId}]");
                }
                DsState.State.Sleeping = true;
                return true;
            }
            if (DsState.State.Sleeping)
            {
                DsState.State.Sleeping = false;
                if (!_isDedicated && _tick60 && InControlPanel && InThisTerminal) TerminalRefresh();
                if (Session.Enforced.Debug >= 3) Log.Line($"Sleep: Controller was sleeping but is now waking, shield mode: {ShieldMode} - ShieldId [{Shield.EntityId}]");
            }

            DsState.State.Sleeping = false;
            return false;
        }

        private bool Suspended()
        {
            var primeMode = ShieldMode == ShieldType.Station && IsStatic && ShieldComp.StationEmitter == null;
            var betaMode = ShieldMode != ShieldType.Station && !IsStatic && ShieldComp.ShipEmitter == null;
            var notStation = ShieldMode != ShieldType.Station && IsStatic;
            var notShip = ShieldMode == ShieldType.Station && !IsStatic;
            var unKnown = ShieldMode == ShieldType.Unknown;
            var wrongOwner = !DsState.State.ControllerGridAccess;
            var nullShield = ShieldComp.DefenseShields == null;
            var myShield = ShieldComp.DefenseShields == this;
            var wrongRole = notStation || notShip || unKnown;
            if (!nullShield && !myShield || !MyCube.IsFunctional || primeMode || betaMode || wrongOwner || wrongRole)
            {

                if (!DsState.State.Suspended) {
                    Suspend();

                    if (!wrongRole && ShieldMode != ShieldType.Station && !myShield && DsState.State.Charge > 0)
                        DsState.State.Charge = 0;
                }

                if (myShield) ShieldComp.DefenseShields = null;
                if (wrongRole) SetShieldType(true);
    
                return true;
            }

            if (DsState.State.Suspended || nullShield)
            {
                UnSuspend();
                return true;
            }

            return !MyCube.IsWorking;
        }

        private void Suspend()
        {
            SetShieldType(false);
            DsState.State.Suspended = true;
            var keepCharge = MyCube.IsWorking && DsState.State.ControllerGridAccess;
            OfflineShield(true, true, keepCharge);
            bool value;
            Session.Instance.BlockTagBackup(Shield);
            Session.Instance.FunctionalShields.TryRemove(this, out value);
        }

        private void UnSuspend()
        {
            if (_tick > ResetEntityTick && DsState.State.Suspended)
            {
                ResetEntityTick = _tick + 1800;
            }

            DsState.State.Suspended = false;
            ShieldComp.DefenseShields = this;
            Session.Instance.BlockTagActive(Shield);
            Session.Instance.FunctionalShields[this] = false;
            UpdateEntity();
            GetEnhancernInfo();
            GetModulationInfo();
            if (Session.Enforced.Debug == 3) Log.Line($"Unsuspended: CM:{ShieldMode} - EW:{DsState.State.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - Range:{BoundingRange} - ShieldId [{Shield.EntityId}]");
        }

        private bool ShieldWaking()
        {
            if (_tick < ResetEntityTick)
            {
                if (!DsState.State.Waking)
                {
                    DsState.State.Waking = true;
                    _sendMessage = true;
                    if (Session.Enforced.Debug >= 2) Log.Line($"Waking: ShieldId [{Shield.EntityId}]");
                }
                return true;
            }
            if (ResetEntityTick != uint.MinValue && _tick >= ResetEntityTick)
            {
                ResetShape(false);
                _updateRender = true;
                ResetEntityTick = uint.MinValue;

                if (Session.Enforced.Debug >= 2) Log.Line($"Woke: ShieldId [{Shield.EntityId}]");
            }
            else if (_shapeTick != uint.MinValue && _tick >= _shapeTick)
            {
                _shapeEvent = true;
                _shapeTick = uint.MinValue;
            }
            DsState.State.Waking = false;
            return false;
        }

        private bool ClientOfflineStates()
        {
            var shieldUp = DsState.State.Online && !DsState.State.Suspended && !DsState.State.Sleeping;
            if (ShieldComp.DefenseShields != this && shieldUp)
            {
                ShieldComp.DefenseShields = this;
            }

            if (!shieldUp)
            {
                if (_clientOn)
                {
                    if (ShieldMaxPower <= 0) BroadcastMessage(true);
                    if (!GridIsMobile) _ellipsoidOxyProvider.UpdateOxygenProvider(MatrixD.Zero, 0);
                    ShellVisibility(true);
                    _clientOn = false;
                    TerminalRefresh();
                    _power = 0.001f;
                    _sink.Update();
                }

                return true;
            }

            if (!_clientOn)
            {
                ComingOnlineSetup();
                _clientOn = true;
            }
            return false;
        }

        private bool ClientShieldShieldRaised()
        {
            if (WarmedUp && DsState.State.Lowered)
            {
                if (!_clientAltered)
                {
                    if (!GridIsMobile) _ellipsoidOxyProvider.UpdateOxygenProvider(MatrixD.Zero, 0);
                    ShellVisibility(true);
                    _clientAltered = true;
                }
                return false;
            }

            if (_clientAltered)
            {
                ShellVisibility();
                _clientAltered = false;
            }

            return true;
        }

        internal void StartRedirectTimer()
        {
            RedirectUpdateTime = Session.Instance.Tick + 120;
        }

        private void UpdateRedirectState()
        {
            ShieldRedirectState = DsSet.Settings.ShieldRedirects;
            UpdateMapping();
        }

        internal void UpdateSettings(ControllerSettingsValues newSettings)
        {
            if (newSettings.MId > DsSet.Settings.MId)
            {
                if (!_isServer)
                {
                    if (MyGrid != null && Session.Enforced.Debug == 3) 
                        Log.Line($"{MyGrid.DebugName} received settings packet");

                    if (newSettings.Visible != DsSet.Settings.Visible) 
                        _clientAltered = true;
                }
                var newShape = newSettings.Fit != DsSet.Settings.Fit || newSettings.FortifyShield != DsSet.Settings.FortifyShield ;
                
                DsSet.Settings = newSettings;
                SettingsUpdated = true;
                if (newShape) FitChanged = true;

                if (RedirectUpdateTime <= Session.Instance.Tick && ShieldRedirectState != newSettings.ShieldRedirects)
                    StartRedirectTimer();
            }
        }

        internal void UpdateState(ControllerStateValues newState)
        {
            if (newState.MId > DsState.State.MId)
            {
                if (!_isServer)
                {
                    ClientInitPacket = true;
                    if (Session.Enforced.Debug == 3) Log.Line($"[Shield Update]: On:{newState.Online} - Suspend:{newState.Suspended} - Sleep:{newState.Sleeping} - ClientOn:{_clientOn} - SId:{MyCube.EntityId} - Name:{MyGrid.DebugName}");
                    if (!newState.EllipsoidAdjust.Equals(DsState.State.EllipsoidAdjust) || !newState.ShieldFudge.Equals(DsState.State.ShieldFudge) || !newState.GridHalfExtents.Equals(DsState.State.GridHalfExtents))
                    {
                        _updateMobileShape = true;
                    }
                }
                DsState.State = newState;

                if (!_isDedicated && _clientMessageCount < DsState.State.MessageCount)
                    BroadcastMessage();

                _clientNotReady = false;
            }
        }

        private void UpdateSettings()
        {
            if (_tick % 33 == 0)
            {
                if (SettingsUpdated)
                {
                    SettingsUpdated = false;
                    DsSet.SaveSettings();
                    ResetShape(false);
                }
            }
            else if (_tick % 34 == 0)
            {
                if (ClientUiUpdate)
                {
                    ClientUiUpdate = false;
                    DsSet.NetworkUpdate();
                }
            }
        }

        private void ShieldChangeState()
        {
            if (_isServer && _sendMessage)
                ++DsState.State.MessageCount;
            _sendMessage = false;

            if (Session.Instance.MpActive && Session.Instance.IsServer)
            {
                DsState.NetworkUpdate();
                if (_isServer) TerminalRefresh(false);
            }

            DsState.SaveState();
        }
    }
}
