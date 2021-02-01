using System.Collections.Concurrent;
using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace DefenseShields
{
    public class ShieldGridComponent : MyEntityComponentBase
    {
        public DefenseShields DefenseShields;

        public ShieldGridComponent(DefenseShields defenseShields)
        {
            DefenseShields = defenseShields;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public readonly ConcurrentDictionary<MyCubeGrid, byte> SubGrids = new ConcurrentDictionary<MyCubeGrid, byte>();
        public readonly ConcurrentDictionary<MyCubeGrid, byte> LinkedGrids = new ConcurrentDictionary<MyCubeGrid, byte>();

        public Vector3D[] PhysicsOutside { get; set; } = new Vector3D[642];

        public Vector3D[] PhysicsOutsideLow { get; set; } = new Vector3D[162];
        
        public Enhancers Enhancer { get; set; }

        public Modulators Modulator { get; set; }
        public int EmitterMode { get; set; } = -1;
        public long ActiveEmitterId { get; set; }

        public Emitters StationEmitter { get; set; }
        public Emitters ShipEmitter { get; set; }

        public O2Generators ActiveO2Generator { get; set; }

        public string ModulationPassword { get; set; }

        public bool EmitterLos { get; set; }
        public bool SkipLos { get; set; }
        public bool EmittersSuspended { get; set; }

        public bool O2Updated { get; set; }

        public float DefaultO2 { get; set; }

        public bool CheckEmitters { get; set; }

        public bool GridIsMoving { get; set; }

        public bool EmitterEvent { get; set; }

        public double ShieldVolume { get; set; }

        public override string ComponentTypeDebugString
        {
            get { return "Shield"; }
        }
    }
}
