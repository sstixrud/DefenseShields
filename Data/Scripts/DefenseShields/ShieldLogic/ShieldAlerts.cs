using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields
    {

        private void PlayerMessages(PlayerNotice notice)
        {
            double radius;
            if (notice == PlayerNotice.EmpOverLoad || notice == PlayerNotice.OverLoad) radius = 500;
            else radius = ShieldSphere.Radius * 2;

            var sphere = new BoundingSphereD(DetectionCenter, radius);
            var sendMessage = false;
            IMyPlayer targetPlayer = null;

            foreach (var player in Session.Instance.Players.Values)
            {
                if (player.IdentityId != MyAPIGateway.Session.Player.IdentityId) continue;
                if (!sphere.Intersects(player.Character.WorldVolume)) continue;
                var relation = MyAPIGateway.Session.Player.GetRelationTo(MyCube.OwnerId);
                if (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.Enemies) continue;
                sendMessage = true;
                targetPlayer = player;
                break;
            }
            if (sendMessage && !DsSet.Settings.NoWarningSounds) BroadcastSound(targetPlayer.Character, notice);

            switch (notice)
            {
                case PlayerNotice.EmitterInit:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- shield is reinitializing and checking LOS, attempting startup in 30 seconds!", 4816);
                    break;
                case PlayerNotice.FieldBlocked:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + "-- the shield's field cannot form when in contact with a solid body", 6720, "Blue");
                    break;
                case PlayerNotice.OverLoad:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- shield has overloaded, restarting in 45 seconds!!", 8000, "Red");
                    break;
                case PlayerNotice.EmpOverLoad:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- shield was EMPed, restarting in 60 seconds!!", 8000, "Red");
                    break;
                case PlayerNotice.Remodulate:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- shield remodulating, restarting in 5 seconds.", 4800);
                    break;
                case PlayerNotice.NoLos:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- Emitter does not have line of sight, shield offline", 8000, "Red");
                    break;
                case PlayerNotice.NoPower:
                    if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- Insufficient Power, shield is failing!", 5000, "Red");
                    break;
            }
            if (Session.Enforced.Debug == 3) Log.Line($"[PlayerMessages] Sending:{sendMessage} - rangeToClinetPlayer:{Vector3D.Distance(sphere.Center, MyAPIGateway.Session.Player.Character.WorldVolume.Center)}");
        }

        private void BroadcastSound(IMyCharacter character, PlayerNotice notice)
        {
            if (character == null || _alertAudio == null || _alertAudio.IsPlaying) return;
            _alertAudio.CustomVolume = MyAPIGateway.Session.Config.GameVolume * 0.6f;
            _alertAudio.CustomMaxDistance = (float?) ShieldSphere.Radius * 2;

            _alertAudio.Entity = (MyEntity) character;
            MySoundPair pair = null;
            switch (notice)
            {
                case PlayerNotice.EmitterInit:
                    pair = _audioReInit;
                    break;
                case PlayerNotice.FieldBlocked:
                    pair = _audioSolidBody;
                    break;
                case PlayerNotice.OverLoad:
                    pair = _audioOverload;
                    break;
                case PlayerNotice.EmpOverLoad:
                    pair = _audioEmp;
                    break;
                case PlayerNotice.Remodulate:
                    pair = _audioRemod;
                    break;
                case PlayerNotice.NoLos:
                    pair = _audioLos;
                    break;
                case PlayerNotice.NoPower:
                    pair = _audioNoPower;
                    break;
            }
            if (_alertAudio.Entity != null && pair != null) _alertAudio.PlaySingleSound(pair, true);
        }

        private void BroadcastMessage(bool forceNoPower = false)
        {
            if (Session.Enforced.Debug >= 3) Log.Line($"Broadcasting message to local playerId{Session.Instance.Players.Count} - Server:{_isServer} - Dedicated:{_isDedicated} - Id:{MyAPIGateway.Multiplayer.MyId}");

            if (!DsState.State.EmitterLos && GridIsMobile && !DsState.State.Waking) PlayerMessages(PlayerNotice.NoLos);
            else if (DsState.State.NoPower || forceNoPower) PlayerMessages(PlayerNotice.NoPower);
            else if (DsState.State.Overload) PlayerMessages(PlayerNotice.OverLoad);
            else if (DsState.State.EmpOverLoad) PlayerMessages(PlayerNotice.EmpOverLoad);
            else if (DsState.State.FieldBlocked) PlayerMessages(PlayerNotice.FieldBlocked);
            else if (DsState.State.Waking) PlayerMessages(PlayerNotice.EmitterInit);
            else if (DsState.State.Remodulate) PlayerMessages(PlayerNotice.Remodulate);
            _clientMessageCount = DsState.State.MessageCount;
        }
    }
}
