using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;

namespace DefenseShields
{
    public partial class Enhancers
    {
        internal ShieldGridComponent ShieldComp;
        internal MyResourceSinkInfo ResourceInfo;

        private const float Power = 0.01f;
        private const int SyncCount = 60;

        private readonly MyDefinitionId _gId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        private uint _tick;
        private int _count = -1;
        private int _bCount;
        private int _bTime;
        private bool _firstLoop = true;
        private bool _readyToSync;
        private bool _firstSync;
        private bool _tick60;
        private bool _isServer;
        private bool _isDedicated;
        private bool _bInit;

        private MyEntitySubpart _subpartRotor;

        internal EnhancerState EnhState { get; set; }
        internal MyResourceSinkComponent Sink { get; set; }

        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal bool InThisTerminal => Session.Instance.LastTerminalId == Enhancer.EntityId;

        internal int RotationTime { get; set; }
        internal bool ContainerInited { get; set; }
        internal IMyUpgradeModule Enhancer { get; set; }
        internal MyCubeGrid MyGrid { get; set; }
        internal MyCubeBlock MyCube { get; set; }

    }
}
