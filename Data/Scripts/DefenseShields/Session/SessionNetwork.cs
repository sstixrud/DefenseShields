namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.ModAPI;
    using VRageMath;

    public partial class Session
    {
        #region Network sync
        internal void RequestEnforcement(ulong requestorId)
        {
            try
            {
                Enforced.SenderId = requestorId;
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataEnforce(0, Enforced));
                MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_ID, bytes, true);
            }
            catch (Exception ex) { Log.Line($"Exception in PacketizeEnforcementToServer: {ex}"); }
        }

        internal void PacketizeToClientsInRange(IMyFunctionalBlock block, PacketBase packet)
        {
            try
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
                var localSteamId = MyAPIGateway.Multiplayer.MyId;
                foreach (var p in Players.Values)
                {
                    var id = p.SteamUserId;
                    if (id != localSteamId && id != packet.SenderId && Vector3D.DistanceSquared(p.GetPosition(), block.PositionComp.WorldAABB.Center) <= SyncBufferedDistSqr)
                    {
                        MyAPIGateway.Multiplayer.SendMessageTo(PACKET_ID, bytes, p.SteamUserId);
                        //Log.Line($"packet for: {packet.Entity.DebugName} - CSet:{packet is DataControllerSettings} = CState:{packet is DataControllerState} - MSet:{packet is DataModulatorSettings} - MState:{packet is DataModulatorState} - EmState:{packet is DataEmitterState} - EhSet:{packet is DataEnhancerSettings} - EhState:{packet is DataEnhancerState}");
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in PacketizeToClientsInRange: {ex}"); }
        }

        private void ReceivedPacket(byte[] rawData)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(rawData);
                if (packet.Received(IsServer) && packet.Entity != null)
                {
                    var localSteamId = MyAPIGateway.Multiplayer.MyId;
                    foreach (var p in Players.Values)
                    {
                        var id = p.SteamUserId;
                        if (id != localSteamId && id != packet.SenderId && Vector3D.DistanceSquared(p.GetPosition(), packet.Entity.PositionComp.WorldAABB.Center) <= SyncBufferedDistSqr)
                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_ID, rawData, p.SteamUserId);
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in ReceivedPacket: {ex}"); }
        }
        #endregion
    }
}
