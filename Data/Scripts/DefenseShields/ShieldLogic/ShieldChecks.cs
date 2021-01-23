using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        private void LosCheck()
        {
            LosCheckTick = uint.MaxValue;
            ShieldComp.CheckEmitters = true;
            FitChanged = true;
            _adjustShape = true;
        }

        private void Debug()
        {
            var name = Shield.CustomName;
            if (name.Length == 5 && name == "DEBUG")
            {
                if (_tick <= 1800) Shield.CustomName = "DEBUGAUTODISABLED";
                else UserDebug();
            }
        }

        private void UserDebug()
        {
            var active = false;
            lock (Session.Instance.ActiveShields) active = Session.Instance.ActiveShields.Contains(this);
            var message = $"User({MyAPIGateway.Multiplayer.Players.TryGetSteamId(Shield.OwnerId)}) Debugging\n" +
                          $"On:{DsState.State.Online} - Sus:{DsState.State.Suspended} - Act:{active}\n" +
                          $"Sleep:{Asleep} - Tick/Woke:{_tick}/{LastWokenTick}\n" +
                          $"Mode:{DsState.State.Mode} - Waking:{DsState.State.Waking}\n" +
                          $"Low:{DsState.State.Lowered} - Sl:{DsState.State.Sleeping}\n" +
                          $"Failed:{!NotFailed} - PNull:{MyResourceDist == null}\n" +
                          $"NoP:{DsState.State.NoPower} - PSys:{MyResourceDist?.SourcesEnabled}\n" +
                          $"Access:{DsState.State.ControllerGridAccess} - EmitterLos:{DsState.State.EmitterLos}\n" +
                          $"ProtectedEnts:{ProtectedEntCache.Count} - ProtectMyGrid:{Session.Instance.GlobalProtect.ContainsKey(MyGrid)}\n" +
                          $"ShieldMode:{ShieldMode} - pFail:{_powerFail}\n" +
                          $"Sink:{_sink.CurrentInputByType(GId)} - PFS:{_powerNeeded}/{ShieldMaxPower}\n" +
                          $"AvailPoW:{ShieldAvailablePower} - MTPoW:{_shieldMaintaintPower}\n" +
                          $"Pow:{_power} HP:{DsState.State.Charge}: {ShieldMaxCharge}";

            if (!_isDedicated) MyAPIGateway.Utilities.ShowNotification(message, 28800);
            else Log.Line(message);
        }

        private static void CreativeModeWarning()
        {
            if (Session.Instance.CreativeWarn || Session.Instance.Tick < 600) return;
            Session.Instance.CreativeWarn = true;
            const string message = "DefenseShields is not fully supported in\n" +
                                   "Creative Mode, due to unlimited power and \n" +
                                   "it will not operate as designed.\n";
            MyAPIGateway.Utilities.ShowNotification(message, 6720);
        }

        private readonly List<IMyCubeGrid> _tempSubGridList = new List<IMyCubeGrid>();


        private bool SubGridUpdateSkip()
        {
            _tempSubGridList.Clear();
            MyAPIGateway.GridGroups.GetGroup(MyGrid, GridLinkTypeEnum.Physical, _tempSubGridList);
            var newCount = _tempSubGridList.Count;
            var sameCount = newCount == _linkedGridCount;
            var oneAndSame = newCount == 1 && sameCount;

            if (oneAndSame && ShieldComp.LinkedGrids.ContainsKey(MyGrid))
                return true;

            if (sameCount) {

                for (int i = 0; i < _tempSubGridList.Count; i++) {
                    if (!ShieldComp.LinkedGrids.ContainsKey((MyCubeGrid)_tempSubGridList[i]))
                        return false;
                }
            }
            else return false;

            return true;
        }

        private void UpdateSubGrids()
        {
            _subUpdate = false;
            if (_subUpdatedTick  == _tick || SubGridUpdateSkip())
                return;

            _subUpdatedTick = _tick;
            ShieldComp.LinkedGrids.Clear();

            lock (SubLock)
            {
                foreach (var s in ShieldComp.SubGrids) Session.Instance.IdToBus.Remove(s.EntityId);
                ShieldComp.SubGrids.Clear();
                for (int i = 0; i < _tempSubGridList.Count; i++)
                {
                    var sub = _tempSubGridList[i];
                    if (sub == null) continue;
                    sub.Flags |= (EntityFlags)(1 << 31);
                    if (MyAPIGateway.GridGroups.HasConnection(MyGrid, sub, GridLinkTypeEnum.Mechanical))
                    {
                        ShieldComp.SubGrids.Add((MyCubeGrid)sub);
                        Session.Instance.IdToBus[sub.EntityId] = ShieldComp;
                    }
                    ShieldComp.LinkedGrids.TryAdd((MyCubeGrid)sub, new SubGridInfo(sub as MyCubeGrid, sub == MyGrid, false));
                }
            }
            _linkedGridCount = ShieldComp.LinkedGrids.Count;
            _blockChanged = true;
            _functionalChanged = true;
            _updateGridDistributor = true;
            _subTick = _tick;
        }

        private void BlockMonitor()
        {
            if (_blockChanged)
            {
                _blockEvent = true;
                _shapeEvent = true;
                LosCheckTick = _tick + 1800;
                if (_blockAdded) _shapeTick = _tick + 300;
                else _shapeTick = _tick + 1800;
            }
            if (_functionalChanged) _functionalEvent = true;

            if (_functionalAdded || _functionalRemoved)
            {
                _updateCap = true;
                _functionalAdded = false;
                _functionalRemoved = false;
            }

            _functionalChanged = false;

            _blockChanged = false;
            _blockRemoved = false;
            _blockAdded = false;
        }

        private void BlockChanged(bool backGround, bool forceCap = false)
        {
            if (_blockEvent)
            {
                var notReady = !FuncTask.IsComplete || DsState.State.Sleeping || DsState.State.Suspended;
                if (notReady) return;
                if (Session.Enforced.Debug == 3) Log.Line($"BlockChanged: functional:{_functionalEvent} - funcComplete:{FuncTask.IsComplete} - Sleeping:{DsState.State.Sleeping} - Suspend:{DsState.State.Suspended} - ShieldId [{Shield.EntityId}]");
                if (_functionalEvent) FunctionalChanged(backGround, forceCap);
                _blockEvent = false;
                _funcTick = _tick + 60;
            }
        }

        private void FunctionalChanged(bool backGround, bool forceCap)
        {
            if (_isServer && (_updateCap || forceCap))
                ComputeCap();

            if (backGround) FuncTask = MyAPIGateway.Parallel.StartBackground(BackGroundChecks);
            else BackGroundChecks();
            _functionalEvent = false;
        }

        private void BackGroundChecks()
        {
            var gridDistNeedUpdate = _updateGridDistributor || MyResourceDist?.SourcesEnabled == MyMultipleEnabledEnum.NoObjects;
            _updateGridDistributor = false;
            lock (SubLock)
            {
                _powerSources.Clear();
                _functionalBlocks.Clear();
                _batteryBlocks.Clear();
                _displayBlocks.Clear();

                foreach (var grid in ShieldComp.LinkedGrids.Keys)
                {
                    var mechanical = ShieldComp.SubGrids.Contains(grid);
                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (mechanical)
                        {
                            if (gridDistNeedUpdate)
                            {
                                var controller = block as MyShipController;
                                if (controller != null)
                                {
                                    var distributor = controller.GridResourceDistributor;
                                    if (distributor.SourcesEnabled != MyMultipleEnabledEnum.NoObjects)
                                    {
                                        if (Session.Enforced.Debug == 3) Log.Line($"Found MyGridDistributor from type:{block.BlockDefinition} - ShieldId [{Shield.EntityId}]");
                                        MyResourceDist = controller.GridResourceDistributor;
                                        gridDistNeedUpdate = false;
                                    }
                                }
                            }
                        }

                        if (!_isDedicated)
                        {
                            _functionalBlocks.Add(block);
                            var display = block as IMyTextPanel;
                            if (display != null) _displayBlocks.Add(display);
                        }

                        var battery = block as IMyBatteryBlock;
                        if (battery != null) _batteryBlocks.Add(battery);

                        var source = block.Components.Get<MyResourceSourceComponent>();
                        if (source == null) continue;

                        foreach (var type in source.ResourceTypes)
                        {
                            if (type != MyResourceDistributorComponent.ElectricityId) continue;
                            _powerSources.Add(source);
                            break;
                        }
                    }
                }
            }
        }

        private void GridOwnsController()
        {
            if (MyGrid.BigOwners.Count == 0)
            {
                DsState.State.ControllerGridAccess = false;
                return;
            }

            _gridOwnerId = MyGrid.BigOwners[0];
            _controllerOwnerId = MyCube.OwnerId;

            if (_controllerOwnerId == 0) MyCube.ChangeOwner(_gridOwnerId, MyOwnershipShareModeEnum.Faction);

            var controlToGridRelataion = MyCube.GetUserRelationToOwner(_gridOwnerId);
            DsState.State.InFaction = controlToGridRelataion == MyRelationsBetweenPlayerAndBlock.FactionShare;
            DsState.State.IsOwner = controlToGridRelataion == MyRelationsBetweenPlayerAndBlock.Owner;

            if (controlToGridRelataion != MyRelationsBetweenPlayerAndBlock.Owner && controlToGridRelataion != MyRelationsBetweenPlayerAndBlock.FactionShare)
            {
                if (DsState.State.ControllerGridAccess)
                {
                    DsState.State.ControllerGridAccess = false;
                    Shield.RefreshCustomInfo();
                    if (Session.Enforced.Debug == 4) Log.Line($"GridOwner: controller is not owned: {ShieldMode} - ShieldId [{Shield.EntityId}]");
                }
                DsState.State.ControllerGridAccess = false;
                return;
            }

            if (!DsState.State.ControllerGridAccess)
            {
                DsState.State.ControllerGridAccess = true;
                Shield.RefreshCustomInfo();
                if (Session.Enforced.Debug == 4) Log.Line($"GridOwner: controller is owned: {ShieldMode} - ShieldId [{Shield.EntityId}]");
            }
            DsState.State.ControllerGridAccess = true;
        }

        private bool SubGridSlaveControllerLink()
        {
            var notTime = !_tick60 && _subTick + 10 < _tick;
            if (notTime && _slavedToGrid != null) return true;
            if (IsStatic || (notTime && !_firstLoop)) return false;

            var mySize = MyGrid.PositionComp.LocalAABB.Size.Volume;
            var myEntityId = MyGrid.EntityId;
            foreach (var grid in ShieldComp.LinkedGrids.Keys)
            {
                if (grid == MyGrid) continue;
                ShieldGridComponent shieldComponent;
                if (grid.Components.TryGet(out shieldComponent) && shieldComponent?.DefenseShields != null && shieldComponent.DefenseShields.DsState.State.Online && shieldComponent.DefenseShields.IsWorking) {

                    var ds = shieldComponent.DefenseShields;
                    var otherSize = ds.MyGrid.PositionComp.LocalAABB.Size.Volume;
                    var otherEntityId = ds.MyGrid.EntityId;
                    if ((!IsStatic && ds.IsStatic) || mySize < otherSize || (mySize.Equals(otherEntityId) && myEntityId < otherEntityId))
                    {
                        _slavedToGrid = ds.MyGrid;
                        if (_slavedToGrid != null) return true;
                    }
                }
            }

            if (_slavedToGrid != null) {

                if (_slavedToGrid.IsInSameLogicalGroupAs(MyGrid))
                    ResetEntityTick = _tick + 1800;
            }
            _slavedToGrid = null;
            return false;
        }

        private bool FieldShapeBlocked()
        {
            ModulatorGridComponent modComp;
            MyGrid.Components.TryGet(out modComp);
            if (ShieldComp.Modulator == null || ShieldComp.Modulator.ModSet.Settings.ModulateVoxels || Session.Enforced.DisableVoxelSupport == 1) return false;

            var pruneSphere = new BoundingSphereD(DetectionCenter, BoundingRange);
            var pruneList = new List<MyVoxelBase>();
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref pruneSphere, pruneList);

            if (pruneList.Count == 0) return false;
            Icosphere.ReturnPhysicsVerts(DetectMatrixOutside, ShieldComp.PhysicsOutsideLow);
            foreach (var voxel in pruneList)
            {
                if (voxel.RootVoxel == null || voxel != voxel.RootVoxel) continue;
                if (!CustomCollision.VoxelContact(ShieldComp.PhysicsOutsideLow, voxel)) continue;

                Shield.Enabled = false;
                DsState.State.FieldBlocked = true;
                DsState.State.Message = true;
                if (Session.Enforced.Debug == 3) Log.Line($"Field blocked: - ShieldId [{Shield.EntityId}]");
                return true;
            }
            DsState.State.FieldBlocked = false;
            return false;
        }

        private void FailureDurations()
        {
            if (_overLoadLoop == 0 || _empOverLoadLoop == 0 || _reModulationLoop == 0)
            {
                if (DsState.State.Online || !WarmedUp)
                {
                    if (_overLoadLoop != -1)
                    {
                        DsState.State.Overload = true;
                        DsState.State.Message = true;
                    }

                    if (_empOverLoadLoop != -1)
                    {
                        DsState.State.EmpOverLoad = true;
                        DsState.State.Message = true;
                    }

                    if (_reModulationLoop != -1)
                    {
                        DsState.State.Remodulate = true;
                        DsState.State.Message = true;
                    }
                }
            }

            if (_reModulationLoop > -1)
            {
                _reModulationLoop++;
                if (_reModulationLoop == ReModulationCount)
                {
                    DsState.State.Remodulate = false;
                    _reModulationLoop = -1;
                }
            }

            if (_overLoadLoop > -1)
            {
                _overLoadLoop++;
                if (_overLoadLoop == ShieldDownCount - 1) ShieldComp.CheckEmitters = true;
                if (_overLoadLoop == ShieldDownCount)
                {
                    if (!DsState.State.EmitterLos)
                    {
                        DsState.State.Overload = false;
                        _overLoadLoop = -1;
                    }
                    else
                    {
                        DsState.State.Overload = false;
                        _overLoadLoop = -1;
                        var recharged = ShieldChargeRate * ShieldDownCount / 60;
                        DsState.State.Charge = MathHelper.Clamp(recharged, ShieldMaxCharge * 0.10f, ShieldMaxCharge * 0.25f);
                    }
                }
            }

            if (_empOverLoadLoop > -1)
            {
                _empOverLoadLoop++;
                if (_empOverLoadLoop == EmpDownCount - 1) ShieldComp.CheckEmitters = true;
                if (_empOverLoadLoop == EmpDownCount)
                {
                    if (!DsState.State.EmitterLos)
                    {
                        DsState.State.EmpOverLoad = false;
                        _empOverLoadLoop = -1;
                    }
                    else
                    {
                        DsState.State.EmpOverLoad = false;
                        _empOverLoadLoop = -1;
                        _empOverLoad = false;
                        var recharged = ShieldChargeRate * EmpDownCount / 60;
                        DsState.State.Charge = MathHelper.Clamp(recharged, ShieldMaxCharge * 0.25f, ShieldMaxCharge * 0.62f);
                    }
                }
            }
        }
    }
}
