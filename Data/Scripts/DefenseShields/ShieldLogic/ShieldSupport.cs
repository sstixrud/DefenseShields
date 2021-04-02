using System;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;

namespace DefenseShields
{
    using Support;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.Utils;
    using VRageMath;
    using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
    using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

    public partial class DefenseShields
    {
        #region Shield Support Blocks
        public void GetModulationInfo()
        {
            var update = false;
            if (ShieldComp.Modulator != null && ShieldComp.Modulator.ModState.State.Online)
            {
                var modEnergyRatio = ShieldComp.Modulator.ModState.State.ModulateEnergy * 0.01f;
                var modKineticRatio = ShieldComp.Modulator.ModState.State.ModulateKinetic * 0.01f;
                if (!DsState.State.ModulateEnergy.Equals(modEnergyRatio) || !DsState.State.ModulateKinetic.Equals(modKineticRatio) || !DsState.State.EmpProtection.Equals(ShieldComp.Modulator.ModSet.Settings.EmpEnabled) || !DsState.State.ReInforce.Equals(ShieldComp.Modulator.ModSet.Settings.ReInforceEnabled)) update = true;
                DsState.State.ModulateEnergy = modEnergyRatio;
                DsState.State.ModulateKinetic = modKineticRatio;
                if (DsState.State.Enhancer)
                {
                    DsState.State.EmpProtection = ShieldComp.Modulator.ModSet.Settings.EmpEnabled;
                    DsState.State.ReInforce = ShieldComp.Modulator.ModSet.Settings.ReInforceEnabled;
                }

                if (_isServer && update) ShieldChangeState();
            }
            else if (_tick - InitTick > 30)
            {
                if (!DsState.State.ModulateEnergy.Equals(1f) || !DsState.State.ModulateKinetic.Equals(1f) || DsState.State.EmpProtection || DsState.State.ReInforce) update = true;
                DsState.State.ModulateEnergy = 1f;
                DsState.State.ModulateKinetic = 1f;
                DsState.State.EmpProtection = false;
                DsState.State.ReInforce = false;
                if (_isServer && update) ShieldChangeState();

            }
        }

        public void GetEnhancernInfo()
        {
            var update = false;
            if (ShieldComp.Enhancer != null && ShieldComp.Enhancer.EnhState.State.Online)
            {
                if (!DsState.State.EnhancerPowerMulti.Equals(2) || !DsState.State.EnhancerProtMulti.Equals(1000) || !DsState.State.Enhancer) update = true;
                DsState.State.EnhancerPowerMulti = 2;
                DsState.State.EnhancerProtMulti = 1000;
                DsState.State.Enhancer = true;
                
                if (update) {
                    UpdateDimensions = true;
                    ShieldChangeState();
                }
            }
            else if (_tick - InitTick > 30)
            {
                if (!DsState.State.EnhancerPowerMulti.Equals(1) || !DsState.State.EnhancerProtMulti.Equals(1) || DsState.State.Enhancer) update = true;
                DsState.State.EnhancerPowerMulti = 1;
                DsState.State.EnhancerProtMulti = 1;
                DsState.State.Enhancer = false;
                if (!DsState.State.Overload) DsState.State.ReInforce = false;
                
                if (update) {
                    UpdateDimensions = true;
                    ShieldChangeState();
                }
            }
        }
        #endregion

        internal void Awake()
        {
            Asleep = false;
            LastWokenTick = _tick;
        }

        internal void TerminalRefresh(bool update = true)
        {
            Shield.RefreshCustomInfo();
            if (update && InControlPanel && InThisTerminal)
            {
                MyCube.UpdateTerminal();
            }
        }

        public BoundingBoxD GetMechnicalGroupAabb()
        {
            BoundingBoxD worldAabb = new BoundingBoxD();
            foreach (var sub in ShieldComp.SubGrids.Keys)
                worldAabb.Include(sub.PositionComp.WorldAABB);

            return worldAabb;
        }

        internal int RedirectedSideCount()
        {
            return Math.Abs(ShieldRedirectState.X) + Math.Abs(ShieldRedirectState.Y) + Math.Abs(ShieldRedirectState.Z);
        }

        public void UpdateMapping()
        {
            var orientation = MyGrid.MainCockpit?.Orientation ?? MyCube.Orientation;
            var fwdReverse = Base6Directions.GetOppositeDirection(orientation.Forward);
            var upReverse = Base6Directions.GetOppositeDirection(orientation.Up);
            var leftReverse = Base6Directions.GetOppositeDirection(orientation.Left);

            RealSideStates[(Session.ShieldSides)fwdReverse] = IsSideRedirected(Session.ShieldSides.Forward);
            RealSideStates[(Session.ShieldSides)orientation.Forward] = IsSideRedirected(Session.ShieldSides.Backward);
            
            RealSideStates[(Session.ShieldSides)orientation.Up] = IsSideRedirected(Session.ShieldSides.Up);
            RealSideStates[(Session.ShieldSides)upReverse] = IsSideRedirected(Session.ShieldSides.Down);
            
            RealSideStates[(Session.ShieldSides)leftReverse] = IsSideRedirected(Session.ShieldSides.Left);
            RealSideStates[(Session.ShieldSides)orientation.Left] = IsSideRedirected(Session.ShieldSides.Right);

            //foreach (var pair in RealSideStates) Log.CleanLine($"realSide:{pair.Key} - enabled:{pair.Value}");
        }


        private bool _toggle;
        public bool RedirectVisualUpdate()
        {
            var turnedOff = !DsSet.Settings.SideRedirect || ShieldRedirectState == Vector3.Zero;

            if (turnedOff && !_toggle)
                return false;

            if (!_toggle)
            {

                var relation = MyAPIGateway.Session.Player.GetRelationTo(MyCube.OwnerId);
                var enemy = relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.Enemies;
                if (!enemy && !DsSet.Settings.ShowRedirect)
                    return false;
            }

            _toggle = !_toggle;
            foreach (var pair in RenderingSides)
            {
                var side = pair.Key;
                var draw = pair.Value;
                
                var redirecting = RealSideStates[side];
                var showStale = _toggle && (redirecting && !draw || !redirecting && draw);
                var hideStale = !_toggle && draw;

                if (showStale || hideStale)
                    return true;
            }


            return false;
        }

        public void UpdateShieldRedirectVisuals(MyEntity shellActive)
        {
            foreach (var key in RealSideStates)
            {
                var side = key.Key;
                var enabled = key.Value;
                MyEntitySubpart part;
                if (shellActive.TryGetSubpart(Session.Instance.ShieldDirectedSides[side], out part))
                {
                    var redirecting = enabled && _toggle;
                    RenderingSides[side] = redirecting;
                    part.Render.UpdateRenderObject(redirecting);
                }
            }
        }

        public bool IsSideRedirected(Session.ShieldSides side)
        {
            switch (side)
            {
                case Session.ShieldSides.Left:
                    if (ShieldRedirectState.X == -1 || ShieldRedirectState.X == 2)
                        return true;
                    break;
                case Session.ShieldSides.Right:
                    if (ShieldRedirectState.X == 1 || ShieldRedirectState.X == 2)
                        return true;
                    break;
                case Session.ShieldSides.Up:
                    if (ShieldRedirectState.Y == 1 || ShieldRedirectState.Y == 2)
                        return true;
                    break;
                case Session.ShieldSides.Down:
                    if (ShieldRedirectState.Y == -1 || ShieldRedirectState.Y == 2)
                        return true;
                    break;
                case Session.ShieldSides.Forward:
                    if (ShieldRedirectState.Z == -1 || ShieldRedirectState.Z == 2)
                        return true;
                    break;
                case Session.ShieldSides.Backward:
                    if (ShieldRedirectState.Z == 1 || ShieldRedirectState.Z == 2)
                        return true;
                    break;
            }
            return false;
        }


        public void ResetDamageEffects()
        {
            if (DsState.State.Online && !DsState.State.Lowered)
            {
                lock (SubLock)
                {
                    foreach (var funcBlock in _functionalBlocks)
                    {
                        if (funcBlock == null) continue;
                        if (funcBlock.IsFunctional) funcBlock.SetDamageEffect(false);
                    }
                }
            }
        }

        internal void AddShieldHit(long attackerId, float amount, MyStringHash damageType, IMySlimBlock block, bool reset, Vector3D? hitPos = null)
        {
            lock (ShieldHit)
            {
                ShieldHit.Amount += amount;
                ShieldHit.DamageType = damageType.String;

                if (block != null && !hitPos.HasValue && ShieldHit.HitPos == Vector3D.Zero)
                {
                    if (block.FatBlock != null) ShieldHit.HitPos = block.FatBlock.PositionComp.WorldAABB.Center;
                    else block.ComputeWorldCenter(out ShieldHit.HitPos);
                }
                else if (hitPos.HasValue) ShieldHit.HitPos = hitPos.Value;

                if (attackerId != 0) ShieldHit.AttackerId = attackerId;
                if (amount > 0) _lastSendDamageTick = _tick;
                if (reset) ShieldHitReset(true);
            }
        }

        internal void SendShieldHits()
        {
            while (ShieldHitsToSend.Count != 0)
                Session.Instance.PacketizeToClientsInRange(Shield, new DataShieldHit(MyCube.EntityId, ShieldHitsToSend.Dequeue()));
        }

        private void ShieldHitReset(bool enQueue)
        {
            if (enQueue)
            {
                if (_isServer)
                {
                    if (_mpActive) ShieldHitsToSend.Enqueue(CloneHit());
                    if (!_isDedicated) AddLocalHit();
                }
            }
            _lastSendDamageTick = uint.MaxValue;
            _forceBufferSync = true;
            ShieldHit.AttackerId = 0;
            ShieldHit.Amount = 0;
            ShieldHit.DamageType = string.Empty;
            ShieldHit.HitPos = Vector3D.Zero;
        }

        private ShieldHitValues CloneHit()
        {
            var hitClone = new ShieldHitValues
            {
                Amount = ShieldHit.Amount,
                AttackerId = ShieldHit.AttackerId,
                HitPos = ShieldHit.HitPos,
                DamageType = ShieldHit.DamageType
            };

            return hitClone;
        }

        private void AddLocalHit()
        {
            ShieldHits.Add(new ShieldHit(MyEntities.GetEntityById(ShieldHit.AttackerId), ShieldHit.Amount, MyStringHash.GetOrCompute(ShieldHit.DamageType), ShieldHit.HitPos));
        }

        private void AbsorbClientShieldHits()
        {
            for (int i = 0; i < ShieldHits.Count; i++)
            {
                var hit = ShieldHits[i];
                var damageType = hit.DamageType;

                if (!NotFailed) continue;

                if (damageType == Session.Instance.MPExplosion)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Energy;
                    Absorb += hit.Amount * ConvToWatts;
                    UtilsStatic.CreateFakeSmallExplosion(WorldImpactPosition);
                    if (hit.Attacker != null)
                    {
                        ((IMyDestroyableObject) hit.Attacker).DoDamage(1, Session.Instance.MPKinetic, false, null, ShieldEnt.EntityId);
                    }
                    continue;
                }
                if (damageType == Session.Instance.MPKinetic)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Kinetic;
                    Absorb += hit.Amount * ConvToWatts;
                    continue;
                }
                if (damageType == Session.Instance.MPEnergy)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Energy;
                    Absorb += hit.Amount * ConvToWatts;
                    continue;
                }
                if (damageType == Session.Instance.MPEMP)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Energy;
                    Absorb += hit.Amount * ConvToWatts;
                }
            }
            ShieldHits.Clear();
        }
    }
}
