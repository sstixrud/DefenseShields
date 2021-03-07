using System;
using System.Text;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    public partial class Enhancers 
    {

        internal void UpdateState(EnhancerStateValues newState)
        {
            if (newState.MId > EnhState.State.MId)
            {
                EnhState.State = newState;
                if (Session.Enforced.Debug >= 3) Log.Line($"UpdateState: EnhancerId [{Enhancer.EntityId}]");
            }
        }

        private void SaveAndSendAll()
        {
            _firstSync = true;
            if (!_isServer) return;
            EnhState.SaveState();
            EnhState.NetworkUpdate();
            if (Session.Enforced.Debug >= 3) Log.Line($"SaveAndSendAll: EnhancerId [{Enhancer.EntityId}]");
        }

        private void Timing()
        {
            if (_count++ == 59) _count = 0;

            if (_count == 29 && !_isDedicated)
            {
                TerminalRefresh(true);
            }
        }

        private bool EnhancerReady()
        {
            if (_subpartRotor == null)
            {
                Entity.TryGetSubpart("Rotor", out _subpartRotor);
                if (_subpartRotor == null) return false;
            }

            if (ShieldComp?.DefenseShields?.MyGrid != MyGrid) MyGrid.Components.TryGet(out ShieldComp);
            if (_isServer)
            {
                if (!_firstSync && _readyToSync) SaveAndSendAll();

                if (!BlockWorking()) return false;
            }
            else
            {
                if (ShieldComp?.DefenseShields == null) return false;

                if (!EnhState.State.Backup && ShieldComp.Enhancer != this) ShieldComp.Enhancer = this;

                if (!EnhState.State.Online) return false;
            }

            return BlockMoveAnimationReset();
        }

        private bool BlockWorking()
        {
            if (!MyCube.IsWorking || ShieldComp?.DefenseShields == null)
            {
                NeedUpdate(EnhState.State.Online, false);
                return false;
            }

            if (ShieldComp.Enhancer != this)
            {
                if (ShieldComp.Enhancer == null)
                {
                    Session.Instance.BlockTagActive(Enhancer);
                    ShieldComp.Enhancer = this;
                    EnhState.State.Backup = false;
                }
                else if (ShieldComp.Enhancer != this)
                {
                    if (!EnhState.State.Backup || _firstLoop) Session.Instance.BlockTagBackup(Enhancer);
                    EnhState.State.Backup = true;
                    EnhState.State.Online = false;
                }
            }

            _firstLoop = false;
            var ds = ShieldComp.DefenseShields;
            if (!EnhState.State.Backup && ShieldComp.Enhancer == this && (ds.NotFailed || ds.DsState.State.Lowered || ds.DsState.State.Overload || ds.DsState.State.EmpOverLoad || !ds.WarmedUp))
            {
                NeedUpdate(EnhState.State.Online, true);
                return true;
            }

            NeedUpdate(EnhState.State.Online, false);

            return false;
        }

        private void NeedUpdate(bool onState, bool turnOn)
        {
            if (!onState && turnOn)
            {
                EnhState.State.Online = true;
                EnhState.SaveState();
                EnhState.NetworkUpdate();
                if (!_isDedicated) Enhancer.RefreshCustomInfo();
            }
            else if (onState & !turnOn)
            {
                EnhState.State.Online = false;
                EnhState.SaveState();
                EnhState.NetworkUpdate();
                if (!_isDedicated) Enhancer.RefreshCustomInfo();
            }
        }

        private void StorageSetup()
        {
            if (EnhState == null) EnhState = new EnhancerState(Enhancer);
            EnhState.StorageInit();
            EnhState.LoadState();
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                EnhState.State.Backup = false;
                EnhState.State.Online = false;
            }
        }

        private void PowerPreInit()
        {
            try
            {
                if (Sink == null)
                {
                    Sink = new MyResourceSinkComponent();
                }

                ResourceInfo = new MyResourceSinkInfo()
                {
                    ResourceTypeId = _gId,
                    MaxRequiredInput = 0.02f,
                    RequiredInputFunc = () => Power
                };

                Sink.Init(MyStringHash.GetOrCompute("Utility"), ResourceInfo);
                Sink.AddType(ref ResourceInfo);
                Entity.Components.Add(Sink);
            }
            catch (Exception ex)
            {
                Log.Line($"Exception in PowerPreInit: {ex}");
            }
        }

        private void PowerInit()
        {
            try
            {
                var enableState = Enhancer.Enabled;

                if (enableState)
                {
                    Enhancer.Enabled = false;
                    Enhancer.Enabled = true;
                }

                Sink.Update();
                if (Session.Enforced.Debug == 3) Log.Line($"PowerInit: EnhancerId [{Enhancer.EntityId}]");
            }
            catch (Exception ex)
            {
                Log.Line($"Exception in AddResourceSourceComponent: {ex}");
            }
        }

        private bool BlockMoveAnimationReset()
        {
            if (!MyCube.IsFunctional) return false;

            if (_subpartRotor == null)
            {
                return Entity.TryGetSubpart("Rotor", out _subpartRotor);
            }

            if (!MyCube.NeedsWorldMatrix)
                MyCube.NeedsWorldMatrix = true;

            if (!_subpartRotor.Closed) return true;

            _subpartRotor.Subparts.Clear();
            return Entity.TryGetSubpart("Rotor", out _subpartRotor);
        }

        private void BlockMoveAnimation()
        {
            if (!BlockMoveAnimationReset()) return;
            RotationTime -= 1;
            var rotationMatrix = Matrix.CreateRotationY(0.05f * RotationTime);
            _subpartRotor.PositionComp.SetLocalMatrix(ref rotationMatrix, null, true);
            }

        internal void TerminalRefresh(bool update = true)
        {
            Enhancer.RefreshCustomInfo();
            if (update && InControlPanel && InThisTerminal)
            {
                MyCube.UpdateTerminal();
            }
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            if (ShieldComp?.DefenseShields == null)
            {
                stringBuilder.Append("[Controller Link]: False");
            }
            else if (!EnhState.State.Backup && ShieldComp.DefenseShields.ShieldMode == DefenseShields.ShieldType.Station)
            {
                stringBuilder.Append("[Online]: " + EnhState.State.Online +
                                     "\n" +
                                     "\n[Amplifying Shield]: " + EnhState.State.Online +
                                     "\n[Enhancer Mode]: Fortress" +
                                     "\n[Bonsus] MaxHP, Repel Grids");
            }
            else if (!EnhState.State.Backup)
            {
                stringBuilder.Append("[Online]: " + EnhState.State.Online +
                                     "\n" +
                                     "\n[Shield Detected]: " + EnhState.State.Online +
                                     "\n[Enhancer Mode]: EMP Option");
            }
            else
            {
                stringBuilder.Append("[Backup]: " + EnhState.State.Backup);
            }
        }

        private void RegisterEvents(bool register = true)
        {
            if (register)
            {
                Enhancer.AppendingCustomInfo += AppendingCustomInfo;
            }
            else
            {
                Enhancer.AppendingCustomInfo -= AppendingCustomInfo;
            }
        }
    }
}
