
using Sandbox.Game.Entities;

namespace DefenseShields.Support
{
    using Sandbox.ModAPI;
    using System.Collections.Generic;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRageMath;

    public interface IThreadEvent
    {
        void Execute();
    }

    public class ShieldVsShieldThreadEvent : IThreadEvent
    {
        public DefenseShields Shield;
        public float Damage;
        public Vector3D CollisionAvg;
        public long AttackerId;

        public void Init(DefenseShields shield, float damage, Vector3D collisionAvg, long attackerId)
        {
            Shield = shield;
            Damage = damage;
            CollisionAvg = collisionAvg;
            AttackerId = attackerId;
        }

        public void Clean()
        {
            Shield = null;
            Damage = 0;
            CollisionAvg = Vector3D.Zero;
            AttackerId = 0;
        }

        public void Execute()
        {
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(AttackerId, Damage, Session.Instance.MPEnergy, null, true, CollisionAvg);
            }
            else
            {
                Shield.EnergyHit = DefenseShields.HitType.Energy;
                Shield.ImpactSize = Damage;
                Shield.WorldImpactPosition = CollisionAvg;
            }
            Shield.WebDamage = true;
            Shield.Absorb += Damage;
            Session.Instance.ShieldEventPool.Return(this);
        }
    }

    public class MissileThreadEvent : IThreadEvent
    {
        public MyEntity Entity;
        public DefenseShields Shield;

        public void Init(MyEntity entity, DefenseShields shield)
        {
            Entity = entity;
            Shield = shield;
        }

        public void Clean()
        {
            Entity = null;
            Shield = null;
        }

        public void Execute()
        {
            if (Entity == null || !Entity.InScene || Entity.MarkedForClose) return;
            var computedDamage = UtilsStatic.ComputeAmmoDamage(Entity);

            var damage = computedDamage * Shield.DsState.State.ModulateKinetic;
            if (computedDamage < 0) damage = computedDamage;

            var rayDir = Vector3D.Normalize(Entity.Physics.LinearVelocity);
            var ray = new RayD(Entity.PositionComp.WorldVolume.Center, rayDir);
            var intersect = CustomCollision.IntersectEllipsoid(Shield.DetectMatrixOutsideInv, Shield.DetectionMatrix, ray);
            var hitDist = intersect ?? 0;
            var hitPos = ray.Position + (ray.Direction * -hitDist);

            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Entity.EntityId, damage, Session.Instance.MPExplosion, null, true, hitPos);
                Entity.Close();
                Entity.InScene = false;
            }
            else
            {
                Shield.EnergyHit = DefenseShields.HitType.Energy;
                Shield.WorldImpactPosition = hitPos;
                Shield.ImpactSize = damage;
                UtilsStatic.CreateFakeSmallExplosion(hitPos);
                Entity.Close();
                Entity.InScene = false;
            }
            Shield.WebDamage = true;
            Shield.Absorb += damage;
            Session.Instance.MissilePool.Return(this);
        }
    }

    public class FloaterThreadEvent : IThreadEvent
    {
        public MyEntity Entity;
        public DefenseShields Shield;

        public void Init(MyEntity entity, DefenseShields shield)
        {
            Entity = entity;
            Shield = shield;
        }

        public void Clean()
        {
            Entity = null;
            Shield = null;
        }

        public void Execute()
        {
            if (Entity == null || Entity.MarkedForClose) return;
            var floater = (MyFloatingObject)Entity;

            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Entity.EntityId, 1, Session.Instance.MPKinetic, null, false, Entity.PositionComp.WorldVolume.Center);
                floater.DoDamage(9999999, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive,  Shield.MyCube.EntityId);
            }
            else
            {
                Shield.WorldImpactPosition = Entity.PositionComp.WorldVolume.Center;
                Shield.ImpactSize = 10;
                floater.DoDamage(9999999, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive, Shield.MyCube.EntityId);
            }
            Shield.WebDamage = true;
            Shield.Absorb += 1;

            Session.Instance.FloaterPool.Return(this);
        }
    }
    public class CollisionDataThreadEvent : IThreadEvent
    {
        public MyCollisionPhysicsData CollisionData;
        public DefenseShields Shield;

        public void Init(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Clean()
        {
            CollisionData = new MyCollisionPhysicsData();
            Shield = null;
        }

        public void Execute()
        {
            if (CollisionData.Entity1 == null || CollisionData.Entity2 == null || CollisionData.Entity1.MarkedForClose || CollisionData.Entity2.MarkedForClose) return;
            var tick = Session.Instance.Tick;
            EntIntersectInfo entInfo;

            var foundInfo = Shield.WebEnts.TryGetValue(CollisionData.Entity1, out entInfo);
            if (!foundInfo || entInfo.LastCollision == tick) return;

            if (entInfo.LastCollision >= tick - 8) entInfo.ConsecutiveCollisions++;
            else entInfo.ConsecutiveCollisions = 0;
            entInfo.LastCollision = tick;
            if (entInfo.ConsecutiveCollisions > 0) if (Session.Enforced.Debug >= 2) Log.Line($"Consecutive:{entInfo.ConsecutiveCollisions}");
            if (!CollisionData.E1IsStatic)
            {
                if (entInfo.ConsecutiveCollisions == 0) CollisionData.Entity1.Physics.ApplyImpulse(CollisionData.ImpDirection1, CollisionData.CollisionCorrection1);
                if (CollisionData.E2IsHeavier)
                {
                    var accelCap = CollisionData.E1IsStatic ? 10 : 25;
                    var accelClamp = MathHelper.Clamp(CollisionData.Mass2 / CollisionData.Mass1, 1, accelCap);
                    var collisions = entInfo.ConsecutiveCollisions + 1;
                    var sizeAccel = accelClamp > collisions ? accelClamp : collisions;
                    var forceMulti = (CollisionData.Mass1 * (collisions * sizeAccel));
                    var currentSpeed = CollisionData.Entity1.Physics.LinearVelocity.Length();
                    CollisionData.Entity1.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force1, null, null, currentSpeed + 1.666f, CollisionData.Immediate);
                }
            }

            if (!CollisionData.E2IsStatic)
            {
                if (entInfo.ConsecutiveCollisions == 0) CollisionData.Entity2.Physics.ApplyImpulse(CollisionData.ImpDirection2, CollisionData.CollisionCorrection2);
                if (CollisionData.E1IsHeavier)
                {
                    var accelCap = CollisionData.E1IsStatic ? 10 : 50;
                    var accelClamp = MathHelper.Clamp(CollisionData.Mass1 / CollisionData.Mass2, 1, accelCap);
                    var collisions = entInfo.ConsecutiveCollisions + 1;
                    var sizeAccel = accelClamp > collisions ? accelClamp : collisions;
                    var forceMulti = (CollisionData.Mass2 * (collisions * sizeAccel));
                    if (CollisionData.Entity2.Physics.LinearVelocity.Length() <= (Session.Instance.MaxEntitySpeed * 0.75))
                        CollisionData.Entity2.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force2, null, null, null, CollisionData.Immediate);
                }
            }
            Session.Instance.CollisionPool.Return(this);
        }
    }

    public class StationCollisionDataThreadEvent : IThreadEvent
    {
        public MyCollisionPhysicsData CollisionData;
        public DefenseShields Shield;

        public void Init(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Clean()
        {
            CollisionData = new MyCollisionPhysicsData();
            Shield = null;
        }

        public void Execute()
        {
            if (CollisionData.Entity1 == null || CollisionData.Entity1.MarkedForClose) return;
            var tick = Session.Instance.Tick;
            EntIntersectInfo entInfo;

            var foundInfo = Shield.WebEnts.TryGetValue(CollisionData.Entity1, out entInfo);
            if (!foundInfo || entInfo.LastCollision == tick) return;

            if (entInfo.LastCollision >= tick - 8) entInfo.ConsecutiveCollisions++;
            else entInfo.ConsecutiveCollisions = 0;
            entInfo.LastCollision = tick;
            if (entInfo.ConsecutiveCollisions > 0) if (Session.Enforced.Debug >= 2) Log.Line($"Consecutive Station hits:{entInfo.ConsecutiveCollisions}");

            if (entInfo.ConsecutiveCollisions == 0) CollisionData.Entity1.Physics.ApplyImpulse(CollisionData.ImpDirection1, CollisionData.CollisionAvg);

            var collisions = entInfo.ConsecutiveCollisions + 1;
            var forceMulti = CollisionData.Mass1 * (collisions * 60);
            if (CollisionData.Entity1.Physics.LinearVelocity.Length() <= (Session.Instance.MaxEntitySpeed * 0.75))
                CollisionData.Entity1.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force1, null, null, null, CollisionData.Immediate);

            CollisionData.Entity1.Physics.ApplyImpulse((CollisionData.Mass1 * 0.075) * CollisionData.ImpDirection2, CollisionData.CollisionAvg);
            Session.Instance.StaticCollisionPool.Return(this);
        }
    }

    public class PlayerCollisionThreadEvent : IThreadEvent
    {
        public MyCollisionPhysicsData CollisionData;
        public DefenseShields Shield;

        public void Init(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Clean()
        {
            CollisionData = new MyCollisionPhysicsData();
            Shield = null;
        }

        public void Execute()
        {
            const int forceMulti = 200000;
            CollisionData.Entity1.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force1, null, null, null, CollisionData.Immediate);
            var character = CollisionData.Entity1 as IMyCharacter;
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(CollisionData.Entity1.EntityId, 1, Session.Instance.MPKinetic, null, false, CollisionData.CollisionAvg);
                character?.DoDamage(51f, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive, null, Shield.MyCube.EntityId);
            }
            else
            {
                Shield.ImpactSize = 1;
                Shield.WorldImpactPosition = CollisionData.CollisionAvg;
                character?.DoDamage(51f, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive, null, Shield.MyCube.EntityId);
            }
            Session.Instance.PlayerCollisionPool.Return(this);
        }
    }

    public class CharacterEffectThreadEvent : IThreadEvent
    {
        public IMyCharacter Character;
        public DefenseShields Shield;

        public void Init(IMyCharacter character, DefenseShields shield)
        {
            Character = character;
            Shield = shield;
        }

        public void Clean()
        {
            Character = null;
            Shield = null;
        }

        public void Execute()
        {
            var npcname = Character.ToString();
            if (npcname.Equals("Space_Wolf"))
            {
                Character.Delete();
            }
            Session.Instance.PlayerEffectPool.Return(this);
        }
    }

    public class ManyBlocksThreadEvent : IThreadEvent
    {
        public DefenseShields Shield;
        public HashSet<CubeAccel> AccelSet;
        public float Damage;
        public Vector3D CollisionAvg;
        public long AttackerId;

        public void Init(HashSet<CubeAccel> accelSet, DefenseShields shield, float damage, Vector3D collisionAvg, long attackerId)
        {
            AccelSet = accelSet;
            Shield = shield;
            Damage = damage;
            CollisionAvg = collisionAvg;
            AttackerId = attackerId;
        }

        public void Clean()
        {
            AccelSet = null;
            Shield = null;
            Damage = 0;
            CollisionAvg = Vector3D.Zero;
            AttackerId = 0;
        }

        public void Execute()
        {
            foreach (var accel in AccelSet)
            {
                EntIntersectInfo entInfo;
                if (accel.Grid != accel.Block.CubeGrid)
                {
                    if (Shield.WebEnts.TryGetValue(accel.Grid, out entInfo))
                    {
                        entInfo.RefreshNow = true;
                    }
                    AccelSet.Clear();
                    Session.Instance.SetCubeAccelPool.Return(AccelSet);
                    return;
                }

                if (accel.Block.IsDestroyed)
                {
                    if (Shield.WebEnts.TryGetValue(accel.Grid, out entInfo)) entInfo.RefreshNow = true;
                    AccelSet.Clear();
                    Session.Instance.SetCubeAccelPool.Return(AccelSet);
                    return;
                }

                var blockDamage = Shield.ShieldMode == DefenseShields.ShieldType.Station ? accel.Block.MaxIntegrity : accel.Block.MaxIntegrity / 5;
                accel.Block.DoDamage(blockDamage, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive, null, Shield.MyCube.EntityId);

                if (accel.Block.IsDestroyed)
                {
                    if (Shield.WebEnts.TryGetValue(accel.Grid, out entInfo)) entInfo.RefreshNow = true;
                }
            }
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(AttackerId, Damage, Session.Instance.MPKinetic, null, true, CollisionAvg);
            }
            else
            {
                Shield.ImpactSize = Damage;
                Shield.WorldImpactPosition = CollisionAvg;
            }
            Shield.WebDamage = true;
            Shield.Absorb += Damage;
            AccelSet.Clear();
            Session.Instance.SetCubeAccelPool.Return(AccelSet);
            Session.Instance.ManyBlocksPool.Return(this);
        }
    }

    public class VoxelCollisionDmgThreadEvent : IThreadEvent
    {
        public MyEntity Entity;
        public DefenseShields Shield;
        public float Damage;
        public Vector3D CollisionAvg;

        public void Init(MyEntity entity, DefenseShields shield, float damage, Vector3D collisionAvg)
        {
            Entity = entity;
            Shield = shield;
            Damage = damage;
            CollisionAvg = collisionAvg;
        }

        public void Clean()
        {
            Entity = null;
            Shield = null;
            Damage = 0;
            CollisionAvg = Vector3D.Zero;
        }

        public void Execute()
        {
            if (Entity == null || Entity.MarkedForClose) return;
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Entity.EntityId, Damage, Session.Instance.MPKinetic, null, false, CollisionAvg);
            }
            else
            {
                Shield.WorldImpactPosition = CollisionAvg;
                Shield.ImpactSize = 12000;
            }
            Shield.WebDamage = true;
            Shield.Absorb += Damage;
            Session.Instance.VoxelCollisionDmgPool.Return(this);
        }
    }

    public class VoxelCollisionPhysicsThreadEvent : IThreadEvent
    {
        public MyCollisionPhysicsData CollisionData;
        public DefenseShields Shield;

        public void Init(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Clean()
        {
            CollisionData = new MyCollisionPhysicsData();
            Shield = null;
        }

        public void Execute()
        {
            Vector3 velAtPoint;
                var point = CollisionData.CollisionCorrection2;
                CollisionData.Entity2.Physics.GetVelocityAtPointLocal(ref point, out velAtPoint);
                var speed = MathHelper.Clamp(velAtPoint.Length(), 2f, 20f);
                var forceMulti = (CollisionData.Mass2 * 10) * speed;
                CollisionData.Entity2.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force2, null, null, speed, CollisionData.Immediate);
                Session.Instance.VoxelCollisionPhysicsPool.Return(this);
        }
    }

    public class MeteorDmgThreadEvent : IThreadEvent
    {
        public IMyMeteor Meteor;
        public DefenseShields Shield;

        public void Init(IMyMeteor meteor, DefenseShields shield)
        {
            Meteor = meteor;
            Shield = shield;
        }

        public void Clean()
        {
            Meteor = null;
            Shield = null;
        }
        public void Execute()
        {
            if (Meteor == null || Meteor.MarkedForClose) return;
            var damage = 5000 * Shield.DsState.State.ModulateEnergy;
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Meteor.EntityId, damage, Session.Instance.MPKinetic, null, false, Meteor.PositionComp.WorldVolume.Center);
                Meteor.DoDamage(10000f, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive, null, Shield.MyCube.EntityId);
            }
            else
            {
                Shield.WorldImpactPosition = Meteor.PositionComp.WorldVolume.Center;
                Shield.ImpactSize = damage;
                Meteor.DoDamage(10000f, Session.Instance.MpIgnoreDamage, Session.Instance.MpActive, null, Shield.MyCube.EntityId);
            }
            Shield.WebDamage = true;
            Shield.Absorb += damage;
            Session.Instance.MeteorPool.Return(this);
        }
    }

    public class ForceDataThreadEvent : IThreadEvent
    {
        public MyForceData ForceData;
        public DefenseShields Shield;

        public void Init(MyForceData forceData, DefenseShields shield)
        {
            ForceData = forceData;
            Shield = shield;
        }

        public void Clean()
        {
            ForceData = new MyForceData(null, Vector3D.Zero, null, null, null, false);
            Shield = null;
        }

        public void Execute()
        {
            if (ForceData.Entity == null || ForceData.Entity.MarkedForClose) return;
            ForceData.Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, ForceData.Force, null, Vector3D.Zero, ForceData.MaxSpeed, ForceData.Immediate);
            Session.Instance.ForceDataPool.Return(this);
        }
    }
}
