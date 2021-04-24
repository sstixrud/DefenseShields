using VRage.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

using Color = VRageMath.Color;
using Quaternion = VRageMath.Quaternion;
using Vector3 = VRageMath.Vector3;

namespace DefenseShields.Support
{
    internal static class UtilsStatic
    {
        public static void PrepConfigFile()
        {
            const int BaseScaler = 175;
            const float HeatScaler = 0.004f;
            const float Unused = 0f;
            const int StationRatio = 100;
            const int LargeShipRate = 100;
            const int SmallShipRatio = 100;
            const int DisableVoxel = 0;
            const int DisableEntityBarrier = 0;
            const int Debug = 1;
            const int SuperWeapons = 1;
            const int Version = 79;
            const float CapScaler = 1f;
            const float HpsEfficiency = 0.25f;
            const float MaintenanceCost = 0.5f;
            const int DisableBlockDamage = 0;
            const int DisableLineOfSight = 0;
            const int OverloadTime = 2700;

            var dsCfgExists = MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg");
            if (dsCfgExists)
            {
                var unPackCfg = MyAPIGateway.Utilities.ReadFileInGlobalStorage("DefenseShields.cfg");
                var unPackedData = MyAPIGateway.Utilities.SerializeFromXML<DefenseShieldsEnforcement>(unPackCfg.ReadToEnd());

                var invalidValue = unPackedData.HpsEfficiency <= 0 || unPackedData.BaseScaler < 1 || unPackedData.MaintenanceCost <= 0;
                if (invalidValue)
                {
                    if (unPackedData.HpsEfficiency <= 0) unPackedData.HpsEfficiency = HpsEfficiency;
                    if (unPackedData.BaseScaler < 1) unPackedData.BaseScaler = BaseScaler;
                    if (unPackedData.MaintenanceCost <= 0) unPackedData.MaintenanceCost = MaintenanceCost;
                }
                if (unPackedData.Version == Version && !invalidValue) return;

                if (!invalidValue) Log.Line($"outdated config file regenerating, file version: {unPackedData.Version} - current version: {Version}");
                else Log.Line("Invalid config file, fixing");

                Session.Enforced.BaseScaler = !unPackedData.BaseScaler.Equals(-1) ? unPackedData.BaseScaler : BaseScaler;
                Session.Enforced.HeatScaler = !unPackedData.HeatScaler.Equals(-1f) ? unPackedData.HeatScaler : HeatScaler;
                Session.Enforced.Unused = !unPackedData.Unused.Equals(-1f) ? unPackedData.Unused : Unused;
                Session.Enforced.StationRatio = !unPackedData.StationRatio.Equals(-1) ? unPackedData.StationRatio : StationRatio;
                Session.Enforced.LargeShipRatio = !unPackedData.LargeShipRatio.Equals(-1) ? unPackedData.LargeShipRatio : LargeShipRate;
                Session.Enforced.SmallShipRatio = !unPackedData.SmallShipRatio.Equals(-1) ? unPackedData.SmallShipRatio : SmallShipRatio;
                Session.Enforced.DisableVoxelSupport = !unPackedData.DisableVoxelSupport.Equals(-1) ? unPackedData.DisableVoxelSupport : DisableVoxel;
                Session.Enforced.DisableEntityBarrier = !unPackedData.DisableEntityBarrier.Equals(-1) ? unPackedData.DisableEntityBarrier : DisableEntityBarrier;
                Session.Enforced.Debug = !unPackedData.Debug.Equals(-1) ? unPackedData.Debug : Debug;
                Session.Enforced.SuperWeapons = !unPackedData.SuperWeapons.Equals(-1) ? unPackedData.SuperWeapons : SuperWeapons;
                Session.Enforced.CapScaler = !unPackedData.CapScaler.Equals(-1f) ? unPackedData.CapScaler : CapScaler;
                Session.Enforced.HpsEfficiency = !unPackedData.HpsEfficiency.Equals(-1f) ? unPackedData.HpsEfficiency : HpsEfficiency;
                Session.Enforced.MaintenanceCost = !unPackedData.MaintenanceCost.Equals(-1f) ? unPackedData.MaintenanceCost : MaintenanceCost;
                Session.Enforced.DisableBlockDamage = !unPackedData.DisableBlockDamage.Equals(-1) ? unPackedData.DisableBlockDamage : DisableBlockDamage;
                Session.Enforced.DisableLineOfSight = !unPackedData.DisableLineOfSight.Equals(-1) ? unPackedData.DisableLineOfSight : DisableLineOfSight;
                Session.Enforced.OverloadTime = !unPackedData.OverloadTime.Equals(-1) ? unPackedData.OverloadTime : OverloadTime;

                if (unPackedData.Version <= 78)
                {
                    Session.Enforced.OverloadTime = 2700;
                }
                Session.Enforced.Version = Version;
                UpdateConfigFile(unPackCfg);
            }
            else
            {
                Session.Enforced.BaseScaler = BaseScaler;
                Session.Enforced.HeatScaler = HeatScaler;
                Session.Enforced.Unused = Unused;
                Session.Enforced.StationRatio = StationRatio;
                Session.Enforced.LargeShipRatio = LargeShipRate;
                Session.Enforced.SmallShipRatio = SmallShipRatio;
                Session.Enforced.DisableVoxelSupport = DisableVoxel;
                Session.Enforced.DisableEntityBarrier = DisableEntityBarrier;
                Session.Enforced.Debug = Debug;
                Session.Enforced.SuperWeapons = SuperWeapons;
                Session.Enforced.CapScaler = CapScaler;
                Session.Enforced.HpsEfficiency = HpsEfficiency;
                Session.Enforced.MaintenanceCost = MaintenanceCost;
                Session.Enforced.Version = Version;
                Session.Enforced.DisableBlockDamage = DisableBlockDamage;
                Session.Enforced.DisableLineOfSight = DisableLineOfSight;
                Session.Enforced.OverloadTime = OverloadTime;

                WriteNewConfigFile();

                Log.Line($"wrote new config file - file exists: {MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg")}");
            }
        }

        public static void ReadConfigFile()
        {
            var dsCfgExists = MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg");

            if (Session.Enforced.Debug == 3) Log.Line($"Reading config, file exists? {dsCfgExists}");

            if (!dsCfgExists) return;

            var cfg = MyAPIGateway.Utilities.ReadFileInGlobalStorage("DefenseShields.cfg");
            var data = MyAPIGateway.Utilities.SerializeFromXML<DefenseShieldsEnforcement>(cfg.ReadToEnd());
            Session.Enforced = data;

            if (Session.Enforced.Debug == 3) Log.Line($"Writing settings to mod:\n{data}");
        }

        public static float ComputeAmmoDamage(IMyEntity ammoEnt)
        {
            AmmoInfo ammoInfo;
            Session.Instance.AmmoCollection.TryGetValue(ammoEnt.Model.AssetName, out ammoInfo);
            var damage = 10f;
            if (ammoInfo == null)
            {
                return damage;
            }
            var dmgMulti = GetDmgMulti(ammoInfo.BackKickForce);
            if (dmgMulti > 0)
            {
                if (ammoInfo.Explosive) damage = (ammoInfo.Damage * (ammoInfo.Radius * 0.5f)) * 7.5f * dmgMulti;
                else damage = ammoInfo.Mass * ammoInfo.Speed * dmgMulti;
                return damage;
            }
            if (dmgMulti.Equals(-1f))
            {
                damage = -damage;
                return damage;
            }
            if (ammoInfo.BackKickForce < 0 && dmgMulti.Equals(0)) damage = float.NegativeInfinity;
            else if (ammoInfo.Explosive) damage = ammoInfo.Damage * (ammoInfo.Radius * 0.5f) * 7.5f;
            else damage = ammoInfo.Mass * ammoInfo.Speed;

            if (ammoInfo.Mass < 0 && ammoInfo.Radius <= 0) damage = -damage;
            return damage;
        }

        private static void Deviation(List<MyCubeBlock> blocks)
        {
            double avgX = 0;
            double avgY = 0;
            double avgZ = 0;

            for (int i = 0; i < blocks.Count; i++)
            {

                var cube = blocks[i];
                avgX = (cube.Min.X + cube.Max.X) * cube.CubeGrid.GridSize / 2.0;
                avgY = (cube.Min.Y + cube.Max.Y) * cube.CubeGrid.GridSize / 2.0;
                avgZ = (cube.Min.Z + cube.Max.Z) * cube.CubeGrid.GridSize / 2.0;
            }

            double devX = 0;
            double devY = 0;
            double devZ = 0;

            for (int i = 0; i < blocks.Count; i++)
            {

                var cube = blocks[i];
                var dx = ((cube.Min.X + cube.Max.X) * cube.CubeGrid.GridSize / 2.0) - avgX;
                var dy = ((cube.Min.Y + cube.Max.Y) * cube.CubeGrid.GridSize / 2.0) - avgY;
                var dz = ((cube.Min.Z + cube.Max.Z) * cube.CubeGrid.GridSize / 2.0) - avgY;
                devX += dx * dx;
                devY += dy * dy;
                devZ += dz * dz;
            }
            devX = Math.Sqrt(devX / blocks.Count);
            devY = Math.Sqrt(devY / blocks.Count);
            devZ = Math.Sqrt(devZ / blocks.Count);

        }

        public static float ComputeAmmoDamage2(IMyEntity ammoEnt)
        {
            AmmoInfo ammoInfo;
            Session.Instance.AmmoCollection.TryGetValue(ammoEnt.Model.AssetName, out ammoInfo);
            var damage = 10f;
            if (ammoInfo == null)
            {
                return damage;
            }
            /*
            if (ammoInfo.KineticWeapon)
                damage = ammoInfo.shieldDamage * DsState.State.ModulateEnergy; //kinetic weapon
            else
                damage = ammoInfo.shieldDamage * DsState.State.ModulateKinetic; //energy weapon
            */
            return damage;
        }

        public static void FibonacciSeq(int magicNum)
        {
            var root5 = Math.Sqrt(5);
            var phi = (1 + root5) / 2;

            var n = 0;
            int Fn;
            do
            {
                Fn = (int)((Math.Pow(phi, n) - Math.Pow(-phi, -n)) / ((2 * phi) - 1));
                //Console.Write("{0} ", Fn);
                ++n;
            }
            while (Fn < magicNum);
        }

        public static void SphereCloud(int pointLimit, Vector3D[] physicsArray, MyEntity shieldEnt, bool transformAndScale, bool debug, Random rnd = null)
        {
            if (pointLimit > 10000) pointLimit = 10000;
            if (rnd == null) rnd = new Random(0);

            var sPosComp = shieldEnt.PositionComp;
            var unscaledPosWorldMatrix = MatrixD.Rescale(MatrixD.CreateTranslation(sPosComp.WorldAABB.Center), sPosComp.WorldVolume.Radius);
            var radius = sPosComp.WorldVolume.Radius;
            for (int i = 0; i < pointLimit; i++)
            {
                var value = rnd.Next(0, physicsArray.Length - 1);
                var phi = 2 * Math.PI * i / pointLimit;
                var x = (float)(radius * Math.Sin(phi) * Math.Cos(value));
                var z = (float)(radius * Math.Sin(phi) * Math.Sin(value));
                var y = (float)(radius * Math.Cos(phi));
                var v = new Vector3D(x, y, z);

                if (transformAndScale) v = Vector3D.Transform(Vector3D.Normalize(v), unscaledPosWorldMatrix);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
                physicsArray[i] = v;
            }
        }

        public static void UnitSphereCloudQuick(int pointLimit, ref Vector3D[] physicsArray, MyEntity shieldEnt, bool translateAndScale, bool debug, Random rnd = null)
        {
            if (pointLimit > 10000) pointLimit = 10000;
            if (rnd == null) rnd = new Random(0);

            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;
            var v = Vector3D.Zero;

            for (int i = 0; i < pointLimit; i++)
            {
                while (true)
                {
                    v.X = (rnd.NextDouble() * 2) - 1;
                    v.Y = (rnd.NextDouble() * 2) - 1;
                    v.Z = (rnd.NextDouble() * 2) - 1;
                    var len2 = v.LengthSquared();
                    if (len2 < .0001) continue;
                    v *= radius / Math.Sqrt(len2);
                    break;
                }

                if (translateAndScale) physicsArray[i] = v += center;
                else physicsArray[i] = v;
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void UnitSphereRandomOnly(ref Vector3D[] physicsArray, Random rnd = null)
        {
            if (rnd == null) rnd = new Random(0);
            var v = Vector3D.Zero;

            for (int i = 0; i < physicsArray.Length; i++)
            {
                v.X = 0;
                v.Y = 0;
                v.Z = 0;
                while ((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z) < 0.0001)
                {
                    v.X = (rnd.NextDouble() * 2) - 1;
                    v.Y = (rnd.NextDouble() * 2) - 1;
                    v.Z = (rnd.NextDouble() * 2) - 1;
                }
                v.Normalize();
                physicsArray[i] = v;
            }
        }

        public static void UnitSphereTranslateScale(int pointLimit, ref Vector3D[] physicsArray, ref Vector3D[] scaledCloudArray, MyEntity shieldEnt, bool debug)
        {
            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;

            for (int i = 0; i < pointLimit; i++)
            {
                var v = physicsArray[i];
                scaledCloudArray[i] = v = center + (radius * v);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void UnitSphereTranslateScaleList(int pointLimit, ref Vector3D[] physicsArray, ref List<Vector3D> scaledCloudList, ref BoundingSphereD sphere, MyEntity shieldEnt, bool debug, MyEntity grid, bool rotate = true)
        {
            var sPosComp = shieldEnt.PositionComp;
            var radius = sphere.Radius;
            var center = sphere.Center;
            var gMatrix = grid.PositionComp.WorldMatrixRef;
            for (int i = 0; i < pointLimit; i++)
            {
                var v = physicsArray[i];
                if (rotate) Vector3D.Rotate(ref v, ref gMatrix, out v);
                v = center + (radius * v);
                scaledCloudList.Add(v);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrixRef, 0.5);
            }
        }

        public static void DetermisticSphereCloud(List<Vector3D> physicsArray, int pointsInSextant)
        {
            physicsArray.Clear();
            int stepsPerCoord = (int)Math.Sqrt(pointsInSextant);
            double radPerStep = MathHelperD.PiOver2 / stepsPerCoord;

            for (double az = -MathHelperD.PiOver4; az < MathHelperD.PiOver4; az += radPerStep)
            {
                for (double el = -MathHelperD.PiOver4; el < MathHelperD.PiOver4; el += radPerStep)
                {
                    Vector3D vec;
                    Vector3D.CreateFromAzimuthAndElevation(az, el, out vec);
                    Vector3D vec2 = new Vector3D(vec.Z, vec.X, vec.Y);
                    Vector3D vec3 = new Vector3D(vec.Y, vec.Z, vec.X);
                    physicsArray.Add(vec); //first sextant
                    physicsArray.Add(vec2); //2nd sextant
                    physicsArray.Add(vec3); //3rd sextant
                    physicsArray.Add(-vec); //4th sextant
                    physicsArray.Add(-vec2); //5th sextant
                    physicsArray.Add(-vec3); //6th sextant
                }
            }
        }

        public static Vector3D? GetLineIntersectionExactAll(MyCubeGrid grid, ref LineD line, out double distance, out IMySlimBlock intersectedBlock)
        {
            intersectedBlock = (IMySlimBlock)null;
            distance = 3.40282346638529E+38;
            Vector3I? nullable = new Vector3I?();
            Vector3I zero = Vector3I.Zero;
            double distanceSquared = double.MaxValue;
            if (grid.GetLineIntersectionExactGrid(ref line, ref zero, ref distanceSquared))
            {
                distanceSquared = Math.Sqrt(distanceSquared);
                nullable = new Vector3I?(zero);
            }
            if (!nullable.HasValue)
                return new Vector3D?();
            distance = distanceSquared;
            intersectedBlock = grid.GetCubeBlock(nullable.Value);
            if (intersectedBlock == null)
                return new Vector3D?();
            return new Vector3D?((Vector3D)zero);
        }

        public static float GetDmgMulti(float damage)
        {
            float tableVal;
            DmgTable.TryGetValue(damage, out tableVal);
            return tableVal;
        }

        public static Color GetShieldColorFromFloat(float percent)
        {
            if (percent > 90) return Session.Instance.Color90;
            if (percent > 80) return Session.Instance.Color80;
            if (percent > 70) return Session.Instance.Color70;
            if (percent > 60) return Session.Instance.Color60;
            if (percent > 50) return Session.Instance.Color50;
            if (percent > 40) return Session.Instance.Color40;
            if (percent > 30) return Session.Instance.Color30;
            if (percent > 20) return Session.Instance.Color20;
            if (percent > 10) return Session.Instance.Color10;
            return Session.Instance.Color00;
        }

        public static string GetShieldThyaFromFloat(float percent, int mode)
        {
            var pad = 0;
            var padString = "";
            var iPercent = (int)percent;

            if (iPercent < 10) pad = 2;
            else if (iPercent < 100) pad = 1;

            switch (pad)
            {
                case 1:
                    padString = "0";
                    break;
                case 2:
                    padString = "00";
                    break;
            }
            var imageString = Session.Instance.Thya[mode] + padString + iPercent.ToString();
            return imageString;
        }

        public static Color GetAirEmissiveColorFromDouble(double percent)
        {
            if (percent >= 80) return Color.Green;
            if (percent > 10) return Color.Yellow;
            return Color.Red;
        }

        public static void UpdateTerminal(this MyCubeBlock block)
        {
            MyOwnershipShareModeEnum shareMode;
            long ownerId;
            if (block.IDModule != null)
            {
                ownerId = block.IDModule.Owner;
                shareMode = block.IDModule.ShareMode;
            }
            else
            {
                return;
            }
            block.ChangeOwner(ownerId, shareMode == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            block.ChangeOwner(ownerId, shareMode);
        }

        public static long IntPower(int x, short power)
        {
            if (power == 0) return 1;
            if (power == 1) return x;
            int n = 15;
            while ((power <<= 1) >= 0) n--;

            long tmp = x;
            while (--n > 0)
                tmp = tmp * tmp *
                      (((power <<= 1) < 0) ? x : 1);
            return tmp;
        }

        public static double InverseSqrDist(Vector3D source, Vector3D target, double range)
        {
            var rangeSq = range * range;
            var distSq = (target - source).LengthSquared();
            if (distSq > rangeSq)
                return 0.0;
            return 1.0 - (distSq / rangeSq);
        }

        internal static bool FaceIntersected(MatrixD shieldShape, MatrixD gridLocalMatrix, Vector3D worldPos, Vector3I faces)
        {
            var referenceLocalPosition = gridLocalMatrix.Translation;
            var worldDirection = worldPos - referenceLocalPosition;
            var localPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(gridLocalMatrix));
            var impactTransNorm = localPosition - shieldShape.Translation;

            var boxMax = shieldShape.Backward + shieldShape.Right + shieldShape.Up;
            var boxMin = -boxMax;
            var box = new BoundingBoxD(boxMin, boxMax);

            var maxWidth = box.Max.LengthSquared();
            Vector3D norm;
            Vector3D.Normalize(ref impactTransNorm, out norm);
            var testLine = new LineD(Vector3D.Zero, norm * maxWidth); //This is to ensure we intersect the box
            LineD testIntersection;
            box.Intersect(ref testLine, out testIntersection);

            var intersection = testIntersection.To;

            var projFront = VectorProjection(intersection, shieldShape.Forward);
            if (projFront.LengthSquared() >= 0.8 * shieldShape.Forward.LengthSquared()) //if within the side thickness
            {
                var face = intersection.Dot(shieldShape.Forward) > 0 ? 5 : 4;
                Log.Line($"FaceIntersected (4-5): {face} - {faces}");
                if (faces.Z == 2 || faces.Z != 0 && face == 5 && faces.Z == -1 || face == 4 && faces.Z == 1)
                    return true;
            }

            var projLeft = VectorProjection(intersection, shieldShape.Left);
            if (projLeft.LengthSquared() >= 0.8 * shieldShape.Left.LengthSquared()) //if within the side thickness
            {
                var face = intersection.Dot(shieldShape.Left) > 0 ? 1 : 0;
                Log.Line($"FaceIntersected (1-0): {face}  - {faces}");
                if (faces.X == 2 || faces.X != 0 && face == 1 && faces.X == -1 || face == 0 && faces.X == 1)
                    return true;
            }

            var projUp = VectorProjection(intersection, shieldShape.Up);
            if (projUp.LengthSquared() >= 0.8 * shieldShape.Up.LengthSquared()) //if within the side thickness
            {
                var face = intersection.Dot(shieldShape.Up) > 0 ? 2 : 3;
                Log.Line($"FaceIntersected(2-3): {face} - {faces}");
                if (faces.Y == 2 || faces.Y != 0 && face == 2 && faces.Y == -1 || face == 3 && faces.Y == 1)
                    return true;
            }

            return false;
        }

        public static double GetIntersectingSurfaceArea(MatrixD matrix, Vector3D hitPosLocal)
        {
            var surfaceArea = -1d; 

            var boxMax = matrix.Backward + matrix.Right + matrix.Up;
            var boxMin = -boxMax;
            var box = new BoundingBoxD(boxMin, boxMax);

            var maxWidth = box.Max.LengthSquared();
            var testLine = new LineD(Vector3D.Zero, Vector3D.Normalize(hitPosLocal) * maxWidth); 
            LineD testIntersection;
            box.Intersect(ref testLine, out testIntersection);

            var intersection = testIntersection.To;

            var epsilon = 1e-6; 
            var projFront = VectorProjection(intersection, matrix.Forward);
            if (Math.Abs(projFront.LengthSquared() - matrix.Forward.LengthSquared()) < epsilon)
            {
                var a = Vector3D.Distance(matrix.Left, matrix.Right);
                var b = Vector3D.Distance(matrix.Up, matrix.Down);
                surfaceArea = a * b;
            }

            var projLeft = VectorProjection(intersection, matrix.Left);
            if (Math.Abs(projLeft.LengthSquared() - matrix.Left.LengthSquared()) < epsilon) 
            {
                var a = Vector3D.Distance(matrix.Forward, matrix.Backward);
                var b = Vector3D.Distance(matrix.Up, matrix.Down);
                surfaceArea = a * b;
            }

            var projUp = VectorProjection(intersection, matrix.Up);
            if (Math.Abs(projUp.LengthSquared() - matrix.Up.LengthSquared()) < epsilon) 
            {
                var a = Vector3D.Distance(matrix.Forward, matrix.Backward);
                var b = Vector3D.Distance(matrix.Left, matrix.Right);
                surfaceArea = a * b;
            }
            return surfaceArea;
        }

        public static bool DistanceCheck(IMyCubeBlock block, int x, double range)
        {
            if (MyAPIGateway.Session.Player.Character == null) return false;

            var pPosition = MyAPIGateway.Session.Player.Character.PositionComp.WorldVolume.Center;
            var cPosition = block.CubeGrid.PositionComp.WorldVolume.Center;
            var dist = Vector3D.DistanceSquared(cPosition, pPosition) <= (x + range) * (x + range);
            return dist;
        }

        public static void GetDefinitons()
        {
            try
            {
                var defintions = MyDefinitionManager.Static.GetAllDefinitions();
                foreach (var def in defintions)
                {
                    if (!(def is MyAmmoMagazineDefinition)) continue;
                    var ammoDef = def as MyAmmoMagazineDefinition;
                    var ammo = MyDefinitionManager.Static.GetAmmoDefinition(ammoDef.AmmoDefinitionId);
                    if (!(ammo is MyMissileAmmoDefinition)) continue;
                    var shot = ammo as MyMissileAmmoDefinition;
                    if (Session.Instance.AmmoCollection.ContainsKey(shot.MissileModelName)) continue;
                    Session.Instance.AmmoCollection.Add(shot.MissileModelName, new AmmoInfo(shot.IsExplosive, shot.MissileExplosionDamage, shot.MissileExplosionRadius, shot.DesiredSpeed, shot.MissileMass, shot.BackkickForce));
                }
                if (Session.Enforced.Debug == 3) Log.Line("Definitions: Session");
            }
            catch (Exception ex) { Log.Line($"Exception in GetAmmoDefinitions: {ex}"); }
        }

        public static double GetFit(int size)
        {
            var fitSeq = Session.Instance.Fits[size];
            return MathHelper.Lerp(fitSeq.SqrtStart, fitSeq.SqrtEnd, fitSeq.SeqMulti);
        }

        public static double CreateNormalFit(MyCubeBlock shield, Vector3D gridHalfExtents, List<IMySlimBlock> fitblocks, Vector3D[] fitPoints)
        {

            var subGrids = MyAPIGateway.GridGroups.GetGroup(shield.CubeGrid, GridLinkTypeEnum.Mechanical);
            
            foreach (var grid in subGrids) {
                if (grid.MarkedForClose)
                    continue;
                fitblocks.AddRange(((MyCubeGrid)grid).GetBlocks());
            }

            var bQuaternion = Quaternion.CreateFromRotationMatrix(shield.CubeGrid.PositionComp.WorldMatrixRef);

            var end = 10;
            var wasOutside = false;

            for (int i = 0; i <= end + 1; i++)
            {
                var fitSeq = Session.Instance.FitSeq[i];

                var ellipsoidAdjust = MathHelper.Lerp(fitSeq.SqrtStart, fitSeq.SqrtEnd, fitSeq.SeqMulti);

                var shieldSize = gridHalfExtents * ellipsoidAdjust;
                var mobileMatrix = MatrixD.CreateScale(shieldSize);
                mobileMatrix.Translation = shield.CubeGrid.PositionComp.LocalAABB.Center;
                var matrixInv = MatrixD.Invert(mobileMatrix * shield.CubeGrid.PositionComp.WorldMatrixRef);

                var pointOutside = false;
                for (int j = 0; j < fitblocks.Count; j++)
                {
                    var block = fitblocks[j];
                    var fat = block.FatBlock as MyCubeBlock;
                    if (fat != null && fat.MarkedForClose || block.IsDestroyed)
                        continue;

                    BoundingBoxD blockBox;
                    Vector3D center;
                    if (fat != null)
                    {
                        blockBox = fat.PositionComp.LocalAABB;
                        center = fat.PositionComp.WorldAABB.Center;
                    }
                    else
                    {
                        Vector3 halfExt;
                        block.ComputeScaledHalfExtents(out halfExt);
                        blockBox = new BoundingBoxD(-halfExt, halfExt);
                        block.ComputeWorldCenter(out center);
                    }

                    var bOriBBoxD = new MyOrientedBoundingBoxD(center, blockBox.HalfExtents, bQuaternion);

                    bOriBBoxD.GetCorners(fitPoints, 0);
                    foreach (var point in fitPoints)
                    {
                        if (!CustomCollision.PointInShield(point, matrixInv) && ((MyCubeGrid)block.CubeGrid).IsSameConstructAs(shield.CubeGrid))
                        {
                            pointOutside = true;
                            break;
                        }
                    }
                }

                if (pointOutside)
                {
                    wasOutside = true;
                    if (i == 0)
                        return ellipsoidAdjust;

                    if (i == 2)
                    {
                        i = 10;
                        end = 19;
                    }
                }

                if (!pointOutside)
                {
                    if (i == 1)
                        return ellipsoidAdjust;
                    if (wasOutside)
                        return ellipsoidAdjust;
                }

                if (i == end)
                    return ellipsoidAdjust;
            }
            return Math.Sqrt(5);
        }

        public static double CreateExtendedFit(MyCubeBlock shield, Vector3D gridHalfExtents, List<IMySlimBlock> fitblocks, Vector3D[] fitPoints)
        {
            var subGrids = MyAPIGateway.GridGroups.GetGroup(shield.CubeGrid, GridLinkTypeEnum.Mechanical);
            
            foreach (var grid in subGrids) {
                if (grid.MarkedForClose)
                    continue;
                fitblocks.AddRange(((MyCubeGrid)grid).GetBlocks());
            }

            var bQuaternion = Quaternion.CreateFromRotationMatrix(shield.CubeGrid.PositionComp.WorldMatrixRef);

            var sqrt3 = Math.Sqrt(3);
            var sqrt5 = Math.Sqrt(5);
            var last = 0;
            var repeat = 0;
            for (int i = 0; i <= 10; i++)
            {
                var ellipsoidAdjust = MathHelper.Lerp(sqrt3, sqrt5, i * 0.1);

                var shieldSize = gridHalfExtents * ellipsoidAdjust;
                var mobileMatrix = MatrixD.CreateScale(shieldSize);
                mobileMatrix.Translation = shield.CubeGrid.PositionComp.LocalVolume.Center;
                var matrixInv = MatrixD.Invert(mobileMatrix * shield.CubeGrid.PositionComp.WorldMatrixRef);

                var c = 0;
                for (int j = 0; j < fitblocks.Count; j++)
                {
                    var block = fitblocks[j];
                    var fat = block.FatBlock as MyCubeBlock;
                    
                    if (fat != null && fat.MarkedForClose || block.IsDestroyed)
                        continue;

                    BoundingBoxD blockBox;
                    Vector3D center;
                    if (fat != null)
                    {
                        blockBox = fat.PositionComp.LocalAABB;
                        center = fat.PositionComp.WorldAABB.Center;
                    }
                    else
                    {
                        Vector3 halfExt;
                        block.ComputeScaledHalfExtents(out halfExt);
                        blockBox = new BoundingBoxD(-halfExt, halfExt);
                        block.ComputeWorldCenter(out center);
                    }

                    var bOriBBoxD = new MyOrientedBoundingBoxD(center, blockBox.HalfExtents, bQuaternion);

                    bOriBBoxD.GetCorners(fitPoints, 0);
                    foreach (var point in fitPoints)
                        if (!CustomCollision.PointInShield(point, matrixInv) && ((MyCubeGrid)block.CubeGrid).IsSameConstructAs(shield.CubeGrid)) c++;
                }

                if (c == last) repeat++;
                else repeat = 0;

                if (c == 0)
                {
                    return MathHelper.Lerp(sqrt3, sqrt5, i * 0.1);
                }
                last = c;
                if (i == 10 && repeat > 2) return MathHelper.Lerp(sqrt3, sqrt5, ((10 - repeat) + 1) * 0.1);
            }
            return sqrt5;
        }

        public static int BlockCount(IMyCubeBlock shield)
        {
            var subGrids = MyAPIGateway.GridGroups.GetGroup(shield.CubeGrid, GridLinkTypeEnum.Mechanical);
            var blockCnt = 0;
            foreach (var grid in subGrids)
            {
                blockCnt += ((MyCubeGrid)grid).BlocksCount;
            }
            return blockCnt;
        }

        public static void CreateExplosion(Vector3D position, float radius, int damage = 5000)
        {
            MyExplosionTypeEnum explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_50;
            if (radius < 2.0)
                explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
            else if (radius < 15.0)
                explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_15;
            else if (radius < 30.0)
                explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_30;
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0.0f,
                Damage = damage,
                ExplosionType = explosionTypeEnum,
                ExplosionSphere = new BoundingSphereD(position, radius),
                LifespanMiliseconds = 700,
                ParticleScale = 1f,
                Direction = Vector3.Down,
                VoxelExplosionCenter = position,
                ExplosionFlags = MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION,
                VoxelCutoutScale = 1f,
                PlaySound = true,
                ApplyForceAndDamage = true,
                ObjectsRemoveDelayInMiliseconds = 40
            };
            MyExplosions.AddExplosion(ref explosionInfo);
        }

        public static void CreateFakeSmallExplosion(Vector3D position)
        {
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0.0f,
                Damage = 0f,
                ExplosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02,
                ExplosionSphere = new BoundingSphereD(position, 0d),
                LifespanMiliseconds = 0,
                ParticleScale = 1f,
                Direction = Vector3.Down,
                VoxelExplosionCenter = position,
                ExplosionFlags = MyExplosionFlags.CREATE_PARTICLE_EFFECT,
                VoxelCutoutScale = 0f,
                PlaySound = true,
                ApplyForceAndDamage = false,
                ObjectsRemoveDelayInMiliseconds = 0
            };
            MyExplosions.AddExplosion(ref explosionInfo);
        }

        private static void UpdateConfigFile(TextReader unPackCfg)
        {
            unPackCfg.Close();
            unPackCfg.Dispose();
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage("DefenseShields.cfg");
            var newCfg = MyAPIGateway.Utilities.WriteFileInGlobalStorage("DefenseShields.cfg");
            var newData = MyAPIGateway.Utilities.SerializeToXML(Session.Enforced);
            newCfg.Write(newData);
            newCfg.Flush();
            newCfg.Close();
            Log.Line($"wrote modified config file - file exists: {MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg")}");
        }

        private static void WriteNewConfigFile()
        {
            var cfg = MyAPIGateway.Utilities.WriteFileInGlobalStorage("DefenseShields.cfg");
            var data = MyAPIGateway.Utilities.SerializeToXML(Session.Enforced);
            cfg.Write(data);
            cfg.Flush();
            cfg.Close();
        }

        internal static Vector3D VectorProjection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a.Dot(b) / b.LengthSquared() * b;
        }

        private static readonly Dictionary<float, float> DmgTable = new Dictionary<float, float>
        {
            [0.00000000001f] = -1f,
            [0.0000000001f] = 0.1f,
            [0.0000000002f] = 0.2f,
            [0.0000000003f] = 0.3f,
            [0.0000000004f] = 0.4f,
            [0.0000000005f] = 0.5f,
            [0.0000000006f] = 0.6f,
            [0.0000000007f] = 0.7f,
            [0.0000000008f] = 0.8f,
            [0.0000000009f] = 0.9f,
            [0.0000000010f] = 1,
            [0.0000000020f] = 2,
            [0.0000000030f] = 3,
            [0.0000000040f] = 4,
            [0.0000000050f] = 5,
            [0.0000000060f] = 6,
            [0.0000000070f] = 7,
            [0.0000000080f] = 8,
            [0.0000000090f] = 9,
            [0.0000000100f] = 10,
        };

        private const string OB = @"<?xml version=""1.0"" encoding=""utf-16""?>
<MyObjectBuilder_Cockpit xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
   <SubtypeName>LargeBlockCockpit</SubtypeName>
   <Owner>0</Owner>
   <CustomName>Control Stations</CustomName>
</MyObjectBuilder_Cockpit> ";

        private static Random _random = new Random();
        /// <summary>
        /// Hacky way to get the ResourceDistributorComponent from a grid
        /// without benefit of the GridSystems.
        /// <para>Unfriendly to performance. Use sparingly and cache result.</para>
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static MyResourceDistributorComponent GetDistributor(MyCubeGrid grid)
        {
            if (grid == null || !grid.CubeBlocks.Any())
                return null;

            //attempt to grab the distributor from an extant ship controller
            var controller = grid.GetFatBlocks().FirstOrDefault(b => (b as MyShipController)?.GridResourceDistributor != null);
            if (controller != null)
                return ((MyShipController)controller).GridResourceDistributor;
            //didn't find a controller, so let's make one

            var ob = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_Cockpit>(OB);
            //assign a random entity ID and hope we don't get collisions
            ob.EntityId = _random.Next(int.MinValue, int.MaxValue);
            //block position to something that will probably not have a block there already
            ob.Min = grid.WorldToGridInteger(grid.PositionComp.WorldAABB.Min) - new Vector3I(2);
            //note that this will slightly inflate the grid's boundingbox, but the Raze call later triggers a bounds recalc in 30 seconds

            //not exposed in the class but is in the interface???
            //also not synced
            var blk = ((IMyCubeGrid)grid).AddBlock(ob, false);
            var distributor = (blk.FatBlock as MyShipController)?.GridResourceDistributor;
            //hack to make it work on clients (removal not synced)
            grid.RazeBlocksClient(new List<Vector3I>() { blk.Position });
            //we don't need the block itself, we grabbed the distributor earlier
            blk.FatBlock?.Close();

            return distributor;
        }
        static double AreaCuboid(double l, double h,
            double w)
        {
            return (l * h * w);
        }

        public static double SurfaceAreaCuboid(double l, double h, double w)
        {
            return (2 * l * w + 2 * w * h + 2 * l * h);
        }




        /*
        private static double PowerCalculation(IMyEntity breaching, IMyCubeGrid grid)
        {
            var bPhysics = breaching.Physics;
            var sPhysics = grid.Physics;

            const double wattsPerNewton = (3.36e6 / 288000);
            var velTarget = sPhysics.GetVelocityAtPoint(breaching.Physics.CenterOfMassWorld);
            var accelLinear = sPhysics.LinearAcceleration;
            var velTargetNext = velTarget + accelLinear * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            var velModifyNext = bPhysics.LinearVelocity;
            var linearImpulse = bPhysics.Mass * (velTargetNext - velModifyNext);
            var powerCorrectionInJoules = wattsPerNewton * linearImpulse.Length();

            return powerCorrectionInJoules * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
        }
        */



        /*
        public static float ImpactFactor(MatrixD obbMatrix, Vector3 obbExtents, Vector3D impactPos, Vector3 direction)
        {
            var impactPosLcl = (Vector3)(impactPos - obbMatrix.Translation);
            var xProj = (Vector3)obbMatrix.Right;
            var yProj = (Vector3)obbMatrix.Up;
            var zProj = (Vector3)obbMatrix.Backward;

            // quick inverse transform normal: dot(xProj, pos), dot(yProj, pos), dot(zProj, pos)
            impactPosLcl = new Vector3(impactPosLcl.Dot(xProj), impactPosLcl.Dot(yProj), impactPosLcl.Dot(zProj));
            direction = new Vector3(direction.Dot(xProj), direction.Dot(yProj), direction.Dot(zProj));

            // find point outside of box along ray, then scale by inverse box size
            const float expandFactor = 25;
            var faceDirection = (impactPosLcl - direction * obbExtents.AbsMax() * expandFactor) / obbExtents;

            // dominant axis project, then sign
            // faceNormal = Vector3.Sign(Vector3.DominantAxisProjection(faceDirection));
            Vector3 faceNormal;
            if (Math.Abs(faceDirection.X) > Math.Abs(faceDirection.Y))
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Math.Abs(faceDirection.X) > Math.Abs(faceDirection.Z))
                    faceNormal = new Vector3(Math.Sign(faceDirection.X), 0, 0);
                else
                    faceNormal = new Vector3(0, 0, Math.Sign(faceDirection.Z));
            }
            else if (Math.Abs(faceDirection.Y) > Math.Abs(faceDirection.Z))
                faceNormal = new Vector3(0, Math.Sign(faceDirection.Y), 0);
            else
                faceNormal = new Vector3(0, 0, Math.Sign(faceDirection.Z));

            return Math.Abs(faceNormal.Dot(direction));
        }

        // This method only exists for consistency, so you can *always* call
        // MoreMath.Max instead of alternating between MoreMath.Max and Math.Max
        // depending on your argument count.
        public static int Max(int x, int y)
        {
            return Math.Max(x, y);
        }

        public static int Max(int x, int y, int z)
        {
            // Or inline it as x < y ? (y < z ? z : y) : (x < z ? z : x);
            // Time it before micro-optimizing though!
            return Math.Max(x, Math.Max(y, z));
        }

        public static int Max(int w, int x, int y, int z)
        {
            return Math.Max(w, Math.Max(x, Math.Max(y, z)));
        }

        public static void GetRealPlayers(Vector3D center, float radius, HashSet<long> realPlayers)
        {
            var realPlayersIdentities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(realPlayersIdentities, p => !string.IsNullOrEmpty(p?.DisplayName));
            var pruneSphere = new BoundingSphereD(center, radius);
            var pruneList = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref pruneSphere, pruneList);

            foreach (var ent in pruneList)
            {
                if (ent == null || !(ent is IMyCubeGrid || ent is IMyCharacter)) continue;

                IMyPlayer player = null;

                if (ent is IMyCharacter)
                {
                    player = MyAPIGateway.Players.GetPlayerControllingEntity(ent);
                    if (player == null) continue;
                }
                else
                {
                    var playerTmp = MyAPIGateway.Players.GetPlayerControllingEntity(ent);

                    if (playerTmp?.Character != null) player = playerTmp;
                }

                if (player == null) continue;
                if (realPlayersIdentities.Contains(player.Identity)) realPlayers.Add(player.IdentityId);
            }
        }

        public static long ThereCanBeOnlyOne(IMyCubeBlock shield)
        {
            if (Session.Enforced.Debug == 3) Log.Line($"ThereCanBeOnlyOne start");
            var shieldBlocks = new List<MyCubeBlock>();
            foreach (var block in ((MyCubeGrid)shield.CubeGrid).GetFatBlocks())
            {
                if (block == null) continue;

                if (block.BlockDefinition.BlockPairName == "DS_Control" || block.BlockDefinition.BlockPairName == "DS_Control_Table")
                {
                    if (block.IsWorking) return block.EntityId;
                    shieldBlocks.Add(block);
                }
            }
            var shieldDistFromCenter = double.MinValue;
            var shieldId = long.MinValue;
            foreach (var s in shieldBlocks)
            {
                if (s == null) continue;

                var dist = Vector3D.DistanceSquared(s.PositionComp.WorldVolume.Center, shield.CubeGrid.WorldVolume.Center);
                if (dist > shieldDistFromCenter)
                {
                    shieldDistFromCenter = dist;
                    shieldId = s.EntityId;
                }
            }
            if (Session.Enforced.Debug == 3) Log.Line($"ThereCanBeOnlyOne complete, found shield: {shieldId}");
            return shieldId;
        }
        */
    }
    }
