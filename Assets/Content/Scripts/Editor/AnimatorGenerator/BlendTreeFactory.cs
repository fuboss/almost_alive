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

      tree.AddChild(_clipProvider.Get("General", "Idle_A"), new Vector2(0, 0));
      tree.AddChild(_clipProvider.Get("MovementBasic", "Walking_A"), new Vector2(0, 0.3f));
      tree.AddChild(_clipProvider.Get("MovementBasic", "Running_A"), new Vector2(0, 1f));
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Walking_Backwards"), new Vector2(0, -0.5f));
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Running_Strafe_Left"), new Vector2(-1, 0.7f));
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Running_Strafe_Right"), new Vector2(1, 0.7f));

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateCrouch(AnimatorController controller) {
      var tree = new BlendTree {
        name = "CrouchLocomotion",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "Speed"
      };

      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Crouching"), 0f);
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Sneaking"), 0.5f);
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Crawling"), 1f);

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

      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Dodge_Forward"), new Vector2(0, 1));
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Dodge_Backward"), new Vector2(0, -1));
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Dodge_Left"), new Vector2(-1, 0));
      tree.AddChild(_clipProvider.Get("MovementAdvanced", "Dodge_Right"), new Vector2(1, 0));

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }

    public BlendTree CreateWork(AnimatorController controller) {
      var tree = new BlendTree {
        name = "WorkAnimations",
        blendType = BlendTreeType.Simple1D,
        blendParameter = "ToolType"
      };

      // ToolType enum: None=0, Axe=1, Pickaxe=2, Shovel=3, Hammer=4, Saw=5, FishingRod=6
      tree.AddChild(_clipProvider.Get("General", "Idle_A"), 0f);
      tree.AddChild(_clipProvider.Get("Tools", "Chopping"), (float)ToolAnimationType.Axe);
      tree.AddChild(_clipProvider.Get("Tools", "Pickaxing"), (float)ToolAnimationType.Pickaxe);
      tree.AddChild(_clipProvider.Get("Tools", "Digging"), (float)ToolAnimationType.Shovel);
      tree.AddChild(_clipProvider.Get("Tools", "Hammering"), (float)ToolAnimationType.Hammer);
      tree.AddChild(_clipProvider.Get("Tools", "Sawing"), (float)ToolAnimationType.Saw);
      tree.AddChild(_clipProvider.Get("Tools", "Fishing_Idle"), (float)ToolAnimationType.FishingRod);

      AssetDatabase.AddObjectToAsset(tree, controller);
      return tree;
    }
  }
}

