using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace DefenseShields
{
    public class ModulatorGridComponent : MyEntityComponentBase
    {
        public Modulators Modulator;
        public string Password;

        public ModulatorGridComponent(Modulators modulator)
        {
            Modulator = modulator;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            if (Container.Entity.InScene)
            {
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {

            if (Container.Entity.InScene)
            {
            }

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

        public HashSet<IMyCubeGrid> SubGrids = new HashSet<IMyCubeGrid>();

        public string ModulationPassword
        {
            get { return Password; }
            set { Password = value; }
        }

        public override string ComponentTypeDebugString
        {
            get { return "Shield"; }
        }
    }
}
