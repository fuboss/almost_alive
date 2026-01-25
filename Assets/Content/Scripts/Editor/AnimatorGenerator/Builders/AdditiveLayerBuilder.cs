using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class AdditiveLayerBuilder : ILayerBuilder {
    public string LayerName => "Additive";
    public int LayerIndex => 3;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      Debug.Log($"[{LayerName}] Building additive layer...");
      controller.AddLayer(LayerName);

      var layers = controller.layers;
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;
      layers[LayerIndex].blendingMode = AnimatorLayerBlendingMode.Additive;
      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;

      var emptyState = CreateEmptyState(stateMachine, "Empty", new Vector3(200, 0, 0), config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      var hitA = ctx.ClipProvider.Get("General", "Hit_A");
      var hitB = ctx.ClipProvider.Get("General", "Hit_B");

      if (hitA == null && hitB == null) {
        Debug.LogWarning($"[{LayerName}] No hit clips found, additive layer will be minimal");
        return;
      }

      AnimatorState hitState = null;
      
      if (hitA != null && hitB != null) {
        // Create BlendTree for hit variations
        var hitBlendTree = new BlendTree {
          name = "HitVariants",
          blendType = BlendTreeType.Simple1D,
          blendParameter = "Direction"
        };
        hitBlendTree.AddChild(hitA, 0f);
        hitBlendTree.AddChild(hitB, 1f);
        UnityEditor.AssetDatabase.AddObjectToAsset(hitBlendTree, controller);
        
        hitState = CreateState(stateMachine, "Hit", new Vector3(400, 0, 0), hitBlendTree, config.useWriteDefaults);
      } else {
        hitState = CreateState(stateMachine, "Hit", new Vector3(400, 0, 0), 
          hitA ?? hitB, config.useWriteDefaults);
      }

      tf.AddTriggerTransition(emptyState, hitState, "Hit", LayerIndex);
      tf.AddExitTimeTransition(hitState, emptyState, 0.9f, LayerIndex);
      
      Debug.Log($"[{LayerName}] Additive layer built successfully");
    }

    private AnimatorState CreateEmptyState(AnimatorStateMachine sm, string name, Vector3 pos, bool writeDefaults) {
      var state = sm.AddState(name, pos);
      state.motion = null;
      state.writeDefaultValues = writeDefaults;
      return state;
    }

    private AnimatorState CreateState(AnimatorStateMachine sm, string name, Vector3 pos, Motion motion, bool writeDefaults) {
      if (motion == null) {
        Debug.LogWarning($"[{LayerName}] Cannot create state {name} - motion is null");
        return null;
      }
      var state = sm.AddState(name, pos);
      state.motion = motion;
      state.writeDefaultValues = writeDefaults;
      return state;
    }
  }
}

