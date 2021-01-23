

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
                if (GridIsMobile && ShieldComp.ShipEmitter != null && !ShieldComp.ShipEmitter.EmiState.State.Los) DsState.State.Message = true;
                else if (!GridIsMobile && ShieldComp.StationEmitter != null && !ShieldComp.StationEmitter.EmiState.State.Los) DsState.State.Message = true;
                if (Session.Enforced.Debug >= 3) Log.Line($"EmitterEvent: no emitter is working, shield mode: {ShieldMode} - WarmedUp:{WarmedUp} - MaxPower:{ShieldMaxPower} - Radius:{ShieldSphere.Radius} - Broadcast:{DsState.State.Message} - ShieldId [{Shield.EntityId}]");
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
            
            for (int i = 0; i < _tmpCapLists.Length; i++)
                _tmpCapLists[i] = new List<MyCubeBlock>();
            
            PowerInit();
            MyAPIGateway.Session.OxygenProviderSystem.AddOxygenGenerator(_ellipsoidOxyProvider);

            if (_isServer) Enforcements.SaveEnforcement(Shield, Session.Enforced, true);
            
			Session.Instance.FunctionalShields[this] = false;
            Session.Instance.Controllers.Add(this);
			
            //if (MyAPIGateway.Session.CreativeMode) CreativeModeWarning();
            IsWorking = MyCube.IsWorking;
            IsFunctional = MyCube.IsFunctional;
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
                if (_isServer && (ShieldComp.EmitterMode < 0 || ShieldComp.EmitterMode == 0 && ShieldComp.StationEmitter == null || ShieldComp.EmitterMode != 0 && ShieldComp.ShipEmitter == null || ShieldComp.EmittersSuspended || !IsFunctional))
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

                if (_isServer && !IsFunctional) return false;

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
            Icosphere.ShellActive = null;
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
            if (refreshBlocks) BlockChanged(false, true);
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
                IsWorking = MyCube.IsWorking;
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
                    _modelActive = "\\Models\\Cubes\\ShieldActiveBase_LOD4.mwm";
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
            _shellActive?.Close();
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

                _shellActive = Spawn.EmptyEntity("dShellActive", $"{Session.Instance.ModPath()}{_modelActive}", parent, true);
                _shellActive.Render.CastShadows = false;
                _shellActive.IsPreview = true;
                _shellActive.Render.Visible = true;
                _shellActive.Render.RemoveRenderObjects();
                _shellActive.Render.UpdateRenderObject(true);
                _shellActive.Render.UpdateRenderObject(false);
                _shellActive.Save = false;
                _shellActive.SyncFlag = false;
                _shellActive.SetEmissiveParts("ShieldEmissiveAlpha", Color.Transparent, 0f);
                _shellActive.RemoveFromGamePruningStructure();
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

        private List<MyCubeBlock> _testRemoves = new List<MyCubeBlock>();
        private enum LocalDir
        {
            Xp,
            Xm,
            Yp,
            Ym,
            Zp,
            Zm
        }

        private void ComputeCap2()
        {
            _updateCap = false;

            if (ShieldComp.SubGrids.Count == 0)
                UpdateSubGrids();

            float boxsArea = 0;

            var totalFat = 0;
            var subGridId = 0;
            foreach (var sub in ShieldComp.SubGrids.Keys) {

                var fatBlocks = sub.GetFatBlocks();
                var fatCount = fatBlocks.Count;
                totalFat += fatCount;

                Vector3I center = Vector3I.Zero;
                BoundingBox newBox = BoundingBox.Invalid;

                for (int i = 0; i < fatCount; i++) {

                    var cube = fatBlocks[i];
                    var pos = cube.Position;
                    center += pos;
                    if (Math.Abs(pos.X) > Math.Abs(pos.Y)) {

                        if (Math.Abs(pos.X) > Math.Abs(pos.Z)) {

                            if (pos.X > 0)
                                _tmpCapLists[0].Add(cube);
                            else _tmpCapLists[1].Add(cube);
                        }
                        else {

                            if (pos.Z > 0)
                                _tmpCapLists[4].Add(cube);
                            else _tmpCapLists[5].Add(cube);
                        }
                    }
                    else if (Math.Abs(pos.Y) > Math.Abs(pos.Z)) {

                        if (pos.Y > 0)
                            _tmpCapLists[2].Add(cube);
                        else _tmpCapLists[3].Add(cube);
                    }
                    else {

                        if (pos.Z > 0)
                            _tmpCapLists[4].Add(cube);
                        else _tmpCapLists[5].Add(cube);
                    }
                }

                Array.Sort(_tmpCapLists, Comparison);

                int removed = 0;
                for (int x = 0; x < 6; x++) {

                    var collection = _tmpCapLists[x];
                    if (collection.Count == 0)
                        continue;

                    ShellSort(collection, center);


                    var numToRemove = (int)(collection.Count * 0.05);

                    if (numToRemove > 0) {

                        collection.RemoveRange(collection.Count - numToRemove, numToRemove);
                        removed += numToRemove;

                    }

                    for (int i = 0; i < collection.Count; i++) {
                        var cube = collection[i];
                        newBox.Min = Vector3.Min(newBox.Min, cube.Min * cube.CubeGrid.GridSize);
                        newBox.Max = Vector3.Max(newBox.Max, cube.Max * cube.CubeGrid.GridSize);
                    }

                    Log.CleanLine($"subGridId:{subGridId} - collection{x}: - collectionCount:{collection.Count} - removedSoFar:{removed} - percentile95th:{(int)(collection.Count * 0.05)} - targetToRemove:{fatCount * 0.05}");

                    _tmpCapLists[x].Clear();
                }

                boxsArea += newBox.SurfaceArea();
                subGridId++;
            }

            Log.Line($"boxsArea:{boxsArea}");

            var unitLen = MyGrid.GridSize;
            if (boxsArea < 1)
                boxsArea = (float)UtilsStatic.SurfaceAreaCuboid(totalFat * unitLen, unitLen, unitLen);

            var surfaceArea = (float)Math.Sqrt(boxsArea);
            DsState.State.GridIntegrity = (surfaceArea * MagicRatio);
        }

        private int Comparison(List<MyCubeBlock> x, List<MyCubeBlock> y)
        {
            return -x.Count.CompareTo(y.Count);
        }


        private readonly List<MyCubeBlock> _cubeList = new List<MyCubeBlock>();

        private void ComputeCap()
        {
            ComputeCap2();
            return;
            _updateCap = false;

            if (ShieldComp.SubGrids.Count == 0)
                UpdateSubGrids();

            float boxsArea = 0;

            var totalFat = 0;
            foreach (var sub in ShieldComp.SubGrids.Keys) {

                _cubeList.Clear();
                var fatBlocks = sub.GetFatBlocks();
                var fatCount = fatBlocks.Count;
                totalFat += fatCount;

                for (int i = 0; i < fatCount; i++) {
                    var cube = fatBlocks[i];
                    _cubeList.Add(cube);
                }

                var center = GetAverage(_cubeList);
                ShellSort(_cubeList, center);
                var percentile95Th = (int)(fatCount * 0.10);
                _cubeList.RemoveRange(_cubeList.Count - percentile95Th, percentile95Th);

                BoundingBox newBox = BoundingBox.Invalid;

                for (int i = 0; i < _cubeList.Count; i++) {
                    var cube = _cubeList[i];
                    newBox.Min = Vector3.Min(newBox.Min, cube.Min * cube.CubeGrid.GridSize);
                    newBox.Max = Vector3.Max(newBox.Max, cube.Max * cube.CubeGrid.GridSize);
                }

                boxsArea += newBox.SurfaceArea();
                Log.Line($"{boxsArea}");

            }

            var unitLen = MyGrid.GridSize;
            if (boxsArea < 1)
                boxsArea = (float)UtilsStatic.SurfaceAreaCuboid(totalFat * unitLen, unitLen, unitLen);

            var surfaceArea = (float)Math.Sqrt(boxsArea);
            DsState.State.GridIntegrity = (surfaceArea * MagicRatio);
        }

        static void ShellSort(List<MyCubeBlock> list, Vector3I center)
        {
            int length = list.Count;
            for (int h = length / 2; h > 0; h /= 2)
            {
                for (int i = h; i < length; i += 1)
                {
                    var tempValue = list[i];
                    var dist = Vector3.DistanceSquared(list[i].Position, center);

                    int j;
                    for (j = i; j >= h && Vector3.DistanceSquared(list[j - h].Position, center) > dist; j -= h)
                    {
                        list[j] = list[j - h];
                    }

                    list[j] = tempValue;
                }
            }
        }

        private Vector3I GetAverage(List<MyCubeBlock> blocks)
        {
            Vector3I vec = Vector3I.Zero;
            for (int i = 0; i < blocks.Count; i++)
            {
                var cube = blocks[i];
                vec += cube.Position;
            }
            return vec / blocks.Count;
        }

        private void Deviation(List<MyCubeBlock> blocks)
        {
            double avgX = 0;
            double avgY = 0;
            double avgZ = 0;

            for (int i = 0; i < blocks.Count; i++) {

                var cube = blocks[i];
                avgX = (cube.Min.X + cube.Max.X) * cube.CubeGrid.GridSize / 2.0;
                avgY = (cube.Min.Y + cube.Max.Y) * cube.CubeGrid.GridSize / 2.0;
                avgZ = (cube.Min.Z + cube.Max.Z) * cube.CubeGrid.GridSize / 2.0;
            }

            double devX = 0;
            double devY = 0;
            double devZ = 0;

            for (int i = 0; i < blocks.Count; i++) {

                var cube = blocks[i];
                var dx = ((cube.Min.X + cube.Max.X) * cube.CubeGrid.GridSize / 2.0) - avgX;
                var dy = ((cube.Min.Y + cube.Max.Y) * cube.CubeGrid.GridSize / 2.0) - avgY;
                var dz = ((cube.Min.Z + cube.Max.Z) * cube.CubeGrid.GridSize / 2.0) - avgY;
                devX += dx * dx;
                devY += dy * dy;
                devZ += dz * dz;
            }
            devX = Math.Sqrt(devX / blocks.Count);
            devY = Math.Sqrt(devY / blocks.Count);
            devZ = Math.Sqrt(devZ / blocks.Count);

        }
        #endregion
    }
}
