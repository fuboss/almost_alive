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
      controller.AddLayer(LayerName);

      var layers = controller.layers;
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;

      var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(_maskPath);
      if (mask != null) {
        layers[LayerIndex].avatarMask = mask;
      }

      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;
      var clips = ctx.ClipProvider;

      var emptyState = CreateState(stateMachine, "Empty", new Vector3(200, 0, 0), null, config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      // Work states - one per tool type
      var workAxe = CreateState(stateMachine, "Work_Axe", new Vector3(0, 150, 0),
        clips.Get("Tools", "Chopping"), config.useWriteDefaults);
      var workPickaxe = CreateState(stateMachine, "Work_Pickaxe", new Vector3(100, 150, 0),
        clips.Get("Tools", "Pickaxing"), config.useWriteDefaults);
      var workShovel = CreateState(stateMachine, "Work_Shovel", new Vector3(200, 150, 0),
        clips.Get("Tools", "Digging"), config.useWriteDefaults);
      var workHammer = CreateState(stateMachine, "Work_Hammer", new Vector3(300, 150, 0),
        clips.Get("Tools", "Hammering"), config.useWriteDefaults);
      var workSaw = CreateState(stateMachine, "Work_Saw", new Vector3(400, 150, 0),
        clips.Get("Tools", "Sawing"), config.useWriteDefaults);
      var workFishing = CreateState(stateMachine, "Work_Fishing", new Vector3(500, 150, 0),
        clips.Get("Tools", "Fishing_Idle"), config.useWriteDefaults);

      // Transitions from Empty to Work states by ToolType
      AddWorkTransition(emptyState, workAxe, (int)ToolAnimationType.Axe, config);
      AddWorkTransition(emptyState, workPickaxe, (int)ToolAnimationType.Pickaxe, config);
      AddWorkTransition(emptyState, workShovel, (int)ToolAnimationType.Shovel, config);
      AddWorkTransition(emptyState, workHammer, (int)ToolAnimationType.Hammer, config);
      AddWorkTransition(emptyState, workSaw, (int)ToolAnimationType.Saw, config);
      AddWorkTransition(emptyState, workFishing, (int)ToolAnimationType.FishingRod, config);

      // Transitions back to Empty when IsWorking = false
      tf.AddBoolTransition(workAxe, emptyState, "IsWorking", false, LayerIndex);
      tf.AddBoolTransition(workPickaxe, emptyState, "IsWorking", false, LayerIndex);
      tf.AddBoolTransition(workShovel, emptyState, "IsWorking", false, LayerIndex);
      tf.AddBoolTransition(workHammer, emptyState, "IsWorking", false, LayerIndex);
      tf.AddBoolTransition(workSaw, emptyState, "IsWorking", false, LayerIndex);
      tf.AddBoolTransition(workFishing, emptyState, "IsWorking", false, LayerIndex);

      var interactState = CreateState(stateMachine, "Interact", new Vector3(400, 0, 0),
        clips.Get("General", "Interact"), config.useWriteDefaults);

      var useItemState = CreateState(stateMachine, "UseItem", new Vector3(400, -50, 0),
        clips.Get("General", "Use_Item"), config.useWriteDefaults);

      var throwState = CreateState(stateMachine, "Throw", new Vector3(400, -100, 0),
        clips.Get("General", "Throw"), config.useWriteDefaults);

      var waveState = CreateState(stateMachine, "Wave", new Vector3(400, -150, 0),
        clips.Get("Simulation", "Waving"), config.useWriteDefaults);

      var cheerState = CreateState(stateMachine, "Cheer", new Vector3(400, -200, 0),
        clips.Get("Simulation", "Cheering"), config.useWriteDefaults);

      tf.AddTriggerTransition(emptyState, interactState, "Interact", LayerIndex);
      tf.AddTriggerTransition(emptyState, useItemState, "UseItem", LayerIndex);
      tf.AddTriggerTransition(emptyState, throwState, "Throw", LayerIndex);
      tf.AddTriggerTransition(emptyState, waveState, "Wave", LayerIndex);
      tf.AddTriggerTransition(emptyState, cheerState, "Cheer", LayerIndex);

      tf.AddExitTimeTransition(interactState, emptyState, 0.9f, LayerIndex);
      tf.AddExitTimeTransition(useItemState, emptyState, 0.9f, LayerIndex);
      tf.AddExitTimeTransition(throwState, emptyState, 0.9f, LayerIndex);
      tf.AddExitTimeTransition(waveState, emptyState, 0.9f, LayerIndex);
      tf.AddExitTimeTransition(cheerState, emptyState, 0.9f, LayerIndex);
    }

    private void AddWorkTransition(AnimatorState from, AnimatorState to, int toolType, AnimatorGeneratorConfig config) {
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "IsWorking");
      transition.AddCondition(AnimatorConditionMode.Equals, toolType, "ToolType");
      transition.duration = config.GetTransitionDuration(LayerIndex);
      transition.hasExitTime = false;
    }

    private AnimatorState CreateState(AnimatorStateMachine sm, string name, Vector3 pos, Motion motion, bool writeDefaults) {
      var state = sm.AddState(name, pos);
      state.motion = motion;
      state.writeDefaultValues = writeDefaults;
      return state;
    }
  }
}

