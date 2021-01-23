using System.Threading;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace DefenseShields
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Support;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Weapons;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRageMath;

    public partial class Session
    {
        #region WebMonitor
        internal void WebMonitor()
        {
            try
            {
                MyAPIGateway.Parallel.ForEach(FunctionalShields.Keys, s =>
                {
                    if (s.MarkedForClose || !s.Warming) return;
                    var reInforce = s.DsState.State.ReInforce;
                    if (!IsServer)
                    {
                        if (reInforce != s.ReInforcedShield)
                        {
                            lock (s.SubLock) foreach (var sub in s.ShieldComp.SubGrids) EntRefreshQueue.Enqueue(sub);
                            s.ReInforcedShield = reInforce;
                        }

                        if (EntSlotTick && RefreshCycle == s.MonitorSlot)
                        {
                            var newSubClient = false;
                            
                            var monitorListClient = ListMyEntityPool.Get();
                            
                            MonitorRefreshTasks(s, ref monitorListClient, reInforce, ref newSubClient);
                            
                            monitorListClient.Clear();
                            ListMyEntityPool.Return(monitorListClient);

                        }
                        s.TicksWithNoActivity = 0;
                        s.LastWokenTick = Tick;
                        s.Asleep = false;
                        return;
                    }

                    bool shieldActive;
                    lock (ActiveShields) shieldActive = ActiveShields.Contains(s);

                    if (s.LostPings > 59)
                    {
                        if (shieldActive)
                        {
                            if (Enforced.Debug >= 2) Log.Line("Logic Paused by lost pings");
                            lock (ActiveShields) ActiveShields.Remove(s);
                            s.WasPaused = true;
                        }
                        s.Asleep = false;
                        return;
                    }
                    if (Enforced.Debug >= 2 && s.LostPings > 0) Log.Line($"Lost Logic Pings:{s.LostPings}");
                    
                    if (shieldActive)
                        s.LostPings++;

                    if (s.Asleep && EmpStore.Count != 0 && Vector3D.DistanceSquared(s.DetectionCenter, EmpWork.EpiCenter) <= SyncDistSqr)
                    {
                        s.TicksWithNoActivity = 0;
                        s.LastWokenTick = Tick;
                        s.Asleep = false;
                        return;
                    }

                    if (!shieldActive && s.LostPings > 59)
                    {
                        s.Asleep = true;
                        return;
                    }

                    var newSub = false;
                    
                    var monitorList = ListMyEntityPool.Get();
                    if (EntSlotTick && RefreshCycle == s.MonitorSlot) MonitorRefreshTasks(s, ref monitorList, reInforce, ref newSub);

                    if (reInforce) return;
                    if (Tick < s.LastWokenTick + 400 || s.Missiles.Count > 0)
                    {
                        s.Asleep = false;
                        return;
                    }

                    if (s.GridIsMobile && s.MyGrid.Physics.IsMoving)
                    {
                        s.LastWokenTick = Tick;
                        s.Asleep = false;
                        return;
                    }

                    if (!s.PlayerByShield && !s.MoverByShield && !s.NewEntByShield)
                    {
                        if (s.TicksWithNoActivity++ % EntCleanCycle == 0) s.EntCleanUpTime = true;
                        if (shieldActive && !s.WasPaused && Tick > 1200)
                        {
                            if (Enforced.Debug >= 2) Log.Line($"Logic Paused by monitor");
                            lock (ActiveShields) ActiveShields.Remove(s);
                            s.WasPaused = true;
                            s.Asleep = false;
                            s.TicksWithNoActivity = 0;
                            s.LastWokenTick = Tick;
                        }
                        else s.Asleep = true;
                        return;
                    }

                    var intersect = false;
                    if (!(EntSlotTick && RefreshCycle == s.MonitorSlot)) MyGamePruningStructure.GetTopmostEntitiesInBox(ref s.WebBox, monitorList, MyEntityQueryType.Dynamic);
                    for (int i = 0; i < monitorList.Count; i++)
                    {
                        var ent = monitorList[i];

                        if (ent.Physics == null || ent.Physics.IsPhantom || !(ent is MyCubeGrid || ent is IMyCharacter || ent is IMyMeteor)) continue;
                        if (ent.Physics.IsMoving)
                        {
                            if (s.WebBox.Intersects(ent.PositionComp.WorldAABB))
                            {
                                intersect = true;
                                break;
                            }
                        }
                    }

                    monitorList.Clear();
                    ListMyEntityPool.Return(monitorList);

                    if (!intersect)
                    {
                        s.Asleep = true;
                        return;
                    }
                    s.TicksWithNoActivity = 0;
                    s.LastWokenTick = Tick;
                    s.Asleep = false;
                });

                if (Tick % 180 == 0 && Tick > 1199) {

                    foreach (var info in _globalEntTmp) {

                        if (Tick - 540 > info.Value) {

                            var ent = info.Key;
                            EntRefreshQueue.Enqueue(ent);
                            uint value;
                            _globalEntTmp.TryRemove(ent, out value);
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in WebMonitor: {ex}"); }
        }

        internal void MonitorRefreshTasks(DefenseShields s, ref List<MyEntity> monitorList, bool reInforce, ref bool newSub)
        {
            if (reInforce)
            {
                var newMode = !s.ReInforcedShield;
                if (!newMode) return;

                HashSet<MyCubeGrid> subs;
                lock (s.SubLock)
                {
                    subs = SetMyCubeGridPool.Get();
                    subs.UnionWith(s.ShieldComp.SubGrids);
                }
                foreach (var sub in subs)
                {
                    if (!_globalEntTmp.ContainsKey(sub)) newSub = true;
                    EntRefreshQueue.Enqueue(sub);
                    if (!s.WasPaused) _globalEntTmp[sub] = Tick;
                }
                subs.Clear();
                SetMyCubeGridPool.Return(subs);

                s.ReInforcedShield = true;
                s.TicksWithNoActivity = 0;
                s.LastWokenTick = Tick;
                s.Asleep = false;
            }
            else
            {
                var newMode = false;
                if (s.ReInforcedShield)
                {
                    HashSet<MyCubeGrid> subs;
                    lock (s.SubLock)
                    {
                        subs = SetMyCubeGridPool.Get();
                        subs.UnionWith(s.ShieldComp.SubGrids);
                    } 
                    foreach (var sub in subs)
                    {
                        EntRefreshQueue.Enqueue(sub);
                        if (!s.WasPaused) _globalEntTmp[sub] = Tick;
                    }
                    subs.Clear();
                    SetMyCubeGridPool.Return(subs);

                    s.ReInforcedShield = false;
                    s.TicksWithNoActivity = 0;
                    s.LastWokenTick = Tick;
                    s.Asleep = false;
                    newMode = true;
                }

                if (!newMode)
                {
                    var foundNewEnt = false;
                    var disableVoxels = Enforced.DisableVoxelSupport == 1 || s.ShieldComp.Modulator == null || s.ShieldComp.Modulator.ModSet.Settings.ModulateVoxels;
                    MyGamePruningStructure.GetTopmostEntitiesInBox(ref s.WebBox, monitorList);
                    if (!s.WasPaused)
                    {
                        foreach (var ent in monitorList)
                        {
                            var voxel = ent as MyVoxelBase;
                            if (ent == null || ent.MarkedForClose || (voxel == null && (ent.Physics == null || ent.Physics.IsPhantom || ent.DefinitionId == null)) || (!s.GridIsMobile && voxel != null) || (disableVoxels && voxel != null) || (voxel != null && voxel != voxel.RootVoxel))
                            {
                                continue;
                            }

                            if (ent is IMyFloatingObject || ent is IMyEngineerToolBase || !s.WebSphere.Intersects(ent.PositionComp.WorldVolume)) continue;

                            if (CustomCollision.NewObbPointsInShield(ent, s.DetectMatrixOutsideInv) > 0)
                            {
                                if (!_globalEntTmp.ContainsKey(ent))
                                {
                                    foundNewEnt = true;
                                    s.Asleep = false;
                                }

                                _globalEntTmp[ent] = Tick;
                            }
                            s.NewEntByShield = foundNewEnt;
                        }
                    }
                    else s.NewEntByShield = false;

                    if (!s.NewEntByShield)
                    {
                        var foundPlayer = false;
                        foreach (var player in Players.Values)
                        {
                            var character = player.Character;
                            if (character == null) continue;

                            if (Vector3D.DistanceSquared(character.PositionComp.WorldMatrixRef.Translation, s.DetectionCenter) < SyncDistSqr)
                            {
                                foundPlayer = true;
                                break;
                            }
                        }
                        s.PlayerByShield = foundPlayer;
                    }
                    if (!s.PlayerByShield)
                    {
                        s.MoverByShield = false;
                        var newMover = false;
                        
                        var moverList = ListMyEntityPool.Get();
                        MyGamePruningStructure.GetTopMostEntitiesInBox(ref s.ShieldBox3K, moverList, MyEntityQueryType.Dynamic);
                        for (int i = 0; i < moverList.Count; i++)
                        {
                            var ent = moverList[i];

                            var meteor = ent as IMyMeteor;
                            if (meteor != null)
                            {
                                if (CustomCollision.FutureIntersect(s, ent, s.DetectMatrixOutside, s.DetectMatrixOutsideInv))
                                {
                                    if (Enforced.Debug >= 2) Log.Line($"[Future Intersecting Meteor] distance from shieldCenter: {Vector3D.Distance(s.DetectionCenter, ent.WorldMatrix.Translation)} - waking:");
                                    newMover = true;
                                    break;
                                }
                                continue;
                            }

                            if (!(ent.Physics == null || ent.Physics.IsPhantom || ent is MyCubeGrid || ent is IMyCharacter)) continue;
                            var entPos = ent.PositionComp.WorldAABB.Center;

                            var keyFound = s.EntsByMe.ContainsKey(ent);
                            if (keyFound)
                            {
                                if (!s.EntsByMe[ent].Pos.Equals(entPos, 1e-3))
                                {
                                    MoverInfo moverInfo;
                                    s.EntsByMe.TryRemove(ent, out moverInfo);
                                    s.EntsByMe.TryAdd(ent, new MoverInfo(entPos, Tick));
                                    if (moverInfo.CreationTick == Tick - 1)
                                    {
                                        if (Enforced.Debug >= 3 && s.WasPaused) Log.Line($"[Moved] Ent:{ent.DebugName} - howMuch:{Vector3D.Distance(entPos, s.EntsByMe[ent].Pos)} - ShieldId [{s.Shield.EntityId}]");
                                        newMover = true;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                if (Enforced.Debug >= 3) Log.Line($"[NewMover] Ent:{ent.DebugName} - ShieldId [{s.Shield.EntityId}]");
                                s.EntsByMe.TryAdd(ent, new MoverInfo(entPos, Tick));
                            }
                        }
                        moverList.Clear();
                        ListMyEntityPool.Return(moverList);

                        s.MoverByShield = newMover;
                    }

                    if (Tick < s.LastWokenTick + 400)
                    {
                        s.Asleep = false;
                        return;
                    }
                }

                if (s.EntCleanUpTime)
                {
                    s.EntCleanUpTime = false;

                    foreach (var info in s.EntsByMe) {

                        if (!info.Key.InScene || info.Key.MarkedForClose || Tick - info.Value.CreationTick > EntMaxTickAge) {
                            MoverInfo mInfo;
                            s.EntsByMe.TryRemove(info.Key, out mInfo);
                        }
                    }
                }
            }
        }
        #endregion

        #region Timings / LoadBalancer
        private void Timings()
        {
            Tick = (uint)(Session.ElapsedPlayTime.TotalMilliseconds * TickTimeDiv);
            Tick20 = Tick % 20 == 0;
            Tick30 = Tick % 30 == 0;
            Tick60 = Tick % 60 == 0;
            Tick60 = Tick % 60 == 0;
            Tick180 = Tick % 180 == 0;
            Tick300 = Tick % 300 == 0;
            Tick600 = Tick % 600 == 0;
            Tick1800 = Tick % 1800 == 0;

            if (_count++ == 59)
            {
                _count = 0;
                _lCount++;
                if (_lCount == 10)
                {
                    _lCount = 0;
                    _eCount++;
                    if (_eCount == 10)
                    {
                        _eCount = 0;
                        _previousEntId = -1;
                    }
                }
            }
            if (!GameLoaded)
            {
                if (!MiscLoaded)
                {
                    if (SessionReady && GlobalProtect.Count > 0 && (IsServer || !IsServer && ClientLoadCount++ > 60))
                    {
                        UtilsStatic.GetDefinitons();
                        if (!IsServer) Players.TryAdd(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.Player);
                        Api.Init();
                        MiscLoaded = true;
                        GameLoaded = true;
                    }
                }

                if (MiscLoaded && !WarHeadLoaded && WarTerminalReset != null)
                {
                    WarTerminalReset.ShowInTerminal = true;
                    WarTerminalReset = null;
                    WarHeadLoaded = true;
                    GameLoaded = true;
                }
            }

            if (EmpWork.EventRunning && EmpWork.Computed) EmpWork.EventComplete();

            if (Tick20)
            {
                Scale();
                EntSlotTick = Tick % (180 / EntSlotScaler) == 0;
                if (EntSlotTick || FastRefresh) LoadBalancer();
            }
            else EntSlotTick = false;
        }

        internal static int GetSlot()
        {
            if (++_entSlotAssigner >= Instance.EntSlotScaler) _entSlotAssigner = 0;
            return _entSlotAssigner;
        }

        private void Scale()
        {
            if (Tick < 600) return;
            var oldScaler = EntSlotScaler;
            var globalProtCnt = GlobalProtect.Count;

            if (globalProtCnt <= 25) EntSlotScaler = 1;
            else if (globalProtCnt <= 50) EntSlotScaler = 2;
            else if (globalProtCnt <= 75) EntSlotScaler = 3;
            else if (globalProtCnt <= 100) EntSlotScaler = 4;
            else if (globalProtCnt <= 150) EntSlotScaler = 5;
            else if (globalProtCnt <= 200) EntSlotScaler = 6;
            else EntSlotScaler = 9;

            if (EntSlotScaler < MinScaler) EntSlotScaler = MinScaler;

            if (oldScaler != EntSlotScaler)
            {
                GlobalProtect.Clear();
                ProtSets.Clean();
                foreach (var s in FunctionalShields.Keys)
                {
                    s.AssignSlots();
                    s.Asleep = false;
                }
                foreach (var c in Controllers)
                {
                    if (FunctionalShields.ContainsKey(c)) continue;
                    c.AssignSlots();
                    c.Asleep = false;
                }
                ScalerChanged = true;
            }
            else ScalerChanged = false;
        }

        private void LoadBalancer()
        {
            FastRefresh = false;
            var shieldsWaking = 0;
            var entsUpdated = 0;
            var entsremoved = 0;
            var entsLostShield = 0;
            if (EntSlotTick && ++RefreshCycle >= EntSlotScaler) RefreshCycle = 0;
            MyEntity ent;
            while (EntRefreshQueue.TryDequeue(out ent))
            {
                MyProtectors myProtector;
                if (!GlobalProtect.TryGetValue(ent, out myProtector)) continue;

                var entShields = myProtector.Shields;
                var refreshCount = 0;
                DefenseShields iShield = null;
                var removeIShield = false;

                foreach (var s in entShields)
                {
                    if (s.WasPaused) continue;
                    lock (s.SubLock)
                    {
                        if (s.DsState.State.ReInforce && s.ShieldComp.SubGrids.Contains(ent))
                        {
                            iShield = s;
                            refreshCount++;
                        }
                        else if (!ent.InScene || ent.MarkedForClose || !s.ResetEnts(ent, Tick))
                        {
                            myProtector.Shields.Remove(s);
                            entsLostShield++;
                        }
                        else refreshCount++;
                    }

                    if (iShield == null && myProtector.IntegrityShield == s)
                    {
                        removeIShield = true;
                        myProtector.IntegrityShield = null;
                    }

                    var detectedStates = s.PlayerByShield || s.MoverByShield || Tick <= s.LastWokenTick + 580 || iShield != null || removeIShield;
                    if (ScalerChanged || detectedStates)
                    {
                        s.Asleep = false;
                        shieldsWaking++;
                    }
                }

                if (iShield != null)
                {
                    myProtector.Shields.Remove(iShield);
                    myProtector.IntegrityShield = iShield;
                }

                myProtector.Shields.ApplyChanges();

                if (refreshCount == 0)
                {
                    GlobalProtect.Remove(ent);
                    ProtSets.Return(myProtector);
                    entsremoved++;
                }
                else entsUpdated++;
            }
            if (Tick1800 && Enforced.Debug >= 3)
            {
                for (int i = 0; i < SlotCnt.Length; i++) SlotCnt[i] = 0;
                foreach (var pair in GlobalProtect) SlotCnt[pair.Value.RefreshSlot]++;
                Log.Line($"[NewRefresh] SlotScaler:{EntSlotScaler} - EntsUpdated:{entsUpdated} - ShieldsWaking:{shieldsWaking} - EntsRemoved: {entsremoved} - EntsLostShield:{entsLostShield} - EntInRefreshSlots:({SlotCnt[0]} - {SlotCnt[1]} - {SlotCnt[2]} - {SlotCnt[3]} - {SlotCnt[4]} - {SlotCnt[5]} - {SlotCnt[6]} - {SlotCnt[7]} - {SlotCnt[8]}) \n" +
                         $"                                     ProtectedEnts:{GlobalProtect.Count} - FunctionalShields:{FunctionalShields.Count} - AllControllerBlocks:{Controllers.Count}");
            }
        }
        #endregion

        #region LogicUpdates
        private void LogicUpdates()
        {
            if (!Dispatched)
            {
                lock (ActiveShields)
                {
                    foreach (var s in ActiveShields)
                    {
                        if (Tick60) s.CleanWebEnts();
                        if (s.Asleep) continue;
                        if (s.DsState.State.ReInforce)
                        {
                            s.DeformEnabled = true;
                            s.ProtectSubs(Tick);
                            continue;
                        }

                        if (!DedicatedServer && Tick20 && s.EffectsDirty) s.ResetDamageEffects();
                        s.WebEntities();
                    }
                }
                if (WebWrapperOn)
                {
                    Dispatched = true;
                    WebDispatch();
                    WebDispatchDone();
                    WebWrapperOn = false;
                }
            }
        }

        private void WebDispatch()
        {
            MyAPIGateway.Parallel.For(0, WebWrapper.Count, i =>
            {
                var shield = WebWrapper[i];
                if (shield == null || shield.MarkedForClose) return;
                if (!shield.VoxelsToIntersect.IsEmpty) MyAPIGateway.Parallel.StartBackground(shield.VoxelIntersect);
                
                if (!shield.WebEnts.IsEmpty) {
                    foreach (var pair in shield.WebEnts)
                        shield.EntIntersectSelector(pair);
                }
            });
            WebWrapper.Clear();
        }

        private void WebDispatchDone()
        {
            Dispatched = false;
        }
        #endregion
    }
}
