using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  [CreateAssetMenu(fileName = "AnimatorGeneratorConfig", menuName = "Animation/Generator Config")]
  public class AnimatorGeneratorConfig : ScriptableObject {
    [TitleGroup("Preset")]
    [EnumToggleButtons, HideLabel]
    public AnimatorPreset preset = AnimatorPreset.Balanced;

    [TitleGroup("Global Settings")]
    [Range(0.01f, 2f), LabelText("Transition Duration Multiplier")]
    [InfoBox("Lower = snappier, Higher = smoother")]
    public float transitionMultiplier = 1f;

    [TitleGroup("Global Settings")]
    [Range(0.5f, 1.5f), LabelText("Exit Time Multiplier")]
    public float exitTimeMultiplier = 1f;

    [TitleGroup("Global Settings")]
    [LabelText("Base Transition Duration (sec)")]
    [Range(0.01f, 0.5f)]
    public float baseTransitionDuration = 0.15f;

    [TitleGroup("Global Settings")]
    [LabelText("Use Write Defaults")]
    public bool useWriteDefaults = false;

    [TitleGroup("Layer Settings")]
    [ListDrawerSettings(ShowFoldout = true)]
    public LayerConfig[] layers = {
      new() { layerName = "Base", enabled = true, weight = 1f, transitionOverride = -1f },
      new() { layerName = "Combat", enabled = true, weight = 1f, transitionOverride = -1f },
      new() { layerName = "UpperBody", enabled = true, weight = 1f, transitionOverride = -1f },
      new() { layerName = "Additive", enabled = true, weight = 0.5f, transitionOverride = -1f, isAdditive = true }
    };

    [TitleGroup("Combat Tuning")]
    [Range(0.01f, 0.3f), LabelText("Attack Transition Speed")]
    public float attackTransitionDuration = 0.08f;

    [TitleGroup("Combat Tuning")]
    [Range(0.01f, 0.2f), LabelText("Block Reaction Time")]
    public float blockReactionTime = 0.05f;

    [TitleGroup("Combat Tuning")]
    [Range(0.5f, 3f), LabelText("Combo Window (sec)")]
    public float comboWindow = 1.5f;

    [TitleGroup("Locomotion Tuning")]
    [Range(0.001f, 0.1f), LabelText("Idle → Move Threshold")]
    public float idleToMoveThreshold = 0.01f;

    [TitleGroup("Locomotion Tuning")]
    [Range(0.05f, 0.3f), LabelText("Locomotion Blend Duration")]
    public float locomotionBlendDuration = 0.15f;

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
    [TitleGroup("Preset")]
    public void ApplyPreset() {
      switch (preset) {
        case AnimatorPreset.Snappy:
          transitionMultiplier = 0.3f;
          exitTimeMultiplier = 0.85f;
          baseTransitionDuration = 0.05f;
          attackTransitionDuration = 0.03f;
          blockReactionTime = 0.02f;
          locomotionBlendDuration = 0.08f;
          break;
        case AnimatorPreset.Smooth:
          transitionMultiplier = 1.5f;
          exitTimeMultiplier = 1.1f;
          baseTransitionDuration = 0.25f;
          attackTransitionDuration = 0.15f;
          blockReactionTime = 0.1f;
          locomotionBlendDuration = 0.25f;
          break;
        case AnimatorPreset.Balanced:
          transitionMultiplier = 1f;
          exitTimeMultiplier = 1f;
          baseTransitionDuration = 0.15f;
          attackTransitionDuration = 0.08f;
          blockReactionTime = 0.05f;
          locomotionBlendDuration = 0.15f;
          break;
        case AnimatorPreset.Custom:
          break;
      }
    }

    public float GetTransitionDuration(int layerIndex = 0) {
      if (layerIndex >= 0 && layerIndex < layers.Length && layers[layerIndex].transitionOverride > 0) {
        return layers[layerIndex].transitionOverride;
      }
      return baseTransitionDuration * transitionMultiplier;
    }

    public float GetExitTime(float baseExitTime) {
      return Mathf.Clamp01(baseExitTime * exitTimeMultiplier);
    }
  }

  public enum AnimatorPreset {
    Snappy,
    Balanced,
    Smooth,
    Custom
  }

  [Serializable]
  public class LayerConfig {
    [HorizontalGroup("Main", Width = 150)]
    [HideLabel, ReadOnly]
    public string layerName;

    [HorizontalGroup("Main", Width = 30)]
    [HideLabel]
    public bool enabled = true;

    [HorizontalGroup("Main", Width = 80)]
    [Range(0f, 1f), HideLabel]
    public float weight = 1f;

    [HorizontalGroup("Main")]
    [LabelText("Transition Override"), LabelWidth(120)]
    [Tooltip("-1 = use global")]
    public float transitionOverride = -1f;

    [HideInInspector]
    public bool isAdditive;
  }
}

