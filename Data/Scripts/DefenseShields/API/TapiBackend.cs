using System;
using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    internal class ApiBackend
    {
        private static readonly MyConcurrentPool<List<MyLineSegmentOverlapResult<MyEntity>>> SegmentPool = new MyConcurrentPool<List<MyLineSegmentOverlapResult<MyEntity>>>(10);
        internal readonly Dictionary<string, Delegate> ModApiMethods = new Dictionary<string, Delegate>()
        {
            ["RayAttackShield"] = new Func<IMyTerminalBlock, RayD, long, float, bool, bool, Vector3D?>(TAPI_RayAttackShield),
            ["LineAttackShield"] = new Func<IMyTerminalBlock, LineD, long, float, bool, bool, Vector3D?>(TAPI_LineAttackShield),
            ["IntersectEntToShieldFast"] = new Func<List<MyEntity>, RayD, bool, bool, long, float, MyTuple<bool, float>>(TAPI_IntersectEntToShieldFast),
            ["PointAttackShield"] = new Func<IMyTerminalBlock, Vector3D, long, float, bool, bool, bool, bool>(TAPI_PointAttackShield),
            ["PointAttackShieldExt"] = new Func<IMyTerminalBlock, Vector3D, long, float, bool, bool, bool, float?>(TAPI_PointAttackShieldExt),
            ["PointAttackShieldCon"] = new Func<IMyTerminalBlock, Vector3D, long, float, float, bool, bool, bool, float?>(TAPI_PointAttackShieldCon),
            ["SetShieldHeat"] = new Action<IMyTerminalBlock, int>(TAPI_SetShieldHeat),
            ["SetSkipLos"] = new Action<IMyTerminalBlock>(TAPI_SetSkipLos),
            ["OverLoadShield"] = new Action<IMyTerminalBlock>(TAPI_OverLoadShield),
            ["SetCharge"] = new Action<IMyTerminalBlock, float>(TAPI_SetCharge),
            ["RayIntersectShield"] = new Func<IMyTerminalBlock, RayD, Vector3D?>(TAPI_RayIntersectShield),
            ["LineIntersectShield"] = new Func<IMyTerminalBlock, LineD, Vector3D?>(TAPI_LineIntersectShield),
            ["PointInShield"] = new Func<IMyTerminalBlock, Vector3D, bool>(TAPI_PointInShield),
            ["GetShieldPercent"] = new Func<IMyTerminalBlock, float>(TAPI_GetShieldPercent),
            ["GetShieldHeat"] = new Func<IMyTerminalBlock, int>(TAPI_GetShieldHeatLevel),
            ["GetChargeRate"] = new Func<IMyTerminalBlock, float>(TAPI_GetChargeRate),
            ["HpToChargeRatio"] = new Func<IMyTerminalBlock, int>(TAPI_HpToChargeRatio),
            ["GetMaxCharge"] = new Func<IMyTerminalBlock, float>(TAPI_GetMaxCharge),
            ["GetCharge"] = new Func<IMyTerminalBlock, float>(TAPI_GetCharge),
            ["GetPowerUsed"] = new Func<IMyTerminalBlock, float>(TAPI_GetPowerUsed),
            ["GetPowerCap"] = new Func<IMyTerminalBlock, float>(TAPI_GetPowerCap),
            ["GetMaxHpCap"] = new Func<IMyTerminalBlock, float>(TAPI_GetMaxHpCap),
            ["IsShieldUp"] = new Func<IMyTerminalBlock, bool>(TAPI_IsShieldUp),
            ["ShieldStatus"] = new Func<IMyTerminalBlock, string>(TAPI_ShieldStatus),
            ["EntityBypass"] = new Func<IMyTerminalBlock, IMyEntity, bool, bool>(TAPI_EntityBypass),
            ["GridHasShield"] = new Func<IMyCubeGrid, bool>(TAPI_GridHasShield),
            ["GridShieldOnline"] = new Func<IMyCubeGrid, bool>(TAPI_GridShieldOnline),
            ["ProtectedByShield"] = new Func<IMyEntity, bool>(TAPI_ProtectedByShield),
            ["GetShieldBlock"] = new Func<IMyEntity, IMyTerminalBlock>(TAPI_GetShieldBlock),
            ["MatchEntToShieldFast"] = new Func<IMyEntity, bool, IMyTerminalBlock>(TAPI_MatchEntToShieldFast),
            ["MatchEntToShieldFastExt"] = new Func<MyEntity, bool, MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>, MyTuple<MatrixD, MatrixD>>?>(TAPI_MatchEntToShieldFastExt),
            ["ClosestShieldInLine"] = new Func<LineD, bool, MyTuple<float?, IMyTerminalBlock>>(TAPI_ClosestShieldInLine),
            ["IsShieldBlock"] = new Func<IMyTerminalBlock, bool>(TAPI_IsShieldBlock),
            ["GetClosestShield"] = new Func<Vector3D, IMyTerminalBlock>(TAPI_GetClosestShield),
            ["GetDistanceToShield"] = new Func<IMyTerminalBlock, Vector3D, double>(TAPI_GetDistanceToShield),
            ["GetClosestShieldPoint"] = new Func<IMyTerminalBlock, Vector3D, Vector3D?>(TAPI_GetClosestShieldPoint),
            ["GetShieldInfo"] = new Func<MyEntity, MyTuple<bool, bool, float, float, float, int>>(TAPI_GetShieldInfo),
            ["GetModulationInfo"] = new Func<MyEntity, MyTuple<bool, bool, float, float>>(TAPI_GetModulationInfo),
            ["GetFaceInfo"] = new Func<IMyTerminalBlock, Vector3D, bool, MyTuple<bool, int, int, float, float>>(TAPI_GetFaceInfo),
            ["AddAttacker"] = new Action<long>(TAPI_AddAttacker),
            ["IsBlockProtected"] = new Func<IMySlimBlock, bool>(TAPI_IsBlockProtected),

        };

        private readonly Dictionary<string, Delegate> _terminalPbApiMethods = new Dictionary<string, Delegate>()
        {
            ["RayIntersectShield"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, RayD, Vector3D?>(TAPI_RayIntersectShield),
            ["LineIntersectShield"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, LineD, Vector3D?>(TAPI_LineIntersectShield),
            ["PointInShield"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, bool>(TAPI_PointInShield),
            ["GetShieldPercent"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetShieldPercent),
            ["GetShieldHeat"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int>(TAPI_GetShieldHeatLevel),
            ["GetChargeRate"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetChargeRate),
            ["HpToChargeRatio"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int>(TAPI_HpToChargeRatio),
            ["GetMaxCharge"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetMaxCharge),
            ["GetCharge"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetCharge),
            ["GetPowerUsed"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetPowerUsed),
            ["GetPowerCap"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetPowerCap),
            ["GetMaxHpCap"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(TAPI_GetMaxHpCap),
            ["IsShieldUp"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>(TAPI_IsShieldUp),
            ["ShieldStatus"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, string>(TAPI_ShieldStatus),
            ["EntityBypass"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, VRage.Game.ModAPI.Ingame.IMyEntity, bool, bool>(TAPI_EntityBypass),
            ["EntityBypassPb"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool, bool>(TAPI_EntityBypass),
            ["GridHasShield"] = new Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>(TAPI_GridHasShield),
            ["GridShieldOnline"] = new Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>(TAPI_GridShieldOnline),
            ["ProtectedByShield"] = new Func<VRage.Game.ModAPI.Ingame.IMyEntity, bool>(TAPI_ProtectedByShield),
            ["GetShieldBlock"] = new Func<VRage.Game.ModAPI.Ingame.IMyEntity, Sandbox.ModAPI.Ingame.IMyTerminalBlock>(TAPI_GetShieldBlock),
            ["IsShieldBlock"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>(TAPI_IsShieldBlock),
            ["GetClosestShield"] = new Func<Vector3D, Sandbox.ModAPI.Ingame.IMyTerminalBlock>(TAPI_GetClosestShieldPb), // need to switch to entityId
            ["GetDistanceToShield"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, double>(TAPI_GetDistanceToShield),
            ["GetClosestShieldPoint"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, Vector3D?>(TAPI_GetClosestShieldPoint),
        };

        internal void Init()
        {
            var mod = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<string, Delegate>, IMyTerminalBlock>("DefenseSystemsAPI");
            mod.Getter = (b) => ModApiMethods;
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(mod);

            var pb = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<string, Delegate>, IMyTerminalBlock>("DefenseSystemsPbAPI");
            pb.Getter = (b) => _terminalPbApiMethods;
            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(pb);
        }

        // ModApi only methods below
        private static Vector3D? TAPI_RayAttackShield(IMyTerminalBlock block, RayD ray, long attackerId, float damage, bool energy, bool drawParticle)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return null;

            float? intersectDist;
            intersectDist = CustomCollision.IntersectEllipsoid(logic.DetectMatrixOutsideInv, logic.DetectMatrixOutside, ray);

            if (!intersectDist.HasValue) return null;
            var ellipsoid = intersectDist ?? 0;
            var hitPos = ray.Position + (ray.Direction * ellipsoid);

            if (energy) damage *= logic.DsState.State.ModulateKinetic;
            else damage *= logic.DsState.State.ModulateEnergy;

            if (Session.Instance.MpActive)
            {
                var damageType = energy ? Session.Instance.MPEnergy : Session.Instance.MPKinetic;
                logic.AddShieldHit(attackerId, damage, damageType, null, true, hitPos);
            }
            else
            {
                if (!drawParticle) logic.EnergyHit = DefenseShields.HitType.Other;
                else if (energy) logic.EnergyHit = DefenseShields.HitType.Energy;
                else logic.EnergyHit = DefenseShields.HitType.Kinetic;

                logic.ImpactSize = damage;
                logic.WorldImpactPosition = hitPos;
            }

            if (energy)
                logic.EnergyDamage += damage;
            else
                logic.KineticDamage += damage;

            logic.WebDamage = true;
            logic.Absorb += damage;


            return hitPos;
        }

        private static Vector3D? TAPI_LineAttackShield(IMyTerminalBlock block, LineD line, long attackerId, float damage, bool energy, bool drawParticle)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return null;

            var ray = new RayD(line.From, line.Direction);
            var intersectDist = CustomCollision.IntersectEllipsoid(logic.DetectMatrixOutsideInv, logic.DetectMatrixOutside, ray);

            if (!intersectDist.HasValue) return null;
            var ellipsoid = intersectDist ?? 0;
            if (ellipsoid > line.Length) return null;

            var hitPos = ray.Position + (ray.Direction * ellipsoid);

            if (energy) damage *= logic.DsState.State.ModulateKinetic;
            else damage *= logic.DsState.State.ModulateEnergy;

            if (Session.Instance.MpActive)
            {
                var damageType = energy ? Session.Instance.MPEnergy : Session.Instance.MPKinetic;
                logic.AddShieldHit(attackerId, damage, damageType, null, true, hitPos);
            }
            else
            {
                if (!drawParticle) logic.EnergyHit = DefenseShields.HitType.Other;
                else if (energy) logic.EnergyHit = DefenseShields.HitType.Energy;
                else logic.EnergyHit = DefenseShields.HitType.Kinetic;

                logic.ImpactSize = damage;
                logic.WorldImpactPosition = hitPos;
            }

            if (energy)
                logic.EnergyDamage += damage;
            else
                logic.KineticDamage += damage;

            logic.WebDamage = true;
            logic.Absorb += damage;

            return hitPos;
        }

        private static bool TAPI_PointAttackShield(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage, bool energy, bool drawParticle, bool posMustBeInside = false)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return false;
            if (posMustBeInside)
                if (!CustomCollision.PointInShield(pos, logic.DetectMatrixOutsideInv)) return false;

            if (energy) damage *= logic.DsState.State.ModulateKinetic;
            else damage *= logic.DsState.State.ModulateEnergy;

            if (Session.Instance.MpActive)
            {
                var damageType = energy ? Session.Instance.MPEnergy : Session.Instance.MPKinetic;
                logic.AddShieldHit(attackerId, damage, damageType, null, true, pos);
            }
            else
            {
                logic.ImpactSize = damage;
                logic.WorldImpactPosition = pos;
            }

            if (!drawParticle) logic.EnergyHit = DefenseShields.HitType.Other;
            else if (energy) logic.EnergyHit = DefenseShields.HitType.Energy;
            else logic.EnergyHit = DefenseShields.HitType.Kinetic;

            if (energy)
                logic.EnergyDamage += damage;
            else
                logic.KineticDamage += damage;

            logic.WebDamage = true;
            logic.Absorb += damage;

            return true;
        }

        private static float? TAPI_PointAttackShieldExt(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage, bool energy, bool drawParticle, bool posMustBeInside = false)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return null;
            if (posMustBeInside)
                if (!CustomCollision.PointInShield(pos, logic.DetectMatrixOutsideInv)) return null;

            float hpRemaining;
            var pendingDamage = logic.Absorb > 0 ? logic.Absorb : 0;

            var shieldHp = logic.DsState.State.Charge * DefenseShields.ConvToHp;
            if (energy) {
                damage *= logic.DsState.State.ModulateKinetic;
                hpRemaining = ((shieldHp - pendingDamage) - damage) / logic.DsState.State.ModulateEnergy;
            }
            else {
                damage *= logic.DsState.State.ModulateEnergy;
                hpRemaining = ((shieldHp - pendingDamage) - damage) / logic.DsState.State.ModulateKinetic;
            }

            if (Session.Instance.MpActive) {
                var damageType = energy ? Session.Instance.MPEnergy : Session.Instance.MPKinetic;
                logic.AddShieldHit(attackerId, damage, damageType, null, true, pos);
            }
            else {
                logic.ImpactSize = damage;
                logic.WorldImpactPosition = pos;
            }

            if (!drawParticle) logic.EnergyHit = DefenseShields.HitType.Other;
            else if (energy) logic.EnergyHit = DefenseShields.HitType.Energy;
            else logic.EnergyHit = DefenseShields.HitType.Kinetic;

            if (energy)
                logic.EnergyDamage += damage;
            else
                logic.KineticDamage += damage;

            logic.WebDamage = true;
            logic.Absorb += damage;

            return hpRemaining * 0.01f;
        }

        private static MyTuple<bool, int, int, float, float> TAPI_GetFaceInfo(IMyTerminalBlock block, Vector3D pos, bool posMustBeInside = false)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null)
                return new MyTuple<bool, int, int, float, float>();


            if (posMustBeInside && !CustomCollision.PointInShield(pos, logic.DetectMatrixOutsideInv))
                return new MyTuple<bool, int, int, float, float>();

            if (logic.DsSet.Settings.SideShunting)
            {
                var referenceLocalPosition = logic.MyGrid.PositionComp.LocalMatrixRef.Translation;
                var worldDirection = pos - referenceLocalPosition;
                var localPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(logic.MyGrid.PositionComp.LocalMatrixRef));
                var impactTransNorm = localPosition - logic.ShieldShapeMatrix.Translation;

                var boxMax = logic.ShieldShapeMatrix.Backward + logic.ShieldShapeMatrix.Right + logic.ShieldShapeMatrix.Up;
                var boxMin = -boxMax;
                var box = new BoundingBoxD(boxMin, boxMax);

                var maxWidth = box.Max.LengthSquared();
                Vector3D norm;
                Vector3D.Normalize(ref impactTransNorm, out norm);
                var testLine = new LineD(Vector3D.Zero, norm * maxWidth); //This is to ensure we intersect the box
                LineD testIntersection;
                box.Intersect(ref testLine, out testIntersection);
                var intersection = testIntersection.To;
                var projForward = Vector3D.IsZero(logic.ShieldShapeMatrix.Forward) ? Vector3D.Zero : intersection.Dot(logic.ShieldShapeMatrix.Forward) / logic.ShieldShapeMatrix.Forward.LengthSquared() * logic.ShieldShapeMatrix.Forward;
                int faceHit = -1;
                if (projForward.LengthSquared() >= 0.8 * logic.ShieldShapeMatrix.Forward.LengthSquared()) //if within the side thickness
                {
                    var dot = intersection.Dot(logic.ShieldShapeMatrix.Forward);
                    var face = dot > 0 ? Session.ShieldSides.Forward: Session.ShieldSides.Backward;
                    //Log.Line($"face: {logic.RealSideStates[face].Side}({face}) - redirected:{logic.RealSideStates[face].Redirected}");
                    if (logic.RealSideStates[face].Redirected)
                    {
                        //Log.Line($"hit: {face}");
                        faceHit = (int)face;
                    }
                }

                var projLeft = Vector3D.IsZero(logic.ShieldShapeMatrix.Left) ? Vector3D.Zero : intersection.Dot(logic.ShieldShapeMatrix.Left) / logic.ShieldShapeMatrix.Left.LengthSquared() * logic.ShieldShapeMatrix.Left;
                if (projLeft.LengthSquared() >= 0.8 * logic.ShieldShapeMatrix.Left.LengthSquared()) //if within the side thickness
                {
                    var dot = intersection.Dot(logic.ShieldShapeMatrix.Left);
                    var face = dot > 0 ? Session.ShieldSides.Left : Session.ShieldSides.Right;
                    var lengDiffSqr = projLeft.LengthSquared() - logic.ShieldShapeMatrix.Left.LengthSquared();
                    var validFace = faceHit == -1 || !MyUtils.IsZero(lengDiffSqr);

                    //Log.Line($"face: {logic.RealSideStates[face].Side}({face}) - redirected:{logic.RealSideStates[face].Redirected}");
                    if (validFace && logic.RealSideStates[face].Redirected)
                    {
                        //Log.Line($"hit: {face}");
                        faceHit = (int)face;
                    }

                }

                var projUp = Vector3D.IsZero(logic.ShieldShapeMatrix.Up) ? Vector3D.Zero : intersection.Dot(logic.ShieldShapeMatrix.Up) / logic.ShieldShapeMatrix.Up.LengthSquared() * logic.ShieldShapeMatrix.Up;
                if (projUp.LengthSquared() >= 0.8 * logic.ShieldShapeMatrix.Up.LengthSquared()) //if within the side thickness
                {
                    var dot = intersection.Dot(logic.ShieldShapeMatrix.Up);
                    var face = dot > 0 ? Session.ShieldSides.Up : Session.ShieldSides.Down;
                    var lengDiffSqr = projUp.LengthSquared() - logic.ShieldShapeMatrix.Up.LengthSquared();
                    var validFace = faceHit == -1 || !MyUtils.IsZero(lengDiffSqr);


                    //Log.Line($"face: {logic.RealSideStates[face].Side}({face}) - redirected:{logic.RealSideStates[face].Redirected}");
                    if (validFace && logic.RealSideStates[face].Redirected)
                    {                                                                                 
                        //Log.Line($"hit: {face}");
                        faceHit = (int)face;
                    }

                }
                var hitShuntedSide = faceHit != -1;
                var shuntedFaces = Math.Abs(logic.ShieldRedirectState.X) + Math.Abs(logic.ShieldRedirectState.Y) + Math.Abs(logic.ShieldRedirectState.Z);
                var shuntMod = !hitShuntedSide ? 1 - (shuntedFaces * Session.ShieldShuntBonus) : 1f;
                var preventBypassMod = MathHelper.Clamp(shuntedFaces * Session.ShieldBypassBonus, 0f, 1f);

                return new MyTuple<bool, int, int, float, float>(hitShuntedSide, faceHit, shuntedFaces, shuntMod, preventBypassMod);
            }

            return new MyTuple<bool, int, int, float, float>();
        }

        private static float? TAPI_PointAttackShieldCon(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage, float secondaryDamage, bool energy, bool drawParticle, bool posMustBeInside = false)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null)
                return null;

            if (posMustBeInside) {

                if (!CustomCollision.PointInShield(pos, logic.DetectMatrixOutsideInv)) 
                    return null;
            }

            float hpRemaining;
            
            var pendingDamage = logic.Absorb > 0 ? logic.Absorb : 0;
            
            var primaryDamage = damage;
            var shieldHp = logic.DsState.State.Charge * DefenseShields.ConvToHp;

            if (energy) {
                primaryDamage *= logic.DsState.State.ModulateKinetic;
                hpRemaining = ((shieldHp - pendingDamage) - primaryDamage) / logic.DsState.State.ModulateEnergy;
            }
            else {
                primaryDamage *= logic.DsState.State.ModulateEnergy;
                hpRemaining = ((shieldHp - pendingDamage) - primaryDamage) / logic.DsState.State.ModulateKinetic;
            }

            if (hpRemaining > 0) {

                damage += secondaryDamage;
                if (energy) {
                    damage *= logic.DsState.State.ModulateKinetic;
                    hpRemaining = ((shieldHp - pendingDamage) - damage) / logic.DsState.State.ModulateEnergy;
                }
                else {
                    damage *= logic.DsState.State.ModulateEnergy;
                    hpRemaining = ((shieldHp - pendingDamage) - damage) / logic.DsState.State.ModulateKinetic;
                }
            }
            else {
                damage = primaryDamage;
            }

            if (Session.Instance.MpActive) {
                var damageType = energy ? Session.Instance.MPEnergy : Session.Instance.MPKinetic;
                logic.AddShieldHit(attackerId, damage, damageType, null, true, pos);
            }
            else {
                logic.ImpactSize = damage;
                logic.WorldImpactPosition = pos;
            }

            if (!drawParticle) logic.EnergyHit = DefenseShields.HitType.Other;
            else if (energy) logic.EnergyHit = DefenseShields.HitType.Energy;
            else logic.EnergyHit = DefenseShields.HitType.Kinetic;

            if (energy)
                logic.EnergyDamage += damage;
            else
                logic.KineticDamage += damage;

            logic.WebDamage = true;
            logic.Absorb += damage;
            return hpRemaining * 0.01f;
        }

        private static void TAPI_SetSkipLos(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic?.ShieldComp == null) return;

            logic.ShieldComp.SkipLos = true;
            logic.ShieldComp.CheckEmitters = true;
        }

        private static void TAPI_SetShieldHeat(IMyTerminalBlock block, int value)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null || !Session.Instance.IsServer) return;

            logic.DsState.State.Heat = value;
        }

        private static void TAPI_OverLoadShield(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null || !Session.Instance.IsServer) return;

            logic.DsState.State.Charge = -(logic.ShieldMaxCharge * 2);
        }


        private static void TAPI_SetCharge(IMyTerminalBlock block, float value)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null || !Session.Instance.IsServer) return;

            logic.DsState.State.Charge = value;
        }

        // ModApi and PB methods below.
        private static Vector3D? TAPI_RayIntersectShield(IMyTerminalBlock block, RayD ray)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return null;

            var intersectDist = CustomCollision.IntersectEllipsoid(logic.DetectMatrixOutsideInv, logic.DetectMatrixOutside, ray);

            if (!intersectDist.HasValue) return null;
            var ellipsoid = intersectDist ?? 0;
            return ray.Position + (ray.Direction * ellipsoid);
        }

        private static Vector3D? TAPI_LineIntersectShield(IMyTerminalBlock block, LineD line)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return null;
            var ray = new RayD(line.From, line.Direction);

            var intersectDist = CustomCollision.IntersectEllipsoid(logic.DetectMatrixOutsideInv, logic.DetectMatrixOutside, ray);

            if (!intersectDist.HasValue) return null;
            var ellipsoid = intersectDist ?? 0;
            if (ellipsoid > line.Length) return null;
            return ray.Position + (ray.Direction * ellipsoid);
        }

        private static bool TAPI_PointInShield(IMyTerminalBlock block, Vector3D pos)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return false;

            var pointInShield = CustomCollision.PointInShield(pos, logic.DetectMatrixOutsideInv);
            return pointInShield;
        }

        private static float TAPI_GetShieldPercent(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.DsState.State.ShieldPercent;
        }

        private static int TAPI_GetShieldHeatLevel(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.DsState.State.Heat;
        }

        private static int TAPI_HpToChargeRatio(IMyTerminalBlock block)
        {
            return DefenseShields.ConvToHp;
        }

        private static float TAPI_GetChargeRate(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.ShieldChargeRate * DefenseShields.ConvToDec;
        }

        private static float TAPI_GetMaxCharge(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.ShieldMaxCharge;
        }

        private static float TAPI_GetCharge(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.DsState.State.Charge;
        }

        private static float TAPI_GetPowerUsed(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.ShieldCurrentPower;
        }

        private static float TAPI_GetPowerCap(IMyTerminalBlock block)
        {
            return float.MinValue;
        }

        private static float TAPI_GetMaxHpCap(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return logic.ShieldMaxCharge * DefenseShields.ConvToDec;
        }

        private static bool TAPI_IsShieldUp(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return false;

            return logic.DsState.State.Online && !logic.DsState.State.Lowered;
        }

        private static string TAPI_ShieldStatus(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return string.Empty;

            return logic.GetShieldStatus();
        }

        private static bool TAPI_EntityBypass(IMyTerminalBlock block, IMyEntity entity, bool remove)
        {
            var ent = (MyEntity)entity;
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null || ent == null) return false;

            var success = remove ? logic.EntityBypass.Remove(ent) : logic.EntityBypass.Add(ent);

            return success;
        }

        private static bool TAPI_GridHasShield(IMyCubeGrid grid)
        {
            if (grid == null) return false;

            MyProtectors protectors;
            var myGrid = (MyCubeGrid)grid;

            if (Session.Instance.GlobalProtect.TryGetValue(myGrid, out protectors)) {

                foreach (var s in protectors.Shields.Keys)
                    if (s?.ShieldComp != null && s.ShieldComp.SubGrids.ContainsKey(myGrid)) return true;
            }
            return false;
        }

        private static bool TAPI_GridShieldOnline(IMyCubeGrid grid)
        {
            if (grid == null) return false;

            MyProtectors protectors;
            var myGrid = (MyCubeGrid)grid;
            if (Session.Instance.GlobalProtect.TryGetValue(myGrid, out protectors))
            {
                foreach (var s in protectors.Shields.Keys)
                {
                    if (s?.ShieldComp != null && s.ShieldComp.SubGrids.ContainsKey(myGrid) && s.DsState.State.Online && !s.DsState.State.Lowered) return true;
                }
            }
            return false;
        }

        private static bool TAPI_ProtectedByShield(IMyEntity entity)
        {
            if (entity == null) return false;

            MyProtectors protectors;
            var ent = (MyEntity)entity;
            if (Session.Instance.GlobalProtect.TryGetValue(ent, out protectors))
            {
                if (protectors?.Shields == null) return false;

                foreach (var s in protectors.Shields.Keys)
                {
                    if (s?.DsState?.State == null) continue;
                    if (s.DsState.State.Online && !s.DsState.State.Lowered) return true;
                }
            }
            return false;
        }

        private static IMyTerminalBlock TAPI_GetShieldBlock(IMyEntity entity)
        {
            var ent = entity as MyEntity;
            if (ent == null) return null;

            MyProtectors protectors;
            if (Session.Instance.GlobalProtect.TryGetValue(ent, out protectors))
            {
                DefenseShields firstShield = null;
                var grid = ent as MyCubeGrid;
                foreach (var s in protectors.Shields.Keys)
                {
                    if (s == null) continue;

                    if (firstShield == null) firstShield = s;

                    if (grid != null && s.ShieldComp?.SubGrids != null && s.ShieldComp.SubGrids.ContainsKey(grid)) 
                       return s.MyCube as IMyTerminalBlock;
                }
                if (firstShield != null) return firstShield.MyCube as IMyTerminalBlock;
            }
            return null;
        }

        private static IMyTerminalBlock TAPI_MatchEntToShieldFast(IMyEntity entity, bool onlyIfOnline)
        {
            if (entity == null) return null;
            ShieldGridComponent c;
            if (Session.Instance.IdToBus.TryGetValue(entity.EntityId, out c) && c?.DefenseShields != null)
            {
                using (c.DefenseShields?.MyCube?.Pin())
                {
                    if (c.DefenseShields?.MyCube == null || c.DefenseShields.MyCube.MarkedForClose || onlyIfOnline && (!c.DefenseShields.DsState.State.Online || c.DefenseShields.DsState.State.Lowered) || c.DefenseShields.ReInforcedShield) return null;
                    return c.DefenseShields.Shield;
                }
            }

            return null;
        }

        private static MyTuple<bool, float> TAPI_IntersectEntToShieldFast(List<MyEntity> entities, RayD ray, bool onlyIfOnline, bool enenmyOnly = false, long requesterId = 0, float maxLengthSqr = float.MaxValue)
        {
            if (enenmyOnly)
                if (requesterId == 0)
                    return new MyTuple<bool, float>(false, 0);

            float closestOtherDist = float.MaxValue;
            float closestFriendDist = float.MaxValue;
            bool closestOther = false;
            bool closestFriend = false;

            for (int i = 0; i < entities.Count; i++) {

                var entity = entities[i];
                ShieldGridComponent c;
                if (entity != null && Session.Instance.IdToBus.TryGetValue(entity.EntityId, out c) && c?.DefenseShields?.DsState?.State != null && c.DefenseShields.MyCube != null) {
                    
                    var s = c.DefenseShields;
                    if (onlyIfOnline && (!s.DsState.State.Online || s.DsState.State.Lowered) || s.ReInforcedShield)
                        continue;


                    var normSphere = new BoundingSphereD(Vector3.Zero, 1f);
                    var kRay = new RayD(Vector3D.Zero, Vector3D.Forward);

                    var ellipsoidMatrixInv = s.DetectMatrixOutsideInv;
                    Vector3D krayPos;
                    Vector3D.Transform(ref ray.Position, ref ellipsoidMatrixInv, out krayPos);

                    Vector3D nDir;
                    Vector3D.TransformNormal(ref ray.Direction, ref ellipsoidMatrixInv, out nDir);

                    Vector3D krayDir;
                    Vector3D.Normalize(ref nDir, out krayDir);

                    kRay.Direction = krayDir;
                    kRay.Position = krayPos;
                    var nullDist = normSphere.Intersects(kRay);

                    if (!nullDist.HasValue)
                        continue;

                    var hitPos = krayPos + (krayDir * -nullDist.Value);

                    var ellipsoidMatrix = s.DetectMatrixOutside;
                    Vector3D worldHitPos;
                    Vector3D.Transform(ref hitPos, ref ellipsoidMatrix, out worldHitPos);
                    var intersectDist = Vector3.DistanceSquared(worldHitPos, ray.Position);
                    if (intersectDist <= 0 || intersectDist > maxLengthSqr)
                        continue;

                    var firstOrLast = enenmyOnly && (!closestFriend || intersectDist < closestFriendDist);
                    var notEnemyCheck = false;
                    if (firstOrLast)
                    {
                        var relationship = MyIDModule.GetRelationPlayerBlock(requesterId, s.MyCube.OwnerId);
                        var enemy = relationship != MyRelationsBetweenPlayerAndBlock.Owner && relationship != MyRelationsBetweenPlayerAndBlock.FactionShare;
                        notEnemyCheck = !enemy;
                    }

                    if (notEnemyCheck) {
                        closestFriendDist = intersectDist;
                        closestFriend = true;
                    }
                    else {
                        closestOtherDist = intersectDist;
                        closestOther = true;
                    }
                }
            }

            if (!enenmyOnly && closestOther || closestOther && !closestFriend)
            {
                return new MyTuple<bool, float>(true, closestOtherDist);
            }

            if (closestFriend && !closestOther || closestFriendDist < closestOtherDist)
            {
                return new MyTuple<bool, float>(false, closestFriendDist);
            }

            if (!closestOther)
            {
                return new MyTuple<bool, float>(false, 0);
            }

            return new MyTuple<bool, float>(true, closestOtherDist);
        }

        private static MyTuple<bool, bool, float, float, float, int> TAPI_GetShieldInfo(MyEntity entity)
        {
            var info = new MyTuple<bool, bool, float, float, float, int>();

            if (entity == null) return info;
            ShieldGridComponent c;
            if (Session.Instance.IdToBus.TryGetValue(entity.EntityId, out c) && c?.DefenseShields != null)
            {
                var s = c.DefenseShields;
                info.Item1 = true;
                var state = s.DsState.State;
                if (state.Online)
                {
                    info.Item2 = true;
                    info.Item3 = state.Charge;
                    info.Item4 = s.ShieldMaxCharge;
                    info.Item5 = state.ShieldPercent;
                    info.Item6 = state.Heat;
                }
            }

            return info;
        }

        private static MyTuple<bool, bool, float, float> TAPI_GetModulationInfo(MyEntity entity)
        {
            var info = new MyTuple<bool, bool, float, float>();

            if (entity == null) return info;
            ShieldGridComponent c;
            if (Session.Instance.IdToBus.TryGetValue(entity.EntityId, out c) && c?.DefenseShields != null)
            {
                var s = c.DefenseShields;
                var state = s.DsState.State;
                info.Item1 = state.ReInforce;
                info.Item2 = state.EmpProtection;
                info.Item3 = state.ModulateKinetic;
                info.Item4 = state.ModulateEnergy;
            }

            return info;
        }

        private static MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>, MyTuple<MatrixD, MatrixD>>? TAPI_MatchEntToShieldFastExt(MyEntity entity, bool onlyIfOnline)
        {
            if (entity == null) return null;
            ShieldGridComponent c;
            if (Session.Instance.IdToBus.TryGetValue(entity.EntityId, out c) && c?.DefenseShields != null)
            {
                if (onlyIfOnline && (!c.DefenseShields.DsState.State.Online || c.DefenseShields.DsState.State.Lowered) || c.DefenseShields.ReInforcedShield) return null;
                var s = c.DefenseShields;
                var state = s.DsState.State;
                var info = new MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>, MyTuple<MatrixD, MatrixD>>
                {
                    Item1 = s.Shield,
                    Item2 =
                    {
                        Item1 = true,
                        Item2 = s.DsSet.Settings.SideShunting,
                        Item3 = state.Charge,
                        Item4 = s.ShieldMaxCharge,
                        Item5 = state.ShieldPercent,
                        Item6 = state.Heat,
                    },
                    Item3 = { Item1 = s.DetectMatrixOutsideInv, Item2 = s.DetectMatrixOutside }
                };
                return info;
            }
            return null;
        }

        private static MyTuple<float?, IMyTerminalBlock> TAPI_ClosestShieldInLine(LineD line, bool onlyIfOnline)
        {
            var segment = SegmentPool.Get();
            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref line, segment, MyEntityQueryType.Dynamic);
            var ray = new RayD(line.From, line.Direction);

            var closest = float.MaxValue;
            IMyTerminalBlock closestShield = null;
            for (int i = 0; i < segment.Count; i++)
            {
                var ent = segment[i].Element;
                if (ent == null || ent.Physics != null && !ent.Physics.IsPhantom) continue;
                ShieldGridComponent c;
                if (Session.Instance.IdToBus.TryGetValue(ent.EntityId, out c) && c.DefenseShields != null)
                {
                    if (onlyIfOnline && (!c.DefenseShields.DsState.State.Online || c.DefenseShields.DsState.State.Lowered)) continue;
                    var s = c.DefenseShields;
                    var intersectDist = CustomCollision.IntersectEllipsoid(s.DetectMatrixOutsideInv, s.DetectMatrixOutside, ray);
                    if (!intersectDist.HasValue) continue;
                    var ellipsoid = intersectDist ?? 0;
                    if (ellipsoid > line.Length || ellipsoid > closest || CustomCollision.PointInShield(ray.Position, s.DetectMatrixOutsideInv)) continue;
                    closest = ellipsoid;
                    closestShield = s.Shield;
                }
            }
            segment.Clear();
            SegmentPool.Return(segment);
            var response = new MyTuple<float?, IMyTerminalBlock>();
            if (closestShield == null)
            {
                response.Item1 = null;
                response.Item2 = null;
                return response;
            }
            response.Item1 = closest;
            response.Item2 = closestShield;
            return response;
        }

        private static bool TAPI_IsShieldBlock(IMyTerminalBlock block)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>();
            return logic != null;
        }

        private static IMyTerminalBlock TAPI_GetClosestShield(Vector3D pos)
        {
            MyCubeBlock cloestSBlock = null;
            var closestDist = double.MaxValue;
            foreach (var s in Session.Instance.ActiveShields.Keys)
            {
                if (Vector3D.DistanceSquared(s.DetectionCenter, pos) > Session.Instance.SyncDistSqr) continue;

                var sDist = CustomCollision.EllipsoidDistanceToPos(s.DetectMatrixOutsideInv, s.DetectMatrixOutside, pos);
                if (sDist > 0 && sDist < closestDist)
                {
                    cloestSBlock = s.MyCube;
                    closestDist = sDist;
                }
            }
            return cloestSBlock as IMyTerminalBlock;
        }

        private static IMyTerminalBlock TAPI_GetClosestShieldPb(Vector3D pos)
        {
            return null;
        }

        private static double TAPI_GetDistanceToShield(IMyTerminalBlock block, Vector3D pos)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return -1;

            return CustomCollision.EllipsoidDistanceToPos(logic.DetectMatrixOutsideInv, logic.DetectMatrixOutside, pos);
        }

        private static Vector3D? TAPI_GetClosestShieldPoint(IMyTerminalBlock block, Vector3D pos)
        {
            var logic = block?.GameLogic?.GetAs<DefenseShields>()?.ShieldComp?.DefenseShields;
            if (logic == null) return null;

            Vector3D? closestShieldPoint = CustomCollision.ClosestEllipsoidPointToPos(logic.DetectMatrixOutsideInv, logic.DetectMatrixOutside, pos);

            return closestShieldPoint;
        }

        private static void TAPI_AddAttacker(long attacker)
        {
            Session.Instance.ManagedAttackers[attacker] = byte.MaxValue;
        }

        private static bool TAPI_IsBlockProtected(IMySlimBlock block)
        {

            if (block == null) return false;
            var grid = (MyCubeGrid)block.CubeGrid;
            ShieldGridComponent c;
            if (Session.Instance.IdToBus.TryGetValue(grid.EntityId, out c) && c?.DefenseShields != null)
            {
                var ds = c.DefenseShields;
                var pointInShield = Vector3D.Transform(grid.GridIntegerToWorld(block.Position), ref ds.DetectMatrixOutsideInv).LengthSquared() <= 1;
                return pointInShield;
            }
            return false;
        }

        // PB overloads
        private static Vector3D? TAPI_RayIntersectShield(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, RayD arg2) => TAPI_RayIntersectShield(arg1 as IMyTerminalBlock, arg2);
        private static Vector3D? TAPI_LineIntersectShield(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, LineD arg2) => TAPI_LineIntersectShield(arg1 as IMyTerminalBlock, arg2);
        private static bool TAPI_PointInShield(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, Vector3D arg2) => TAPI_PointInShield(arg1 as IMyTerminalBlock, arg2);
        private static float TAPI_GetShieldPercent(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetShieldPercent(arg as IMyTerminalBlock);
        private static int TAPI_GetShieldHeatLevel(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetShieldHeatLevel(arg as IMyTerminalBlock);
        private static float TAPI_GetChargeRate(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetChargeRate(arg as IMyTerminalBlock);
        private static int TAPI_HpToChargeRatio(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_HpToChargeRatio(arg as IMyTerminalBlock);
        private static float TAPI_GetMaxCharge(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetMaxCharge(arg as IMyTerminalBlock);
        private static float TAPI_GetCharge(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetCharge(arg as IMyTerminalBlock);
        private static float TAPI_GetPowerUsed(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetPowerUsed(arg as IMyTerminalBlock);
        private static float TAPI_GetPowerCap(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetPowerCap(arg as IMyTerminalBlock);
        private static bool TAPI_IsShieldBlock(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_IsShieldBlock(arg as IMyTerminalBlock);
        private static float TAPI_GetMaxHpCap(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_GetMaxHpCap(arg as IMyTerminalBlock);
        private static string TAPI_ShieldStatus(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_ShieldStatus(arg as IMyTerminalBlock);
        private static bool TAPI_EntityBypass(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, VRage.Game.ModAPI.Ingame.IMyEntity arg2, bool arg3) =>TAPI_EntityBypass(arg1 as IMyTerminalBlock, arg2 as IMyEntity, arg3);
        private static bool TAPI_EntityBypass(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, long arg2, bool arg3) => TAPI_EntityBypass(arg1 as IMyTerminalBlock, MyEntities.GetEntityById(arg2), arg3);

        private static bool TAPI_GridHasShield(VRage.Game.ModAPI.Ingame.IMyCubeGrid arg) => TAPI_GridHasShield(arg as IMyCubeGrid);
        private static bool TAPI_GridShieldOnline(VRage.Game.ModAPI.Ingame.IMyCubeGrid arg) => TAPI_GridShieldOnline(arg as IMyCubeGrid);
        private static bool TAPI_ProtectedByShield(VRage.Game.ModAPI.Ingame.IMyEntity arg) => TAPI_ProtectedByShield(arg as IMyEntity);
        private static bool TAPI_IsShieldUp(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg) => TAPI_IsShieldUp(arg as IMyTerminalBlock);
        private static Sandbox.ModAPI.Ingame.IMyTerminalBlock TAPI_GetShieldBlock(VRage.Game.ModAPI.Ingame.IMyEntity arg) => TAPI_GetShieldBlock(arg as IMyEntity);
        private static double TAPI_GetDistanceToShield(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, Vector3D arg2) => TAPI_GetDistanceToShield(arg1 as IMyTerminalBlock, arg2);
        private static Vector3D? TAPI_GetClosestShieldPoint(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, Vector3D arg2) => TAPI_GetClosestShieldPoint(arg1 as IMyTerminalBlock, arg2);

    }
}
