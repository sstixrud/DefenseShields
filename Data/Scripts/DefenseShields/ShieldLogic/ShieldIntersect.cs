namespace DefenseShields
{
    using System;
    using System.Collections.Generic;
    using Support;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRageMath;

    public partial class DefenseShields
    {
        #region Intersect
        internal void EntIntersectSelector(KeyValuePair<MyEntity, EntIntersectInfo> pair)
        {
            var entInfo = pair.Value;
            var webent = pair.Key;

            if (entInfo == null || webent == null || webent.MarkedForClose) return;
            var relation = entInfo.Relation;

            var tick = Session.Instance.Tick;
            var entCenter = webent.PositionComp.WorldVolume.Center;
            if (entInfo.LastTick != tick) return;
            if (entInfo.RefreshNow && (relation == Ent.NobodyGrid || relation == Ent.EnemyGrid))
            {
                entInfo.CacheBlockList.Clear();
                var grid = webent as MyCubeGrid;
                if (grid != null)
                {
                    var checkSphere = WebSphere;
                    checkSphere.Radius += 10;
                    GetBlocksInsideSphereFast(grid, ref checkSphere, true, entInfo.CacheBlockList);
                }
            }
            entInfo.RefreshNow = false;

            switch (relation)
            {
                case Ent.EnemyPlayer:
                    {
                        PlayerIntersect(webent);
                        return;
                    }
                case Ent.EnemyInside:
                {
                    if (!CustomCollision.PointInShield(entCenter, DetectMatrixOutsideInv))
                    {
                        entInfo.RefreshNow = true;
                        entInfo.EnemySafeInside = false;
                    }
                    return;
                }
                case Ent.NobodyGrid:
                    {
                        if (Session.Enforced.Debug == 3) Log.Line($"Ent NobodyGrid: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                        GridIntersect(webent);
                        return;
                    }
                case Ent.EnemyGrid:
                    {
                        if (Session.Enforced.Debug == 3) Log.Line($"Ent LargeEnemyGrid: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                        GridIntersect(webent);
                        return;
                    }
                case Ent.Shielded:
                    {
                        if (Session.Enforced.Debug == 3) Log.Line($"Ent Shielded: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                        ShieldIntersect(webent);
                        return;
                    }
                case Ent.Floater:
                    {
                        if (!_isServer || webent.MarkedForClose) return;
                        if (CustomCollision.PointInShield(entCenter, DetectMatrixOutsideInv))
                        {
                            var floater = Session.Instance.FloaterPool.Get();
                            floater.Init(webent, this);
                            Session.Instance.ThreadEvents.Enqueue(floater);
                        }
                        return;
                    }
                case Ent.Other:
                    {
                        if (!_isServer) return;
                        if (Session.Enforced.Debug == 3) Log.Line($"Ent Other: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                        if (webent.MarkedForClose || !webent.InScene) return;
                        var meteor = webent as IMyMeteor;
                        if (meteor != null)
                        {
                            if (CustomCollision.PointInShield(entCenter, DetectMatrixOutsideInv))
                            {
                                var meteorEvent = Session.Instance.MeteorPool.Get();
                                meteorEvent.Init(meteor, this);
                                Session.Instance.ThreadEvents.Enqueue(meteorEvent);
                            }
                        }
                        else
                        {
                            var predictedHit = CustomCollision.FutureIntersect(this, webent, DetectionMatrix, DetectMatrixOutsideInv);
                            if (predictedHit)
                            {
                                var missileEvent = Session.Instance.MissilePool.Get();
                                missileEvent.Init(webent, this);
                                Session.Instance.ThreadEvents.Enqueue(missileEvent);
                            }
                        }
                        return;
                    }

                default:
                    return;
            }
        }

        private bool EntInside(MyEntity entity, MyOrientedBoundingBoxD bOriBBoxD)
        {
            if (entity != null && CustomCollision.PointInShield(entity.PositionComp.WorldVolume.Center, DetectMatrixOutsideInv))
            {
                if (CustomCollision.ObbCornersInShield(bOriBBoxD, DetectMatrixOutsideInv, _obbCorners))
                {
                    var bPhysics = entity.Physics;
                    var sPhysics = Shield.CubeGrid.Physics;
                    var sLSpeed = sPhysics.LinearVelocity;
                    var sASpeed = sPhysics.AngularVelocity * 50;
                    var sLSpeedLen = sLSpeed.LengthSquared();
                    var sASpeedLen = sASpeed.LengthSquared();
                    var sSpeedLen = sLSpeedLen > sASpeedLen ? sLSpeedLen : sASpeedLen;
                    var direction = Vector3D.Normalize(entity.PositionComp.WorldAABB.Center - DetectionCenter);
                    var forceData = new MyForceData { Entity = entity, Force = direction * (bPhysics.Mass * 10), MaxSpeed = sSpeedLen + 3 };
                    if (!bPhysics.IsStatic)
                    {
                        var forceEvent = Session.Instance.ForceDataPool.Get();
                        forceEvent.Init(forceData, this);
                        Session.Instance.ThreadEvents.Enqueue(forceEvent);
                    }
                    return true;
                }
            }
            return false;
        }

        private void GridIntersect(MyEntity ent)
        {
            var grid = (MyCubeGrid)ent;
            if (grid == null) return;
            EntIntersectInfo entInfo;
            WebEnts.TryGetValue(ent, out entInfo);
            if (entInfo == null) return;

            var bOriBBoxD = MyOrientedBoundingBoxD.CreateFromBoundingBox(grid.PositionComp.WorldAABB);
            if (entInfo.Relation != Ent.EnemyGrid && entInfo.WasInside && EntInside(grid, bOriBBoxD)) return;
            BlockIntersect(grid, bOriBBoxD, ref entInfo);
        }

        private void ShieldIntersect(MyEntity ent)
        {
            var grid = ent as MyCubeGrid;
            if (grid == null) return;
            if (EntInside(grid, MyOrientedBoundingBoxD.CreateFromBoundingBox(grid.PositionComp.WorldAABB))) return;
            ShieldGridComponent shieldComponent;
            grid.Components.TryGet(out shieldComponent);
            if (shieldComponent?.DefenseShields == null) return;

            var ds = shieldComponent.DefenseShields;
            if (!ds.NotFailed)
            {
                EntIntersectInfo entInfo;
                WebEnts.TryRemove(ent, out entInfo);
                Session.Instance.EntIntersectInfoPool.Return(entInfo);
            }
            var dsVerts = ds.ShieldComp.PhysicsOutside;
            var dsMatrixInv = ds.DetectMatrixOutsideInv;

            var insidePoints = new List<Vector3D>();
            CustomCollision.ShieldX2PointsInside(dsVerts, dsMatrixInv, ShieldComp.PhysicsOutside, DetectMatrixOutsideInv, insidePoints);

            var collisionAvg = Vector3D.Zero;
            var numOfPointsInside = insidePoints.Count;
            for (int i = 0; i < numOfPointsInside; i++) collisionAvg += insidePoints[i];

            if (numOfPointsInside > 0) collisionAvg /= numOfPointsInside;
            if (collisionAvg == Vector3D.Zero) return;

            if (MyGrid.EntityId > grid.EntityId) ComputeCollisionPhysics(grid, MyGrid, collisionAvg);
            else if (!_isServer) return;

            var damage = ((ds._shieldMaxChargeRate * ConvToHp) * DsState.State.ModulateKinetic) * 0.01666666666f;
            var shieldEvent = Session.Instance.ShieldEventPool.Get();
            shieldEvent.Init(this, damage, collisionAvg, grid.EntityId);
            Session.Instance.ThreadEvents.Enqueue(shieldEvent);
        }

        internal void VoxelIntersect()
        {
            foreach (var item in VoxelsToIntersect)
            {
                var voxelBase = item.Key;
                var newVoxel = item.Value == 1;
                var stage1Check = false;

                if (item.Value > 1) stage1Check = true;
                else if (newVoxel)
                {
                    var aabb = (BoundingBox)ShieldEnt.PositionComp.WorldAABB;
                    aabb.Translate(-voxelBase.RootVoxel.PositionLeftBottomCorner);
                    if (voxelBase.RootVoxel.Storage.Intersect(ref aabb, false) != ContainmentType.Disjoint) stage1Check = true;
                }

                if (!stage1Check)
                {
                    int oldValue;
                    VoxelsToIntersect.TryRemove(voxelBase, out oldValue);
                    continue;
                }

                var collision = CustomCollision.VoxelEllipsoidCheck(MyGrid, ShieldComp.PhysicsOutsideLow, voxelBase);
                if (collision.HasValue)
                {
                    ComputeVoxelPhysics(voxelBase, MyGrid, collision.Value);

                    VoxelsToIntersect[voxelBase]++;
                    if (_isServer)
                    {
                        var mass = MyGrid.GetCurrentMass();
                        var sPhysics = Shield.CubeGrid.Physics;
                        var momentum = mass * sPhysics.GetVelocityAtPoint(collision.Value);
                        var damage = (momentum.Length() / 500) * DsState.State.ModulateEnergy;
                        
                        var voxelEvent = Session.Instance.VoxelCollisionDmgPool.Get();
                        voxelEvent.Init(voxelBase, this, damage, collision.Value);
                        Session.Instance.ThreadEvents.Enqueue(voxelEvent);
                    }
                }
                else VoxelsToIntersect[voxelBase] = 0;
            }
        }

        private void PlayerIntersect(MyEntity ent)
        {
            var character = ent as IMyCharacter;
            if (character == null || character.MarkedForClose || character.IsDead) return;

            var npcname = character.ToString();
            if (npcname.Equals(SpaceWolf))
            {
                if (_isServer)
                {
                    var charEvent = Session.Instance.PlayerEffectPool.Get();
                    charEvent.Init(character, this);
                    Session.Instance.ThreadEvents.Enqueue(charEvent);
                }
                return;
            }
            var player = MyAPIGateway.Multiplayer.Players.GetPlayerControllingEntity(ent);
            if (player == null || player.PromoteLevel == MyPromoteLevel.Owner || player.PromoteLevel == MyPromoteLevel.Admin) return;
            var obb = new MyOrientedBoundingBoxD(ent.PositionComp.WorldAABB.Center, ent.PositionComp.LocalAABB.HalfExtents, Quaternion.CreateFromRotationMatrix(ent.WorldMatrix));
            var playerIntersect = CustomCollision.ObbIntersect(obb, DetectMatrixOutside, DetectMatrixOutsideInv);
            if (playerIntersect != null)
            {
                var collisionData = new MyCollisionPhysicsData
                {
                    Entity1 = ent,
                    Force1 = -Vector3.Normalize(ShieldEnt.PositionComp.WorldAABB.Center - (Vector3D)playerIntersect),
                    CollisionAvg = (Vector3D)playerIntersect
                };
                if (_isServer)
                {
                    var charEvent = Session.Instance.PlayerCollisionPool.Get();
                    charEvent.Init(collisionData, this);
                    Session.Instance.ThreadEvents.Enqueue(charEvent);
                }
            }
        }

        private void BlockIntersect(MyCubeGrid breaching, MyOrientedBoundingBoxD bOriBBoxD, ref EntIntersectInfo entInfo)
        {
            try
            {
                if (entInfo == null || breaching == null || breaching.MarkedForClose) return;

                if (bOriBBoxD.Intersects(ref SOriBBoxD))
                {
                    var collisionAvg = Vector3D.Zero;
                    var damageBlocks = Session.Enforced.DisableBlockDamage == 0;
                    var bQuaternion = Quaternion.CreateFromRotationMatrix(breaching.WorldMatrix);

                    const int blockDmgNum = 250;

                    var rawDamage = 0f;
                    var blockSize = breaching.GridSize;
                    var scaledBlockSize = blockSize * 3;
                    var gc = breaching.WorldToGridInteger(DetectionCenter);
                    var rc = ShieldSize.AbsMax() / blockSize;
                    rc *= rc;
                    rc = rc + 1;
                    rc = Math.Ceiling(rc);
                    var hits = 0;
                    var blockPoints = new Vector3D[9];

                    var cloneCacheList = Session.Instance.ListCubeAccelPool.Get();
                    cloneCacheList.AddRange(entInfo.CacheBlockList);
                    var cubeHitSet = Session.Instance.SetCubeAccelPool.Get();

                    for (int i = 0; i < cloneCacheList.Count; i++)
                    {
                        var accel = cloneCacheList[i];
                        var blockPos = accel.BlockPos;
                        var num1 = gc.X - blockPos.X;
                        var num2 = gc.Y - blockPos.Y;
                        var num3 = gc.Z - blockPos.Z;
                        var result = (num1 * num1) + (num2 * num2) + (num3 * num3);

                        if (_isServer)
                        {
                            if (result > rc || accel.CubeExists && result > rc + scaledBlockSize) continue;
                            if (accel.Block == null || accel.Block.CubeGrid != breaching) continue;
                        }
                        else
                        {
                            if (hits > blockDmgNum) break;
                            if (result > rc || accel.CubeExists && result > rc + scaledBlockSize || accel.Block == null || accel.Block.CubeGrid != breaching || accel.Block.IsDestroyed) continue;
                        }

                        var block = accel.Block;
                        var point = CustomCollision.BlockIntersect(block, accel.CubeExists, bQuaternion, DetectMatrixOutside, DetectMatrixOutsideInv, ref blockPoints);
                        if (point == null) continue;
                        collisionAvg += (Vector3D)point;
                        hits++;
                        if (!_isServer) continue;

                        if (hits > blockDmgNum) break;

                        rawDamage += block.Integrity;
                        if (damageBlocks)
                        {
                            cubeHitSet.Add(accel);
                        }
                    }
                    cloneCacheList.Clear();
                    Session.Instance.ListCubeAccelPool.Return(cloneCacheList);

                    if (collisionAvg != Vector3D.Zero)
                    {
                        collisionAvg /= hits;
                        ComputeCollisionPhysics(breaching, MyGrid, collisionAvg);
                        entInfo.Touched = true;
                    }
                    else return;
                    if (!_isServer) return;

                    var damage = rawDamage * DsState.State.ModulateEnergy;

                    var blockEvent = Session.Instance.ManyBlocksPool.Get();
                    blockEvent.Init(cubeHitSet, this, damage, collisionAvg, breaching.EntityId);

                    Session.Instance.ThreadEvents.Enqueue(blockEvent);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in BlockIntersect: {ex}"); }
        }

        public static void GetBlocksInsideSphereFast(MyCubeGrid grid, ref BoundingSphereD sphere, bool checkDestroyed, List<CubeAccel> blocks)
        {
            var radius = sphere.Radius;
            radius *= grid.GridSizeR;
            var center = grid.WorldToGridInteger(sphere.Center);
            var gridMin = grid.Min;
            var gridMax = grid.Max;
            double radiusSq = radius * radius;
            int radiusCeil = (int)Math.Ceiling(radius);
            int i, j, k;
            Vector3I max2 = Vector3I.Min(Vector3I.One * radiusCeil, gridMax - center);
            Vector3I min2 = Vector3I.Max(Vector3I.One * -radiusCeil, gridMin - center);
            for (i = min2.X; i <= max2.X; ++i)
            {
                for (j = min2.Y; j <= max2.Y; ++j)
                {
                    for (k = min2.Z; k <= max2.Z; ++k)
                    {
                        if (i * i + j * j + k * k < radiusSq)
                        {
                            MyCube cube;
                            var vector3I = center + new Vector3I(i, j, k);

                            if (grid.TryGetCube(vector3I, out cube))
                            {
                                var slim = (IMySlimBlock)cube.CubeBlock;
                                if (slim.Position == vector3I)
                                {
                                    if (checkDestroyed && slim.IsDestroyed)
                                        continue;

                                    blocks.Add(new CubeAccel {Block = slim, BlockPos = slim.Position, CubeExists = slim.FatBlock != null, Grid = (MyCubeGrid)slim.CubeGrid});

                                }
                            }
                        }
                    }
                }
            }
        }

        private void ComputeCollisionPhysics(MyCubeGrid entity1, MyCubeGrid entity2, Vector3D collisionAvg)
        {
            var e1Physics = ((IMyCubeGrid)entity1).Physics;
            var e2Physics = ((IMyCubeGrid)entity2).Physics;
            var e1IsStatic = e1Physics.IsStatic;
            var e2IsStatic = e2Physics.IsStatic;

            float bMass;
            if (e1IsStatic) bMass = float.MaxValue * 0.001f;
            else bMass = entity1.GetCurrentMass();

            float sMass;
            if (e2IsStatic) sMass = float.MaxValue * 0.001f;
            else sMass = entity2.GetCurrentMass();
            var bCom = e1Physics.CenterOfMassWorld;
            var bMassRelation = bMass / sMass;
            var bRelationClamp = MathHelper.Clamp(bMassRelation, 0, 1);
            var bCollisionCorrection = Vector3D.Lerp(bCom, collisionAvg, bRelationClamp);
            Vector3 bVelAtPoint;
            e1Physics.GetVelocityAtPointLocal(ref bCollisionCorrection, out bVelAtPoint);

            var sCom = e2IsStatic ? DetectionCenter : e2Physics.CenterOfMassWorld;
            var sMassRelation = sMass / bMass;
            var sRelationClamp = MathHelper.Clamp(sMassRelation, 0, 1);
            var sCollisionCorrection = Vector3D.Lerp(sCom, collisionAvg, sRelationClamp);
            Vector3 sVelAtPoint;
            e2Physics.GetVelocityAtPointLocal(ref sCollisionCorrection, out sVelAtPoint);

            var momentum = (bMass * bVelAtPoint) + (sMass * sVelAtPoint);
            var resultVelocity = momentum / (bMass + sMass);

            var bDir = (resultVelocity - bVelAtPoint) * bMass;
            var bForce = Vector3D.Normalize(bCom - collisionAvg);

            var sDir = (resultVelocity - sVelAtPoint) * sMass;
            var sforce = Vector3D.Normalize(sCom - collisionAvg);

            if (!e2IsStatic)
            {
                var collisionData = new MyCollisionPhysicsData
                {
                    Entity1 = entity1,
                    Entity2 = entity2,
                    E1IsStatic = e1IsStatic,
                    E2IsStatic = e2IsStatic,
                    E1IsHeavier = e1IsStatic || bMass > sMass,
                    E2IsHeavier = e2IsStatic || sMass > bMass,
                    Mass1 = bMass,
                    Mass2 = sMass,
                    Com1 = bCom,
                    Com2 = sCom,
                    CollisionCorrection1 = bCollisionCorrection,
                    CollisionCorrection2 = sCollisionCorrection,
                    ImpDirection1 = bDir,
                    ImpDirection2 = sDir,
                    Force1 = bForce,
                    Force2 = sforce,
                    CollisionAvg = collisionAvg,
                };
                var collisionEvent = Session.Instance.CollisionPool.Get();
                collisionEvent.Init(collisionData, this);

                Session.Instance.ThreadEvents.Enqueue(collisionEvent);
            }
            else
            {
                var altMomentum = (bMass * bVelAtPoint);
                var altResultVelocity = altMomentum / (bMass + (bMass * 0.5f));
                var bDir2 = (altResultVelocity - bVelAtPoint) * bMass;

                var transformInv = DetectMatrixOutsideInv;
                var normalMat = MatrixD.Transpose(transformInv);
                var localNormal = Vector3D.Transform(collisionAvg, transformInv);
                var surfaceNormal = Vector3D.Normalize(Vector3D.TransformNormal(localNormal, normalMat));
                Vector3 velAtPoint;
                e1Physics.GetVelocityAtPointLocal(ref collisionAvg, out velAtPoint);
                var bSurfaceDir = -Vector3D.Dot(velAtPoint, surfaceNormal) * surfaceNormal;
                var collisionData = new MyCollisionPhysicsData
                {
                    Entity1 = entity1,
                    Mass1 = bMass,
                    Com1 = bCom,
                    CollisionCorrection1 = bCollisionCorrection,
                    ImpDirection1 = bDir2,
                    ImpDirection2 = bSurfaceDir,
                    Force1 = bForce,
                    CollisionAvg = collisionAvg
                };

                var collisionEvent = Session.Instance.StaticCollisionPool.Get();
                collisionEvent.Init(collisionData, this);

                Session.Instance.ThreadEvents.Enqueue(collisionEvent);
            }
        }

        private void ComputeVoxelPhysics(MyEntity entity1, MyCubeGrid entity2, Vector3D collisionAvg)
        {
            var e2Physics = ((IMyCubeGrid)entity2).Physics;
            var e2IsStatic = e2Physics.IsStatic;

            float bMass;
            if (e2IsStatic) bMass = float.MaxValue * 0.001f;
            else bMass = entity2.GetCurrentMass();

            var sMass = float.MaxValue * 0.001f;

            var bCom = e2Physics.CenterOfMassWorld;
            var bMassRelation = bMass / sMass;
            var bRelationClamp = MathHelper.Clamp(bMassRelation, 0, 1);
            var bCollisionCorrection = Vector3D.Lerp(bCom, collisionAvg, bRelationClamp);
            Vector3 bVelAtPoint;
            e2Physics.GetVelocityAtPointLocal(ref bCollisionCorrection, out bVelAtPoint);

            var momentum = (bMass * bVelAtPoint) + (sMass * 0);
            var resultVelocity = momentum / (bMass + sMass);

            var bDir = (resultVelocity - bVelAtPoint) * bMass;
            var bForce = Vector3D.Normalize(bCom - collisionAvg);

            var collisionData = new MyCollisionPhysicsData
            {
                Entity1 = entity1,
                Entity2 = entity2,
                E1IsStatic = true,
                E2IsStatic = false,
                E1IsHeavier = true,
                E2IsHeavier = false,
                Mass1 = sMass,
                Mass2 = bMass,
                Com1 = Vector3D.Zero,
                Com2 = bCom,
                CollisionCorrection1 = Vector3D.Zero,
                CollisionCorrection2 = bCollisionCorrection,
                ImpDirection1 = Vector3D.Zero,
                ImpDirection2 = bDir,
                ImpPosition1 = Vector3D.Zero,
                ImpPosition2 = bCollisionCorrection,
                Force1 = Vector3D.Zero,
                Force2 = bForce,
                ForcePos1 = null,
                ForcePos2 = null,
                ForceTorque1 = null,
                ForceTorque2 = null,
                CollisionAvg = collisionAvg,
                Immediate = false
            };
            var collisionEvent = Session.Instance.VoxelCollisionPhysicsPool.Get();
            collisionEvent.Init(collisionData, this);
            Session.Instance.ThreadEvents.Enqueue(collisionEvent);
        }
        #endregion
    }
}
