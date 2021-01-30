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

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "LargeEnhancer", "SmallEnhancer")]
    public partial class Enhancers : MyGameLogicComponent
    {

        public override void OnAddedToContainer()
        {
            if (!ContainerInited)
            {
                PowerPreInit();
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                if (!MyAPIGateway.Utilities.IsDedicated) NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                else NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                Enhancer = (IMyUpgradeModule)Entity;
                ContainerInited = true;
                if (Session.Enforced.Debug == 3) Log.Line($"ContainerInited:  EnhancerId [{Enhancer.EntityId}]");
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

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            try
            {
                if (!_bInit) BeforeInit();
                else if (_bCount < SyncCount * _bTime)
                {
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    if (ShieldComp?.DefenseShields?.MyGrid == MyGrid) _bCount++;
                }
                else _readyToSync = true;
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateOnceBeforeFrame: {ex}"); }
        }

        private void BeforeInit()
        {
            if (Enhancer.CubeGrid.Physics == null) return;
            Session.Instance.Enhancers.Add(this);
            PowerInit();
            Entity.TryGetSubpart("Rotor", out _subpartRotor);
            _isServer = Session.Instance.IsServer;
            _isDedicated = Session.Instance.DedicatedServer;
            Enhancer.RefreshCustomInfo();
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            _bTime = _isDedicated ? 10 : 1;
            _bInit = true;
        }

        public override bool IsSerialized()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (Enhancer.Storage != null) EnhState.SaveState();
            }
            return false;
        }

        public override void OnAddedToScene()
        {
            try
            {
                MyGrid = (MyCubeGrid)Enhancer.CubeGrid;
                MyCube = Enhancer as MyCubeBlock;
                RegisterEvents();
                if (Session.Enforced.Debug == 3) Log.Line($"OnAddedToScene: - EnhancerId [{Enhancer.EntityId}]");
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToScene: {ex}"); }
        }

        public override void UpdateBeforeSimulation()
        {
            try
            {
                _tick = Session.Instance.Tick;
                _tick60 = _tick % 60 == 0;
                var wait = _isServer && !_tick60 && EnhState.State.Backup;

                MyGrid = MyCube.CubeGrid;
                if (wait || MyGrid?.Physics == null) return;

                Timing();
                if (!EnhancerReady()) return;
                if (!_isDedicated && UtilsStatic.DistanceCheck(Enhancer, 1000, 1))
                {
                    var blockCam = MyCube.PositionComp.WorldVolume;
                    if (MyAPIGateway.Session.Camera.IsInFrustum(ref blockCam) && EnhState.State.Online) BlockMoveAnimation();
                }
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateBeforeSimulation: {ex}"); }
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                _tick = Session.Instance.Tick;
                if (_count++ == 5) _count = 0;
                var wait = _isServer && _count != 0 && EnhState.State.Backup;

                MyGrid = MyCube.CubeGrid;
                if (wait || MyGrid?.Physics == null) return;

                EnhancerReady();
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateBeforeSimulation10: {ex}"); }
        }

        public override void OnRemovedFromScene()
        {
            try
            {
                if (Session.Instance.Enhancers.Contains(this)) Session.Instance.Enhancers.Remove(this);
                if (ShieldComp?.Enhancer == this)
                {
                    ShieldComp.Enhancer = null;
                }
                RegisterEvents(false);

            }
            catch (Exception ex) { Log.Line($"Exception in OnRemovedFromScene: {ex}"); }
        }

        public override void Close()
        {
            try
            {
                base.Close();
                if (Session.Instance.Enhancers.Contains(this)) Session.Instance.Enhancers.Remove(this);
                if (ShieldComp?.Enhancer == this)
                {
                    ShieldComp.Enhancer = null;
                }
                ShieldComp = null;

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
        }

        public override void MarkForClose()
        {
            try
            {
                base.MarkForClose();
            }
            catch (Exception ex) { Log.Line($"Exception in MarkForClose: {ex}"); }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene) OnRemovedFromScene();
        }
    }
}
