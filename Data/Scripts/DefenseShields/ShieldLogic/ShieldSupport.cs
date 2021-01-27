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

                if (update) ShieldChangeState();
            }
            else if (_tick - InitTick > 30)
            {
                if (!DsState.State.ModulateEnergy.Equals(1f) || !DsState.State.ModulateKinetic.Equals(1f) || DsState.State.EmpProtection || DsState.State.ReInforce) update = true;
                DsState.State.ModulateEnergy = 1f;
                DsState.State.ModulateKinetic = 1f;
                DsState.State.EmpProtection = false;
                DsState.State.ReInforce = false;
                if (update) ShieldChangeState();

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

        internal void AddEmpBlastHit(long attackerId, float amount, MyStringHash damageType, Vector3D hitPos)
        {
            ShieldHit.Amount += amount;
            ShieldHit.DamageType = damageType.String;
            ShieldHit.HitPos = hitPos;
            ShieldHit.AttackerId = attackerId;
            _lastSendDamageTick = _tick;
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
                    continue;
                }
            }
            ShieldHits.Clear();
        }

        public void AbsorbEmp()
        {
            if (Vector3D.DistanceSquared(DetectionCenter, Session.Instance.EmpWork.EpiCenter) <= Session.Instance.EmpWork.RangeCapSqr)
            {
                var empResistenceRatio = 1f;
                const long AttackerId = 0L;
                var energyResistenceRatio = DsState.State.ModulateKinetic;
                var epiCenter = Session.Instance.EmpWork.EpiCenter;
                var rangeCap = Session.Instance.EmpWork.RangeCap;
                var empDirYield = Session.Instance.EmpWork.DirYield;

                if (DsState.State.EmpProtection)
                {
                    if (energyResistenceRatio < 0.4) energyResistenceRatio = 0.4f;
                    empResistenceRatio = 0.1f;
                }
                //if (Session.Enforced.Debug >= 2) Log.Line($"[EmpBlastShield - Start] ShieldOwner:{MyGrid.DebugName} - Yield:{warHeadYield} - StackCount:{stackCount} - ProtectionRatio:{energyResistenceRatio * empResistenceRatio} - epiCenter:{epiCenter}");
                var line = new LineD(epiCenter, SOriBBoxD.Center);
                var testDir = Vector3D.Normalize(line.From - line.To);
                var ray = new RayD(line.From, -testDir);
                var ellipsoid = CustomCollision.IntersectEllipsoid(DetectMatrixOutsideInv, DetectionMatrix, ray);
                if (!ellipsoid.HasValue)
                {
                    //if (Session.Enforced.Debug >= 2) Log.Line($"[EmpBlastShield - Ellipsoid null hit] ShieldOwner:{MyGrid.DebugName} - Yield:{warHeadYield} - StackCount:{stackCount} - ProtectionRatio:{energyResistenceRatio * empResistenceRatio} - epiCenter:{epiCenter}");
                    return;
                }
                var impactPos = line.From + (testDir * -ellipsoid.Value);
                IHitInfo hitInfo;
                MyAPIGateway.Physics.CastRay(epiCenter, impactPos, out hitInfo, CollisionLayers.DefaultCollisionLayer);
                if (hitInfo != null)
                {
                    //if (Session.Enforced.Debug >= 2) Log.Line($"[EmpBlastShield - occluded] ShieldOwner:{MyGrid.DebugName} - by {((MyEntity)hitInfo.HitEntity).DebugName}");
                    return;
                }
                var gridLocalMatrix = MyGrid.PositionComp.LocalMatrix;
                var worldDirection = impactPos - gridLocalMatrix.Translation;
                var localPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(gridLocalMatrix));
                var hitFaceSurfaceArea = UtilsStatic.GetIntersectingSurfaceArea(ShieldShapeMatrix, localPosition);

                var invSqrDist = UtilsStatic.InverseSqrDist(epiCenter, impactPos, rangeCap);
                var damageScaler = invSqrDist * hitFaceSurfaceArea;
                if (invSqrDist <= 0)
                {
                    //if (Session.Enforced.Debug >= 2) Log.Line($"[EmpBlastShield - Range] ShieldOwner:{MyGrid.DebugName} - insqrDist was 0");
                    return;
                }

                var targetDamage = (float)(((empDirYield * damageScaler) * energyResistenceRatio) * empResistenceRatio);

                if (targetDamage >= DsState.State.Charge * ConvToHp) _empOverLoad = true;
                //if (Session.Enforced.Debug >= 2) Log.Line($"-----------------------] epiDist:{Vector3D.Distance(epiCenter, impactPos)} - iSqrDist:{invSqrDist} - RangeCap:{rangeCap} - SurfaceA:{hitFaceSurfaceArea}({_ellipsoidSurfaceArea * 0.5}) - dirYield:{empDirYield} - damageScaler:{damageScaler} - Damage:{targetDamage}(toOver:{(targetDamage / (DsState.State.Charge * ConvToHp))})");

                if (_isServer && _mpActive)
                    AddEmpBlastHit(AttackerId, targetDamage, Session.Instance.MPEMP, impactPos);

                EnergyHit = HitType.Energy;
                WorldImpactPosition = epiCenter;
                Absorb += targetDamage;
            }
        }
    }
}
