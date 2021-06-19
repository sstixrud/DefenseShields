using System.Collections.Concurrent;
using VRage.Audio;

namespace DefenseShields.Support
{
    using System;
    using System.Collections.Generic;
    using Sandbox.Game.EntityComponents;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Collections;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.Utils;
    using VRage.Voxels;
    using VRageMath;

    internal struct VoxelHit : IVoxelOperator
    {
        internal bool HasHit;

        public void Op(ref Vector3I pos, MyStorageDataTypeEnum dataType, ref byte content)
        {
            if (content != MyVoxelConstants.VOXEL_CONTENT_EMPTY)
            {
                HasHit = true;
            }
        }

        public VoxelOperatorFlags Flags
        {
            get { return VoxelOperatorFlags.Read; }
        }
    }

    public struct ShieldHit
    {
        public readonly MyEntity Attacker;
        public readonly float Amount;
        public readonly MyStringHash DamageType;
        public readonly Vector3D HitPos;

        public ShieldHit(MyEntity attacker, float amount, MyStringHash damageType, Vector3D hitPos)
        {

            Attacker = attacker;
            Amount = amount;
            DamageType = damageType;
            HitPos = hitPos;
        }
    }

    public class SubGridInfo
    {
        public readonly MyCubeGrid Grid;
        public readonly bool MainGrid;
        public readonly bool MechSub;
        public SubGridInfo(MyCubeGrid grid, bool mainGrid, bool mechSub)
        {
            Grid = grid;
            MainGrid = mainGrid;
            MechSub = mechSub;
        }
    }


    public struct MoverInfo
    {
        public readonly Vector3D Pos;
        public readonly uint CreationTick;
        public MoverInfo(Vector3D pos, uint creationTick)
        {
            Pos = pos;
            CreationTick = creationTick;
        }
    }

    public struct BlockState
    {
        public readonly MyCubeBlock CubeBlock;
        public readonly IMyFunctionalBlock FunctBlock;
        public readonly bool EnableState;
        public readonly uint StartTick;
        public readonly uint Endtick;
        public BlockState(MyCubeBlock cubeBlock, uint startTick, uint endTick)
        {
            CubeBlock = cubeBlock;
            StartTick = startTick;
            Endtick = endTick;
            FunctBlock = cubeBlock as IMyFunctionalBlock;
            EnableState = ((IMyFunctionalBlock)cubeBlock).Enabled;
        }
    }

    public class AmmoInfo
    {
        public readonly bool Explosive;
        public readonly float Damage;
        public readonly float Radius;
        public readonly float Speed;
        public readonly float Mass;
        public readonly float BackKickForce;

        public AmmoInfo(bool explosive, float damage, float radius, float speed, float mass, float backKickForce)
        {
            Explosive = explosive;
            Damage = damage;
            Radius = radius;
            Speed = speed;
            Mass = mass;
            BackKickForce = backKickForce;
        }
    }

    public class AmmoInfo2
    {
        public readonly bool Explosive;
        public readonly float Damage;
        public readonly float Radius;
        public readonly float Speed;
        public readonly float Mass;
        public readonly float BackKickForce;

        public readonly bool KineticWeapon; //0 is energy, 1 is kinetic
        public readonly bool HealingWeapon; //0 is damaging, 1 is healing
        public readonly bool BypassWeapon; //0 is normal, 1 is bypass
        public readonly float DmgMulti;
        public readonly float ShieldDamage;

        public AmmoInfo2(bool explosive, float damage, float radius, float speed, float mass, float backKickForce)
        {
            Explosive = explosive;
            Damage = damage;
            Radius = radius;
            Speed = (float)Math.Truncate(speed);
            Mass = mass;
            BackKickForce = (float)Math.Truncate(backKickForce);

            var backCompat = UtilsStatic.GetDmgMulti(backKickForce);
            if (Math.Abs(backCompat) >= 0.001) //back compat, != 0 might get weird
            {
                KineticWeapon = !Explosive;
                BypassWeapon = false;
                DmgMulti = backCompat;
                if (Mass < 0 && Radius <= 0) //ye olde heal check
                    HealingWeapon = true;
            }
            else if (BackKickForce < 0) //emulates the weirdest old behavior
            {
                KineticWeapon = !Explosive;
                BypassWeapon = false;
                DmgMulti = 0;
                ShieldDamage = float.NegativeInfinity; //bls gob no
                if (Mass < 0 && Radius <= 0)
                    ShieldDamage = -ShieldDamage;
                return;
            }
            else //new API
            {
                var slice = Math.Abs(backKickForce - Math.Truncate(backKickForce)) * 10;
                var opNum = (int)Math.Truncate(slice); ////gets first decimal digit
                DmgMulti = (float)Math.Truncate((slice - Math.Truncate(slice)) * 10); ////gets second decimal digit
                var uuid = (int)Math.Round(Math.Abs(speed - Math.Truncate(speed)) * 1000); ////gets UUID

                if (uuid != 537 || backKickForce >= 131072 || speed >= 16384)
                {   ////confirms UUID or if backkick/speed are out of range of float precision
                    KineticWeapon = !Explosive;
                    HealingWeapon = false;
                    BypassWeapon = false;
                    DmgMulti = 1;
                }
                else if (opNum == 8) ////8 is bypass, ignores all other flags
                {
                    KineticWeapon = !Explosive;
                    HealingWeapon = false;
                    BypassWeapon = true;
                    DmgMulti = 1;
                }
                else //eval flags
                {
                    if (Convert.ToBoolean(opNum & 1))  ////bitcheck first bit; 0 is fractional, 1 is whole num
                    {
                        if (Math.Abs(DmgMulti) <= 0.001d) ////fractional and mult 0 = no damage
                            DmgMulti = 10;
                    }
                    else DmgMulti /= 10;
                    KineticWeapon = Convert.ToBoolean(opNum & 2); //second bit; 0 is energy, 1 is kinetic
                    HealingWeapon = Convert.ToBoolean(opNum & 4); //third bit; 0 is damaging, 1 is healing
                }
            }

            if (Explosive)
                ShieldDamage = (Damage * (Radius * 0.5f)) * 7.5f * DmgMulti;
            else
                ShieldDamage = Mass * Speed * DmgMulti;
            //  shieldDamage = Mass * Math.Pow(Speed,2) * DmgMulti / 2; //kinetic equation
            if (HealingWeapon)
                ShieldDamage = -ShieldDamage;
        }
    }

    public class ProtectCache
    {
        public uint LastTick;
        public uint RefreshTick;
        public uint FirstTick;
        public DefenseShields.Ent Relation;
        public DefenseShields.Ent PreviousRelation;

        public void Init(uint firstTick, uint lastTick, uint refreshTick, DefenseShields.Ent relation, DefenseShields.Ent previousRelation)
        {
            FirstTick = firstTick;
            LastTick = lastTick;
            RefreshTick = refreshTick;
            Relation = relation;
            PreviousRelation = previousRelation;
        }

        public void Clean()
        {
            LastTick = 0;
            RefreshTick = 0;
            FirstTick = 0;
            Relation = DefenseShields.Ent.Ignore;
            PreviousRelation = DefenseShields.Ent.Ignore;
        }
    }

    public class EntIntersectInfo
    {
        public BoundingBox Box;
        public uint LastTick;
        public uint RefreshTick;
        public uint FirstTick;
        public DefenseShields.Ent Relation;
        public List<CubeAccel> CacheBlockList = new List<CubeAccel>();
        public bool RefreshNow;
        public bool EnemySafeInside;
        public bool WasInside;
        public volatile bool Touched;
        public volatile uint LastCollision;
        public int ConsecutiveCollisions;

        public void Init(bool touched, BoundingBox box, uint firstTick, uint lastTick, uint refreshTick, DefenseShields.Ent relation, bool wasInside)
        {
            Touched = touched;
            Box = box;
            FirstTick = firstTick;
            LastTick = lastTick;
            RefreshTick = refreshTick;
            Relation = relation;
            RefreshNow = true;
            WasInside = wasInside;
            if (relation == DefenseShields.Ent.EnemyInside) EnemySafeInside = true;
        }

        public EntIntersectInfo()
        {
        }

        public void Clean()
        {
            EnemySafeInside = false;
            RefreshNow = false;
            Touched = false;
            CacheBlockList.Clear();
            RefreshTick = 0;
            LastCollision = 0;
            ConsecutiveCollisions = 0;
            LastTick = 0;
        }
    }

    public struct MyForceData
    {
        public MyEntity Entity;
        public Vector3D Force;
        public Vector3D? Position;
        public Vector3D? Torque;
        public float? MaxSpeed;
        public bool Immediate;

        public MyForceData(MyEntity entity, Vector3D force, Vector3D? position, Vector3D? torque, float? maxSpeed, bool immediate)
        {
            Entity = entity;
            Force = force;
            Position = position;
            Torque = torque;
            MaxSpeed = maxSpeed;
            Immediate = immediate;
        }
    }

    public struct MyCollisionPhysicsData
    {
        public MyEntity Entity1;
        public bool E1IsStatic;
        public bool E1IsHeavier;
        public float Mass1;
        public Vector3D Com1;
        public Vector3D CollisionCorrection1;
        public Vector3D ImpDirection1;
        public Vector3D ImpPosition1;

        public Vector3D Force1;
        public Vector3D? ForcePos1;
        public Vector3D? ForceTorque1;

        public MyEntity Entity2;
        public bool E2IsStatic;
        public bool E2IsHeavier;
        public float Mass2;
        public Vector3D Com2;
        public Vector3D CollisionCorrection2;
        public Vector3D ImpDirection2;
        public Vector3D ImpPosition2;

        public Vector3D Force2;
        public Vector3D? ForcePos2;
        public Vector3D? ForceTorque2;

        public Vector3D CollisionAvg;
        public bool Immediate;

        public MyCollisionPhysicsData(MyEntity entity1, MyEntity entity2, bool e1IsStatic, bool e2IsStatic, bool e1IsHeavier, bool e2IsHeavier, float mass1, float mass2, Vector3D com1, Vector3D com2, Vector3D collisionCorrection1, Vector3D collisionCorrection2, Vector3D impDirection1, Vector3D impDirection2, Vector3D impPosition1, Vector3D impPosition2, Vector3D force1, Vector3D force2, Vector3D? forcePos1, Vector3D? forcePos2, Vector3D? forceTorque1, Vector3D? forceTorque2, Vector3D collisionAvg, bool immediate)
        {
            Entity1 = entity1;
            E1IsStatic = e1IsStatic;
            Mass1 = mass1;
            Com1 = com1;
            CollisionCorrection1 = collisionCorrection1;
            ImpDirection1 = impDirection1;
            ImpPosition1 = impPosition1;
            Force1 = force1;
            ForcePos1 = forcePos1;
            ForceTorque1 = forceTorque1;

            Entity2 = entity2;
            E2IsStatic = e2IsStatic;
            Mass2 = mass2;
            Com2 = com2;
            CollisionCorrection2 = collisionCorrection2;
            ImpDirection2 = impDirection2;
            ImpPosition2 = impPosition2;
            Force2 = force2;
            ForcePos2 = forcePos2;
            ForceTorque2 = forceTorque1;

            CollisionAvg = collisionAvg;
            Immediate = immediate;

            E1IsHeavier = e1IsHeavier;
            E2IsHeavier = e2IsHeavier;
        }
    }

    public struct CubeAccel
    {
        public IMySlimBlock Block;
        public MyCubeGrid Grid;
        public Vector3I BlockPos;
        public bool CubeExists; 
    }

    public class DamageHandlerHit
    {
        public bool Active = false;
        public MyEntity Attacker = null;
        public IMySlimBlock HitBlock = null;
    }

    public class MyProtectors
    {
        public readonly ConcurrentDictionary<DefenseShields, byte> Shields = new ConcurrentDictionary<DefenseShields, byte>();
        public int RefreshSlot;
        public uint CreationTick;
        public uint BlockingTick;
        public bool LastAttackerWasInside;
        public DefenseShields BlockingShield;
        public DefenseShields IntegrityShield;
        public long IgnoreAttackerId = -1;

        public void Init(int refreshSlot, uint creationTick)
        {
            RefreshSlot = refreshSlot;
            CreationTick = creationTick;
        }
    }

    public struct GetFitSeq
    {
        public readonly double SqrtStart;
        public readonly double SqrtEnd;
        public readonly float SeqMulti;
        public GetFitSeq(double sqrtStart, double sqrtEnd, float seqMulti)
        {
            SqrtStart = sqrtStart;
            SqrtEnd = sqrtEnd;
            SeqMulti = seqMulti;
        }
    }
}
