using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class AdditiveLayerBuilder : ILayerBuilder {
    public string LayerName => "Additive";
    public int LayerIndex => 3;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      controller.AddLayer(LayerName);

      var layers = controller.layers;
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;
      layers[LayerIndex].blendingMode = AnimatorLayerBlendingMode.Additive;
      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;

      var emptyState = CreateState(stateMachine, "Empty", new Vector3(200, 0, 0), null, config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      var hitState = CreateState(stateMachine, "Hit", new Vector3(400, 0, 0),
        ctx.ClipProvider.Get("General", "Hit_A"), config.useWriteDefaults);

      tf.AddTriggerTransition(emptyState, hitState, "Hit", LayerIndex);
      tf.AddExitTimeTransition(hitState, emptyState, 0.9f, LayerIndex);
    }

    private AnimatorState CreateState(AnimatorStateMachine sm, string name, Vector3 pos, Motion motion, bool writeDefaults) {
      var state = sm.AddState(name, pos);
      state.motion = motion;
      state.writeDefaultValues = writeDefaults;
      return state;
    }
  }
}

