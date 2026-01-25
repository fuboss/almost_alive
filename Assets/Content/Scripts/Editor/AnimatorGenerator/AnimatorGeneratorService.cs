using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class AnimatorGeneratorService {
    private readonly AnimatorGeneratorConfig _config;
    private readonly AnimationClipProvider _clipProvider;
    private readonly BlendTreeFactory _blendTreeFactory;
    private readonly TransitionFactory _transitionFactory;
    private readonly List<ILayerBuilder> _layerBuilders;
    private readonly string _outputPath;

    public AnimatorGeneratorService(
      AnimatorGeneratorConfig config,
      string animationsPath,
      string outputPath,
      Dictionary<string, string> folderMapping) {
      
      _config = config;
      _outputPath = outputPath;
      _clipProvider = new AnimationClipProvider(animationsPath, folderMapping);
      _blendTreeFactory = new BlendTreeFactory(_clipProvider);
      _transitionFactory = new TransitionFactory(config);

      _layerBuilders = new List<ILayerBuilder> {
        new BaseLayerBuilder(),
        new CombatLayerBuilder(),
        new UpperBodyLayerBuilder($"{outputPath}/UpperBodyMask.mask"),
        new AdditiveLayerBuilder(),
        new SimulationLayerBuilder()
      };
    }

    public AnimatorController Generate() {
      Debug.Log("[AnimatorGenerator] Starting generation...");
      var controllerPath = $"{_outputPath}/UniversalAnimator.controller";
      var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

      if (controller != null) {
        Debug.Log("[AnimatorGenerator] Clearing existing controller...");
        ClearController(controller);
      } else {
        Debug.Log("[AnimatorGenerator] Creating new controller...");
        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
      }

      Debug.Log("[AnimatorGenerator] Adding parameters...");
      AddParameters(controller);

      var context = new LayerBuildContext(_clipProvider, _blendTreeFactory, _config, _transitionFactory);

      foreach (var builder in _layerBuilders) {
        if (builder.LayerIndex >= _config.layers.Length) {
          Debug.LogWarning($"[AnimatorGenerator] Skipping layer {builder.LayerName} - config has no entry for index {builder.LayerIndex}");
          continue;
        }
        
        if (_config.layers[builder.LayerIndex].enabled) {
          Debug.Log($"[AnimatorGenerator] Building layer: {builder.LayerName}");
          try {
            builder.Build(controller, context);
            Debug.Log($"[AnimatorGenerator] Layer {builder.LayerName} built successfully");
          } catch (System.Exception e) {
            Debug.LogError($"[AnimatorGenerator] Failed to build layer {builder.LayerName}: {e.Message}\n{e.StackTrace}");
            throw;
          }
        } else {
          Debug.Log($"[AnimatorGenerator] Skipping disabled layer: {builder.LayerName}");
        }
      }

      Debug.Log("[AnimatorGenerator] Saving controller...");
      EditorUtility.SetDirty(controller);
      AssetDatabase.SaveAssets();
      Debug.Log("[AnimatorGenerator] Generation completed!");

      return controller;
    }

    public void RegisterLayerBuilder(ILayerBuilder builder) {
      _layerBuilders.Add(builder);
    }

    private void AddParameters(AnimatorController controller) {
      // Float
      controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
      controller.AddParameter("Direction", AnimatorControllerParameterType.Float);
      controller.AddParameter("Vertical", AnimatorControllerParameterType.Float);
      controller.AddParameter("AimAngle", AnimatorControllerParameterType.Float);

      // Int
      controller.AddParameter("WeaponType", AnimatorControllerParameterType.Int);
      controller.AddParameter("ToolType", AnimatorControllerParameterType.Int);
      controller.AddParameter("SitType", AnimatorControllerParameterType.Int);

      // Float (for BlendTrees)
      controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Float);
      controller.AddParameter("DeathType", AnimatorControllerParameterType.Float);

      // Bool
      controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsSneaking", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsAiming", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsWorking", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsSitting", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsLying", AnimatorControllerParameterType.Bool);
      controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
      controller.AddParameter("InCombat", AnimatorControllerParameterType.Bool);

      // Trigger
      controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("UseItem", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Cheer", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
      controller.AddParameter("StartWork", AnimatorControllerParameterType.Trigger);
    }

    private void ClearController(AnimatorController controller) {
      while (controller.parameters.Length > 0) {
        controller.RemoveParameter(0);
      }

      while (controller.layers.Length > 1) {
        controller.RemoveLayer(1);
      }

      var baseStateMachine = controller.layers[0].stateMachine;

      if (baseStateMachine != null) {
        if (baseStateMachine.states != null)
          foreach (var state in baseStateMachine.states.ToArray()) {
            baseStateMachine.RemoveState(state.state);
          }

        if (baseStateMachine.stateMachines != null)
          foreach (var subMachine in baseStateMachine.stateMachines.ToArray()) {
            baseStateMachine.RemoveStateMachine(subMachine.stateMachine);
          }
      }

      // Remove sub-assets but keep state machines
      var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller));
      foreach (var subAsset in subAssets) {
        if (subAsset == null || subAsset == controller) continue;
        if (subAsset is AnimatorStateMachine) continue; // Keep state machines
        AssetDatabase.RemoveObjectFromAsset(subAsset);
      }

      EditorUtility.SetDirty(controller);
      AssetDatabase.SaveAssets();
    }
  }
}

