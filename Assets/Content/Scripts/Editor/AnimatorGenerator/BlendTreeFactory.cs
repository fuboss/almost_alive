using Content.Scripts.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class BlendTreeFactory {
    private readonly AnimationClipProvider _clipProvider;

    public BlendTreeFactory(AnimationClipProvider clipProvider) {
      _clipProvider = clipProvider;
    }

    public BlendTree CreateLocomotion(AnimatorController controller) {
      var tree = new BlendTree {
        name = "Locomotion",
        blendType = BlendTreeType.FreeformDirectional2D,
        blendParameter = "Direction",
        blendParameterY = "Speed"
      };

      int childCount = 0;
      
      // Idle
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Idle_A"), new Vector2(0, 0));
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Idle_B"), new Vector2(0, 0));

      // Walking - all variants
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementBasic", "Walking_A"), new Vector2(0, 0.3f));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementBasic", "Walking_B"), new Vector2(0, 0.35f));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementBasic", "Walking_C"), new Vector2(0, 0.4f));
      
      // Running - all variants
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementBasic", "Running_A"), new Vector2(0, 1f));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementBasic", "Running_B"), new Vector2(0, 1.05f));

      // Backwards
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Walking_Backwards"), new Vector2(0, -0.5f));

      // Strafing
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Running_Strafe_Left"), new Vector2(-1, 0.7f));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Running_Strafe_Right"), new Vector2(1, 0.7f));

      // Armed movement
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Running_HoldingBow"), new Vector2(0, 0.9f));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Running_HoldingRifle"), new Vector2(0, 0.95f));

      if (childCount == 0) {
        Debug.LogError("Locomotion BlendTree has no clips! At least Idle_A should exist.");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    private int AddChildIfExists(BlendTree tree, Motion motion, Vector2 position) {
      if (motion != null) {
        tree.AddChild(motion, position);
        return 1;
      }
      return 0;
    }

    public BlendTree CreateCrouch(AnimatorController controller) {
      var tree = new BlendTree {
        name = "CrouchLocomotion",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "Speed"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Crouching"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Sneaking"), 0.5f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Crawling"), 1f);

      if (childCount == 0) {
        Debug.LogWarning("Crouch BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateDodge(AnimatorController controller) {
      var tree = new BlendTree {
        name = "Dodge",
        blendType = BlendTreeType.SimpleDirectional2D,
        blendParameter = "Direction",
        blendParameterY = "Vertical"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Dodge_Forward"), new Vector2(0, 1));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Dodge_Backward"), new Vector2(0, -1));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Dodge_Left"), new Vector2(-1, 0));
      childCount += AddChildIfExists(tree, _clipProvider.Get("MovementAdvanced", "Dodge_Right"), new Vector2(1, 0));

      if (childCount == 0) {
        Debug.LogWarning("Dodge BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateWork(AnimatorController controller) {
      var tree = new BlendTree {
        name = "WorkAnimations",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "ToolType"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Idle_A"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("Tools", "Chopping"), (float)ToolAnimationType.Axe);
      childCount += AddChildIfExists(tree, _clipProvider.Get("Tools", "Pickaxing"), (float)ToolAnimationType.Pickaxe);
      childCount += AddChildIfExists(tree, _clipProvider.Get("Tools", "Digging"), (float)ToolAnimationType.Shovel);
      childCount += AddChildIfExists(tree, _clipProvider.Get("Tools", "Hammering"), (float)ToolAnimationType.Hammer);
      childCount += AddChildIfExists(tree, _clipProvider.Get("Tools", "Sawing"), (float)ToolAnimationType.Saw);
      childCount += AddChildIfExists(tree, _clipProvider.Get("Tools", "Fishing_Idle"), (float)ToolAnimationType.FishingRod);

      if (childCount == 0) {
        Debug.LogWarning("Work BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateMeleeAttack1H(AnimatorController controller) {
      var tree = new BlendTree {
        name = "Attack1H",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "AttackIndex"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_1H_Attack_Chop"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_1H_Attack_Slice_Horizontal"), 1f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_1H_Attack_Slice_Diagonal"), 2f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_1H_Attack_Stab"), 3f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_1H_Attack_Jump_Chop"), 4f);

      if (childCount == 0) {
        Debug.LogWarning("Attack1H BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateMeleeAttack2H(AnimatorController controller) {
      var tree = new BlendTree {
        name = "Attack2H",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "AttackIndex"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_2H_Attack_Chop"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_2H_Attack_Slice"), 1f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_2H_Attack_Stab"), 2f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_2H_Attack_Spin"), 3f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_2H_Attack_Spinning"), 4f);

      if (childCount == 0) {
        Debug.LogWarning("Attack2H BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateMeleeAttackDualwield(AnimatorController controller) {
      var tree = new BlendTree {
        name = "AttackDualwield",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "AttackIndex"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_Dualwield_Attack_Chop"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_Dualwield_Attack_Slice"), 1f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_Dualwield_Attack_Stab"), 2f);

      if (childCount == 0) {
        Debug.LogWarning("AttackDualwield BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateMeleeAttackUnarmed(AnimatorController controller) {
      var tree = new BlendTree {
        name = "AttackUnarmed",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "AttackIndex"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_Unarmed_Attack_Punch_A"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_Unarmed_Attack_Kick"), 1f);

      if (childCount == 0) {
        Debug.LogWarning("AttackUnarmed BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateDeathVariants(AnimatorController controller) {
      var tree = new BlendTree {
        name = "DeathVariants",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "DeathType"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Death_A"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Death_B"), 1f);

      if (childCount == 0) {
        Debug.LogWarning("DeathVariants BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateIdleVariants(AnimatorController controller) {
      var tree = new BlendTree {
        name = "IdleVariants",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "Direction"
      };

      int childCount = 0;
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Idle_A"), 0f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("General", "Idle_B"), 0.5f);
      childCount += AddChildIfExists(tree, _clipProvider.Get("CombatMelee", "Melee_Unarmed_Idle"), 1f);

      if (childCount == 0) {
        Debug.LogWarning("IdleVariants BlendTree has no clips");
        return null;
      }

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    private int AddChildIfExists(BlendTree tree, Motion motion, float threshold) {
      if (motion != null) {
        tree.AddChild(motion, threshold);
        return 1;
      }
      return 0;
    }
  }
}

