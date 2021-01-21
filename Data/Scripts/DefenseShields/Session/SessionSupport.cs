
using ParallelTasks;

namespace DefenseShields
{
    using Support;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using System;
    using VRage.Game.Entity;
    using VRage.Game;
    using Sandbox.Game.Entities;

    public partial class Session
    {
        public string ModPath()
        {
            var modPath = ModContext.ModPath;
            return modPath;
        }

        public bool TaskHasErrors(ref Task task, string taskName)
        {
            if (task.Exceptions != null && task.Exceptions.Length > 0)
            {
                foreach (var e in task.Exceptions)
                {
                    Log.Line($"{taskName} thread!\n{e}");
                }

                return true;
            }

            return false;
        }

        private void PlayerConnected(long id)
        {
            try
            {
                if (Players.ContainsKey(id))
                {
                    if (Enforced.Debug >= 3) Log.Line($"Player id({id}) already exists");
                    return;
                }
                MyAPIGateway.Multiplayer.Players.GetPlayers(null, myPlayer => FindPlayer(myPlayer, id));
            }
            catch (Exception ex) { Log.Line($"Exception in PlayerConnected: {ex}"); }
        }

        private void PlayerDisconnected(long l)
        {
            try
            {
                IMyPlayer removedPlayer;
                Players.TryRemove(l, out removedPlayer);
                PlayerEventId++;
                if (Enforced.Debug >= 3) Log.Line($"Removed player, new playerCount:{Players.Count}");
            }
            catch (Exception ex) { Log.Line($"Exception in PlayerDisconnected: {ex}"); }
        }

        private bool FindPlayer(IMyPlayer player, long id)
        {
            if (player.IdentityId == id)
            {
                Players[id] = player;
                PlayerEventId++;
                if (Enforced.Debug >= 3) Log.Line($"Added player: {player.DisplayName}, new playerCount:{Players.Count}");
            }
            return false;
        }

        private void SplitMonitor()
        {
            foreach (var pair in CheckForSplits)
            {
                if (WatchForSplits.Add(pair.Key))
                    pair.Key.OnGridSplit += GridSplitWatch;
                else if (Tick - pair.Value > 120)
                    _tmpWatchGridsToRemove.Add(pair.Key);
            }

            for (int i = 0; i < _tmpWatchGridsToRemove.Count; i++)
            {
                var grid = _tmpWatchGridsToRemove[i];
                grid.OnGridSplit -= GridSplitWatch;
                WatchForSplits.Remove(grid);
                CheckForSplits.Remove(grid);
            }
            _tmpWatchGridsToRemove.Clear();

            foreach (var parent in GetParentGrid)
            {
                if (Tick - parent.Value.Age > 120)
                    GetParentGrid.Remove(parent.Key);
            }
            GetParentGrid.ApplyRemovals();
        }

        #region Events

        internal struct ParentGrid
        {
            internal MyCubeGrid Parent;
            internal uint Age;
        }

        private void GridSplitWatch(MyCubeGrid parent, MyCubeGrid child)
        {
            GetParentGrid[child] = new ParentGrid {Parent = parent, Age = Tick};
            GetParentGrid.ApplyAdditionsAndModifications();
        }

        private void OnEntityRemove(MyEntity myEntity)
        {
            var warhead = myEntity as IMyWarhead;
            if (warhead != null)
            {
                var grid = (MyCubeGrid)warhead.CubeGrid;
                if (!warhead.IsFunctional && (warhead.IsArmed || (warhead.DetonationTime <= 0 && warhead.IsCountingDown)) && warhead.CustomData.Length != 0 && !GlobalProtect.ContainsKey(grid))
                {
                    var blastRatio = warhead.CubeGrid.GridSizeEnum == MyCubeSize.Small ? 1 : 5;
                    var epicCenter = warhead.PositionComp.WorldAABB.Center;
                    if (Enforced.Debug >= 2 && EmpStore.Count == 0) Log.Line($"====================================================================== [WarHead EventStart]");
                    EmpStore.Enqueue(new WarHeadBlast(blastRatio, epicCenter, warhead.CustomData));
                }
            }
        }

        private void OnSessionReady()
        {
            SessionReady = true;
        }
        #endregion
    }
}
