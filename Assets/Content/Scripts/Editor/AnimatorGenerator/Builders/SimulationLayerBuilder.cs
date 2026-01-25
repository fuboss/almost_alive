using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class SimulationLayerBuilder : ILayerBuilder {
    public string LayerName => "Simulation";
    public int LayerIndex => 4;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      Debug.Log($"[{LayerName}] Building simulation layer...");
      controller.AddLayer(LayerName);

      var layers = controller.layers;
      
      if (LayerIndex >= layers.Length) {
        Debug.LogError($"[{LayerName}] Layer index {LayerIndex} is out of bounds (layers count: {layers.Length})");
        return;
      }
      
      if (LayerIndex >= ctx.Config.layers.Length) {
        Debug.LogError($"[{LayerName}] Config layer index {LayerIndex} is out of bounds (config layers count: {ctx.Config.layers.Length})");
        return;
      }
      
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;
      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;
      var clips = ctx.ClipProvider;

      var emptyState = CreateEmptyState(stateMachine, "Empty", new Vector3(200, 0, 0), config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      // Sitting Chair states
      var sitChairDown = CreateStateIfClipExists(stateMachine, "SitChair_Down", new Vector3(400, 0, 0),
        clips, "Simulation", "Sit_Chair_Down", config.useWriteDefaults);
      var sitChairIdle = CreateStateIfClipExists(stateMachine, "SitChair_Idle", new Vector3(550, 0, 0),
        clips, "Simulation", "Sit_Chair_Idle", config.useWriteDefaults);
      var sitChairStandUp = CreateStateIfClipExists(stateMachine, "SitChair_StandUp", new Vector3(700, 0, 0),
        clips, "Simulation", "Sit_Chair_StandUp", config.useWriteDefaults);

      // Sitting Floor states
      var sitFloorDown = CreateStateIfClipExists(stateMachine, "SitFloor_Down", new Vector3(400, 100, 0),
        clips, "Simulation", "Sit_Floor_Down", config.useWriteDefaults);
      var sitFloorIdle = CreateStateIfClipExists(stateMachine, "SitFloor_Idle", new Vector3(550, 100, 0),
        clips, "Simulation", "Sit_Floor_Idle", config.useWriteDefaults);
      var sitFloorStandUp = CreateStateIfClipExists(stateMachine, "SitFloor_StandUp", new Vector3(700, 100, 0),
        clips, "Simulation", "Sit_Floor_StandUp", config.useWriteDefaults);

      // Lying states
      var lieDown = CreateStateIfClipExists(stateMachine, "Lie_Down", new Vector3(400, 200, 0),
        clips, "Simulation", "Lie_Down", config.useWriteDefaults);
      var lieIdle = CreateStateIfClipExists(stateMachine, "Lie_Idle", new Vector3(550, 200, 0),
        clips, "Simulation", "Lie_Idle", config.useWriteDefaults);
      var lieStandUp = CreateStateIfClipExists(stateMachine, "Lie_StandUp", new Vector3(700, 200, 0),
        clips, "Simulation", "Lie_StandUp", config.useWriteDefaults);

      // Exercise states
      var pushUps = CreateStateIfClipExists(stateMachine, "PushUps", new Vector3(400, 300, 0),
        clips, "Simulation", "Push_Ups", config.useWriteDefaults);
      var sitUps = CreateStateIfClipExists(stateMachine, "SitUps", new Vector3(550, 300, 0),
        clips, "Simulation", "Sit_Ups", config.useWriteDefaults);

      // Sitting Chair transitions
      if (sitChairDown != null) {
        AddSitTransition(emptyState, sitChairDown, 0, config);
        if (sitChairIdle != null) {
          tf.AddExitTimeTransition(sitChairDown, sitChairIdle, 0.9f, LayerIndex);
          if (sitChairStandUp != null) {
            tf.AddBoolTransition(sitChairIdle, sitChairStandUp, "IsSitting", false, LayerIndex);
            tf.AddExitTimeTransition(sitChairStandUp, emptyState, 0.9f, LayerIndex);
          }
        }
      }

      // Sitting Floor transitions
      if (sitFloorDown != null) {
        AddSitTransition(emptyState, sitFloorDown, 1, config);
        if (sitFloorIdle != null) {
          tf.AddExitTimeTransition(sitFloorDown, sitFloorIdle, 0.9f, LayerIndex);
          if (sitFloorStandUp != null) {
            tf.AddBoolTransition(sitFloorIdle, sitFloorStandUp, "IsSitting", false, LayerIndex);
            tf.AddExitTimeTransition(sitFloorStandUp, emptyState, 0.9f, LayerIndex);
          }
        }
      }

      // Lying transitions
      if (lieDown != null) {
        tf.AddBoolTransition(emptyState, lieDown, "IsLying", true, LayerIndex);
        if (lieIdle != null) {
          tf.AddExitTimeTransition(lieDown, lieIdle, 0.9f, LayerIndex);
          if (lieStandUp != null) {
            tf.AddBoolTransition(lieIdle, lieStandUp, "IsLying", false, LayerIndex);
            tf.AddExitTimeTransition(lieStandUp, emptyState, 0.9f, LayerIndex);
          }
        }
      }

      Debug.Log($"[{LayerName}] Simulation layer built successfully");
    }

    private void AddSitTransition(AnimatorState from, AnimatorState to, int sitType, AnimatorGeneratorConfig config) {
      if (from == null || to == null) {
        Debug.LogWarning($"[{LayerName}] Skipping sit transition - from or to state is null");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "IsSitting");
      transition.AddCondition(AnimatorConditionMode.Equals, sitType, "SitType");
      transition.duration = config.GetTransitionDuration(LayerIndex);
      transition.hasExitTime = false;
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

    private AnimatorState CreateStateIfClipExists(AnimatorStateMachine sm, string name, Vector3 pos, 
      AnimationClipProvider clips, string folder, string clipName, bool writeDefaults) {
      var clip = clips.Get(folder, clipName);
      if (clip == null) {
        Debug.LogWarning($"[{LayerName}] Clip not found: {folder}/{clipName}, skipping state {name}");
        return null;
      }
      return CreateState(sm, name, pos, clip, writeDefaults);
    }
  }
}

