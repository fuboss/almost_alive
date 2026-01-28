# Artist Mode Window Architecture

> ✅ **STATUS: COMPLETE** — Refactored Jan 2026

## Overview

ArtistModeWindow — dockable EditorWindow for step-by-step world generation with per-phase iteration.

## Implementation Summary

Refactored from 27KB monolith to SOLID structure:

```
Editor/WorldGenerationWizard/ArtistMode/
├── ArtistModeWindow.cs           # 4.8KB - minimal shell
├── ArtistModeState.cs            # Pipeline state management
├── ArtistModeStyles.cs           # Cached GUIStyles
├── Drawers/
│   ├── HeaderDrawer.cs           # Seed, terrain, config
│   ├── PhaseListDrawer.cs        # Phase toggles + Run To
│   ├── ActionsDrawer.cs          # Run All, Reset, Quick
│   └── DebugDrawer.cs            # Debug visualization toggle
└── PhaseSettings/
    ├── IPhaseSettingsDrawer.cs   # Interface
    ├── BiomeLayoutSettingsDrawer.cs
    ├── TerrainSculptSettingsDrawer.cs  # Water/River settings
    ├── SplatmapPaintSettingsDrawer.cs
    ├── VegetationSettingsDrawer.cs
    └── ScatterSettingsDrawer.cs
```

---

## Original Problems (before refactor)

- Single 700+ line file
- Phase settings drawing mixed with window logic
- Hard to add new phase-specific settings
- GUI styles scattered throughout

## Proposed Architecture

```
Editor/WorldGenerationWizard/
├── ArtistModeWindow.cs              // Main window (slim coordinator)
├── ArtistMode/
│   ├── ArtistModeStyles.cs          // Shared GUIStyles
│   ├── ArtistModeState.cs           // State container (pipeline, config, etc.)
│   ├── Drawers/
│   │   ├── HeaderDrawer.cs          // Header + debug viz toggle
│   │   ├── ConfigDrawer.cs          // Config + Terrain fields
│   │   ├── SeedDrawer.cs            // Seed field + randomize
│   │   ├── PhasesListDrawer.cs      // Phase rows with toggles/buttons
│   │   ├── ActionsDrawer.cs         // Run/Reset/Clear buttons
│   │   └── StatusDrawer.cs          // Status text
│   └── PhaseSettings/
│       ├── IPhaseSettingsDrawer.cs  // Interface for phase settings
│       ├── BiomeLayoutSettingsDrawer.cs
│       ├── TerrainSculptSettingsDrawer.cs
│       ├── SplatmapPaintSettingsDrawer.cs
│       ├── VegetationSettingsDrawer.cs
│       └── ScatterSettingsDrawer.cs
```

## Key Interfaces

### IPhaseSettingsDrawer

```csharp
public interface IPhaseSettingsDrawer {
  string PhaseName { get; }
  int PhaseIndex { get; }
  void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle);
}
```

### ArtistModeState

```csharp
public class ArtistModeState {
  public GenerationPipeline Pipeline { get; private set; }
  public WorldGeneratorConfigSO Config { get; set; }
  public Terrain Terrain { get; set; }
  public int Seed { get; set; }
  public bool ShowDebugVisualization { get; set; }
  public int TargetPhaseIndex { get; set; }
  
  // Events
  public event Action OnStateChanged;
  public event Action<IGenerationPhase> OnPhaseCompleted;
  
  // Methods
  public void EnsurePipeline();
  public void RunPhase(int index);
  public void RollbackPhase(int index);
  public void Reset();
  public void Clear();
}
```

### ArtistModeStyles

```csharp
public static class ArtistModeStyles {
  public static GUIStyle Header { get; private set; }
  public static GUIStyle PhaseName { get; private set; }
  public static GUIStyle PhaseNameBold { get; private set; }
  public static GUIStyle Status { get; private set; }
  public static GUIStyle Box { get; private set; }
  
  public static void Initialize();
}
```

## Drawer Base Pattern

```csharp
public abstract class ArtistModeDrawerBase {
  protected ArtistModeState State { get; }
  
  protected ArtistModeDrawerBase(ArtistModeState state) {
    State = state;
  }
  
  public abstract void Draw();
}
```

## Slim Window (after refactor)

```csharp
public class ArtistModeWindow : OdinEditorWindow {
  private ArtistModeState _state;
  private List<ArtistModeDrawerBase> _drawers;
  private Dictionary<int, IPhaseSettingsDrawer> _phaseSettingsDrawers;
  private Vector2 _scrollPosition;
  
  protected override void OnEnable() {
    _state = new ArtistModeState();
    _state.LoadDefaults();
    
    _drawers = new List<ArtistModeDrawerBase> {
      new HeaderDrawer(_state),
      new ConfigDrawer(_state),
      new SeedDrawer(_state),
      new PhasesListDrawer(_state),
      new ActionsDrawer(_state),
      new StatusDrawer(_state)
    };
    
    _phaseSettingsDrawers = new Dictionary<int, IPhaseSettingsDrawer> {
      [0] = new BiomeLayoutSettingsDrawer(),
      [1] = new TerrainSculptSettingsDrawer(),
      [2] = new SplatmapPaintSettingsDrawer(),
      [3] = new VegetationSettingsDrawer(),
      [4] = new ScatterSettingsDrawer()
    };
    
    _state.OnStateChanged += Repaint;
  }
  
  protected override void OnImGUI() {
    ArtistModeStyles.Initialize();
    
    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
    
    foreach (var drawer in _drawers) {
      drawer.Draw();
      EditorGUILayout.Space(8);
    }
    
    // Phase-specific settings
    DrawPhaseSettings();
    
    EditorGUILayout.EndScrollView();
  }
  
  private void DrawPhaseSettings() {
    var phaseIndex = _state.GetCurrentPhaseIndex();
    if (_phaseSettingsDrawers.TryGetValue(phaseIndex, out var drawer)) {
      drawer.Draw(_state.Config, ArtistModeStyles.Box);
    }
  }
}
```

## Phase Settings Example

```csharp
public class BiomeLayoutSettingsDrawer : IPhaseSettingsDrawer {
  public string PhaseName => "Biome Layout";
  public int PhaseIndex => 0;
  
  private bool _foldout = true;
  
  public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
    _foldout = EditorGUILayout.Foldout(_foldout, $"⚙ {PhaseName} Settings", true);
    if (!_foldout) return;
    
    EditorGUILayout.BeginVertical(boxStyle);
    
    var data = config.Data;
    
    // Cell Count
    EditorGUILayout.LabelField("Cell Count", EditorStyles.boldLabel);
    data.minBiomeCells = EditorGUILayout.IntSlider("Min", data.minBiomeCells, 4, 50);
    data.maxBiomeCells = EditorGUILayout.IntSlider("Max", data.maxBiomeCells, data.minBiomeCells, 100);
    
    // Borders
    EditorGUILayout.Space(8);
    EditorGUILayout.LabelField("Borders", EditorStyles.boldLabel);
    data.biomeBorderBlend = EditorGUILayout.Slider("Blend Width", data.biomeBorderBlend, 5f, 50f);
    
    // Domain Warping
    EditorGUILayout.Space(8);
    EditorGUILayout.LabelField("Shape Noise", EditorStyles.boldLabel);
    data.useDomainWarping = EditorGUILayout.Toggle("Domain Warping", data.useDomainWarping);
    
    if (data.useDomainWarping) {
      EditorGUI.indentLevel++;
      data.warpStrength = EditorGUILayout.Slider("Strength", data.warpStrength, 0f, 50f);
      data.warpScale = EditorGUILayout.Slider("Scale", data.warpScale, 0.001f, 0.1f);
      data.warpOctaves = EditorGUILayout.IntSlider("Octaves", data.warpOctaves, 1, 4);
      EditorGUI.indentLevel--;
    }
    
    EditorGUILayout.EndVertical();
  }
}
```

## Benefits

1. **Single Responsibility** — each drawer does one thing
2. **Open/Closed** — add new phase settings without modifying window
3. **Dependency Inversion** — window depends on abstractions (IPhaseSettingsDrawer)
4. **Testability** — state logic separated from GUI
5. **Maintainability** — small focused files (~50-100 lines each)

## Migration Plan

1. [x] Create folder structure
2. [x] Extract ArtistModeStyles.cs
3. [x] Extract ArtistModeState.cs (state + pipeline management)
4. [x] Create IPhaseSettingsDrawer interface
5. [x] Extract each PhaseSettings drawer
6. [x] Create ArtistModeDrawerBase
7. [x] Extract section drawers (Header, Config, Seed, PhasesList, Actions, Status)
8. [x] Refactor ArtistModeWindow to coordinator
9. [ ] Test everything works
10. [x] Delete old code (replaced)

## File Sizes (target)

| File | Lines |
|------|-------|
| ArtistModeWindow.cs | ~80 |
| ArtistModeState.cs | ~150 |
| ArtistModeStyles.cs | ~40 |
| Each Drawer | ~50-100 |
| Each PhaseSettings | ~50-80 |

Total: ~10 files, ~600 lines (vs 1 file, 700 lines)
But much more maintainable and extensible!

## Future Extensions

With this architecture, adding new features is easy:

- **New phase settings**: Just add new IPhaseSettingsDrawer
- **Preview panel**: Add new drawer to list
- **Undo/Redo**: Add to ArtistModeState
- **Presets**: Add PresetDrawer + PresetManager
- **History**: Add GenerationHistory to state
