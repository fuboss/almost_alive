using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class BaseLayerBuilder : ILayerBuilder {
    public string LayerName => "Base Layer";
    public int LayerIndex => 0;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      Debug.Log($"[{LayerName}] Building base layer...");
      
      if (controller == null) {
        Debug.LogError($"[{LayerName}] Controller is null!");
        return;
      }
      
      if (controller.layers == null || controller.layers.Length == 0) {
        Debug.LogError($"[{LayerName}] Controller has no layers!");
        return;
      }
      
      var stateMachine = controller.layers[0].stateMachine;
      
      if (stateMachine == null) {
        Debug.LogWarning($"[{LayerName}] Base layer state machine is null, creating new one...");
        stateMachine = new AnimatorStateMachine { name = "Base Layer" };
        UnityEditor.AssetDatabase.AddObjectToAsset(stateMachine, controller);
        var layers = controller.layers;
        layers[0].stateMachine = stateMachine;
        controller.layers = layers;
      }
      
      var config = ctx.Config;
      var clips = ctx.ClipProvider;
      var bt = ctx.BlendTreeFactory;
      var tf = ctx.TransitionFactory;

      var idleState = CreateStateIfClipExists(stateMachine, "Idle", new Vector3(300, 0, 0), 
        clips, "General", "Idle_A", config.useWriteDefaults);
      
      if (idleState == null) {
        Debug.LogError($"[{LayerName}] Critical: Cannot create Idle state - Idle_A clip not found!");
        return;
      }
      
      stateMachine.defaultState = idleState;

      var locomotionState = CreateState(stateMachine, "Locomotion", new Vector3(300, 100, 0),
        bt.CreateLocomotion(controller), config.useWriteDefaults);

      var crouchState = CreateState(stateMachine, "Crouch", new Vector3(500, 100, 0),
        bt.CreateCrouch(controller), config.useWriteDefaults);

      var sneakingState = CreateStateIfClipExists(stateMachine, "Sneaking", new Vector3(500, 150, 0),
        clips, "MovementAdvanced", "Sneaking", config.useWriteDefaults);

      var crawlingState = CreateStateIfClipExists(stateMachine, "Crawling", new Vector3(500, 200, 0),
        clips, "MovementAdvanced", "Crawling", config.useWriteDefaults);

      // Jump states
      var jumpStart = CreateStateIfClipExists(stateMachine, "JumpStart", new Vector3(300, 250, 0),
        clips, "MovementBasic", "Jump_Start", config.useWriteDefaults);
      var jumpIdle = CreateStateIfClipExists(stateMachine, "JumpIdle", new Vector3(450, 250, 0),
        clips, "MovementBasic", "Jump_Idle", config.useWriteDefaults);
      var jumpLand = CreateStateIfClipExists(stateMachine, "JumpLand", new Vector3(600, 250, 0),
        clips, "MovementBasic", "Jump_Land", config.useWriteDefaults);
      var jumpFullShort = CreateStateIfClipExists(stateMachine, "JumpFullShort", new Vector3(300, 300, 0),
        clips, "MovementBasic", "Jump_Full_Short", config.useWriteDefaults);
      var jumpFullLong = CreateStateIfClipExists(stateMachine, "JumpFullLong", new Vector3(450, 300, 0),
        clips, "MovementBasic", "Jump_Full_Long", config.useWriteDefaults);

      var dodgeState = CreateState(stateMachine, "Dodge", new Vector3(500, 0, 0),
        bt.CreateDodge(controller), config.useWriteDefaults);

      // Death states with variants
      var deathState = CreateState(stateMachine, "Death", new Vector3(100, 400, 0),
        bt.CreateDeathVariants(controller), config.useWriteDefaults);
      var deathPoseA = CreateStateIfClipExists(stateMachine, "DeathPoseA", new Vector3(250, 400, 0),
        clips, "General", "Death_A_Pose", config.useWriteDefaults);
      var deathPoseB = CreateStateIfClipExists(stateMachine, "DeathPoseB", new Vector3(400, 400, 0),
        clips, "General", "Death_B_Pose", config.useWriteDefaults);

      // Spawn states
      var spawnAir = CreateStateIfClipExists(stateMachine, "SpawnAir", new Vector3(100, 500, 0),
        clips, "General", "Spawn_Air", config.useWriteDefaults);
      var spawnGround = CreateStateIfClipExists(stateMachine, "SpawnGround", new Vector3(250, 500, 0),
        clips, "General", "Spawn_Ground", config.useWriteDefaults);

      if (idleState == null) {
        Debug.LogError($"[{LayerName}] Critical: Idle_A clip not found!");
        return;
      }

      // Locomotion transitions
      if (locomotionState != null) {
        tf.AddFloatTransition(idleState, locomotionState, "Speed", config.idleToMoveThreshold, true, LayerIndex);
        tf.AddFloatTransition(locomotionState, idleState, "Speed", config.idleToMoveThreshold, false, LayerIndex);
      }

      // Crouch transitions
      if (crouchState != null) {
        tf.AddBoolTransition(idleState, crouchState, "IsCrouching", true, LayerIndex);
        if (locomotionState != null)
          tf.AddBoolTransition(locomotionState, crouchState, "IsCrouching", true, LayerIndex);
        tf.AddBoolTransition(crouchState, idleState, "IsCrouching", false, LayerIndex);
      }

      // Sneak transitions
      if (sneakingState != null) {
        tf.AddBoolTransition(idleState, sneakingState, "IsSneaking", true, LayerIndex);
        tf.AddBoolTransition(sneakingState, idleState, "IsSneaking", false, LayerIndex);
      }

      // Jump transitions
      if (jumpStart != null) {
        tf.AddTriggerTransition(idleState, jumpStart, "Jump", LayerIndex);
        if (locomotionState != null)
          tf.AddTriggerTransition(locomotionState, jumpStart, "Jump", LayerIndex);
        
        if (jumpIdle != null) {
          tf.AddExitTimeTransition(jumpStart, jumpIdle, 0.9f, LayerIndex);
          if (jumpLand != null) {
            tf.AddBoolTransition(jumpIdle, jumpLand, "IsGrounded", true, LayerIndex);
            tf.AddExitTimeTransition(jumpLand, idleState, 0.9f, LayerIndex);
          }
        }
      }

      // Dodge transitions
      if (dodgeState != null) {
        tf.AddTriggerTransition(idleState, dodgeState, "Dodge", LayerIndex);
        if (locomotionState != null)
          tf.AddTriggerTransition(locomotionState, dodgeState, "Dodge", LayerIndex);
        tf.AddExitTimeTransition(dodgeState, idleState, 0.9f, LayerIndex);
      }

      // Death transitions
      if (deathState != null) {
        tf.AddTriggerTransition(idleState, deathState, "Die", LayerIndex);
        if (locomotionState != null)
          tf.AddTriggerTransition(locomotionState, deathState, "Die", LayerIndex);
        
        if (deathPoseA != null) {
          tf.AddExitTimeTransition(deathState, deathPoseA, 0.95f, LayerIndex);
        }
      }

      // Spawn transitions
      if (spawnGround != null) {
        tf.AddTriggerTransition(idleState, spawnGround, "Spawn", LayerIndex);
        tf.AddExitTimeTransition(spawnGround, idleState, 0.9f, LayerIndex);
      }

      Debug.Log($"[{LayerName}] Base layer built successfully");
    }

    private AnimatorState CreateState(AnimatorStateMachine sm, string name, Vector3 pos, Motion motion, bool writeDefaults) {
      if (sm == null) {
        Debug.LogError($"[{LayerName}] Cannot create state {name} - state machine is null!");
        return null;
      }
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

