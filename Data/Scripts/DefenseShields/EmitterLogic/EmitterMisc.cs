using System;
using System.Text;
using DefenseShields.Support;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace DefenseShields
{
    public partial class Emitters
    {
        #region Init/Misc

        private void BeforeInit()
        {
            if (Emitter.CubeGrid.Physics == null) return;
            Session.Instance.Emitters.Add(this);
            PowerInit();
            _isServer = Session.Instance.IsServer;
            _isDedicated = Session.Instance.DedicatedServer;
            IsStatic = Emitter.CubeGrid.IsStatic;
            _disableLos = Session.Enforced.DisableLineOfSight == 1;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            _bTime = _isDedicated ? 10 : 1;
            _bInit = true;
        }


        private void StorageSetup()
        {
            if (EmiState == null) EmiState = new EmitterState(Emitter);
            EmiState.StorageInit();
            EmiState.LoadState();

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                EmiState.State.ActiveEmitterId = 0;
                EmiState.State.Backup = false;
                EmiState.State.Los = true;
                if (EmiState.State.Suspend)
                {
                    EmiState.State.Suspend = false;
                    EmiState.State.Link = false;
                    EmiState.State.Mode = -1;
                    EmiState.State.BoundingRange = -1;
                }
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
                    MaxRequiredInput = 0f,
                    RequiredInputFunc = () => _power
                };
                Sink.Init(MyStringHash.GetOrCompute("Utility"), ResourceInfo);
                Sink.AddType(ref ResourceInfo);
                Entity.Components.Add(Sink);
                Sink.Update();
            }
            catch (Exception ex) { Log.Line($"Exception in PowerPreInit: {ex}"); }
        }

        private void PowerInit()
        {
            try
            {
                var enableState = Emitter.Enabled;
                if (enableState)
                {
                    Emitter.Enabled = false;
                    Emitter.Enabled = true;
                }
                Sink.Update();
                if (Session.Enforced.Debug == 3) Log.Line($"PowerInit: EmitterId [{Emitter.EntityId}]");
            }
            catch (Exception ex) { Log.Line($"Exception in AddResourceSourceComponent: {ex}"); }
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                var mode = Enum.GetName(typeof(EmitterType), EmiState.State.Mode);
                if (!EmiState.State.Link)
                {
                    stringBuilder.Append("[ No Valid Controller ]" +
                                         "\n" +
                                         "\n[Emitter Type]: " + mode +
                                         "\n[Grid Compatible]: " + EmiState.State.Compatible +
                                         "\n[Controller Link]: " + EmiState.State.Link +
                                         "\n[Controller Bus]: " + (ShieldComp?.DefenseShields != null) +
                                         "\n[Line of Sight]: " + EmiState.State.Los +
                                         "\n[Is Suspended]: " + EmiState.State.Suspend +
                                         "\n[Is a Backup]: " + EmiState.State.Backup);
                }
                //else if (!EmiState.State.Online)
                else if (EmiState.State.ActiveEmitterId == 0)
                {
                    stringBuilder.Append("[ Emitter Offline ]" +
                                         "\n" +
                                         "\n[Emitter Type]: " + mode +
                                         "\n[Grid Compatible]: " + EmiState.State.Compatible +
                                         "\n[Controller Link]: " + EmiState.State.Link +
                                         "\n[Line of Sight]: " + EmiState.State.Los +
                                         "\n[Is Suspended]: " + EmiState.State.Suspend +
                                         "\n[Is a Backup]: " + EmiState.State.Backup);
                }
                else
                {
                    stringBuilder.Append("[ Emitter Online ]" +
                                         "\n" +
                                         "\n[Emitter Type]: " + mode +
                                         "\n[Grid Compatible]: " + EmiState.State.Compatible +
                                         "\n[Controller Link]: " + EmiState.State.Link +
                                         "\n[Line of Sight]: " + EmiState.State.Los +
                                         "\n[Is Suspended]: " + EmiState.State.Suspend +
                                         "\n[Is a Backup]: " + EmiState.State.Backup);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in AppendingCustomInfo: {ex}"); }
        }

        private void RegisterEvents(bool register = true)
        {
            if (register)
            {
                Emitter.EnabledChanged += CheckEmitter;
            }
            else
            {
                Emitter.AppendingCustomInfo -= AppendingCustomInfo;
                Emitter.EnabledChanged -= CheckEmitter;
            }
        }

        internal void TerminalRefresh(bool update = true)
        {
            Emitter.RefreshCustomInfo();
            if (update && InControlPanel && InThisTerminal)
            {
                MyCube.UpdateTerminal();
            }
        }

        private void SaveAndSendAll()
        {
            _firstSync = true;
            EmiState.SaveState();
            EmiState.NetworkUpdate();
            if (Session.Enforced.Debug >= 3) Log.Line($"SaveAndSendAll: EmitterId [{Emitter.EntityId}]");

        }

        private void Timing()
        {
            ++_age;
            if (_count++ == 59)
            {
                _count = 0;
                _lCount++;
                if (_lCount == 10) _lCount = 0;
            }

            if (_count == 29 && !_isDedicated)
            {
                TerminalRefresh(true);
            }
        }
        #endregion

    }
}
