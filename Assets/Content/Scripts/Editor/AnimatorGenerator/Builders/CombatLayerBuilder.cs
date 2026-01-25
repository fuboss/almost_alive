using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class CombatLayerBuilder : ILayerBuilder {
    public string LayerName => "Combat";
    public int LayerIndex => 1;

    public void Build(AnimatorController controller, LayerBuildContext ctx) {
      Debug.Log($"[{LayerName}] Building combat layer...");
      controller.AddLayer(LayerName);
      
      var layers = controller.layers;
      layers[LayerIndex].defaultWeight = ctx.Config.layers[LayerIndex].weight;
      controller.layers = layers;

      var stateMachine = controller.layers[LayerIndex].stateMachine;
      var config = ctx.Config;
      var tf = ctx.TransitionFactory;
      var clips = ctx.ClipProvider;
      var bt = ctx.BlendTreeFactory;

      var emptyState = CreateEmptyState(stateMachine, "Empty", new Vector3(200, 0, 0), config.useWriteDefaults);
      stateMachine.defaultState = emptyState;

      // Combat Idle states
      var combatIdle = CreateStateIfClipExists(stateMachine, "CombatIdle", new Vector3(400, 0, 0),
        clips, "General", "Idle_A", config.useWriteDefaults);
      var unarmedIdle = CreateStateIfClipExists(stateMachine, "UnarmedIdle", new Vector3(400, -50, 0),
        clips, "CombatMelee", "Melee_Unarmed_Idle", config.useWriteDefaults);
      var melee2HIdle = CreateStateIfClipExists(stateMachine, "Melee2HIdle", new Vector3(400, -100, 0),
        clips, "CombatMelee", "Melee_2H_Idle", config.useWriteDefaults);

      // Melee Attack BlendTrees
      var attackUnarmed = CreateState(stateMachine, "AttackUnarmed", new Vector3(600, 100, 0),
        bt.CreateMeleeAttackUnarmed(controller), config.useWriteDefaults);
      var attack1H = CreateState(stateMachine, "Attack1H", new Vector3(750, 100, 0),
        bt.CreateMeleeAttack1H(controller), config.useWriteDefaults);
      var attack2H = CreateState(stateMachine, "Attack2H", new Vector3(900, 100, 0),
        bt.CreateMeleeAttack2H(controller), config.useWriteDefaults);
      var attackDualwield = CreateState(stateMachine, "AttackDualwield", new Vector3(1050, 100, 0),
        bt.CreateMeleeAttackDualwield(controller), config.useWriteDefaults);

      // Block states
      var blockState = CreateStateIfClipExists(stateMachine, "Block", new Vector3(600, 200, 0),
        clips, "CombatMelee", "Melee_Block", config.useWriteDefaults);
      var blockingState = CreateStateIfClipExists(stateMachine, "Blocking", new Vector3(750, 200, 0),
        clips, "CombatMelee", "Melee_Blocking", config.useWriteDefaults);
      var blockHit = CreateStateIfClipExists(stateMachine, "BlockHit", new Vector3(900, 200, 0),
        clips, "CombatMelee", "Melee_Block_Hit", config.useWriteDefaults);
      var blockAttack = CreateStateIfClipExists(stateMachine, "BlockAttack", new Vector3(750, 250, 0),
        clips, "CombatMelee", "Melee_Block_Attack", config.useWriteDefaults);

      // Ranged Bow states
      var bowIdle = CreateStateIfClipExists(stateMachine, "BowIdle", new Vector3(600, 350, 0),
        clips, "CombatRanged", "Ranged_Bow_Idle", config.useWriteDefaults);
      var bowAiming = CreateStateIfClipExists(stateMachine, "BowAiming", new Vector3(750, 350, 0),
        clips, "CombatRanged", "Ranged_Bow_Aiming_Idle", config.useWriteDefaults);
      var bowDraw = CreateStateIfClipExists(stateMachine, "BowDraw", new Vector3(900, 350, 0),
        clips, "CombatRanged", "Ranged_Bow_Draw", config.useWriteDefaults);
      var bowRelease = CreateStateIfClipExists(stateMachine, "BowRelease", new Vector3(1050, 350, 0),
        clips, "CombatRanged", "Ranged_Bow_Release", config.useWriteDefaults);

      // Ranged Gun states (1H)
      var gun1HAiming = CreateStateIfClipExists(stateMachine, "Gun1HAiming", new Vector3(600, 450, 0),
        clips, "CombatRanged", "Ranged_1H_Aiming", config.useWriteDefaults);
      var gun1HShoot = CreateStateIfClipExists(stateMachine, "Gun1HShoot", new Vector3(750, 450, 0),
        clips, "CombatRanged", "Ranged_1H_Shoot", config.useWriteDefaults);
      var gun1HShooting = CreateStateIfClipExists(stateMachine, "Gun1HShooting", new Vector3(900, 450, 0),
        clips, "CombatRanged", "Ranged_1H_Shooting", config.useWriteDefaults);
      var gun1HReload = CreateStateIfClipExists(stateMachine, "Gun1HReload", new Vector3(1050, 450, 0),
        clips, "CombatRanged", "Ranged_1H_Reload", config.useWriteDefaults);

      // Ranged Gun states (2H)
      var gun2HAiming = CreateStateIfClipExists(stateMachine, "Gun2HAiming", new Vector3(600, 550, 0),
        clips, "CombatRanged", "Ranged_2H_Aiming", config.useWriteDefaults);
      var gun2HShoot = CreateStateIfClipExists(stateMachine, "Gun2HShoot", new Vector3(750, 550, 0),
        clips, "CombatRanged", "Ranged_2H_Shoot", config.useWriteDefaults);
      var gun2HShooting = CreateStateIfClipExists(stateMachine, "Gun2HShooting", new Vector3(900, 550, 0),
        clips, "CombatRanged", "Ranged_2H_Shooting", config.useWriteDefaults);
      var gun2HReload = CreateStateIfClipExists(stateMachine, "Gun2HReload", new Vector3(1050, 550, 0),
        clips, "CombatRanged", "Ranged_2H_Reload", config.useWriteDefaults);

      // Magic states
      var magicRaise = CreateStateIfClipExists(stateMachine, "MagicRaise", new Vector3(600, 650, 0),
        clips, "CombatRanged", "Ranged_Magic_Raise", config.useWriteDefaults);
      var magicSpellcasting = CreateStateIfClipExists(stateMachine, "MagicSpellcasting", new Vector3(750, 650, 0),
        clips, "CombatRanged", "Ranged_Magic_Spellcasting", config.useWriteDefaults);
      var magicShoot = CreateStateIfClipExists(stateMachine, "MagicShoot", new Vector3(900, 650, 0),
        clips, "CombatRanged", "Ranged_Magic_Shoot", config.useWriteDefaults);
      var magicSummon = CreateStateIfClipExists(stateMachine, "MagicSummon", new Vector3(1050, 650, 0),
        clips, "CombatRanged", "Ranged_Magic_Summon", config.useWriteDefaults);

      // Transitions: Empty <-> Combat
      if (combatIdle != null) {
        tf.AddBoolTransition(emptyState, combatIdle, "InCombat", true, LayerIndex);
        tf.AddBoolTransition(combatIdle, emptyState, "InCombat", false, LayerIndex);

        // Melee attacks from idle
        if (attackUnarmed != null) {
          tf.AddAttackTransition(combatIdle, attackUnarmed, 0);
          tf.AddExitTimeTransition(attackUnarmed, combatIdle, 0.9f, LayerIndex);
        }
        if (attack1H != null) {
          tf.AddAttackTransition(combatIdle, attack1H, 1);
          tf.AddExitTimeTransition(attack1H, combatIdle, 0.9f, LayerIndex);
        }
        if (attack2H != null) {
          tf.AddAttackTransition(combatIdle, attack2H, 2);
          tf.AddExitTimeTransition(attack2H, combatIdle, 0.9f, LayerIndex);
        }
        if (attackDualwield != null) {
          tf.AddAttackTransition(combatIdle, attackDualwield, 3);
          tf.AddExitTimeTransition(attackDualwield, combatIdle, 0.9f, LayerIndex);
        }

        // Block transitions
        if (blockState != null) {
          tf.AddBlockTransition(combatIdle, blockState);
          if (blockingState != null) {
            tf.AddExitTimeTransition(blockState, blockingState, 0.9f, LayerIndex);
            tf.AddBoolTransition(blockingState, combatIdle, "IsBlocking", false, LayerIndex);

            if (blockHit != null) {
              tf.AddTriggerTransition(blockingState, blockHit, "Hit", LayerIndex);
              tf.AddExitTimeTransition(blockHit, blockingState, 0.9f, LayerIndex);
            }
            if (blockAttack != null) {
              tf.AddTriggerTransition(blockingState, blockAttack, "Attack", LayerIndex);
              tf.AddExitTimeTransition(blockAttack, blockingState, 0.9f, LayerIndex);
            }
          }
        }

        // Bow transitions
        if (bowAiming != null) {
          tf.AddBoolTransition(combatIdle, bowAiming, "IsAiming", true, LayerIndex);
          tf.AddBoolTransition(bowAiming, combatIdle, "IsAiming", false, LayerIndex);
          
          if (bowRelease != null) {
            tf.AddTriggerTransition(bowAiming, bowRelease, "Attack", LayerIndex);
            tf.AddExitTimeTransition(bowRelease, bowAiming, 0.9f, LayerIndex);
          }
        }
      }

      Debug.Log($"[{LayerName}] Combat layer built successfully");
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

