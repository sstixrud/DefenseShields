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
                if (!DsState.State.ModulateEnergy.Equals(modEnergyRatio) || !DsState.State.ModulateKinetic.Equals(modKineticRatio) || !DsState.State.EwarProtection.Equals(ShieldComp.Modulator.ModSet.Settings.EmpEnabled) || !DsState.State.ReInforce.Equals(ShieldComp.Modulator.ModSet.Settings.ReInforceEnabled)) update = true;
                DsState.State.ModulateEnergy = modEnergyRatio;
                DsState.State.ModulateKinetic = modKineticRatio;
                if (DsState.State.Enhancer)
                {
                    DsState.State.EwarProtection = ShieldComp.Modulator.ModSet.Settings.EmpEnabled;
                    DsState.State.ReInforce = ShieldComp.Modulator.ModSet.Settings.ReInforceEnabled;
                }

                if (_isServer && update) ShieldChangeState();
            }
            else if (_tick - InitTick > 30)
            {
                if (!DsState.State.ModulateEnergy.Equals(1f) || !DsState.State.ModulateKinetic.Equals(1f) || DsState.State.EwarProtection || DsState.State.ReInforce) update = true;
                DsState.State.ModulateEnergy = 1f;
                DsState.State.ModulateKinetic = 1f;
                DsState.State.EwarProtection = false;
                DsState.State.ReInforce = false;
                if (_isServer && update) ShieldChangeState();

            }
        }


        private void SetModulatorQuickKey()
        {
            if (ShieldComp.Modulator != null && ShieldComp.Modulator.ModState.State.Online) {

                if (Session.Instance.UiInput.KineticReleased)
                    Session.Instance.ActionAddDamageMod(ShieldComp.Modulator.Modulator);
                else if (Session.Instance.UiInput.EnergyReleased)
                    Session.Instance.ActionSubtractDamageMod(ShieldComp.Modulator.Modulator);
                if (Session.Instance.Settings.ClientConfig.Notices)
                    Session.Instance.SendNotice($"Shield modulation -- Kinetic [{ShieldComp.Modulator.ModState.State.ModulateKinetic}] - Energy [{ShieldComp.Modulator.ModState.State.ModulateEnergy}]");
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

        private void UpdateSides()
        {
            if (!Session.Instance.DedicatedServer && (Session.Instance.UiInput.AnyKeyPressed || Session.Instance.UiInput.KeyPrevPressed))
                ShieldHotKeys();
            
            if (ShieldRedirectState != DsSet.Settings.ShieldRedirects && Session.Instance.Tick >= RedirectUpdateTime)
                UpdateRedirectState();

            if (Session.Instance.Tick180 && MyGrid.MainCockpit != null && LastCockpit != MyGrid.MainCockpit)
                UpdateMapping();
        }

        private void ShieldHotKeys()
        {
            if (Session.Instance.HudComp != this || !Shield.HasPlayerAccess(MyAPIGateway.Session.Player.IdentityId) || Session.Instance.Settings.ClientConfig.DisableKeys)
                return;

            var input = Session.Instance.UiInput;
            var shuntCount = ShuntedSideCount();
            if (input.LeftReleased)
                QuickShuntUpdate(Session.ShieldSides.Left, shuntCount);
            else if (input.RightReleased)
                QuickShuntUpdate(Session.ShieldSides.Right, shuntCount);
            else if (input.FrontReleased)
                QuickShuntUpdate(Session.ShieldSides.Forward, shuntCount);
            else if (input.BackReleased)
                QuickShuntUpdate(Session.ShieldSides.Backward, shuntCount);
            else if (input.UpReleased)
                QuickShuntUpdate(Session.ShieldSides.Up, shuntCount);
            else if (input.DownReleased)
                QuickShuntUpdate(Session.ShieldSides.Down, shuntCount);
            else if (input.ShuntReleased) 
                DsUi.SetSideShunting(Shield, !DsSet.Settings.SideShunting);
            else if (input.KineticReleased || input.EnergyReleased)
                SetModulatorQuickKey();
        }



        private void QuickShuntUpdate(Session.ShieldSides side, int shuntedCount)
        {
            var isShunted = IsSideShunted(side);
            if (Session.Instance.UiInput.LongShuntKey) {

                if (!isShunted && shuntedCount >= 5) {

                    foreach (var pair in Session.Instance.ShieldShuntedSides) { 

                        if (pair.Key != side)
                            CallSideControl(pair.Key, false);
                    }
                }
                else {

                    CallSideControl(side, false);
                    foreach (var pair in Session.Instance.ShieldShuntedSides) {

                        if (pair.Key != side) 
                            CallSideControl(pair.Key, true);
                    }
                }
            }
            else
                CallSideControl(side, !isShunted);
        }

        private void CallSideControl(Session.ShieldSides side, bool enable)
        {
            switch (side)
            {
                case Session.ShieldSides.Left:
                    DsUi.SetLeftShield(Shield, enable);
                    break;
                case Session.ShieldSides.Right:
                    DsUi.SetRightShield(Shield, enable);
                    break;
                case Session.ShieldSides.Up:
                    DsUi.SetTopShield(Shield, enable);
                    break;
                case Session.ShieldSides.Down:
                    DsUi.SetBottomShield(Shield, enable);
                    break;
                case Session.ShieldSides.Forward:
                    DsUi.SetFrontShield(Shield, enable);
                    break;
                case Session.ShieldSides.Backward:
                    DsUi.SetBackShield(Shield, enable);
                    break;
            }
        }

        internal int ShuntedSideCount()
        {
            return Math.Abs(ShieldRedirectState.X) + Math.Abs(ShieldRedirectState.Y) + Math.Abs(ShieldRedirectState.Z);
        }

        public void UpdateMapping()
        {
            LastCockpit = MyGrid.MainCockpit as MyCockpit;
            var orientation = LastCockpit?.Orientation ?? MyCube.Orientation;
            var fwdReverse = Base6Directions.GetOppositeDirection(orientation.Forward);
            var upReverse = Base6Directions.GetOppositeDirection(orientation.Up);
            var leftReverse = Base6Directions.GetOppositeDirection(orientation.Left);

            RealSideStates[(Session.ShieldSides)orientation.Forward] = new Session.ShieldInfo {Side = Session.ShieldSides.Forward, Redirected = IsSideShunted(Session.ShieldSides.Forward)};
            RealSideStates[(Session.ShieldSides)fwdReverse] = new Session.ShieldInfo { Side = Session.ShieldSides.Backward, Redirected = IsSideShunted(Session.ShieldSides.Backward) };

            RealSideStates[(Session.ShieldSides)orientation.Up] = new Session.ShieldInfo { Side = Session.ShieldSides.Up, Redirected = IsSideShunted(Session.ShieldSides.Up) };
            RealSideStates[(Session.ShieldSides)upReverse] = new Session.ShieldInfo { Side = Session.ShieldSides.Down, Redirected = IsSideShunted(Session.ShieldSides.Down) };

            RealSideStates[(Session.ShieldSides)orientation.Left] = new Session.ShieldInfo { Side = Session.ShieldSides.Left, Redirected = IsSideShunted(Session.ShieldSides.Left) };
            RealSideStates[(Session.ShieldSides)leftReverse] = new Session.ShieldInfo { Side = Session.ShieldSides.Right, Redirected = IsSideShunted(Session.ShieldSides.Right) };
        }


        private bool _toggle;
        public bool RedirectVisualUpdate()
        {
            var turnedOff = !DsSet.Settings.SideShunting || ShieldRedirectState == Vector3.Zero;

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
                
                var redirecting = RealSideStates[side].Redirected;
                var showStale = _toggle && (redirecting && !draw || !redirecting && draw);
                var hideStale = !_toggle && draw;

                if (showStale || hideStale)
                    return true;
            }


            return false;
        }

        public void UpdateShieldRedirectVisuals()
        {
            bool shunting = false;
            foreach (var key in RealSideStates)
            {
                var side = key.Key;
                var enabled = key.Value.Redirected;
                MyEntitySubpart part;
                if (ShellActive.TryGetSubpart(Session.Instance.ShieldShuntedSides[side], out part))
                {
                    var shunted = enabled && _toggle;
                    if (shunted)
                        shunting = true;

                    RenderingSides[side] = shunted;
                    part.Render.UpdateRenderObject(shunted);
                }
            }
            _sidePulsing = shunting;
        }

        private int _pulseCounter = 10;
        private bool _pulseIncrease;
        private bool _sidePulsing;
        private void SidePulseRender()
        {
            if (!_pulseIncrease && _pulseCounter-- <= 0)
            {
                _pulseIncrease = true;
                _pulseCounter = 0;
            }
            else if (_pulseIncrease && _pulseCounter++ >= 9) {
                _pulseIncrease = false;
                _pulseCounter = 9;
            }

            foreach (var key in RealSideStates)
            {
                var side = key.Key;
                var enabled = key.Value.Redirected;
                MyEntitySubpart part;
                if (ShellActive.TryGetSubpart(Session.Instance.ShieldShuntedSides[side], out part))
                {
                    if (enabled)
                    {
                        part.Render.Transparency = _pulseCounter * 0.1f;
                        part.Render.UpdateTransparency();
                    }
                }
            }
        }

        private void ClearSidePulse()
        {
            _pulseCounter = 10;
            _pulseIncrease = false;
        }

        public bool IsSideShunted(Session.ShieldSides side)
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

                    var damage = hit.Amount * ConvToWatts;
                    EnergyDamage += damage;
                    Absorb += damage;

                    UtilsStatic.CreateFakeSmallExplosion(WorldImpactPosition);
                    ((IMyDestroyableObject) hit.Attacker)?.DoDamage(1, Session.Instance.MPKinetic, false, null, ShieldEnt.EntityId);
                    continue;
                }
                if (damageType == Session.Instance.MPKinetic)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Kinetic;
                    var damage = hit.Amount * ConvToWatts;
                    
                    KineticDamage += damage;
                    Absorb += damage;
                    continue;
                }
                if (damageType == Session.Instance.MPEnergy)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Energy;

                    var damage = hit.Amount * ConvToWatts;
                    EnergyDamage += damage;
                    Absorb += damage;
                    continue;
                }
                if (damageType == Session.Instance.MPEMP)
                {
                    ImpactSize = hit.Amount;
                    WorldImpactPosition = hit.HitPos;
                    EnergyHit = HitType.Energy;

                    var damage = hit.Amount * ConvToWatts;
                    EnergyDamage += damage;
                    Absorb += damage;
                }
            }
            ShieldHits.Clear();
        }
    }
}
