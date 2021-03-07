using DefenseShields.Support;
using VRageMath;

namespace DefenseShields
{
    public partial class Emitters
    {
        #region Block Animation
        private void BlockReset(bool clearAnimation)
        {
            if (!MyCube.IsWorking) return;

            if (!_compact && SubpartRotor == null)
            {
                Entity.TryGetSubpart("Rotor", out SubpartRotor);
                if (SubpartRotor == null) return;
            }
            else if (!_compact)
            {
                if (SubpartRotor.Closed) SubpartRotor.Subparts.Clear();
                Entity.TryGetSubpart("Rotor", out SubpartRotor);
            }

            if (clearAnimation)
            {
                _blockReset = true;
                RotationTime = 0;
                TranslationTime = 0;
                AnimationLoop = 0;
                EmissiveIntensity = 0;

                if (!_compact)
                {
                    var rotationMatrix = Matrix.CreateRotationY(0);
                    var matrix = rotationMatrix * Matrix.CreateTranslation(0, 0, 0);
                    SubpartRotor.PositionComp.SetLocalMatrix(ref matrix, null, true);
                    SubpartRotor.SetEmissiveParts(PlasmaEmissive, Color.Transparent, 0);
                }
                else MyCube.SetEmissiveParts(PlasmaEmissive, Color.Transparent, 0);
            }

            if (Session.Enforced.Debug == 3) Log.Line($"EmitterAnimationReset: [EmitterType: {Definition.Name} - Compact({_compact})] - Tick:{_tick.ToString()} - EmitterId [{Emitter.EntityId}]");
        }

        private void BlockMoveAnimation()
        {
            _blockReset = false;
            var percent = ShieldComp.DefenseShields.DsState.State.ShieldPercent;
            if (_compact)
            {
                if (_count == 0) EmissiveIntensity = 2;
                if (_count < 30) EmissiveIntensity += 1;
                else EmissiveIntensity -= 1;
                MyCube.SetEmissiveParts(PlasmaEmissive, UtilsStatic.GetShieldColorFromFloat(percent), 0.1f * EmissiveIntensity);
                return;
            }

            if (!MyCube.NeedsWorldMatrix)
                MyCube.NeedsWorldMatrix = true;

            if (SubpartRotor.Closed) BlockReset(false);
            RotationTime -= 1;
            if (AnimationLoop == 0) TranslationTime = 0;
            if (AnimationLoop < 299) TranslationTime += 1;
            else TranslationTime -= 1;
            if (_count == 0) EmissiveIntensity = 2;
            if (_count < 30) EmissiveIntensity += 1;
            else EmissiveIntensity -= 1;

            var rotationMatrix = Matrix.CreateRotationY(0.025f * RotationTime);
            var matrix = rotationMatrix * Matrix.CreateTranslation(0, Definition.BlockMoveTranslation * TranslationTime, 0);

            SubpartRotor.PositionComp.SetLocalMatrix(ref matrix, null, true);
            SubpartRotor.SetEmissiveParts(PlasmaEmissive, UtilsStatic.GetShieldColorFromFloat(percent), 0.1f * EmissiveIntensity);

            if (AnimationLoop++ == 599) AnimationLoop = 0;
        }
        #endregion

    }
}
