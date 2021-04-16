namespace DefenseShields
{
    using System;
    using System.Text;
    using Support;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Weapons;
    using VRage;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;

    public partial class DefenseShields
    {
        private void RegisterEvents(bool register = true)
        {
            if (register)
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    ((MyCubeGrid)Shield.CubeGrid).OnBlockOwnershipChanged += OwnerChanged;
                    MyEntities.OnEntityAdd += OnEntityAdd;
                    MyEntities.OnEntityRemove += OnEntityRemove;
                }

                ((MyCubeGrid)Shield.CubeGrid).OnHierarchyUpdated += HierarchyChanged;
                RegisterGridEvents();
                Shield.AppendingCustomInfo += AppendingCustomInfo;
                _sink.CurrentInputChanged += CurrentInputChanged;

            }
            else
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    ((MyCubeGrid)Shield.CubeGrid).OnBlockOwnershipChanged -= OwnerChanged;
                    MyEntities.OnEntityAdd -= OnEntityAdd;
                    MyEntities.OnEntityRemove -= OnEntityRemove;
                }

                ((MyCubeGrid)Shield.CubeGrid).OnHierarchyUpdated -= HierarchyChanged;
                RegisterGridEvents(false);
                Shield.AppendingCustomInfo -= AppendingCustomInfo;
                if (_sink != null)_sink.CurrentInputChanged -= CurrentInputChanged;
            }
        }

        private void RegisterGridEvents(bool register = true, MyCubeGrid grid = null)
        {
            if (grid == null) grid = MyGrid;
            if (register)
            {
                grid.OnBlockAdded += BlockAdded;
                grid.OnBlockRemoved += BlockRemoved;
                grid.OnFatBlockAdded += FatBlockAdded;
                grid.OnFatBlockRemoved += FatBlockRemoved;
                grid.OnGridSplit += GridSplit;
            }
            else
            {
                grid.OnBlockAdded -= BlockAdded;
                grid.OnBlockRemoved -= BlockRemoved;
                grid.OnFatBlockAdded -= FatBlockAdded;
                grid.OnFatBlockRemoved -= FatBlockRemoved;
                grid.OnGridSplit -= GridSplit;
            }
        }

        private void OwnerChanged(MyCubeGrid myCubeGrid)
        {
            try
            {
                if (!_isServer || MyCube == null || MyGrid == null || MyCube.OwnerId == _controllerOwnerId && MyGrid.BigOwners.Count != 0 && MyGrid.BigOwners[0] == _gridOwnerId) return;
                GridOwnsController();
            }
            catch (Exception ex) { Log.Line($"Exception in Controller OwnerChanged: {ex}"); }
        }

        private void OnEntityAdd(MyEntity myEntity)
        {
            try
            {
                if (DsState.State.ReInforce) return;
                if (myEntity?.Physics == null || !myEntity.InScene || myEntity.MarkedForClose || myEntity is MyFloatingObject || myEntity is IMyEngineerToolBase) return;
                var isMissile = myEntity.DefinitionId.HasValue && myEntity.DefinitionId.Value.TypeId == typeof(MyObjectBuilder_Missile);
                if (!isMissile && !(myEntity is MyCubeGrid)) return;

                var aabb = myEntity.PositionComp.WorldAABB;
                if (!ShieldBox3K.Intersects(ref aabb)) return;

                Asleep = false;
                if (_isServer && isMissile) Missiles.Add(myEntity);
            }
            catch (Exception ex) { Log.Line($"Exception in Controller OnEntityAdd: {ex}"); }
        }

        private void OnEntityRemove(MyEntity myEntity)
        {
            try
            {
                if (myEntity == null || !_isServer || DsState.State.ReInforce) return;

                if (!(myEntity.DefinitionId.HasValue && myEntity.DefinitionId.Value.TypeId == typeof(MyObjectBuilder_Missile))) return;

                Missiles.Remove(myEntity);
                FriendlyMissileCache.Remove(myEntity);
            }
            catch (Exception ex) { Log.Line($"Exception in Controller OnEntityRemove: {ex}"); }
        }

        private void GridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            if (Asleep)
                Awake();

            newGrid.RecalculateOwners();
            _subUpdate = true;
        }

        private void HierarchyChanged(MyCubeGrid myCubeGrid = null)
        {
            _subUpdate = true;
        }

        private void BlockAdded(IMySlimBlock mySlimBlock)
        {
            try
            {
                if (Asleep)
                    Awake();

                _blockAdded = true;
                _blockChanged = true;
            }
            catch (Exception ex) { Log.Line($"Exception in Controller BlockAdded: {ex}"); }
        }

        private void BlockRemoved(IMySlimBlock mySlimBlock)
        {
            try
            {
                if (Asleep)
                    Awake();

                _blockRemoved = true;
                _blockChanged = true;
            }
            catch (Exception ex) { Log.Line($"Exception in Controller BlockRemoved: {ex}"); }
        }

        private void FatBlockAdded(MyCubeBlock myCubeBlock)
        {
            try
            {
                _functionalAdded = true;
                _functionalChanged = true;
                if (MyResourceDist == null)
                {
                    var controller = myCubeBlock as MyShipController;
                    if (controller != null)
                        if (controller.GridResourceDistributor.SourcesEnabled != MyMultipleEnabledEnum.NoObjects) _updateGridDistributor = true;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in Controller FatBlockAdded: {ex}"); }
        }

        private void FatBlockRemoved(MyCubeBlock myCubeBlock)
        {
            try
            {
                _functionalRemoved = true;
                _functionalChanged = true;
            }
            catch (Exception ex) { Log.Line($"Exception in Controller FatBlockRemoved: {ex}"); }
        }

        internal string GetShieldStatus()
        {
            if (!DsState.State.Online && !MyCube.IsFunctional) return "[Controller Faulty]";
            if (!DsState.State.Online && !MyCube.IsWorking) return "[Controller Offline]";
            if (!DsState.State.Online && DsState.State.NoPower) return "[Insufficient Power]";
            if (!DsState.State.Online && DsState.State.Overload) return "[Overloaded]";
            if (!DsState.State.Online && DsState.State.EmpOverLoad) return "[Emp Overload]";
            if (!DsState.State.ControllerGridAccess) return "[Invalid Owner]";
            if (DsState.State.Waking) return "[Coming Online]";
            if (DsState.State.Suspended || DsState.State.Mode == 4) return "[Controller Standby]";
            if (DsState.State.Lowered) return "[Shield Down]";
            if (DsState.State.Sleeping) return "[Suspended]";
            if (!DsState.State.EmitterLos || DsState.State.ActiveEmitterId == 0) return "[Emitter Failure]";
            if (!DsState.State.Online) return "[Shield Offline]";
            return "[Shield Up]";
        }

        internal uint LastCustomInfoUpdate;
        internal const string MaxString = ": MaxHp: ";
        internal const string CapString = ": CapHp: ";

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                LastCustomInfoUpdate = _tick;
                var secToFull = 0;
                var shieldPercent = !DsState.State.Online ? 0f : 100f;

                if (DsState.State.Charge < ShieldMaxCharge) shieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;

                if (ShieldChargeRate > 0) {
                    var toMax = ShieldMaxCharge - DsState.State.Charge;
                    var secs = toMax / ShieldChargeRate;

                    if (secs.Equals(1)) secToFull = 0;
                    else secToFull = (int)secs;
                }

                var shieldPowerNeeds = _powerNeeded;
                var powerUsage = shieldPowerNeeds;
                var initStage = 1;
                var validEmitterId = DsState.State.ActiveEmitterId != 0;
                if (WarmedUp) initStage = 4;
                else if (Warming) initStage = 3;
                else if (_allInited) initStage = 2;
                var  maxString = _shieldCapped ? CapString : MaxString;
                var hpValue = (ShieldMaxCharge * ConvToHp);


                var status = GetShieldStatus();
                if (status == "[Shield Up]" || status == "[Shield Down]" || status == "[Shield Offline]") {

                    var redirectedSides = ShuntedSideCount();
                    var bonusAmount = redirectedSides * 20;
                    stringBuilder.Append(status + maxString +  hpValue.ToString("N0") +
                                         "\n[Shield HP__]: " + (DsState.State.Charge * ConvToHp).ToString("N0") + " (" + shieldPercent.ToString("0") + "%)" +
                                         "\n[HP Per Sec_]: " + (ShieldChargeRate * ConvToHp).ToString("N0") +
                                         "\n[Damage In__]: " + _damageReadOut.ToString("N0") +
                                         "\n[Charge Rate]: " + ShieldChargeRate.ToString("0.0") + " Mw" +
                                         "\n[Full Charge_]: " + secToFull.ToString("N0") + "s" +
                                         "\n[Over Heated]: " + DsState.State.Heat.ToString("0") + "%" +
                                         "\n[Shunted Sides]: " + redirectedSides.ToString("0") + " ("  + bonusAmount + "%)" +
                                         "\n[Maintenance]: " + _shieldMaintaintPower.ToString("0.0") + " Mw" +
                                         "\n[Shield Power]: " + ShieldCurrentPower.ToString("0.0") + " Mw" +
                                         "\n[Power Use]: " + powerUsage.ToString("0.0") + " (" + GridMaxPower.ToString("0.0") + ")Mw");
                }
                else {

                    stringBuilder.Append("Shield Status " + status +
                                         "\n" +
                                         "\n[Init Stage]: " + initStage + " of 4" +
                                         "\n[Emitter Ok]: " + validEmitterId +
                                         "\n[HP Stored]: " + (DsState.State.Charge * ConvToHp).ToString("N0") + " (" + shieldPercent.ToString("0") + "%)" +
                                         "\n[Shield Mode]: " + ShieldMode +
                                         "\n[Emitter LoS]: " + (DsState.State.EmitterLos) +
                                         "\n[Last Woken]: " + LastWokenTick + "/" + _tick +
                                         "\n[Waking Up]: " + DsState.State.Waking +
                                         "\n[Grid Owns Controller]: " + DsState.State.IsOwner +
                                         "\n[In Grid's Faction]: " + DsState.State.InFaction);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in Controller AppendingCustomInfo: {ex}"); }
        }
    }
}