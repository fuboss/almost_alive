using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class CombatLayerBuilder : ILayerBuilder {
    public string LayerName => "Combat";
    public int LayerIndex => 1;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      controller.AddLayer(LayerName);
      
      var layers = controller.layers;
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;
      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;
      var clips = ctx.ClipProvider;

      var emptyState = CreateState(stateMachine, "Empty", new Vector3(200, 0, 0), null, config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      var combatIdle = CreateState(stateMachine, "CombatIdle", new Vector3(400, 0, 0),
        clips.Get("General", "Idle_A"), config.useWriteDefaults);

      var attackUnarmed = CreateState(stateMachine, "AttackUnarmed", new Vector3(400, 100, 0),
        clips.Get("CombatMelee", "Melee_Unarmed_Attack_Punch_A"), config.useWriteDefaults);

      var attack1H = CreateState(stateMachine, "Attack1H", new Vector3(550, 100, 0),
        clips.Get("CombatMelee", "Melee_1H_Attack_Slice_Horizontal"), config.useWriteDefaults);

      var attack2H = CreateState(stateMachine, "Attack2H", new Vector3(700, 100, 0),
        clips.Get("CombatMelee", "Melee_2H_Attack_Slice"), config.useWriteDefaults);

      var blockState = CreateState(stateMachine, "Block", new Vector3(400, 200, 0),
        clips.Get("CombatMelee", "Melee_Block"), config.useWriteDefaults);

      var blockingState = CreateState(stateMachine, "Blocking", new Vector3(550, 200, 0),
        clips.Get("CombatMelee", "Melee_Blocking"), config.useWriteDefaults);

      var aimState = CreateState(stateMachine, "Aim", new Vector3(400, 300, 0),
        clips.Get("CombatRanged", "Ranged_Bow_Aiming_Idle"), config.useWriteDefaults);

      var shootState = CreateState(stateMachine, "Shoot", new Vector3(550, 300, 0),
        clips.Get("CombatRanged", "Ranged_Bow_Release"), config.useWriteDefaults);

      tf.AddBoolTransition(emptyState, combatIdle, "InCombat", true, LayerIndex);
      tf.AddBoolTransition(combatIdle, emptyState, "InCombat", false, LayerIndex);

      tf.AddAttackTransition(combatIdle, attackUnarmed, 0);
      tf.AddAttackTransition(combatIdle, attack1H, 1);
      tf.AddAttackTransition(combatIdle, attack2H, 2);

      tf.AddExitTimeTransition(attackUnarmed, combatIdle, 0.9f, LayerIndex);
      tf.AddExitTimeTransition(attack1H, combatIdle, 0.9f, LayerIndex);
      tf.AddExitTimeTransition(attack2H, combatIdle, 0.9f, LayerIndex);

      tf.AddBlockTransition(combatIdle, blockState);
      tf.AddExitTimeTransition(blockState, blockingState, 0.9f, LayerIndex);
      tf.AddBoolTransition(blockingState, combatIdle, "IsBlocking", false, LayerIndex);

      tf.AddBoolTransition(combatIdle, aimState, "IsAiming", true, LayerIndex);
      tf.AddTriggerTransition(aimState, shootState, "Attack", LayerIndex);
      tf.AddBoolTransition(aimState, combatIdle, "IsAiming", false, LayerIndex);
      tf.AddExitTimeTransition(shootState, aimState, 0.9f, LayerIndex);
    }

    private AnimatorState CreateState(AnimatorStateMachine sm, string name, Vector3 pos, Motion motion, bool writeDefaults) {
      var state = sm.AddState(name, pos);
      state.motion = motion;
      state.writeDefaultValues = writeDefaults;
      return state;
    }
  }
}

