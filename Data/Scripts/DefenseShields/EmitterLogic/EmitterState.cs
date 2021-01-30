using System;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace DefenseShields
{
    public partial class Emitters
    {
        #region Block Status
        private bool ControllerLink()
        {
            if (ShieldComp?.DefenseShields?.MyGrid != MyGrid) MyGrid.Components.TryGet(out ShieldComp);

            if (!_isServer)
            {
                var link = ClientEmitterReady();
                if (!link && !_blockReset) BlockReset(true);

                return link;
            }

            if (!_firstSync && _readyToSync) SaveAndSendAll();

            var linkWas = EmiState.State.Link;
            var losWas = EmiState.State.Los;
            var idWas = EmiState.State.ActiveEmitterId;
            if (!EmitterReady())
            {
                EmiState.State.Link = false;

                if (linkWas || losWas != EmiState.State.Los || idWas != EmiState.State.ActiveEmitterId)
                {
                    if (!_isDedicated && !_blockReset) BlockReset(true);
                    NeedUpdate();
                }
                return false;
            }

            EmiState.State.Link = true;

            if (!linkWas || losWas != EmiState.State.Los || idWas != EmiState.State.ActiveEmitterId) NeedUpdate();

            return true;
        }

        private bool EmitterReady()
        {
            if (Suspend() || !BlockWorking())
                return false;

            return true;
        }

        private bool ClientEmitterReady()
        {
            if (ShieldComp?.DefenseShields == null) return false;

            if (!_compact)
            {
                if (!MyCube.IsWorking) Entity.TryGetSubpart("Rotor", out SubpartRotor);
                if (SubpartRotor == null) return false;
            }

            if (!EmiState.State.Los) LosLogic();

            if (EmiState.State.Los && !_wasLosState)
            {
                _wasLosState = EmiState.State.Los;
                _updateLosState = false;
                LosScaledCloud.Clear();
            }
            return EmiState.State.Link;
        }

        private bool Suspend()
        {
            EmiState.State.ActiveEmitterId = 0;
            if (!MyCube.IsWorking)
            {
                EmiState.State.Suspend = true;
                if (ShieldComp?.StationEmitter == this) ShieldComp.StationEmitter = null;
                else if (ShieldComp?.ShipEmitter == this) ShieldComp.ShipEmitter = null;
                return true;
            }
            if (!_compact && SubpartRotor == null)
            {
                Entity.TryGetSubpart("Rotor", out SubpartRotor);
                if (SubpartRotor == null)
                {
                    EmiState.State.Suspend = true;
                    return true;
                }
                SubpartRotor.NeedsWorldMatrix = true;
            }

            if (ShieldComp == null)
            {
                EmiState.State.Suspend = true;
                return true;
            }

            var working = MyCube.IsWorking;
            var stationMode = EmitterMode == EmitterType.Station;
            var shipMode = EmitterMode != EmitterType.Station;
            var modes = (IsStatic && stationMode) || (!IsStatic && shipMode);
            var mySlotNull = (stationMode && ShieldComp.StationEmitter == null) || (shipMode && ShieldComp.ShipEmitter == null);
            var myComp = (stationMode && ShieldComp.StationEmitter == this) || (shipMode && ShieldComp.ShipEmitter == this);

            var myMode = working && modes;
            var mySlotOpen = working && mySlotNull;
            var myShield = myMode && myComp;
            var iStopped = !working && myComp && modes;
            if (mySlotOpen)
            {
                Session.Instance.BlockTagActive(Emitter);
                if (stationMode)
                {
                    EmiState.State.Backup = false;
                    ShieldComp.StationEmitter = this;
                    if (myMode)
                    {
                        TookControl = true;
                        ShieldComp.EmitterMode = (int)EmitterMode;
                        ShieldComp.EmitterEvent = true;
                        ShieldComp.EmittersSuspended = false;
                        EmiState.State.Suspend = false;
                        myShield = true;
                        EmiState.State.Backup = false;
                    }
                    else EmiState.State.Suspend = true;
                }
                else
                {
                    EmiState.State.Backup = false;
                    ShieldComp.ShipEmitter = this;

                    if (myMode)
                    {
                        TookControl = true;
                        ShieldComp.EmitterMode = (int)EmitterMode;
                        ShieldComp.EmitterEvent = true;
                        ShieldComp.EmittersSuspended = false;
                        EmiState.State.Suspend = false;
                        myShield = true;
                        EmiState.State.Backup = false;
                    }
                    else EmiState.State.Suspend = true;
                }
                if (Session.Enforced.Debug >= 3) Log.Line($"mySlotOpen: {Definition.Name} - myMode:{myMode} - MyShield:{myShield} - Mode:{EmitterMode} - Static:{IsStatic} - ELos:{ShieldComp.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - ModeM:{(int)EmitterMode == ShieldComp.EmitterMode} - S:{EmiState.State.Suspend} - EmitterId [{Emitter.EntityId}]");
            }
            else if (!myMode)
            {
                var compMode = ShieldComp.EmitterMode;
                if ((!EmiState.State.Suspend && ((compMode == 0 && !IsStatic) || (compMode != 0 && IsStatic))) || (!EmiState.State.Suspend && iStopped))
                {
                    ShieldComp.EmittersSuspended = true;
                    ShieldComp.EmitterLos = false;
                    ShieldComp.EmitterEvent = true;
                    if (Session.Enforced.Debug >= 3) Log.Line($"!myMode: {Definition.Name} suspending - Match:{(int)EmitterMode == ShieldComp.EmitterMode} - ELos:{ShieldComp.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - ModeEq:{(int)EmitterMode == ShieldComp?.EmitterMode} - S:{EmiState.State.Suspend} - Static:{IsStatic} - EmitterId [{Emitter.EntityId}]");
                }
                else if (!EmiState.State.Suspend)
                {
                    if (Session.Enforced.Debug >= 3) Log.Line($"!myMode: {Definition.Name} suspending - Match:{(int)EmitterMode == ShieldComp.EmitterMode} - ELos:{ShieldComp.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - ModeEq:{(int)EmitterMode == ShieldComp?.EmitterMode} - S:{EmiState.State.Suspend} - Static:{IsStatic} - EmitterId [{Emitter.EntityId}]");
                }
                EmiState.State.Suspend = true;
            }
            if (iStopped)
            {
                return EmiState.State.Suspend;
            }

            if (!myShield)
            {
                if (!EmiState.State.Backup)
                {
                    Session.Instance.BlockTagBackup(Emitter);
                    EmiState.State.Backup = true;
                    if (Session.Enforced.Debug >= 3) Log.Line($"!myShield - !otherMode: {Definition.Name} - isStatic:{IsStatic} - myShield:{myShield} - myMode {myMode} - Mode:{EmitterMode} - CompMode: {ShieldComp.EmitterMode} - ELos:{ShieldComp.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - EmitterId [{Emitter.EntityId}]");
                }
                EmiState.State.Suspend = true;
            }

            if (myShield && EmiState.State.Suspend)
            {
                ShieldComp.EmittersSuspended = false;
                ShieldComp.EmitterEvent = true;
                EmiState.State.Backup = false;
                EmiState.State.Suspend = false;
                if (Session.Enforced.Debug >= 3) Log.Line($"Unsuspend - !otherMode: {Definition.Name} - isStatic:{IsStatic} - myShield:{myShield} - myMode {myMode} - Mode:{EmitterMode} - CompMode: {ShieldComp.EmitterMode} - ELos:{ShieldComp.EmitterLos} - ES:{ShieldComp.EmittersSuspended} - EmitterId [{Emitter.EntityId}]");
            }
            else if (EmiState.State.Suspend) return true;

            EmiState.State.Suspend = false;
            return false;
        }

        private bool BlockWorking()
        {
            EmiState.State.ActiveEmitterId = MyCube.EntityId;

            if (ShieldComp.EmitterMode != (int)EmitterMode) ShieldComp.EmitterMode = (int)EmitterMode;
            if (ShieldComp.EmittersSuspended) SuspendCollisionDetected();

            LosLogic();

            ShieldComp.EmitterLos = EmiState.State.Los;
            ShieldComp.ActiveEmitterId = EmiState.State.ActiveEmitterId;

            var comp = ShieldComp;
            var ds = comp.DefenseShields;
            var dsNull = ds == null;
            var shieldWaiting = !dsNull && ds.DsState.State.EmitterLos != EmiState.State.Los;
            if (shieldWaiting) comp.EmitterEvent = true;

            if (!EmiState.State.Los || dsNull || shieldWaiting || !ds.DsState.State.Online || !(_tick >= ds.ResetEntityTick))
            {
                if (!_isDedicated && !_blockReset) BlockReset(true);
                return false;
            }
            return true;
        }

        private void SuspendCollisionDetected()
        {
            ShieldComp.EmitterMode = (int)EmitterMode;
            ShieldComp.EmittersSuspended = false;
            ShieldComp.EmitterEvent = true;
            TookControl = true;
        }
        #endregion

        #region Block States
        internal void UpdateState(EmitterStateValues newState)
        {
            if (newState.MId > EmiState.State.MId)
            {
                if (Session.Enforced.Debug >= 3) Log.Line($"UpdateState - NewLink:{newState.Link} - OldLink:{EmiState.State.Link} - EmitterId [{Emitter.EntityId}]:\n{EmiState.State}");
                EmiState.State = newState;
            }
        }

        private void NeedUpdate()
        {
            EmiState.State.Mode = (int)EmitterMode;
            EmiState.State.BoundingRange = ShieldComp?.DefenseShields?.BoundingRange ?? 0f;
            EmiState.State.Compatible = (IsStatic && EmitterMode == EmitterType.Station) || (!IsStatic && EmitterMode != EmitterType.Station);
            EmiState.SaveState();
            if (Session.Instance.MpActive) EmiState.NetworkUpdate();
        }

        private void CheckEmitter(IMyTerminalBlock myTerminalBlock)
        {
            try
            {
                if (myTerminalBlock.IsWorking && ShieldComp != null) ShieldComp.CheckEmitters = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CheckEmitter: {ex}"); }
        }

        private void SetEmitterType()
        {
            Definition = DefinitionManager.Get(Emitter.BlockDefinition.SubtypeId);
            switch (Definition.Name)
            {
                case "EmitterST":
                    EmitterMode = EmitterType.Station;
                    Entity.TryGetSubpart("Rotor", out SubpartRotor);
                    break;
                case "EmitterL":
                case "EmitterLA":
                case "NPCEmitterLB":
                    EmitterMode = EmitterType.Large;
                    if (Definition.Name == "EmitterLA" || Definition.Name == "NPCEmitterLB") _compact = true;
                    else Entity.TryGetSubpart("Rotor", out SubpartRotor);
                    break;
                case "EmitterS":
                case "EmitterSA":
                case "NPCEmitterSB":
                    EmitterMode = EmitterType.Small;
                    if (Definition.Name == "EmitterSA" || Definition.Name == "NPCEmitterSB") _compact = true;
                    else Entity.TryGetSubpart("Rotor", out SubpartRotor);
                    break;
            }
            Emitter.AppendingCustomInfo += AppendingCustomInfo;
        }
        #endregion

    }
}
