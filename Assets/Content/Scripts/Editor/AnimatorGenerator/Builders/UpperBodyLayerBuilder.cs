using Content.Scripts.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class UpperBodyLayerBuilder : ILayerBuilder {
    private readonly string _maskPath;

    public string LayerName => "UpperBody";
    public int LayerIndex => 2;

    public UpperBodyLayerBuilder(string maskPath) {
      _maskPath = maskPath;
    }

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      Debug.Log($"[{LayerName}] Building upper body layer...");
      controller.AddLayer(LayerName);

      var layers = controller.layers;
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;

      var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(_maskPath);
      if (mask != null) {
        layers[LayerIndex].avatarMask = mask;
        Debug.Log($"[{LayerName}] Avatar mask applied");
      } else {
        Debug.LogWarning($"[{LayerName}] Avatar mask not found at {_maskPath}");
      }

      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;
      var clips = ctx.ClipProvider;

      var emptyState = CreateEmptyState(stateMachine, "Empty", new Vector3(200, 0, 0), config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      // Work states - one per tool type
      var workAxe = CreateStateIfClipExists(stateMachine, "Work_Axe", new Vector3(0, 150, 0),
        clips, "Tools", "Chopping", config.useWriteDefaults);
      var workPickaxe = CreateStateIfClipExists(stateMachine, "Work_Pickaxe", new Vector3(100, 150, 0),
        clips, "Tools", "Pickaxing", config.useWriteDefaults);
      var workShovel = CreateStateIfClipExists(stateMachine, "Work_Shovel", new Vector3(200, 150, 0),
        clips, "Tools", "Digging", config.useWriteDefaults);
      var workHammer = CreateStateIfClipExists(stateMachine, "Work_Hammer", new Vector3(300, 150, 0),
        clips, "Tools", "Hammering", config.useWriteDefaults);
      var workSaw = CreateStateIfClipExists(stateMachine, "Work_Saw", new Vector3(400, 150, 0),
        clips, "Tools", "Sawing", config.useWriteDefaults);

      // Fishing states
      var fishingIdle = CreateStateIfClipExists(stateMachine, "Fishing_Idle", new Vector3(500, 150, 0),
        clips, "Tools", "Fishing_Idle", config.useWriteDefaults);
      var fishingCast = CreateStateIfClipExists(stateMachine, "Fishing_Cast", new Vector3(600, 150, 0),
        clips, "Tools", "Fishing_Cast", config.useWriteDefaults);
      var fishingBite = CreateStateIfClipExists(stateMachine, "Fishing_Bite", new Vector3(700, 150, 0),
        clips, "Tools", "Fishing_Bite", config.useWriteDefaults);
      var fishingReeling = CreateStateIfClipExists(stateMachine, "Fishing_Reeling", new Vector3(800, 150, 0),
        clips, "Tools", "Fishing_Reeling", config.useWriteDefaults);
      var fishingStruggling = CreateStateIfClipExists(stateMachine, "Fishing_Struggling", new Vector3(900, 150, 0),
        clips, "Tools", "Fishing_Struggling", config.useWriteDefaults);
      var fishingCatch = CreateStateIfClipExists(stateMachine, "Fishing_Catch", new Vector3(1000, 150, 0),
        clips, "Tools", "Fishing_Catch", config.useWriteDefaults);
      var fishingTug = CreateStateIfClipExists(stateMachine, "Fishing_Tug", new Vector3(1100, 150, 0),
        clips, "Tools", "Fishing_Tug", config.useWriteDefaults);

      // Additional work states
      var workA = CreateStateIfClipExists(stateMachine, "Work_A", new Vector3(0, 250, 0),
        clips, "Tools", "Working_A", config.useWriteDefaults);
      var workB = CreateStateIfClipExists(stateMachine, "Work_B", new Vector3(100, 250, 0),
        clips, "Tools", "Working_B", config.useWriteDefaults);
      var workC = CreateStateIfClipExists(stateMachine, "Work_C", new Vector3(200, 250, 0),
        clips, "Tools", "Working_C", config.useWriteDefaults);

      // Holding states
      var holdingA = CreateStateIfClipExists(stateMachine, "Holding_A", new Vector3(300, 250, 0),
        clips, "Tools", "Holding_A", config.useWriteDefaults);
      var holdingB = CreateStateIfClipExists(stateMachine, "Holding_B", new Vector3(400, 250, 0),
        clips, "Tools", "Holding_B", config.useWriteDefaults);
      var holdingC = CreateStateIfClipExists(stateMachine, "Holding_C", new Vector3(500, 250, 0),
        clips, "Tools", "Holding_C", config.useWriteDefaults);

      // Lockpicking states
      var lockpicking = CreateStateIfClipExists(stateMachine, "Lockpicking", new Vector3(600, 250, 0),
        clips, "Tools", "Lockpicking", config.useWriteDefaults);

      // Transitions from Empty to Work states by ToolType
      if (workAxe != null) {
        AddWorkTransition(emptyState, workAxe, (int)ToolAnimationType.Axe, config);
        tf.AddBoolTransition(workAxe, emptyState, "IsWorking", false, LayerIndex);
      }
      if (workPickaxe != null) {
        AddWorkTransition(emptyState, workPickaxe, (int)ToolAnimationType.Pickaxe, config);
        tf.AddBoolTransition(workPickaxe, emptyState, "IsWorking", false, LayerIndex);
      }
      if (workShovel != null) {
        AddWorkTransition(emptyState, workShovel, (int)ToolAnimationType.Shovel, config);
        tf.AddBoolTransition(workShovel, emptyState, "IsWorking", false, LayerIndex);
      }
      if (workHammer != null) {
        AddWorkTransition(emptyState, workHammer, (int)ToolAnimationType.Hammer, config);
        tf.AddBoolTransition(workHammer, emptyState, "IsWorking", false, LayerIndex);
      }
      if (workSaw != null) {
        AddWorkTransition(emptyState, workSaw, (int)ToolAnimationType.Saw, config);
        tf.AddBoolTransition(workSaw, emptyState, "IsWorking", false, LayerIndex);
      }
      if (fishingIdle != null) {
        AddWorkTransition(emptyState, fishingIdle, (int)ToolAnimationType.FishingRod, config);
        tf.AddBoolTransition(fishingIdle, emptyState, "IsWorking", false, LayerIndex);
        
        // Fishing sub-transitions
        if (fishingCast != null) {
          tf.AddTriggerTransition(fishingIdle, fishingCast, "StartWork", LayerIndex);
          if (fishingBite != null) {
            tf.AddExitTimeTransition(fishingCast, fishingBite, 0.9f, LayerIndex);
            if (fishingReeling != null) {
              tf.AddExitTimeTransition(fishingBite, fishingReeling, 0.9f, LayerIndex);
              if (fishingCatch != null) {
                tf.AddExitTimeTransition(fishingReeling, fishingCatch, 0.9f, LayerIndex);
                tf.AddExitTimeTransition(fishingCatch, fishingIdle, 0.9f, LayerIndex);
              }
            }
          }
        }
      }

      var interactState = CreateStateIfClipExists(stateMachine, "Interact", new Vector3(400, 0, 0),
        clips, "General", "Interact", config.useWriteDefaults);
      var useItemState = CreateStateIfClipExists(stateMachine, "UseItem", new Vector3(400, -50, 0),
        clips, "General", "Use_Item", config.useWriteDefaults);
      var throwState = CreateStateIfClipExists(stateMachine, "Throw", new Vector3(400, -100, 0),
        clips, "General", "Throw", config.useWriteDefaults);
      var waveState = CreateStateIfClipExists(stateMachine, "Wave", new Vector3(400, -150, 0),
        clips, "Simulation", "Waving", config.useWriteDefaults);
      var cheerState = CreateStateIfClipExists(stateMachine, "Cheer", new Vector3(400, -200, 0),
        clips, "Simulation", "Cheering", config.useWriteDefaults);

      if (interactState != null) {
        tf.AddTriggerTransition(emptyState, interactState, "Interact", LayerIndex);
        tf.AddExitTimeTransition(interactState, emptyState, 0.9f, LayerIndex);
      }
      if (useItemState != null) {
        tf.AddTriggerTransition(emptyState, useItemState, "UseItem", LayerIndex);
        tf.AddExitTimeTransition(useItemState, emptyState, 0.9f, LayerIndex);
      }
      if (throwState != null) {
        tf.AddTriggerTransition(emptyState, throwState, "Throw", LayerIndex);
        tf.AddExitTimeTransition(throwState, emptyState, 0.9f, LayerIndex);
      }
      if (waveState != null) {
        tf.AddTriggerTransition(emptyState, waveState, "Wave", LayerIndex);
        tf.AddExitTimeTransition(waveState, emptyState, 0.9f, LayerIndex);
      }
      if (cheerState != null) {
        tf.AddTriggerTransition(emptyState, cheerState, "Cheer", LayerIndex);
        tf.AddExitTimeTransition(cheerState, emptyState, 0.9f, LayerIndex);
      }

      Debug.Log($"[{LayerName}] Upper body layer built successfully");
    }

    private void AddWorkTransition(AnimatorState from, AnimatorState to, int toolType, AnimatorGeneratorConfig config) {
      if (from == null || to == null) {
        Debug.LogWarning($"[{LayerName}] Skipping work transition - from or to state is null");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "IsWorking");
      transition.AddCondition(AnimatorConditionMode.Equals, toolType, "ToolType");
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

