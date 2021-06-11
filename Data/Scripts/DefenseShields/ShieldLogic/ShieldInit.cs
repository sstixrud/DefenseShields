
using System.Linq;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;

namespace DefenseShields
{
    using Sandbox.Game.Entities;
    using VRage.ModAPI;
    using System;
    using System.Collections.Generic;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Utils;
    using VRageMath;

    public partial class DefenseShields
    {
        #region Startup Logic
        internal void AssignSlots()
        {
            LogicSlot = Session.GetSlot();
            MonitorSlot = LogicSlot - 1 < 0 ? Session.Instance.EntSlotScaler - 1 : LogicSlot - 1;
        }

        private void UnPauseLogic()
        {
            if (Session.Enforced.Debug >= 2) Log.Line($"[Logic Resumed] Player:{PlayerByShield} - Mover:{MoverByShield} - NewEnt:{NewEntByShield} - Lost:{LostPings > 59} - LastWoken:{LastWokenTick} - ASleep:{Asleep} - TicksNoActivity:{TicksWithNoActivity}");
            TicksWithNoActivity = 0;
            LastWokenTick = _tick;
            Asleep = false;
            PlayerByShield = true;
            Session.Instance.ActiveShields[this] = byte.MaxValue;
            WasPaused = false;
        }

        private void EmitterEventDetected()
        {
            ShieldComp.EmitterEvent = false;
            DsState.State.ActiveEmitterId = ShieldComp.ActiveEmitterId;
            DsState.State.EmitterLos = ShieldComp.EmitterLos;
            if (Session.Enforced.Debug >= 3) Log.Line($"EmitterEvent: ShieldMode:{ShieldMode} - Los:{ShieldComp.EmitterLos} - Warmed:{WarmedUp} - SavedEId:{DsState.State.EmitterLos} - NewEId:{ShieldComp.ActiveEmitterId} - ShieldId [{Shield.EntityId}]");
            if (!GridIsMobile)
            {
                UpdateDimensions = true;
                if (UpdateDimensions) RefreshDimensions();
            }

            if (!ShieldComp.EmitterLos)
            {
                if (!WarmedUp)
                {
                    MyGrid.Physics.ForceActivate();
                    if (Session.Enforced.Debug >= 3) Log.Line($"EmitterStartupFailure: Asleep:{Asleep} - MaxPower:{ShieldMaxPower} - {ShieldSphere.Radius} - ShieldId [{Shield.EntityId}]");
                    LosCheckTick = Session.Instance.Tick + 1800;
                    ShieldChangeState();
                    return;
                }
                if (GridIsMobile && ShieldComp.ShipEmitter != null && !ShieldComp.ShipEmitter.EmiState.State.Los) _sendMessage = true;
                else if (!GridIsMobile && ShieldComp.StationEmitter != null && !ShieldComp.StationEmitter.EmiState.State.Los) _sendMessage = true;
                if (Session.Enforced.Debug >= 3) Log.Line($"EmitterEvent: no emitter is working, shield mode: {ShieldMode} - WarmedUp:{WarmedUp} - MaxPower:{ShieldMaxPower} - Radius:{ShieldSphere.Radius} - Broadcast:{_sendMessage} - ShieldId [{Shield.EntityId}]");
            }
        }

        internal void SelectPassiveShell()
        {
            try
            {
                switch (DsSet.Settings.ShieldShell)
                {
                    case 0:
                        _modelPassive = ModelMediumReflective;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 1:
                        _modelPassive = ModelHighReflective;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 2:
                        _modelPassive = ModelLowReflective;
                        _hideColor = false;
                        _supressedColor = false;
                        break;
                    case 3:
                        _modelPassive = ModelRed;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 4:
                        _modelPassive = ModelBlue;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 5:
                        _modelPassive = ModelGreen;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 6:
                        _modelPassive = ModelPurple;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 7:
                        _modelPassive = ModelGold;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 8:
                        _modelPassive = ModelOrange;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    case 9:
                        _modelPassive = ModelCyan;
                        _hideColor = true;
                        _supressedColor = false;
                        break;
                    default:
                        _modelPassive = ModelMediumReflective;
                        _hideColor = false;
                        _supressedColor = false;
                        break;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in SelectPassiveShell: {ex}"); }
        }

        internal void UpdatePassiveModel()
        {
            try
            {
                if (_shellPassive == null) return;
                _shellPassive.Render.Visible = true;
                _shellPassive.RefreshModels($"{Session.Instance.ModPath()}{_modelPassive}", null);
                _shellPassive.Render.RemoveRenderObjects();
                _shellPassive.Render.UpdateRenderObject(true);
                _hideShield = false;
                if (Session.Enforced.Debug == 3) Log.Line($"UpdatePassiveModel: modelString:{_modelPassive} - ShellNumber:{DsSet.Settings.ShieldShell} - ShieldId [{Shield.EntityId}]");
            }
            catch (Exception ex) { Log.Line($"Exception in UpdatePassiveModel: {ex}"); }
        }

        private void SaveAndSendAll()
        {
            _firstSync = true;
            if (!_isServer) return;
            DsSet.SaveSettings();
            DsSet.NetworkUpdate();
            DsState.SaveState();
            DsState.NetworkUpdate();
            if (Session.Enforced.Debug >= 3) Log.Line($"SaveAndSendAll: ShieldId [{Shield.EntityId}]");
        }

        private void BeforeInit()
        {
            if (Shield.CubeGrid.Physics == null) return;

            _isServer = Session.Instance.IsServer;
            _isDedicated = Session.Instance.DedicatedServer;
            _mpActive = Session.Instance.MpActive;
            
            PowerInit();
            MyAPIGateway.Session.OxygenProviderSystem.AddOxygenGenerator(_ellipsoidOxyProvider);

            if (_isServer) Enforcements.SaveEnforcement(Shield, Session.Enforced, true);
            
			Session.Instance.FunctionalShields[this] = false;
            Session.Instance.Controllers.Add(this);
			
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            InitTick = Session.Instance.Tick;
            _bTime = 1;
            _bInit = true;
            if (Session.Enforced.Debug == 3) Log.Line($"UpdateOnceBeforeFrame: ShieldId [{Shield.EntityId}]");

            if (!_isDedicated)
            {
                _alertAudio = new MyEntity3DSoundEmitter(null, true, 1f);

                _audioReInit = new MySoundPair("Arc_reinitializing");
                _audioSolidBody = new MySoundPair("Arc_solidbody");
                _audioOverload = new MySoundPair("Arc_overloaded");
                _audioEmp = new MySoundPair("Arc_EMP");
                _audioRemod = new MySoundPair("Arc_remodulating");
                _audioLos = new MySoundPair("Arc_noLOS");
                _audioNoPower = new MySoundPair("Arc_insufficientpower");
            }
        }

        private bool PostInit()
        {
            try
            {
                if (_isServer && (ShieldComp.EmitterMode < 0 || ShieldComp.EmitterMode == 0 && ShieldComp.StationEmitter == null || ShieldComp.EmitterMode != 0 && ShieldComp.ShipEmitter == null || ShieldComp.EmittersSuspended || !MyCube.IsFunctional))
                {
                    return false;
                }

                MyEntity emitterEnt = null;
                if (!_isServer && (_clientNotReady || Session.Enforced.Version <= 0 || DsState.State.ActiveEmitterId != 0 && !MyEntities.TryGetEntityById(DsState.State.ActiveEmitterId, out emitterEnt) || !(emitterEnt is IMyUpgradeModule)))
                {
                    return false;
                }

                Session.Instance.CreateControllerElements(Shield);
                SetShieldType(false);
                if (!Session.Instance.DsAction)
                {
                    Session.AppendConditionToAction<IMyUpgradeModule>((a) => Session.Instance.DsActions.Contains(a.Id), (a, b) => b.GameLogic.GetAs<DefenseShields>() != null && Session.Instance.DsActions.Contains(a.Id));
                    Session.Instance.DsAction = true;
                }

                if (_isServer && !MyCube.IsFunctional) return false;

                if (_mpActive && _isServer) DsState.NetworkUpdate();

                _allInited = true;

                if (Session.Enforced.Debug == 3) Log.Line($"AllInited: ShieldId [{Shield.EntityId}]");
            }
            catch (Exception ex) { Log.Line($"Exception in Controller PostInit: {ex}"); }
            return true;
        }

        private void UpdateEntity()
        {
            ShieldComp.LinkedGrids.Clear();
            ShieldComp.SubGrids.Clear();
            _linkedGridCount = -1;
            _blockChanged = true;
            _functionalChanged = true;
            ResetShape(false, true);
            ResetShape(false);
            SetShieldType(false);
            if (!_isDedicated) ShellVisibility(true);
            if (Session.Enforced.Debug == 2) Log.Line($"UpdateEntity: sEnt:{ShieldEnt == null} - sPassive:{_shellPassive == null} - controller mode is: {ShieldMode} - EW:{DsState.State.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - ShieldId [{Shield.EntityId}]");
            Icosphere.Shield = null;
            DsState.State.Heat = 0;

            _updateRender = true;
            _currentHeatStep = 0;
            _accumulatedHeat = 0;
            _heatCycle = -1;
        }

        private void ResetEntity()
        {
            if (_allInited)
                ResetEntityTick = _tick + 1800;
            
            _allInited = false;
            Warming = false;
            WarmedUp = false;
            _resetEntity = false;

            ResetComp();

            if (_isServer)
            {
                ComputeCap();
                ShieldChangeState();
            }

            if (Session.Enforced.Debug == 3) Log.Line($"ResetEntity: ShieldId [{Shield.EntityId}]");
        }

        private void ResetComp()
        {
            ShieldGridComponent comp;
            Shield.CubeGrid.Components.TryGet(out comp);
            if (comp == null)
            {
                ShieldComp = new ShieldGridComponent(null);
                Shield.CubeGrid.Components.Add(ShieldComp);
            }
            else Shield.CubeGrid.Components.TryGet(out ShieldComp);
        }

        private void WarmUpSequence()
        {
            CheckBlocksAndNewShape(false);

            _oldGridHalfExtents = DsState.State.GridHalfExtents;
            _oldEllipsoidAdjust = DsState.State.EllipsoidAdjust;
            Warming = true;
        }

        private void CheckBlocksAndNewShape(bool refreshBlocks)
        {
            _blockChanged = true;
            _functionalChanged = true;
            ResetShape(false);
            ResetShape(false, true);
            if (refreshBlocks)
            {
                BlockChanged(false);
                if (_isServer) 
                    ComputeCap();
            }
            _updateRender = true;
        }

        private void StorageSetup()
        {
            try
            {
                var isServer = MyAPIGateway.Multiplayer.IsServer;

                if (DsSet == null) DsSet = new ControllerSettings(Shield);
                if (DsState == null) DsState = new ControllerState(Shield);
                if (Shield.Storage == null) DsState.StorageInit();
                if (!isServer)
                {
                    var enforcement = Enforcements.LoadEnforcement(Shield);
                    if (enforcement != null) Session.Enforced = enforcement;
                }
                DsSet.LoadSettings();
                if (!DsState.LoadState() && !isServer) _clientNotReady = true;
                UpdateSettings(DsSet.Settings);
                if (isServer)
                {
                    DsState.State.Overload = false;
                    DsState.State.NoPower = false;
                    DsState.State.Remodulate = false;
                    if (DsState.State.Suspended)
                    {
                        DsState.State.Suspended = false;
                        DsState.State.Online = false;
                    }
                    DsState.State.Sleeping = false;
                    DsState.State.Waking = false;
                    DsState.State.FieldBlocked = false;
                    DsState.State.Heat = 0;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in StorageSetup: {ex}"); }
        }

        private void ResetDistributor()
        {
            MyResourceDist = FakeController.GridResourceDistributor;
        }

        private void PowerPreInit()
        {
            try
            {
                if (_sink == null) _sink = new MyResourceSinkComponent();
                _resourceInfo = new MyResourceSinkInfo()
                {
                    ResourceTypeId = GId,
                    MaxRequiredInput = 0f,
                    RequiredInputFunc = () => _power
                };
                _sink.Init(MyStringHash.GetOrCompute("Defense"), _resourceInfo);
                _sink.AddType(ref _resourceInfo);
                Entity.Components.Add(_sink);
            }
            catch (Exception ex) { Log.Line($"Exception in PowerPreInit: {ex}"); }
        }

        private void CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            ShieldCurrentPower = sink.CurrentInputByType(GId);
        }

        private void PowerInit()
        {
            try
            {
                _sink.Update();
                Shield.RefreshCustomInfo();

                var enableState = Shield.Enabled;
                if (enableState)
                {
                    Shield.Enabled = false;
                    Shield.Enabled = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"PowerInit: ShieldId [{Shield.EntityId}]");
            }
            catch (Exception ex) { Log.Line($"Exception in AddResourceSourceComponent: {ex}"); }
        }

        private void SetShieldType(bool quickCheck)
        {
            var noChange = false;
            var oldMode = ShieldMode;
            if (_isServer)
            {
                switch (ShieldComp.EmitterMode)
                {
                    case 0:
                        ShieldMode = ShieldType.Station;
                        DsState.State.Mode = (int)ShieldMode;
                        break;
                    case 1:
                        ShieldMode = ShieldType.LargeGrid;
                        DsState.State.Mode = (int)ShieldMode;
                        break;
                    case 2:
                        ShieldMode = ShieldType.SmallGrid;
                        DsState.State.Mode = (int)ShieldMode;
                        break;
                    default:
                        ShieldMode = ShieldType.Unknown;
                        DsState.State.Mode = (int)ShieldMode;
                        DsState.State.Suspended = true;
                        break;
                }
            }
            else ShieldMode = (ShieldType)DsState.State.Mode;

            if (ShieldMode == oldMode) noChange = true;

            if ((quickCheck && noChange) || ShieldMode == ShieldType.Unknown) return;

            switch (ShieldMode)
            {
                case ShieldType.Station:
                    if (Session.Enforced.StationRatio > 0) _shieldTypeRatio = Session.Enforced.StationRatio;
                    break;
                case ShieldType.LargeGrid:
                    if (Session.Enforced.LargeShipRatio > 0) _shieldTypeRatio = Session.Enforced.LargeShipRatio;
                    break;
                case ShieldType.SmallGrid:
                    if (Session.Enforced.SmallShipRatio > 0) _shieldTypeRatio = Session.Enforced.SmallShipRatio;
                    break;
            }

            switch (ShieldMode)
            {
                case ShieldType.Station:
                    _shapeChanged = false;
                    UpdateDimensions = true;
                    break;
                case ShieldType.LargeGrid:
                    _updateMobileShape = true;
                    break;
                case ShieldType.SmallGrid:
                    _updateMobileShape = true;
                    break;
            }
            GridIsMobile = ShieldMode != ShieldType.Station;
            DsUi.CreateUi(Shield);
            InitEntities(true);
        }

        private void InitEntities(bool fullInit)
        {
            if (ShieldEnt != null)
            {
                Session.Instance.IdToBus.Remove(ShieldEnt.EntityId);
                ShieldEnt.Close();
            }
            ShellActive?.Close();
            _shellPassive?.Close();

            if (!fullInit)
            {
                if (Session.Enforced.Debug == 3) Log.Line($"InitEntities: mode: {ShieldMode}, remove complete - ShieldId [{Shield.EntityId}]");
                return;
            }

            SelectPassiveShell();
            var parent = (MyEntity)MyGrid;
            if (!_isDedicated)
            {
                _shellPassive = Spawn.EmptyEntity("dShellPassive", $"{Session.Instance.ModPath()}{_modelPassive}", parent, true);
                _shellPassive.Render.CastShadows = false;
                _shellPassive.IsPreview = true;
                _shellPassive.Render.Visible = true;
                _shellPassive.Render.RemoveRenderObjects();
                _shellPassive.Render.UpdateRenderObject(true);
                _shellPassive.Render.UpdateRenderObject(false);
                _shellPassive.Save = false;
                _shellPassive.SyncFlag = false;
                _shellPassive.RemoveFromGamePruningStructure();

                ShellActive = Spawn.EmptyEntity("dShellActive", $"{Session.Instance.ModPath()}{_modelActive}", parent, true);
                ShellActive.Render.CastShadows = false;
                ShellActive.IsPreview = true;
                ShellActive.Render.Visible = true;
                ShellActive.Render.RemoveRenderObjects();
                ShellActive.Render.UpdateRenderObject(true);
                ShellActive.Render.UpdateRenderObject(false);
                ShellActive.Save = false;
                ShellActive.SyncFlag = false;
                ShellActive.SetEmissiveParts("ShieldEmissiveAlpha", Color.Transparent, 0f);
                ShellActive.SetEmissiveParts("ShieldDamageGlass", Color.Transparent, 0f);
                ShellActive.RemoveFromGamePruningStructure();
            }

            ShieldEnt = Spawn.EmptyEntity("dShield", null, parent);
            ShieldEnt.Render.CastShadows = false;
            ShieldEnt.Render.RemoveRenderObjects();
            ShieldEnt.Render.UpdateRenderObject(true);
            ShieldEnt.Render.Visible = false;
            ShieldEnt.Save = false;
            _shieldEntRendId = ShieldEnt.Render.GetRenderObjectID();
            _updateRender = true;

            if (ShieldEnt != null) Session.Instance.IdToBus[ShieldEnt.EntityId] = ShieldComp;

            if (Icosphere == null) Icosphere = new Icosphere.Instance(Session.Instance.Icosphere);
            if (Session.Enforced.Debug == 3) Log.Line($"InitEntities: mode: {ShieldMode}, spawn complete - ShieldId [{Shield.EntityId}]");
        }

        private readonly List<CapCube> _capcubeBoxList = new List<CapCube>(4096);

        private struct CapCube
        {
            internal MyCubeBlock Cube;
            internal Vector3I Position;
        }

        private void ComputeCap()
        {
            _updateCap = false;
            if (ShieldComp.SubGrids.Count == 0) {

                UpdateSubGrids();
                if (ShieldComp.SubGrids.Count == 0) {
                    Log.Line($"SubGrids remained 0 in ComputeCap");
                    return;
                }
            }

            if (MyUtils.IsZero(ShieldSize)) {
                _updateCap = true;
                return;
            }

            _forceCap = false;
            _delayedCapTick = uint.MaxValue;

            //var sphere = new BoundingSphereD(Vector3I.Round(MyGrid.PositionComp.LocalAABB.Center * MyGrid.GridSizeR), ShieldSize.AbsMax() * MyGrid.GridSizeR);
            var size = 0;
            var maxSize = 0d;
            var powerCount = 0d;

            foreach (var sub in ShieldComp.SubGrids.Keys) {

                //var subWorldCenter = sub.GridIntegerToWorld(Vector3I.Zero);
                //var parentLocalOffset = MyGrid.WorldToGridInteger(subWorldCenter);

                var xCount = MyGrid.PositionComp.LocalAABB.Extents.X / MyGrid.GridSize;
                var yCount = MyGrid.PositionComp.LocalAABB.Extents.Y / MyGrid.GridSize;
                var zCount = MyGrid.PositionComp.LocalAABB.Extents.Z / MyGrid.GridSize;
                var maxCubes = Math.Round(xCount * yCount * zCount, 0);
                maxSize += maxCubes;
                foreach (IMySlimBlock slim in sub.CubeBlocks) {

                    var min = slim.Min;
                    var max = slim.Max;

                    //if (sphere.Contains(new BoundingBoxI(min + parentLocalOffset, max + parentLocalOffset)) != ContainmentType.Contains)
                        //continue;

                    var span = max + 1 - min;
                    var value = (span.X * span.Y * span.Z);
                    size += value;
                    if (slim.FatBlock is IMyPowerProducer)
                    {
                        powerCount += value;
                    }
                }
            }
            _capcubeBoxList.Clear();

            if (size <= 0 || maxSize <= 0 || powerCount <= 0) {
                _updateCap = true;
                return;
            }

            var blockResult = (float)(size / maxSize);
            var blockCapLimit = MathHelper.Clamp(BlockDensityLimit / Session.Enforced.BlockScaler, 0.000001f, 1);
            _blockCapMulti = blockResult / blockCapLimit;
            var blockCapMultiClamp = MathHelper.Clamp(_blockCapMulti, 0.001f, 1);

            var powerResult = (float)(powerCount / size);
            var powerCapLimit = MathHelper.Clamp(PowerDensityLimit * Session.Enforced.PowerScaler, 0.000001f, 1);
            _powerCapMulti = powerCapLimit / powerResult;
            var powerCapMultiClamp = MathHelper.Clamp(_powerCapMulti, 0.001f, 1);

            DsState.State.CapModifier = MathHelper.Clamp(powerCapMultiClamp * blockCapMultiClamp, 0.000001f, 1);
            _shieldCapped = DsState.State.CapModifier < 1;
        }
        #endregion
    }
}
