using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class BaseLayerBuilder : ILayerBuilder {
    public string LayerName => "Base Layer";
    public int LayerIndex => 0;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      var stateMachine = controller.layers[0].stateMachine;
      var config = ctx.Config;

      var idleState = CreateState(stateMachine, "Idle", new Vector3(300, 0, 0), 
        ctx.ClipProvider.Get("General", "Idle_A"), config.useWriteDefaults);
      stateMachine.defaultState = idleState;

      var locomotionState = CreateState(stateMachine, "Locomotion", new Vector3(300, 100, 0),
        ctx.BlendTreeFactory.CreateLocomotion(controller), config.useWriteDefaults);

      var crouchState = CreateState(stateMachine, "Crouch", new Vector3(500, 100, 0),
        ctx.BlendTreeFactory.CreateCrouch(controller), config.useWriteDefaults);

      var jumpStart = CreateState(stateMachine, "JumpStart", new Vector3(300, 200, 0),
        ctx.ClipProvider.Get("MovementBasic", "Jump_Start"), config.useWriteDefaults);

      var jumpIdle = CreateState(stateMachine, "JumpIdle", new Vector3(450, 200, 0),
        ctx.ClipProvider.Get("MovementBasic", "Jump_Idle"), config.useWriteDefaults);

      var jumpLand = CreateState(stateMachine, "JumpLand", new Vector3(600, 200, 0),
        ctx.ClipProvider.Get("MovementBasic", "Jump_Land"), config.useWriteDefaults);

      var dodgeState = CreateState(stateMachine, "Dodge", new Vector3(500, 0, 0),
        ctx.BlendTreeFactory.CreateDodge(controller), config.useWriteDefaults);

      var deathState = CreateState(stateMachine, "Death", new Vector3(300, 300, 0),
        ctx.ClipProvider.Get("General", "Death_A"), config.useWriteDefaults);

      var deathPose = CreateState(stateMachine, "DeathPose", new Vector3(450, 300, 0),
        ctx.ClipProvider.Get("General", "Death_A_Pose"), config.useWriteDefaults);

      var tf = ctx.TransitionFactory;

      tf.AddFloatTransition(idleState, locomotionState, "Speed", config.idleToMoveThreshold, true, LayerIndex);
      tf.AddFloatTransition(locomotionState, idleState, "Speed", config.idleToMoveThreshold, false, LayerIndex);

      tf.AddBoolTransition(idleState, crouchState, "IsCrouching", true, LayerIndex);
      tf.AddBoolTransition(locomotionState, crouchState, "IsCrouching", true, LayerIndex);
      tf.AddBoolTransition(crouchState, idleState, "IsCrouching", false, LayerIndex);

      tf.AddTriggerTransition(idleState, jumpStart, "Jump", LayerIndex);
      tf.AddTriggerTransition(locomotionState, jumpStart, "Jump", LayerIndex);
      tf.AddExitTimeTransition(jumpStart, jumpIdle, 0.9f, LayerIndex);
      tf.AddBoolTransition(jumpIdle, jumpLand, "IsGrounded", true, LayerIndex);
      tf.AddExitTimeTransition(jumpLand, idleState, 0.9f, LayerIndex);

      tf.AddTriggerTransition(idleState, dodgeState, "Dodge", LayerIndex);
      tf.AddTriggerTransition(locomotionState, dodgeState, "Dodge", LayerIndex);
      tf.AddExitTimeTransition(dodgeState, idleState, 0.9f, LayerIndex);

      tf.AddTriggerTransition(idleState, deathState, "Die", LayerIndex);
      tf.AddTriggerTransition(locomotionState, deathState, "Die", LayerIndex);
      tf.AddExitTimeTransition(deathState, deathPose, 0.95f, LayerIndex);
    }

    private AnimatorState CreateState(AnimatorStateMachine sm, string name, Vector3 pos, Motion motion, bool writeDefaults) {
      var state = sm.AddState(name, pos);
      state.motion = motion;
      state.writeDefaultValues = writeDefaults;
      return state;
    }
  }
}

