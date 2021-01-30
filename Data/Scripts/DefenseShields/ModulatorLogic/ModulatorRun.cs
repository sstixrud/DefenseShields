using VRage.Utils;

namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game.Components;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "LargeShieldModulator", "SmallShieldModulator")]
    public partial class Modulators : MyGameLogicComponent
    {

        public override void OnAddedToContainer()
        {
            if (!ContainerInited)
            {
                PowerPreInit();
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                if (!MyAPIGateway.Utilities.IsDedicated) NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                else NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                Modulator = (IMyUpgradeModule)Entity;
                ContainerInited = true;
            }
            if (Entity.InScene) OnAddedToScene();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                StorageSetup();
            }
            catch (Exception ex) { Log.Line($"Exception in EntityInit: {ex}"); }
        }

        public override void OnAddedToScene()
        {
            try
            {
                MyGrid = (MyCubeGrid)Modulator.CubeGrid;
                MyCube = Modulator as MyCubeBlock;
                RegisterEvents();
                if (Session.Enforced.Debug == 3) Log.Line($"OnAddedToScene: - ModulatorId [{Modulator.EntityId}]");
                if (!MainInit) return;
                ResetComp();
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToScene: {ex}"); }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            try
            {
                if (!_bInit) BeforeInit();
                else if (_bCount < SyncCount * _bTime)
                {
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    if (ModulatorComp?.Modulator?.MyGrid == MyGrid) _bCount++;
                }
                else _readyToSync = true;
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateOnceBeforeFrame: {ex}"); }
        }

        public override void UpdateBeforeSimulation()
        {
            try
            {
                _tick = Session.Instance.Tick;
                _tock33 = _tick % 33 == 0;
                _tock34 = _tick % 34 == 0;
                if (_count++ == 59)
                {
                    _count = 0;
                    _tock60 = true;
                }
                else _tock60 = false;

                var wait = _isServer && _count != 0 && ModState.State.Backup;

                MyGrid = MyCube.CubeGrid;
                if (wait || MyGrid?.Physics == null) return;

                Timing();

                if (!ModulatorReady())
                {
                    ModulatorOff();
                    return;
                }
                ModulatorOn();
                if (!_isDedicated && UtilsStatic.DistanceCheck(Modulator, 1000, 1))
                {
                    var blockCam = MyCube.PositionComp.WorldVolume;
                    if (MyAPIGateway.Session.Camera.IsInFrustum(ref blockCam) && ModState.State.Online) BlockMoveAnimation();
                }

                if (_isServer) UpdateStates();
                _firstRun = false;
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateBeforeSimulation: {ex}"); }
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                _tick = Session.Instance.Tick;
                if (_count++ == 5)
                {
                    _count = 0;
                    _tock60 = true;
                }
                else _tock60 = false;
                _tock33 = _count == 3;
                _tock34 = _count == 4;

                var wait = _isServer && _count != 0 && ModState.State.Backup;

                MyGrid = MyCube.CubeGrid;
                if (wait || MyGrid?.Physics == null) return;
                Timing();

                if (!ModulatorReady())
                {
                    ModulatorOff();
                    return;
                }
                ModulatorOn();

                if (_isServer) UpdateStates();

                _firstRun = false;
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateBeforeSimulation10: {ex}"); }
        }

        public override bool IsSerialized()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (Modulator.Storage != null)
                {
                    ModState.SaveState();
                    ModSet.SaveSettings();
                }
            }
            return false;
        }

        public override void OnRemovedFromScene()
        {
            try
            {
                if (Session.Instance.Modulators.Contains(this)) Session.Instance.Modulators.Remove(this);
                if (ShieldComp?.Modulator == this)
                {
                    ShieldComp.Modulator = null;
                }

                if (ModulatorComp?.Modulator == this)
                {
                    ModulatorComp.Modulator = null;
                    ModulatorComp = null;
                }
                RegisterEvents(false);
            }
            catch (Exception ex) { Log.Line($"Exception in OnRemovedFromScene: {ex}"); }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene) OnRemovedFromScene();
        }

        public override void Close()
        {
            try
            {
                if (Session.Instance.Modulators.Contains(this)) Session.Instance.Modulators.Remove(this);
                if (ShieldComp?.Modulator == this)
                {
                    ShieldComp.Modulator = null;
                }
                ShieldComp = null;

                if (ModulatorComp?.Modulator == this)
                {
                    ModulatorComp.Modulator = null;
                }
                ModulatorComp = null;

                if (Sink != null)
                {
                    ResourceInfo = new MyResourceSinkInfo
                    {
                        ResourceTypeId = _gId,
                        MaxRequiredInput = 0f,
                        RequiredInputFunc = null
                    };
                    Sink.Init(MyStringHash.GetOrCompute("Utility"), ResourceInfo);
                    Sink = null;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in Close: {ex}"); }
            base.Close();
        }

        public override void MarkForClose()
        {
            try
            {
            }
            catch (Exception ex) { Log.Line($"Exception in MarkForClose: {ex}"); }
            base.MarkForClose();
        }

    }
}
