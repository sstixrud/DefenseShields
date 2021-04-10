using DefenseShields.Support;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    public partial class Emitters
    {
        #region LosTest
        private void LosLogic()
        {
            if (_disableLos)
            {
                if (!_isServer) return;
                EmiState.State.Los = true;
                ShieldComp.CheckEmitters = false;
                return;
            }

            var controller = ShieldComp.DefenseShields;
            var controllerReady = controller != null && controller.Warming && controller.MyCube.IsWorking && !controller.DsState.State.Suspended && controller.DsState.State.ControllerGridAccess;
            var emitterActive = EmiState.State.ActiveEmitterId == MyCube.EntityId;
            var controllerLinked = emitterActive && controllerReady;
            if (!controllerLinked) return;

            if (!_isDedicated)
            {
                if (!_updateLosState && (EmiState.State.Los != _wasLosState || controller.LosCheckTick == _tick + 1799 || controller.LosCheckTick == _tick + 1800)) _updateLosState = true;
                _wasLosState = EmiState.State.Los;
                if (!_isServer)
                {
                    DrawHelper();
                    return;
                }

                if (!EmiState.State.Los) DrawHelper();
            }
            if ((ShieldComp.CheckEmitters || TookControl))
            {
                CheckShieldLineOfSight();
            }
        }

        private void CheckShieldLineOfSight()
        {
            if (!_compact && SubpartRotor.Closed) BlockReset(false);
            TookControl = false;

            ShieldComp.DefenseShields.ResetShape(false);
            if (EmitterMode == EmitterType.Station || ShieldComp.SkipLos)
            {
                EmiState.State.Los = true;
                ShieldComp.CheckEmitters = false;
            }
            else
            {
                UpdateLosState();
                EmiState.State.Los = _blocksLos.Count <= 1950;

                if (!EmiState.State.Los) ShieldComp.EmitterEvent = true;
                else LosScaledCloud.Clear();

                ShieldComp.CheckEmitters = false;
            }
            if (Session.Enforced.Debug >= 3 && !EmiState.State.Los) Log.Line($"LOS: Mode: {EmitterMode} - blocked verts {_blocksLos.Count.ToString()} - visable verts: {_vertsSighted.Count.ToString()} - LoS: {EmiState.State.Los.ToString()} - EmitterId [{Emitter.EntityId}]");
        }

        private void UpdateLosState(bool updateTestSphere = true)
        {
            if (_age < 1800)
                return;

            _blocksLos.Clear();
            _vertsSighted.Clear();

            if (updateTestSphere) UpdateUnitSphere();
            var testDist = Definition.FieldDist;

            Vector3D testDir;
            if (!_compact) testDir = SubpartRotor.PositionComp.WorldAABB.Center - MyCube.PositionComp.WorldAABB.Center;
            else testDir = MyCube.PositionComp.WorldMatrixRef.Up;

            testDir.Normalize();
            var testCenter = MyCube.PositionComp.WorldAABB.Center;
            //var testPos = testCenter + (testDir * testDist);
            var valid = !MyUtils.IsZero(testCenter) && !MyUtils.IsZero(testDir) && !MyUtils.IsZero(testDist);
            if (!valid) Log.Line($"invalid Los Init check");
            MyAPIGateway.Parallel.For(0, _unitSpherePoints, i =>
            {
                if (valid)
                {
                    var hit = MyGrid.RayCastBlocks(LosScaledCloud[i], testCenter);

                    if (hit.HasValue && MyGrid.GetCubeBlock(hit.Value) != MyCube.SlimBlock)
                        _blocksLos[i] = false;
                }
                else
                    _blocksLos[i] = true;

            });
            for (int i = 0; i < _unitSpherePoints; i++) if (!_blocksLos.ContainsKey(i)) _vertsSighted.Add(i);
        }

        private void DrawHelper()
        {
            if (_age < 1800)
                return;

            if (Vector3D.DistanceSquared(MyAPIGateway.Session.Player.Character.PositionComp.WorldAABB.Center, Emitter.PositionComp.WorldAABB.Center) < 2250000)
            {
                var controller = ShieldComp.DefenseShields;

                var needsUpdate = controller.GridIsMobile && (ShieldComp.GridIsMoving || _updateLosState);

                var blockCam = controller.ShieldEnt.PositionComp.WorldVolume;
                if (MyAPIGateway.Session.Camera.IsInFrustum(ref blockCam))
                {

                    if (_lCount % 2 == 1)
                    {
                        if (_count == 59 && needsUpdate)
                        {
                            UpdateLosState(_updateLosState);
                            _updateLosState = false;
                        }
                        else if (needsUpdate) UpdateUnitSphere();
                    }
                    else
                    {
                        if (needsUpdate) UpdateUnitSphere();
                        foreach (var blocking in _blocksLos.Keys)
                        {
                            var blockedPos = LosScaledCloud[blocking];
                            DsDebugDraw.DrawLosBlocked(blockedPos, MyGrid.PositionComp.LocalMatrixRef, blockCam.Radius / 25);
                        }
                    }

                    foreach (var clear in _vertsSighted)
                    {
                        var blockedPos = LosScaledCloud[clear];
                        DsDebugDraw.DrawLosClear(blockedPos, MyGrid.PositionComp.LocalMatrixRef, blockCam.Radius / 25);
                    }

                    var blocked = _blocksLos.Count;
                    var needed = -50 + _vertsSighted.Count;
                    if (_count == 0) BroadCastLosMessage(blocked, needed, controller);
                }
            }
        }

        private void UpdateUnitSphere()
        {
            var losPointSphere = Session.Instance.LosPointSphere;
            LosScaledCloud.Clear();
            var checkSphere = new BoundingSphereD(ShieldComp.DefenseShields.MyGridCenter, ShieldComp.DefenseShields.DsState.State.GridHalfExtents.Length() + 20);
            UtilsStatic.UnitSphereTranslateScaleList(_unitSpherePoints, ref losPointSphere, ref LosScaledCloud, ref checkSphere, ShieldComp.DefenseShields.ShieldEnt, false, MyGrid);
        }

        private void BroadCastLosMessage(int blocked, int needed, DefenseShields controller)
        {
            var sphere = new BoundingSphereD(Emitter.PositionComp.WorldAABB.Center, 1500);
            var sendMessage = false;
            foreach (var player in Session.Instance.Players.Values)
            {
                if (player.IdentityId != MyAPIGateway.Session.Player.IdentityId) continue;
                if (!sphere.Intersects(player.Character.WorldVolume)) continue;
                sendMessage = true;
                break;
            }

            if (sendMessage)
            {
                var sighted = _vertsSighted.Count;
                if (needed > 0) needed = 0;

                MyAPIGateway.Utilities.ShowNotification("The shield emitter DOES NOT have a CLEAR ENOUGH LINE OF SIGHT to the shield, SHUTTING DOWN.", 960, "Red");
                MyAPIGateway.Utilities.ShowNotification($"Green means clear line of sight, Flashing Orange means blocked | Blocked: {blocked} | Clear: {sighted} | Needed (Approximate): {needed}", 960, "Red");
                if (needed == 0 && controller.LosCheckTick != uint.MaxValue) MyAPIGateway.Utilities.ShowNotification($"Needed is only an approximation, if shield does not start in 30 seconds or is unstable, then it is not clear.", 960, "White");
                else if (needed == 0 && _lCount % 2 == 1) MyAPIGateway.Utilities.ShowNotification($"Shield is still not clear!", 960, "White");
            }
        }
        #endregion
    }
}
