#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Scripts.Game;
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Scatter phase.
  /// Shows per-biome scatter configurations with validation status.
  /// Validates actorKey against Addressables.
  /// </summary>
  public class ScatterSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Scatter";
    public int PhaseIndex => 4;
    public bool IsFoldedOut { get; set; } = true;

    private int _selectedBiomeIndex;
    private Dictionary<string, bool> _scatterFoldouts = new();
    private Vector2 _scrollPosition;
    
    // Addressables validation cache
    private HashSet<string> _validActorKeys;
    private bool _cacheLoaded;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);
      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(350));

      var data = config.Data;

      // Master toggle
      data.createScattersInEditor = EditorGUILayout.Toggle("Create in Editor", data.createScattersInEditor);

      if (!data.createScattersInEditor) {
        EditorGUILayout.HelpBox("Scatter spawning disabled. Enable to spawn actors.", MessageType.Warning);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        return;
      }

      EditorGUILayout.Space(8);

      // Biome selector
      if (data.biomes == null || data.biomes.Count == 0) {
        EditorGUILayout.HelpBox("No biomes configured", MessageType.Warning);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        return;
      }

      var biomeNames = new string[data.biomes.Count];
      for (int i = 0; i < data.biomes.Count; i++) {
        var b = data.biomes[i];
        var scatterCount = b != null && b.scatterConfigs != null ? b.scatterConfigs.Count : 0;
        biomeNames[i] = b != null ? $"{b.name} ({scatterCount} scatters)" : $"[{i}] null";
      }

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Biome", GUILayout.Width(50));
      _selectedBiomeIndex = Mathf.Clamp(_selectedBiomeIndex, 0, data.biomes.Count - 1);
      _selectedBiomeIndex = EditorGUILayout.Popup(_selectedBiomeIndex, biomeNames);
      EditorGUILayout.EndHorizontal();

      var selectedBiome = data.biomes[_selectedBiomeIndex];
      if (selectedBiome == null) {
        EditorGUILayout.HelpBox("Selected biome is null", MessageType.Error);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        return;
      }

      EditorGUILayout.Space(4);

      // Summary statistics
      DrawSummary(data.biomes);
      
      EditorGUILayout.Space(8);
      
      // Draw biome scatter configs
      DrawBiomeScatterConfigs(selectedBiome);

      EditorGUILayout.EndScrollView();

      EditorGUILayout.Space(4);

      // Quick actions (outside scroll)
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Validate All", GUILayout.Height(24))) {
        ValidateAllScatters(data.biomes);
      }
      if (GUILayout.Button("Reload Cache", GUILayout.Height(24))) {
        ReloadActorCache();
      }
      if (GUILayout.Button("Select Biome SO", GUILayout.Height(24))) {
        Selection.activeObject = selectedBiome;
        EditorGUIUtility.PingObject(selectedBiome);
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }

    private void EnsureCacheLoaded() {
      if (_cacheLoaded) return;
      ReloadActorCache();
    }

    private void ReloadActorCache() {
      _validActorKeys = new HashSet<string>();
      
      try {
        var handle = Addressables.LoadAssetsAsync<GameObject>("Actors", null);
        var prefabs = handle.WaitForCompletion();
        
        foreach (var prefab in prefabs) {
          var actor = prefab.GetComponent<ActorDescription>();
          if (actor != null && !string.IsNullOrEmpty(actor.actorKey)) {
            _validActorKeys.Add(actor.actorKey);
          }
        }
        
        Addressables.Release(handle);
        _cacheLoaded = true;
        
        Debug.Log($"[ScatterSettings] Loaded {_validActorKeys.Count} valid actor keys from Addressables");
      }
      catch (System.Exception e) {
        Debug.LogError($"[ScatterSettings] Failed to load Addressables: {e.Message}");
      }
    }

    private bool IsActorKeyValid(string actorKey) {
      if (string.IsNullOrEmpty(actorKey)) return false;
      EnsureCacheLoaded();
      return _validActorKeys != null && _validActorKeys.Contains(actorKey);
    }

    private void DrawSummary(List<BiomeSO> biomes) {
      EnsureCacheLoaded();
      
      var totalScatters = 0;
      var missingActors = 0;
      var missingRules = 0;
      var emptyKeys = 0;

      foreach (var biome in biomes) {
        if (biome?.scatterConfigs == null) continue;
        
        foreach (var sc in biome.scatterConfigs) {
          if (sc == null) {
            missingRules++;
            continue;
          }
          
          if (sc.rule == null) {
            missingRules++;
            continue;
          }
          
          totalScatters++;
          
          if (string.IsNullOrEmpty(sc.rule.actorKey)) {
            emptyKeys++;
          } else if (!IsActorKeyValid(sc.rule.actorKey)) {
            missingActors++;
          }
        }
      }

      var hasErrors = missingActors > 0 || missingRules > 0 || emptyKeys > 0;
      var color = hasErrors ? new Color(1f, 0.8f, 0.3f) : new Color(0.5f, 0.8f, 0.5f);
      var oldColor = GUI.backgroundColor;
      GUI.backgroundColor = color;
      
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField($"📊 Total Scatter Rules: {totalScatters}", EditorStyles.boldLabel);
      
      if (_validActorKeys != null) {
        EditorGUILayout.LabelField($"📦 Available Actors: {_validActorKeys.Count}", EditorStyles.miniLabel);
      }
      
      if (emptyKeys > 0) {
        EditorGUILayout.LabelField($"⚠ Empty actorKey: {emptyKeys}", EditorStyles.miniLabel);
      }
      if (missingActors > 0) {
        EditorGUILayout.LabelField($"❌ Invalid actorKey: {missingActors}", EditorStyles.miniLabel);
      }
      if (missingRules > 0) {
        EditorGUILayout.LabelField($"❌ Null Rules: {missingRules}", EditorStyles.miniLabel);
      }
      if (!hasErrors) {
        EditorGUILayout.LabelField("✓ All rules valid", EditorStyles.miniLabel);
      }
      
      EditorGUILayout.EndVertical();
      GUI.backgroundColor = oldColor;
    }

    private void DrawBiomeScatterConfigs(BiomeSO biome) {
      EditorGUILayout.LabelField($"Scatters in {biome.name}", EditorStyles.boldLabel);
      
      if (biome.scatterConfigs == null || biome.scatterConfigs.Count == 0) {
        EditorGUILayout.HelpBox("No scatter configs. Add ScatterRuleSO references in BiomeSO.", MessageType.Info);
        return;
      }

      EditorGUI.BeginChangeCheck();

      for (int i = 0; i < biome.scatterConfigs.Count; i++) {
        var sc = biome.scatterConfigs[i];
        DrawScatterConfig(biome, sc, i);
      }

      if (EditorGUI.EndChangeCheck()) {
        EditorUtility.SetDirty(biome);
      }
    }

    private void DrawScatterConfig(BiomeSO biome, BiomeScatterConfig sc, int index) {
      if (sc == null) {
        EditorGUILayout.HelpBox($"[{index}] Null scatter config", MessageType.Error);
        return;
      }

      var rule = sc.rule;
      if (rule == null) {
        EditorGUILayout.HelpBox($"[{index}] Null rule reference", MessageType.Error);
        return;
      }

      var key = $"{biome.name}_{rule.name}";
      if (!_scatterFoldouts.ContainsKey(key)) {
        _scatterFoldouts[key] = false;
      }

      // Determine status
      var hasActorKey = !string.IsNullOrEmpty(rule.actorKey);
      var isValid = hasActorKey && IsActorKeyValid(rule.actorKey);
      
      var icon = isValid ? "🌲" : (hasActorKey ? "❓" : "❌");
      var statusColor = isValid 
        ? new Color(0.3f, 0.6f, 0.3f, 0.3f) 
        : new Color(0.7f, 0.3f, 0.2f, 0.3f);

      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      
      // Header with status color
      var rect = EditorGUILayout.BeginHorizontal();
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4, 20), statusColor * 2f);
      
      GUILayout.Space(8);
      _scatterFoldouts[key] = EditorGUILayout.Foldout(_scatterFoldouts[key], $"{icon} {rule.name}", true);
      
      GUILayout.FlexibleSpace();
      
      // Quick stats
      EditorGUILayout.LabelField($"d:{rule.density:F2} s:{rule.minSpacing:F0}m", EditorStyles.miniLabel, GUILayout.Width(100));
      
      // Select rule button
      if (GUILayout.Button("↗", GUILayout.Width(22), GUILayout.Height(18))) {
        Selection.activeObject = rule;
        EditorGUIUtility.PingObject(rule);
      }
      
      EditorGUILayout.EndHorizontal();

      if (_scatterFoldouts[key]) {
        EditorGUI.indentLevel++;
        
        // Actor key status
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ActorKey:", GUILayout.Width(65));
        
        if (!hasActorKey) {
          var oldColor = GUI.color;
          GUI.color = Color.red;
          EditorGUILayout.LabelField("❌ NOT SET - won't spawn!", EditorStyles.boldLabel);
          GUI.color = oldColor;
        } else if (!isValid) {
          var oldColor = GUI.color;
          GUI.color = Color.yellow;
          EditorGUILayout.LabelField($"❓ '{rule.actorKey}' not in Addressables!", EditorStyles.boldLabel);
          GUI.color = oldColor;
        } else {
          EditorGUILayout.LabelField($"✓ {rule.actorKey}", EditorStyles.boldLabel);
        }
        EditorGUILayout.EndHorizontal();

        // Distribution info
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Distribution", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Density: {rule.density:F2}/100m² | Min Spacing: {rule.minSpacing:F1}m", EditorStyles.miniLabel);
        
        // Scale range
        EditorGUILayout.LabelField($"  Scale: [{rule.scaleRange.x:F2} - {rule.scaleRange.y:F2}]", EditorStyles.miniLabel);
        
        // Clustering
        if (rule.useClustering) {
          EditorGUILayout.LabelField($"  Clustering: {rule.clusterSize.x}-{rule.clusterSize.y} @ {rule.clusterSpread:F1}m spread", EditorStyles.miniLabel);
        }

        // Placement override
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Placement: {sc.placement}", EditorStyles.miniLabel);
        
        EditorGUI.indentLevel--;
      }
      
      EditorGUILayout.EndVertical();
    }

    private void ValidateAllScatters(List<BiomeSO> biomes) {
      ReloadActorCache();
      
      var errors = 0;
      var warnings = 0;

      foreach (var biome in biomes) {
        if (biome == null) {
          Debug.LogError("[ScatterValidation] Null biome in config");
          errors++;
          continue;
        }

        if (biome.scatterConfigs == null || biome.scatterConfigs.Count == 0) {
          continue;
        }

        foreach (var sc in biome.scatterConfigs) {
          if (sc == null) {
            Debug.LogError($"[ScatterValidation] {biome.name}: Null scatter config");
            errors++;
            continue;
          }

          if (sc.rule == null) {
            Debug.LogError($"[ScatterValidation] {biome.name}: Scatter config with null rule");
            errors++;
            continue;
          }

          if (string.IsNullOrEmpty(sc.rule.actorKey)) {
            Debug.LogError($"[ScatterValidation] {biome.name}/{sc.rule.name}: Empty actorKey - cannot spawn!");
            errors++;
          } else if (!IsActorKeyValid(sc.rule.actorKey)) {
            Debug.LogWarning($"[ScatterValidation] {biome.name}/{sc.rule.name}: actorKey '{sc.rule.actorKey}' not found in Addressables label 'Actors'");
            warnings++;
          }
        }
      }

      if (errors == 0 && warnings == 0) {
        Debug.Log("[ScatterValidation] ✓ All scatter configurations valid!");
      } else {
        Debug.Log($"[ScatterValidation] Completed with {errors} errors, {warnings} warnings");
      }
    }
  }
}
#endif
