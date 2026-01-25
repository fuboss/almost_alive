# Structure Visuals Management System — Design Document v2

## Architecture Overview

**Centralized Module Approach:**
- `StructureVisualsModule` — singleton service, обрабатывает все структуры из ActorRegistry
- `StructureDecoration` — component на декорациях, содержит visibility rules
- **Strategy Pattern** для construction progression и animations
- **Event-driven** updates (module built, work progress changed)

## Core Components

### 1. StructureVisualsModule (Service)

**Responsibilities:**
- Subscribe to work progress events from UnfinishedStructureActor
- Subscribe to module built events from Structure
- Batch update visuals for all structures
- Manage animation strategies
- Track dirty structures for optimized updates

```csharp
public class StructureVisualsModule : ITickable {
    // Strategy patterns
    private IConstructionProgressionStrategy _progressionStrategy;
    private IDecorationAnimationStrategy _animationStrategy;
    
    // Dirty tracking for optimization
    private HashSet<Structure> _dirtyStructures;
    private HashSet<UnfinishedStructureActor> _dirtyUnfinished;
    
    // Caches
    private Dictionary<Structure, StructureDecoration[]> _structureDecorations;
    private Dictionary<UnfinishedStructureActor, StructureDecoration[]> _unfinishedDecorations;
    
    // Public API
    public void MarkDirty(Structure structure);
    public void MarkDirty(UnfinishedStructureActor unfinished);
    public void RefreshAll();
    
    // Tick processing
    public void Tick();
}
```

### 2. StructureDecoration (Component)

**Responsibilities:**
- Define visibility conditions
- Cache renderers
- Apply visibility changes via animation strategy

```csharp
public enum DecorationVisibilityMode {
    Always,              // Always visible
    OnConstruction,      // Show at construction threshold
    AfterCoreModule,     // Show after core module built
    WithModule,          // Show when specific module installed
    Custom               // Custom evaluation logic
}

public class StructureDecoration : MonoBehaviour {
    [Title("Visibility")]
    public DecorationVisibilityMode visibilityMode = DecorationVisibilityMode.Always;
    
    [ShowIf("visibilityMode", DecorationVisibilityMode.OnConstruction)]
    [Range(0f, 1f)]
    public float constructionThreshold = 0f;
    
    [ShowIf("visibilityMode", DecorationVisibilityMode.WithModule)]
    public string requiredModuleTag;
    
    [Title("Animation")]
    public bool animate = true;
    public float fadeDuration = 0.5f;
    
    // Runtime state
    private bool _isVisible;
    private Renderer[] _renderers;
    
    // Evaluation
    public bool ShouldBeVisible(VisualsContext context);
    
    // Visibility control (called by Module)
    public void SetVisible(bool visible, IDecorationAnimationStrategy animStrategy);
}
```

### 3. VisualsContext (Data)

```csharp
public struct VisualsContext {
    public float constructionProgress;    // 0-1 from UnfinishedStructureActor
    public bool isCoreBuilt;
    public HashSet<string> installedModuleTags;
    public bool isUnfinished;             // true = under construction
    
    public static VisualsContext ForStructure(Structure structure);
    public static VisualsContext ForUnfinished(UnfinishedStructureActor unfinished, float progress);
}
```

## Strategy Patterns

### Construction Progression Strategy

**Purpose:** Define how construction progress maps to decoration visibility.

```csharp
public interface IConstructionProgressionStrategy {
    /// <summary>
    /// Calculate effective progress for decoration threshold check.
    /// </summary>
    float GetEffectiveProgress(float rawProgress, StructureDecoration decoration);
}

// Default: Linear 0-1
public class LinearProgressionStrategy : IConstructionProgressionStrategy {
    public float GetEffectiveProgress(float rawProgress, StructureDecoration decoration) {
        return rawProgress;
    }
}

// Future: Staged progression
public class StagedProgressionStrategy : IConstructionProgressionStrategy {
    public enum Stage { Foundation, Walls, Decorations }
    
    [Serializable]
    public class DecorationStageMapping {
        public Stage stage;
        public float stageStartProgress;  // 0.7 = starts at 70%
        public float stageEndProgress;    // 1.0 = ends at 100%
    }
    
    public float GetEffectiveProgress(float rawProgress, StructureDecoration decoration) {
        // Map decoration to stage, return progress within that stage
        // Can be configured via SO or decoration component
        return rawProgress; // Simplified
    }
}
```

### Decoration Animation Strategy

**Purpose:** Define how decorations appear/disappear.

```csharp
public interface IDecorationAnimationStrategy {
    void Show(StructureDecoration decoration, float duration);
    void Hide(StructureDecoration decoration, float duration);
}

// Default: Fade only
public class FadeAnimationStrategy : IDecorationAnimationStrategy {
    public void Show(StructureDecoration decoration, float duration) {
        // DOTween fade alpha 0->1
        var renderers = decoration.renderers;
        foreach (var renderer in renderers) {
            renderer.material.DOFade(1f, duration);
        }
    }
    
    public void Hide(StructureDecoration decoration, float duration) {
        // DOTween fade alpha 1->0
        var renderers = decoration.renderers;
        foreach (var renderer in renderers) {
            renderer.material.DOFade(0f, duration);
        }
    }
}

// Future: Scale + Fade
public class ScaleFadeAnimationStrategy : IDecorationAnimationStrategy {
    public void Show(StructureDecoration decoration, float duration) {
        // DOTween: scale 0->1 + alpha 0->1 simultaneously
        decoration.transform.DOScale(1f, duration).From(0f);
        // + fade
    }
}
```

## Integration Points

### UnfinishedStructureActor

```csharp
public class UnfinishedStructureActor : UnfinishedActorBase {
    [Inject] private StructureVisualsModule _visualsModule;
    
    protected override void OnWorkProgressChanged() {
        base.OnWorkProgressChanged();
        
        // Disable ghost immediately on first work
        if (workProgress > 0 && _ghostView != null && _ghostView.activeSelf) {
            _ghostView.SetActive(false);
        }
        
        // Mark dirty for visuals update
        _visualsModule?.MarkDirty(this);
    }
}
```

### Structure

```csharp
public class Structure : MonoBehaviour {
    [Inject] private StructureVisualsModule _visualsModule;
    
    public void OnModuleBuilt(Module module) {
        _visualsModule?.MarkDirty(this);
    }
    
    public void SetCoreBuilt(bool value) {
        _isCoreBuilt = value;
        _visualsModule?.MarkDirty(this);
    }
}
```

## File Structure

```
Building/Runtime/Visuals/
├── StructureDecoration.cs
├── VisualsContext.cs
└── DecorationVisibilityMode.cs

Building/Services/Visuals/
├── StructureVisualsModule.cs
├── IConstructionProgressionStrategy.cs
├── LinearProgressionStrategy.cs
├── IDecorationAnimationStrategy.cs
└── FadeAnimationStrategy.cs
```

## Implementation Plan

### Phase 1: Core (согласуем сначала)
1. `DecorationVisibilityMode.cs` — enum
2. `VisualsContext.cs` — struct
3. `StructureDecoration.cs` — component with basic visibility logic
4. `IDecorationAnimationStrategy.cs` + `FadeAnimationStrategy.cs`
5. `IConstructionProgressionStrategy.cs` + `LinearProgressionStrategy.cs`
6. `StructureVisualsModule.cs` — centralized service

### Phase 2: Integration
1. Inject StructureVisualsModule in UnfinishedStructureActor
2. Inject StructureVisualsModule in Structure
3. Add OnWorkProgressChanged hook
4. Add OnModuleBuilt hook
5. Ghost disable on workProgress > 0

### Phase 3: Testing & Polish
1. Test linear construction progression
2. Test module-dependent visibility
3. Test core-dependent visibility
4. Add Odin attributes for better Inspector

### Phase 4: Advanced (опционально)
1. Staged progression strategy
2. Scale+fade animation strategy
3. Custom decoration behaviors (chimney positioning)

## Scenarios

### Scenario 1: Construction Animation (Camp)

**Setup:**
```
Bench (StructureDecoration):
├── visibilityMode = OnConstruction
├── constructionThreshold = 0.3
├── animate = true
├── fadeDuration = 0.5s
```

**Behavior:**
- workProgress 0-29%: invisible
- workProgress 30%: Module calls MarkDirty() → Tick() → ShouldBeVisible=true → FadeIn 0.5s
- workProgress 30-100%: visible

### Scenario 2: Chimney for Fireplace

**Setup:**
```
Chimney (StructureDecoration):
├── visibilityMode = WithModule
├── requiredModuleTag = "fireplace"
├── animate = true
```

**Behavior:**
- OnModuleBuilt("fireplace") → MarkDirty() → ShouldBeVisible=true → FadeIn
- If fireplace removed → ShouldBeVisible=false → FadeOut

### Scenario 3: Camp Core-Dependent

**Setup:**
```
CampProps (StructureDecoration):
├── visibilityMode = AfterCoreModule
├── animate = false
```

**Behavior:**
- Hidden until SetCoreBuilt(true)
- Show immediately (no animation)

## Performance

- **Dirty tracking:** Only update changed structures, не каждый фрейм
- **Caching:** Decorations cached per structure, не GetComponentsInChildren каждый раз
- **Batch processing:** Все updates в одном Tick()
- **Expected overhead:** ~0.1ms per 10 structures with decorations

## Questions Resolved

1. ✅ **Animation:** Fade-only, extensible via IDecorationAnimationStrategy
2. ✅ **Construction staging:** Linear now, strategy pattern for future staged
3. ✅ **Ghost:** Disable сразу при workProgress > 0

---

**Ready for implementation?** Согласен с архитектурой или нужны правки?
