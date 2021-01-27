using System.Diagnostics.Eventing.Reader;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace DefenseShields
{
    using System;
    using Support;
    using VRage.Game;
    using VRageMath;

    public partial class DefenseShields
    {
        #region Shield Shape
        public void ResetShape(bool background, bool newShape = false)
        {
            if (Session.Enforced.Debug == 3) Log.Line($"ResetShape: Mobile:{GridIsMobile} - Mode:{ShieldMode}/{DsState.State.Mode} - newShape:{newShape} - Offline:{!DsState.State.Online} - Sleeping:{DsState.State.Sleeping} - Suspend:{DsState.State.Suspended} - ELos:{ShieldComp.EmitterLos} - ShieldId [{Shield.EntityId}]");

            if (newShape)
            {
                UpdateSubGrids();
                BlockMonitor();
                if (_shapeEvent) CheckExtents();
                if (GridIsMobile) _updateMobileShape = true;
                return;
            }

            if (GridIsMobile) MobileUpdate();
            else
            {
                UpdateDimensions = true;
                if (UpdateDimensions) RefreshDimensions();
            }
        }

        public void MobileUpdate()
        {
            var checkForNewCenter = MyGrid.PositionComp.WorldAABB.Center;
            if (ShieldComp == null)
            {
                Log.Line($"ShieldComp null");
                return;
            }

            if (DsState?.State == null)
            {
                Log.Line($"DsState.State null");
                return;
            }
            if (!checkForNewCenter.Equals(MyGridCenter, 1e-4))
            {
                ShieldComp.GridIsMoving = true;
                MyGridCenter = checkForNewCenter;
            }
            else
            {
                ShieldComp.GridIsMoving = false;
            }

            if (ShieldComp.GridIsMoving || _comingOnline)
            {
                if (DsSet.Settings.FortifyShield && MyGrid.Physics.LinearVelocity.Length() > 15)
                {
                    FitChanged = true;
                    DsSet.Settings.FortifyShield = false;
                }
            }

            _shapeChanged = _halfExtentsChanged || !DsState.State.EllipsoidAdjust.Equals(_oldEllipsoidAdjust) || !DsState.State.ShieldFudge.Equals(_oldShieldFudge) || _updateMobileShape;
            _entityChanged = ShieldComp.GridIsMoving || _comingOnline || _shapeChanged;

            _halfExtentsChanged = false;
            _oldEllipsoidAdjust = DsState.State.EllipsoidAdjust;
            _oldShieldFudge = DsState.State.ShieldFudge;
            if (_entityChanged || BoundingRange <= 0) CreateShieldShape();
            if (_tick300) CreateHalfExtents();
        }

        public void RefreshDimensions()
        {
            UpdateDimensions = false;
            _shapeChanged = true;
            CreateShieldShape();
        }

        public void CreateHalfExtents(bool forceUpdate = false)
        {
            _oldGridHalfExtents = DsState.State.GridHalfExtents;
            var myAabb = MyGrid.PositionComp.LocalAABB;
            var shieldGrid = MyGrid;
            var expandedAabb = myAabb;

            if (ShieldComp.SubGrids.Count > 1)
            {
                foreach (var grid in ShieldComp.SubGrids.Keys)
                {
                    using (grid.Pin())
                    {
                        if (grid == shieldGrid || grid.MarkedForClose) continue;
                        var shieldMatrix = shieldGrid.PositionComp.WorldMatrixNormalizedInv;
                        var gQuaternion = Quaternion.CreateFromRotationMatrix(grid.WorldMatrix);
                        var gOriBBoxD = new MyOrientedBoundingBox(grid.PositionComp.WorldAABB.Center, grid.PositionComp.LocalAABB.HalfExtents, gQuaternion);
                        gOriBBoxD.Transform(shieldMatrix);
                        expandedAabb.Include(gOriBBoxD.GetAABB());
                    }
                }
            }

            if (DsSet.Settings.SphereFit || DsSet.Settings.FortifyShield)
            {
                var extend = DsSet.Settings.Fit > 9 ? 2 : 1;
                var fortify = DsSet.Settings.FortifyShield ? 3 : 1;
                var size = expandedAabb.HalfExtents.Max() * fortify;
                var scaler = 4;
                if (shieldGrid.GridSizeEnum == MyCubeSize.Small && DsSet.Settings.Fit < 10) scaler = 5;
                var vectorSize = new Vector3D(size, size, size);
                var fudge = shieldGrid.GridSize * scaler * extend;
                var extentsDiff = DsState.State.GridHalfExtents.LengthSquared() - vectorSize.LengthSquared();
                if (extentsDiff < -1 || extentsDiff > 1 || DsState.State.GridHalfExtents == Vector3D.Zero || !fudge.Equals(DsState.State.ShieldFudge)) DsState.State.GridHalfExtents = vectorSize;
                DsState.State.ShieldFudge = fudge;
            }
            else
            {
                var blockHalfSize = MyGrid.GridSize * 0.5;
                DsState.State.ShieldFudge = 0f;
                var extentsDiff = DsState.State.GridHalfExtents.LengthSquared() - expandedAabb.HalfExtents.LengthSquared();
                var overThreshold = extentsDiff < -blockHalfSize || extentsDiff > blockHalfSize || forceUpdate;
                if (overThreshold || DsState.State.GridHalfExtents == Vector3D.Zero) DsState.State.GridHalfExtents = expandedAabb.HalfExtents;
            }
            _halfExtentsChanged = !DsState.State.GridHalfExtents.Equals(_oldGridHalfExtents);
            if (_halfExtentsChanged || SettingsUpdated)
            {
                _adjustShape = true;
            }
        }

        private void AdjustShape(bool backGround)
        {
            if (backGround) GetShapeAdjust();
            else GetShapeAdjust();
            _adjustShape = false;
        }

        private void GetShapeAdjust()
        {
            if (DsSet.Settings.SphereFit || DsSet.Settings.FortifyShield) DsState.State.EllipsoidAdjust = 1f;
            else DsState.State.EllipsoidAdjust = UtilsStatic.GetFit(DsSet.Settings.Fit);
        }

        private void CheckExtents()
        {
            FitChanged = false;
            _shapeEvent = false;
            if (!_isServer || !GridIsMobile) return;
            CreateHalfExtents(true);
        }

        internal void CreateShieldShape()
        {
            var width = DsSet.Settings.Width;
            var height = DsSet.Settings.Height;
            var depth = DsSet.Settings.Depth;

            var xMax = GridIsMobile ? DsState.State.GridHalfExtents.X * 0.25f : float.MaxValue;
            var yMax = GridIsMobile ? DsState.State.GridHalfExtents.Y * 0.25f : float.MaxValue;
            var zMax = GridIsMobile ? DsState.State.GridHalfExtents.Z * 0.25f : float.MaxValue;

            var wOffset = MathHelper.Clamp(DsSet.Settings.ShieldOffset.X, -xMax, xMax);
            var hOffset = MathHelper.Clamp(DsSet.Settings.ShieldOffset.Y, -yMax, yMax);
            var dOffset = MathHelper.Clamp(DsSet.Settings.ShieldOffset.Z, -zMax, zMax);

            var localOffsetMeters = new Vector3D(wOffset, hOffset, dOffset) * MyGrid.GridSize;
            var gridMatrix = MyGrid.PositionComp.WorldMatrixRef;
            var worldOffset = Vector3D.TransformNormal(localOffsetMeters, gridMatrix); 

            if (GridIsMobile) {

                DetectionCenter = MyGridCenter + worldOffset;

                _updateMobileShape = false;
                if (_shapeChanged) CreateMobileShape(localOffsetMeters);
                DetectionMatrix = ShieldShapeMatrix * gridMatrix;
                SQuaternion = Quaternion.CreateFromRotationMatrix(gridMatrix);
                ShieldSphere.Center = DetectionCenter;
                ShieldSphere.Radius = ShieldSize.AbsMax();


            }
            else {

                IMyUpgradeModule emitter;
                if (_isServer) 
                    emitter = ShieldComp.StationEmitter.Emitter;
                else 
                    emitter = (IMyUpgradeModule)MyEntities.GetEntityById(DsState.State.ActiveEmitterId, true);

                if (emitter == null) {
                    UpdateDimensions = true;
                    return;
                }


                var blockGridPosMeters = new Vector3D(emitter.Position) * MyGrid.GridSize;
                var localOffsetPosMeters = localOffsetMeters + blockGridPosMeters; 
                var emitterCenter = emitter.PositionComp.GetPosition();
                var offsetLMatrix = Matrix.CreateWorld(localOffsetPosMeters, Vector3D.Forward, Vector3D.Up);

                var translationInWorldSpace = emitterCenter + worldOffset;

                OffsetEmitterWMatrix = MatrixD.CreateWorld(translationInWorldSpace, gridMatrix.Forward, gridMatrix.Up);

                DetectionCenter = OffsetEmitterWMatrix.Translation;

                var halfDistToCenter = 1000 - Vector3D.Distance(DetectionCenter, emitterCenter);
                var vectorScale = new Vector3D(MathHelper.Clamp(width, 30, halfDistToCenter), MathHelper.Clamp(height, 30, halfDistToCenter), MathHelper.Clamp(depth, 30, halfDistToCenter));

                DetectionMatrix = MatrixD.Rescale(OffsetEmitterWMatrix, vectorScale);
                ShieldShapeMatrix = MatrixD.Rescale(offsetLMatrix, vectorScale);

                ShieldSize = DetectionMatrix.Scale;
                SQuaternion = Quaternion.CreateFromRotationMatrix(OffsetEmitterWMatrix);
                ShieldSphere.Center = DetectionCenter;
                ShieldSphere.Radius = ShieldSize.AbsMax();
            }

            ShieldSphere3K.Center = DetectionCenter;
            WebSphere.Center = DetectionCenter;

            SOriBBoxD.Center = DetectionCenter;
            SOriBBoxD.Orientation = SQuaternion;

            if (_shapeChanged) {

                SOriBBoxD.HalfExtent = ShieldSize;
                ShieldAabbScaled.Min = ShieldSize;
                ShieldAabbScaled.Max = -ShieldSize;
                _ellipsoidSa.Update(DetectMatrixOutside.Scale.X, DetectMatrixOutside.Scale.Y, DetectMatrixOutside.Scale.Z);
                BoundingRange = ShieldSize.AbsMax();

                ShieldSphere3K.Radius = BoundingRange + 3000;
                WebSphere.Radius = BoundingRange + 7;

                _ellipsoidSurfaceArea = _ellipsoidSa.Surface;
                EllipsoidVolume = 1.333333 * Math.PI * DetectMatrixOutside.Scale.X * DetectMatrixOutside.Scale.Y * DetectMatrixOutside.Scale.Z;

                var magicMod = DsState.State.Enhancer && ShieldMode == ShieldType.Station ? 100f : 1f;
                var ellipsoidMagic = _ellipsoidSurfaceArea / (MagicEllipsoidRatio * magicMod);
                _sizeScaler = (float)Math.Sqrt(ellipsoidMagic);

                if (_isServer) {
                    ShieldChangeState();
                    ShieldComp.ShieldVolume = DetectMatrixOutside.Scale.Volume;
                }
            }

            if (_shapeChanged) {

                var zeroMatrix = Matrix.Zero;
                var shieldMatrix = (Matrix)ShieldShapeMatrix;

                if (!_isDedicated) {                    

                    _shellPassive.PositionComp.SetLocalMatrix(ref zeroMatrix, null, true);  // Bug - Cannot just change X coord, so I reset first.
                    _shellActive.PositionComp.SetLocalMatrix(ref zeroMatrix, null, true);
                    _shellPassive.PositionComp.SetLocalMatrix(ref shieldMatrix, null, true);
                    _shellActive.PositionComp.SetLocalMatrix(ref shieldMatrix, null, true);
                }
                ShieldEnt.PositionComp.SetLocalMatrix(ref zeroMatrix, null, true);
                ShieldEnt.PositionComp.SetLocalMatrix(ref shieldMatrix, null, true);
                ShieldEnt.PositionComp.LocalAABB = BoundingBox.CreateFromHalfExtent(Vector3.Zero, (float) ShieldSize.AbsMax());
                ShieldEnt.PositionComp.WorldMatrix *= MyGrid.PositionComp.WorldMatrix.GetOrientation();
            }
            ShieldEnt.PositionComp.SetPosition(DetectionCenter);
            BoundingBoxD.CreateFromSphere(ref WebSphere, out WebBox);
            BoundingBoxD.CreateFromSphere(ref ShieldSphere3K, out ShieldBox3K);
        }

        private void CreateMobileShape(Vector3D localOffsetMeters)
        {
            ShieldSize = (DsState.State.GridHalfExtents * DsState.State.EllipsoidAdjust) + DsState.State.ShieldFudge;
            var mobileMatrix = MatrixD.Rescale(MatrixD.Identity, ShieldSize);
            mobileMatrix.Translation = MyGrid.PositionComp.LocalAABB.Center + localOffsetMeters;
            ShieldShapeMatrix = mobileMatrix;
        }
        #endregion
    }
}
