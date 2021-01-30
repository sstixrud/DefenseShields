using System.Collections.Concurrent;
using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace DefenseShields
{
    public partial class Emitters
    {
        internal ShieldGridComponent ShieldComp;
        internal MyResourceSinkInfo ResourceInfo;
        internal List<Vector3D> LosScaledCloud = new List<Vector3D>(2000);
        internal MyEntitySubpart SubpartRotor;
        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal bool InThisTerminal => Session.Instance.LastTerminalId == Emitter.EntityId;
        private const string PlasmaEmissive = "PlasmaEmissive";
        private const int SyncCount = 60;

        private readonly List<int> _vertsSighted = new List<int>();
        private readonly ConcurrentDictionary<int, bool> _blocksLos = new ConcurrentDictionary<int, bool>();
        private readonly MyDefinitionId _gId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        private DSUtils _dsUtil = new DSUtils();

        private uint _tick;
        private int _age;
        private int _count = -1;
        private int _lCount;
        private int _bCount;
        private int _bTime;
        private int _unitSpherePoints = 2000;
        private float _power = 0.01f;

        private bool _readyToSync;
        private bool _firstSync;
        private bool _updateLosState = true;
        private bool _blockReset;
        private bool _tick60;
        private bool _isServer;
        private bool _isDedicated;
        private bool _compact;
        private bool _wasLosState;
        private bool _disableLos;
        private bool _bInit;

        public enum EmitterType
        {
            Station,
            Large,
            Small,
        }

        internal Definition Definition { get; set; }
        internal EmitterState EmiState { get; set; }

        internal IMyUpgradeModule Emitter { get; set; }
        internal EmitterType EmitterMode { get; set; }
        internal MyCubeGrid MyGrid { get; set; }
        internal MyCubeBlock MyCube { get; set; }

        internal MyResourceSinkComponent Sink { get; set; }

        internal int RotationTime { get; set; }
        internal int AnimationLoop { get; set; }
        internal int TranslationTime { get; set; }

        internal float EmissiveIntensity { get; set; }

        internal bool IsStatic { get; set; }
        internal bool TookControl { get; set; }
        internal bool ContainerInited { get; set; }

    }
}
