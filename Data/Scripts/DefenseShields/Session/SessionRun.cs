namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game.Components;
    using VRageMath;
    using MyVisualScriptLogicProvider = Sandbox.Game.MyVisualScriptLogicProvider;

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation, int.MinValue)]
    public partial class Session : MySessionComponentBase
    {
        #region BeforeStart
        public override void BeforeStart()
        {
            try
            {
                MpActive = MyAPIGateway.Multiplayer.MultiplayerActive;
                IsServer = MyAPIGateway.Multiplayer.IsServer;
                DedicatedServer = MyAPIGateway.Utilities.IsDedicated;

                var env = MyDefinitionManager.Static.EnvironmentDefinition;
                if (env.LargeShipMaxSpeed > MaxEntitySpeed) MaxEntitySpeed = env.LargeShipMaxSpeed;
                else if (env.SmallShipMaxSpeed > MaxEntitySpeed) MaxEntitySpeed = env.SmallShipMaxSpeed;

                Log.Init("debugdevelop.log");
                Log.Line($"Logging Started: Server:{IsServer} - Dedicated:{DedicatedServer} - MpActive:{MpActive}");

                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, CheckDamage);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(PACKET_ID, ReceivedPacket);

                if (!DedicatedServer && IsServer) Players.TryAdd(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.Player);
                MyEntities.OnEntityRemove += OnEntityRemove;
                MyAPIGateway.Session.OnSessionReady += OnSessionReady;
                MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
                MyVisualScriptLogicProvider.PlayerRespawnRequest += PlayerConnected;
                if (!DedicatedServer)
                {
                    MyAPIGateway.TerminalControls.CustomControlGetter += CustomControls;
                }

                if (IsServer)
                {
                    Log.Line("LoadConf - Session: This is a server");
                    UtilsStatic.PrepConfigFile();
                    UtilsStatic.ReadConfigFile();
                }

                if (MpActive)
                {
                    SyncDist = MyAPIGateway.Session.SessionSettings.SyncDistance;
                    SyncDistSqr = SyncDist * SyncDist;
                    SyncBufferedDistSqr = SyncDistSqr + 250000;
                    if (Enforced.Debug >= 2) Log.Line($"SyncDistSqr:{SyncDistSqr} - SyncBufferedDistSqr:{SyncBufferedDistSqr} - DistNorm:{SyncDist}");
                }
                else
                {
                    SyncDist = MyAPIGateway.Session.SessionSettings.ViewDistance;
                    SyncDistSqr = SyncDist * SyncDist;
                    SyncBufferedDistSqr = SyncDistSqr + 250000;
                    if (Enforced.Debug >= 2) Log.Line($"SyncDistSqr:{SyncDistSqr} - SyncBufferedDistSqr:{SyncBufferedDistSqr} - DistNorm:{SyncDist}");
                }
                WebMonitor();

                if (!IsServer) RequestEnforcement(MyAPIGateway.Multiplayer.MyId);

                foreach (var mod in MyAPIGateway.Session.Mods)
                    if (mod.PublishedFileId == 540003236) ThyaImages = true;

                ApiServer.Load();
            }
            catch (Exception ex) { Log.Line($"Exception in BeforeStart: {ex}"); }
        }
        #endregion

        #region Simulation
        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (!MonitorTask.IsComplete)
                    MonitorTask.Wait();

                if (MonitorTask.IsComplete && MonitorTask.valid && MonitorTask.Exceptions != null)
                    TaskHasErrors(ref MonitorTask, "PTask");

                Timings();

                if (!ThreadEvents.IsEmpty)
                {
                    IThreadEvent tEvent;
                    while (ThreadEvents.TryDequeue(out tEvent)) tEvent.Execute();
                }

                LogicUpdates();

                if (EmpStore.Count != 0 && !EmpDispatched)
                {
                    EmpDispatched = true;   
                    PrepEmpBlast();
                    if (EmpWork.EventRunning) MyAPIGateway.Parallel.Start(ComputeEmpBlast, EmpCallBack);
                    else EmpDispatched = false;
                }

                if (_warEffect && Tick20) WarEffect();
            }
            catch (Exception ex) { Log.Line($"Exception in SessionBeforeSim: {ex}"); }
        }

        public override void UpdateAfterSimulation()
        {
            
            SplitMonitor();
            lock (ActiveShields)
            {
                foreach (var s in ActiveShields)
                    if (s.GridIsMobile && !s.Asleep) s.MobileUpdate();
            }

            WebMonitor();
        }
        #endregion


        #region Draw
        public override void Draw()
        {
            if (DedicatedServer) return;
            try
            {
                var compCount = Controllers.Count;
                if (compCount == 0) return;

                if (SphereOnCamera.Length != compCount) Array.Resize(ref SphereOnCamera, compCount);

                if (_count == 0 && _lCount == 0) OnCountThrottle = false;
                var onCount = 0;
                for (int i = 0; i < compCount; i++)
                {
                    var s = Controllers[i];
                    if (s.DsState.State.Suspended) continue;

                    if (s.KineticCoolDown > -1)
                    {
                        s.KineticCoolDown++;
                        if (s.KineticCoolDown == 6) s.KineticCoolDown = -1;
                    }

                    if (s.EnergyCoolDown > -1)
                    {
                        s.EnergyCoolDown++;
                        if (s.EnergyCoolDown == 9) s.EnergyCoolDown = -1;
                    }

                    if (!s.WarmedUp || s.DsState.State.Lowered || s.DsState.State.Sleeping || s.DsState.State.Suspended || !s.DsState.State.EmitterLos) continue;

                    var sp = new BoundingSphereD(s.DetectionCenter, s.BoundingRange);
                    if (!MyAPIGateway.Session.Camera.IsInFrustum(ref sp))
                    {
                        SphereOnCamera[i] = false;
                        continue;
                    }
                    SphereOnCamera[i] = true;
                    if (!s.Icosphere.ImpactsFinished) onCount++;
                }

                if (onCount >= OnCount)
                {
                    OnCount = onCount;
                    OnCountThrottle = true;
                }
                else if (!OnCountThrottle && _count == 59 && _lCount == 9) OnCount = onCount;

                for (int i = 0; i < compCount; i++)
                {
                    var s = Controllers[i];
                    var drawSuspended = !s.WarmedUp || s.DsState.State.Lowered || s.DsState.State.Sleeping || s.DsState.State.Suspended || !s.DsState.State.EmitterLos;

                    if (drawSuspended) continue;

                    if (s.DsState.State.Online)
                    {
                        if (SphereOnCamera[i]) s.Draw(OnCount, SphereOnCamera[i]);
                        else if (s.Icosphere.ImpactsFinished)
                        {
                            if (s.WorldImpactPosition != Vector3D.NegativeInfinity)
                            {
                                s.Draw(OnCount, true);
                                s.Icosphere.ImpactPosState = Vector3D.NegativeInfinity;
                            }
                        }
                        else s.Icosphere.StepEffects();
                    }
                    else if (s.WarmedUp && SphereOnCamera[i]) s.DrawShieldDownIcon();
                }
            }
            catch (Exception ex) { Log.Line($"Exception in SessionDraw: {ex}"); }
        }
        #endregion
        #region Data
        public override void LoadData()
        {
            Instance = this;
        }

        protected override void UnloadData()
        {
            ApiServer.Unload();
            Instance = null;
            HudComp = null;
            Enforced = null;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(PACKET_ID, ReceivedPacket);

            MyVisualScriptLogicProvider.PlayerDisconnected -= PlayerDisconnected;
            MyVisualScriptLogicProvider.PlayerRespawnRequest -= PlayerConnected;

            MyEntities.OnEntityRemove -= OnEntityRemove;
            MyAPIGateway.Session.OnSessionReady -= OnSessionReady;

            if (!DedicatedServer) MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControls;

            //Terminate();
            Log.Line("Logging stopped.");
            Log.Close();
        }
        #endregion
    }
}
