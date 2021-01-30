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
    public partial class Modulators
    {
        internal ModulatorGridComponent ModulatorComp;
        internal ShieldGridComponent ShieldComp;
        internal MyResourceSinkInfo ResourceInfo;
        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal bool InThisTerminal => Session.Instance.LastTerminalId == Modulator.EntityId;
        private readonly MyDefinitionId _gId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        private const int SyncCount = 60;

        private MyEntitySubpart _subpartRotor;

        private bool _powered;

        private uint _tick;
        private uint _subTick = 1;

        private float _power = 0.01f;

        private bool _isServer;
        private bool _isDedicated;
        private bool _modulatorFailed;
        private bool _wasLink;
        private bool _wasBackup;
        private bool _firstRun = true;
        private bool _firstLoop = true;
        private bool _readyToSync;
        private bool _firstSync;
        private bool _tock33;
        private bool _tock34;
        private bool _tock60;
        private bool _subDelayed;
        private bool _bInit;

        private int _wasModulateDamage;
        private int _count = -1;
        private int _bCount;
        private int _bTime;

        private float _wasModulateEnergy;
        private float _wasModulateKinetic;

        internal int RotationTime { get; set; }
        internal bool MainInit { get; set; }
        internal bool SettingsUpdated { get; set; }
        internal bool ClientUiUpdate { get; set; }
        internal bool ContainerInited { get; set; }
        internal bool EnhancerLink { get; set; }

        internal ModulatorSettings ModSet { get; set; }
        internal ModulatorState ModState { get; set; }
        internal MyResourceSinkComponent Sink { get; set; }
        internal MyCubeGrid MyGrid { get; set; }
        internal MyCubeBlock MyCube { get; set; }
        internal IMyUpgradeModule Modulator { get; set; }

    }
}
