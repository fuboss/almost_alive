using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Content.Scripts.Editor.World;
#endif

namespace Content.Scripts.World {
  [CreateAssetMenu(menuName = "World/Generator Config", fileName = "WorldGeneratorConfig")]
  public class WorldGeneratorConfigSO : ScriptableObject {
    [BoxGroup("Terrain")]
    [Tooltip("If null, will find Terrain in scene")]
    [SceneObjectsOnly]
    public Terrain terrain;

    [BoxGroup("Terrain")]
    [Tooltip("Margin from terrain edges")]
    [Range(0f, 50f)]
    public float edgeMargin = 10f;

    [BoxGroup("Generation")]
    [Tooltip("Random seed (0 = use system time)")]
    public int seed;

    [BoxGroup("Generation")]
    [ListDrawerSettings(ShowFoldout = true)]
    [Tooltip("Scatter rules to apply in order")]
    public List<ScatterRuleSO> scatterRules = new();

    [BoxGroup("Debug")]
    public bool logGeneration = true;

    [BoxGroup("Debug")]
    [Tooltip("Visualize spawn points in editor")]
    public bool drawGizmos;

    [Button("Validate Rules"), BoxGroup("Debug")]
    private void ValidateRules() {
      var errors = 0;
      foreach (var rule in scatterRules) {
        if (rule == null) {
          Debug.LogError("Null scatter rule in list");
          errors++;
          continue;
        }
        if (string.IsNullOrEmpty(rule.actorKey)) {
          Debug.LogError($"[{rule.name}] Missing actorKey");
          errors++;
        }
      }
      Debug.Log(errors == 0 ? "All rules valid" : $"{errors} errors found");
    }

#if UNITY_EDITOR
    [Button(ButtonSizes.Large), BoxGroup("Editor Generation"), GUIColor(0.4f, 0.8f, 0.4f)]
    [PropertyOrder(-10)]
    private void GenerateInEditor() {
      WorldGeneratorEditor.Generate(this);
    }

    [Button(ButtonSizes.Medium), BoxGroup("Editor Generation"), GUIColor(1f, 0.6f, 0.6f)]
    [PropertyOrder(-9)]
    private void ClearGenerated() {
      WorldGeneratorEditor.Clear();
    }
#endif
  }
}
