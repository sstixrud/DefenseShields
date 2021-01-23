using System.Linq;
using System;
using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
namespace DefenseShields
{
    public partial class DefenseShields
    {
        #region Web Entities
        public void CleanWebEnts()
        {
            AuthenticatedCache.Clear();
            IgnoreCache.Clear();

            _protectEntsTmp.Clear();
            _protectEntsTmp.AddRange(ProtectedEntCache.Where(info => _tick - info.Value.LastTick > 180));
            foreach (var protectedEnt in _protectEntsTmp) ProtectedEntCache.Remove(protectedEnt.Key);

            _webEntsTmp.Clear();
            _webEntsTmp.AddRange(WebEnts.Where(info => _tick - info.Value.LastTick > 180));
            foreach (var webent in _webEntsTmp)
            {
                EntIntersectInfo removedEnt;
                WebEnts.TryRemove(webent.Key, out removedEnt);
                Session.Instance.EntIntersectInfoPool.Return(removedEnt);
                EnemyShields.Remove(webent.Key);
            }
        }

        public void ProtectSubs(uint tick)
        {
            lock (SubLock)
            {
                foreach (var sub in ShieldComp.SubGrids)
                {
                    MyProtectors protectors;
                    Session.Instance.GlobalProtect.TryGetValue(sub, out protectors);

                    if (protectors == null)
                    {
                        protectors = Session.Instance.GlobalProtect[sub] = Session.ProtSets.Get();
                        protectors.Init(LogicSlot, tick);
                    }
                    protectors.IntegrityShield = this;
                }
            }
        }

        public bool ResetEnts(MyEntity ent, uint tick)
        {
            MyProtectors protectors;
            Session.Instance.GlobalProtect.TryGetValue(ent, out protectors);
            if (protectors == null)
            {
                protectors = Session.Instance.GlobalProtect[ent] = Session.ProtSets.Get();
                protectors.Init(LogicSlot, tick);
            }

            var grid = ent as MyCubeGrid;
            if (grid != null)
            {
                if (CustomCollision.CornerOrCenterInShield(grid, DetectMatrixOutsideInv, _resetEntCorners, true) == 0) return false;

                protectors.Shields.Add(this);
                return true;
            }

            if (!CustomCollision.PointInShield(ent.PositionComp.WorldAABB.Center, DetectMatrixOutsideInv)) return false;
            protectors.Shields.Add(this);
            return true;
        }

        public void WebEntities()
        {
            PruneList.Clear();
            MyGamePruningStructure.GetTopMostEntitiesInBox(ref WebBox, PruneList);
            if (Missiles.Count > 0)
            {
                var missileBox = WebBox;
                foreach (var missile in Missiles)
                    if (missile.InScene && !missile.MarkedForClose && missileBox.Intersects(missile.PositionComp.WorldAABB)) PruneList.Add(missile);
            }
            var shieldsStartIndex = PruneList.Count;
            foreach (var eShield in EnemyShields) PruneList.Add(eShield);

            var disableVoxels = Session.Enforced.DisableVoxelSupport == 1 || ShieldComp.Modulator == null || ShieldComp.Modulator.ModSet.Settings.ModulateVoxels;
            var voxelFound = false;
            var shieldFound = false;
            var entChanged = false;
            var iMoving = ShieldComp.GridIsMoving;
            var tick = Session.Instance.Tick;

            _enablePhysics = false;
            for (int i = 0; i < PruneList.Count; i++)
            {
                var ent = PruneList[i];
                var entPhysics = ent.Physics;

                if (i < shieldsStartIndex)
                {
                    var voxel = ent as MyVoxelBase;
                    if (ent == null || (voxel == null && (entPhysics == null || entPhysics.IsPhantom || ent.DefinitionId == null)) || (voxel != null && (!iMoving || !GridIsMobile || disableVoxels || voxel != voxel.RootVoxel))) continue;

                    bool quickReject;
                    if (_isServer) quickReject = ent is IMyEngineerToolBase || IgnoreCache.Contains(ent) || EnemyShields.Contains(ent) || FriendlyMissileCache.Contains(ent) || AuthenticatedCache.Contains(ent);
                    else quickReject = (!(ent is MyCubeGrid) && voxel == null && !(ent is IMyCharacter)) || IgnoreCache.Contains(ent) || EnemyShields.Contains(ent) || AuthenticatedCache.Contains(ent);

                    var floater = ent as IMyFloatingObject;
                    if (quickReject || floater != null && (!iMoving && Vector3.IsZero(entPhysics.LinearVelocity, 1e-2f)) || !WebSphere.Intersects(ent.PositionComp.WorldVolume)) continue;
                    if (voxel != null)
                    {
                        if (VoxelsToIntersect.ContainsKey(voxel)) VoxelsToIntersect[voxel]++;
                        else VoxelsToIntersect[voxel] = 1;
                        voxelFound = true;
                        entChanged = true;
                        _enablePhysics = true;
                        continue;
                    }
                }
                Ent relation;

                ProtectCache protectedEnt;
                EntIntersectInfo entInfo = null;
                ProtectedEntCache.TryGetValue(ent, out protectedEnt);

                var refreshInfo = false;
                if (protectedEnt == null)
                {
                    WebEnts.TryGetValue(ent, out entInfo);
                    if (entInfo != null)
                    {
                        var last = entInfo.LastTick;
                        var refresh = entInfo.RefreshTick;
                        var refreshTick = tick - last > 180 || (tick - last == 180 && tick - refresh >= 3600) || (tick - last == 1 && tick - refresh >= 60);
                        refreshInfo = refreshTick;
                        if (refreshInfo || entInfo.RefreshNow)
                        {
                            entInfo.RefreshTick = tick;
                            entInfo.Relation = EntType(ent);
                        }
                        relation = entInfo.Relation;
                        entInfo.LastTick = tick;
                    }
                    else relation = EntType(ent);
                }
                else
                {
                    var last = protectedEnt.LastTick;
                    var refresh = protectedEnt.RefreshTick;
                    var refreshTick = tick - last > 180 || (tick - last == 180 && tick - refresh >= 3600) || (tick - last == 1 && tick - refresh >= 60);
                    refreshInfo = refreshTick;
                    if (refreshInfo)
                    {
                        protectedEnt.RefreshTick = tick;
                        protectedEnt.PreviousRelation = protectedEnt.Relation;
                        protectedEnt.Relation = EntType(ent);
                    }
                    relation = protectedEnt.Relation;
                    protectedEnt.LastTick = tick;
                }

                var grid = ent as MyCubeGrid;
                
                switch (relation)
                {
                    case Ent.Authenticated:
                        continue;
                    case Ent.Ignore:
                    case Ent.Friendly:
                    case Ent.Protected:
                        if (relation == Ent.Protected)
                        {
                            if (protectedEnt == null) ProtectedEntCache[ent] = new ProtectCache(tick, tick, tick, relation, relation);
                            MyProtectors protectors;
                            Session.Instance.GlobalProtect.TryGetValue(ent, out protectors);
                            if (protectors == null)
                            {
                                protectors = Session.Instance.GlobalProtect[ent] = Session.ProtSets.Get();
                                protectors.Init(LogicSlot, tick);
                            }
                            if (protectors.Shields.Contains(this)) continue;

                            protectors.Shields.Add(this);
                            protectors.Shields.ApplyAdditions();
                            continue;
                        }
                        IgnoreCache.Add(ent);
                        continue;
                }

                if (relation == Ent.Shielded) shieldFound = true;
                try
                {
                    if (entInfo != null)
                    {
                        var interestingEnts = relation == Ent.Floater || relation == Ent.EnemyGrid || relation == Ent.NobodyGrid || relation == Ent.Shielded;
                        if (entPhysics != null && entPhysics.IsMoving) entChanged = true;
                        else if (entInfo.Touched || (refreshInfo && interestingEnts && !ent.PositionComp.LocalAABB.Equals(entInfo.Box)))
                        {
                            entInfo.RefreshTick = tick;
                            entInfo.Box = ent.PositionComp.LocalAABB;
                            entChanged = true;
                        }

                        _enablePhysics = true;
                        if (refreshInfo)
                        {
                            if ((relation == Ent.EnemyGrid || relation == Ent.NobodyGrid)  && entInfo.CacheBlockList.Count != (ent as MyCubeGrid).BlocksCount)
                            {
                                entInfo.RefreshNow = true;
                            }
                        }
                    }
                    else
                    {
                        if (relation == Ent.Other)
                        {
                            var entPast = -Vector3D.Normalize(entPhysics.LinearVelocity) * 6;
                            var entTestLoc = ent.PositionComp.WorldVolume.Center + entPast;
                            var centerStep = -Vector3D.Normalize(entTestLoc - DetectionCenter) * 2f;
                            var counterDrift = centerStep + entTestLoc;
                            if (CustomCollision.PointInShield(counterDrift, DetectMatrixOutsideInv))
                            {
                                FriendlyMissileCache.Add(ent);
                                continue;
                            }
                        }
                        entChanged = true;
                        _enablePhysics = true;
                        ProtectedEntCache.Remove(ent);
                        var entIntersectInfo = Session.Instance.EntIntersectInfoPool.Get();
                        entIntersectInfo.Init(false, ent.PositionComp.LocalAABB, tick, tick, tick, relation);
                        WebEnts.TryAdd(ent, entIntersectInfo);
                    }

                    if (grid != null && entChanged)
                        Session.Instance.CheckForSplits[grid] = Session.Instance.Tick;
                }
                catch (Exception ex) { Log.Line($"Exception in WebEntities entInfo: {ex}"); }
            }
            if (!_enablePhysics)
            {
                return;
            }

            ShieldMatrix = ShieldEnt.PositionComp.WorldMatrixRef;
            if ((_needPhysics && shieldFound) || !ShieldMatrix.EqualsFast(ref OldShieldMatrix))
            {
                OldShieldMatrix = ShieldMatrix;
                if (shieldFound)
                {
                    _needPhysics = false;
                    Icosphere.ReturnPhysicsVerts(DetectMatrixOutside, ShieldComp.PhysicsOutside);
                }
                else _needPhysics = true;
                if (voxelFound) Icosphere.ReturnPhysicsVerts(DetectMatrixOutside, ShieldComp.PhysicsOutsideLow);
            }

            if (iMoving || entChanged)
            {
                Asleep = false;
                LastWokenTick = tick;
                Session.Instance.WebWrapper.Add(this);
                Session.Instance.WebWrapperOn = true;
            }
        }
        #endregion

        #region Gather Entity Information
        public Ent EntType(MyEntity ent)
        {
            if (ent is IMyFloatingObject)
            {
                if (CustomCollision.AllAabbInShield(ent.PositionComp.WorldAABB, DetectMatrixOutsideInv, _obbCorners)) return Ent.Ignore;
                return Ent.Floater;
            }

            var voxel = ent as MyVoxelBase;
            if (voxel != null && (Session.Enforced.DisableVoxelSupport == 1 || ShieldComp.Modulator == null || ShieldComp.Modulator.ModSet.Settings.ModulateVoxels || !GridIsMobile)) return Ent.Ignore;

            if (EntityBypass.Contains(ent)) return Ent.Ignore;

            var character = ent as IMyCharacter;
            if (character != null)
            {

                var getComponentOwner = ent as IMyComponentOwner<MyIDModule>;

                long playerId;
                MyIDModule targetIdModule;
                if (getComponentOwner != null && getComponentOwner.GetComponent(out targetIdModule))
                    playerId = targetIdModule.Owner;
                else
                {
                    var controllingId = character.ControllerInfo?.ControllingIdentityId;
                    playerId = controllingId ?? 0;
                }

                if (playerId == 0 || character.IsDead || character.Integrity <= 0) return Ent.Ignore;

                var playerrelationship = MyIDModule.GetRelationPlayerBlock(MyCube.IDModule.Owner, playerId, MyOwnershipShareModeEnum.Faction);

                if (playerrelationship == MyRelationsBetweenPlayerAndBlock.Owner || playerrelationship == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    var playerInShield = CustomCollision.PointInShield(ent.PositionComp.WorldAABB.Center, DetectMatrixOutsideInv);
                    return playerInShield ? Ent.Protected : Ent.Friendly;
                }

                if (CustomCollision.NewObbPointsInShield(ent, DetectMatrixOutsideInv, _obbPoints) == 9)
                {
                    return Ent.EnemyInside;
                }

                return Ent.EnemyPlayer;
            }
            var grid = ent as MyCubeGrid;
            if (grid != null)
            {
                ModulateGrids = (ShieldComp.Modulator != null && ShieldComp.Modulator.ModSet.Settings.ModulateGrids) || Session.Enforced.DisableEntityBarrier == 1;
                ModulatorGridComponent modComp;
                grid.Components.TryGet(out modComp);
                if (!string.IsNullOrEmpty(modComp?.ModulationPassword) && modComp.ModulationPassword == Shield.CustomData)
                {
                    var modShield = modComp.Modulator?.ShieldComp?.DefenseShields;
                    if (modShield != null)
                    {
                        lock (modShield.SubLock)
                        {
                            foreach (var subGrid in modShield.ShieldComp.SubGrids)
                            {
                                if (ShieldEnt.PositionComp.WorldVolume.Intersects(grid.PositionComp.WorldVolume))
                                {
                                    if (CustomCollision.CornerOrCenterInShield(grid, DetectMatrixOutsideInv, _resetEntCorners) > 0) return Ent.Protected;
                                    AuthenticatedCache.Add(subGrid);
                                }
                                else AuthenticatedCache.Add(subGrid);
                            }
                        }
                        return Ent.Authenticated;
                    }
                    foreach (var subGrid in modComp.SubGrids)
                    {
                        if (ShieldEnt.PositionComp.WorldVolume.Intersects(grid.PositionComp.WorldVolume))
                        {
                            if (CustomCollision.CornerOrCenterInShield(grid, DetectMatrixOutsideInv, _resetEntCorners) > 0) return Ent.Protected;
                            AuthenticatedCache.Add(subGrid);
                        }
                        else AuthenticatedCache.Add(subGrid);
                    }
                }

                List<long> bigOwners;
                int bigOwnersCnt;
                Session.ParentGrid parent;
                if (Session.Instance.GetParentGrid.TryGetValue(grid, out parent) && !CustomCollision.AllAabbInShield(parent.Parent.PositionComp.WorldAABB, DetectMatrixOutsideInv, _obbCorners))
                {
                    bigOwners = grid.BigOwners;
                    bigOwnersCnt = bigOwners.Count;
                    if (bigOwnersCnt == 0 || GridEnemy(grid, bigOwners))
                        return Ent.EnemyGrid;

                }
                else {
                    bigOwners = grid.BigOwners;
                    bigOwnersCnt = bigOwners.Count;
                }


                if (CustomCollision.AllAabbInShield(ent.PositionComp.WorldAABB, DetectMatrixOutsideInv, _obbCorners)) return Ent.Protected;
                if (!ModulateGrids && bigOwnersCnt == 0) return Ent.NobodyGrid;
                var enemy = !ModulateGrids && GridEnemy(grid, bigOwners);
                if (!enemy)
                {
                    lock (SubLock) if (ShieldComp.SubGrids.Contains(grid)) return Ent.Protected;
                    var pointsInShield = CustomCollision.NewObbPointsInShield(grid, DetectMatrixOutsideInv, _obbPoints);
                    return pointsInShield > 0 ? Ent.Protected : Ent.Friendly;
                }

                ShieldGridComponent shieldComponent;
                grid.Components.TryGet(out shieldComponent);
                if (shieldComponent?.DefenseShields?.ShieldComp != null && shieldComponent.DefenseShields.NotFailed)
                {
                    var dsComp = shieldComponent.DefenseShields;
                    var shieldEntity = MyCube.Parent;
                    dsComp.EnemyShields.Add(shieldEntity);
                    return Ent.Shielded;    
                }
                return Ent.EnemyGrid;
            }

            if (ent is IMyMeteor || (ent.DefinitionId.HasValue && ent.DefinitionId.Value.TypeId == Session.Instance.MissileObj)) return Ent.Other;
            if (voxel != null && GridIsMobile) return Ent.VoxelBase;
            return 0;
        }

        public bool GridEnemy(MyCubeGrid grid, List<long> owners = null)
        {
            if (owners == null) owners = grid.BigOwners;
            if (owners.Count == 0) return true;
            var relationship = MyCube.GetUserRelationToOwner(owners[0]);
            var enemy = relationship != MyRelationsBetweenPlayerAndBlock.Owner && relationship != MyRelationsBetweenPlayerAndBlock.FactionShare;
            return enemy;
        }
        #endregion
    }
}
