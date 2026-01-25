# Structure Visuals System - Integration Guide

## Files Created

### Runtime Components
- `Building/Runtime/Visuals/DecorationVisibilityMode.cs` - Visibility mode enum
- `Building/Runtime/Visuals/VisualsContext.cs` - Context struct for evaluation
- `Building/Runtime/Visuals/StructureDecoration.cs` - Component for decorations

### Services
- `Building/Services/Visuals/IDecorationAnimationStrategy.cs` - Animation strategy interface
- `Building/Services/Visuals/FadeAnimationStrategy.cs` - Default fade animation
- `Building/Services/Visuals/IConstructionProgressionStrategy.cs` - Progression strategy interface
- `Building/Services/Visuals/LinearProgressionStrategy.cs` - Default linear progression
- `Building/Services/Visuals/StructureVisualsModule.cs` - Main service

### Integration Changes
- `UnfinishedStructureActor.cs` - Added MarkDirty calls + ghost disable
- `Structure.cs` - Added MarkDirty calls on module/core changes

## DI Registration Required

Add to your DI container (VContainer LifetimeScope or similar):

```csharp
// Register strategies
builder.Register<IConstructionProgressionStrategy, LinearProgressionStrategy>(Lifetime.Singleton);
builder.Register<IDecorationAnimationStrategy, FadeAnimationStrategy>(Lifetime.Singleton);

// Register module
builder.RegisterEntryPoint<StructureVisualsModule>(Lifetime.Singleton);
```

Or if using VContainer's LifetimeScope component:
1. Create a new LifetimeScope GameObject in scene
2. Add component references:
   - LinearProgressionStrategy
   - FadeAnimationStrategy
   - StructureVisualsModule (as ITickable)

## Usage in Prefabs

### Step 1: Add StructureDecoration to decorations

In your foundation prefab:
```
Structure
├── Slots
├── Decorations
│   ├── Campfire (+ StructureDecoration)
│   ├── Bench_1 (+ StructureDecoration)
│   ├── Bench_2 (+ StructureDecoration)
│   └── Props (+ StructureDecoration)
└── View
```

### Step 2: Configure visibility modes

**Always visible (foundation props):**
```
StructureDecoration:
├── visibilityMode = Always
└── animate = false
```

**Construction progression (30% threshold):**
```
StructureDecoration:
├── visibilityMode = OnConstruction
├── constructionThreshold = 0.3
├── animate = true
└── fadeDuration = 0.5
```

**After core module:**
```
StructureDecoration:
├── visibilityMode = AfterCoreModule
├── animate = true
└── fadeDuration = 0.5
```

**With specific module:**
```
StructureDecoration:
├── visibilityMode = WithModule
├── requiredModuleTag = "fireplace"
├── animate = true
└── fadeDuration = 0.5
```

## Flow Examples

### Camp Construction
```
1. Player places camp → UnfinishedStructureActor created
2. Ghost visible, all decorations hidden (except Always mode)
3. Worker starts → workProgress = 0.05
   - Ghost.SetActive(false)
   - MarkDirty() → Module updates decorations
4. workProgress = 0.3
   - Bench (threshold=0.3) → FadeIn over 0.5s
5. workProgress = 1.0 → Structure completed
   - Core module built → More decorations fade in
```

### Module-Dependent Chimney
```
1. Structure built, chimney hidden
2. Player builds fireplace module
   - Structure.AssignModuleToSlots() → MarkDirty()
   - Module Tick() → chimney.ShouldBeVisible=true
   - FadeIn chimney over 0.5s
3. Fireplace removed
   - ClearModule() → MarkDirty()
   - chimney.ShouldBeVisible=false
   - FadeOut chimney
```

## Performance Notes

- **Dirty tracking:** Only updates changed structures
- **Caching:** Decorations cached per structure, no repeated GetComponentsInChildren
- **Batch processing:** All updates in single Tick()
- **Expected overhead:** ~0.1ms per 10 structures with decorations

## Testing

1. Create test structure prefab with decorations
2. Add StructureDecoration components with different modes
3. Place structure in scene
4. Start construction (assign worker)
5. Observe ghost disable and progressive decoration appearance
6. Complete construction
7. Build modules with tags
8. Observe module-dependent decorations appear/disappear

## Future Extensions

### Staged Progression
```csharp
public class StagedProgressionStrategy : IConstructionProgressionStrategy {
    public enum Stage { Foundation = 0, Walls = 1, Decorations = 2 }
    
    public float GetEffectiveProgress(float rawProgress, StructureDecoration decoration) {
        // Map decorations to stages
        // Foundation: 0-30%, Walls: 30-70%, Decorations: 70-100%
        var stage = GetDecorationStage(decoration);
        return MapProgressToStage(rawProgress, stage);
    }
}
```

### Advanced Animations
```csharp
public class ScaleFadeAnimationStrategy : IDecorationAnimationStrategy {
    public void Show(StructureDecoration decoration, float duration) {
        decoration.transform.DOScale(1f, duration).From(0f);
        // + fade materials
    }
}
```

### Custom Decoration Behavior
```csharp
public class ChimneyDecoration : StructureDecoration {
    protected override bool EvaluateCustomVisibility(VisualsContext context) {
        // Find fireplace module, position chimney above it
        var fireplace = FindModuleWithTag("fireplace");
        if (fireplace != null) {
            transform.position = fireplace.transform.position + Vector3.up * 3f;
            return true;
        }
        return false;
    }
}
```

## Troubleshooting

**Decorations not appearing:**
- Check StructureVisualsModule is registered in DI
- Check decorations have StructureDecoration component
- Check visibilityMode is set correctly
- Check threshold values (0-1 range)

**Ghost not disappearing:**
- Check UnfinishedStructureActor has _visualsModule injected
- Check AddWork is being called
- Check ghost is assigned via SetGhostView()

**Animations not working:**
- Check animate = true
- Check fadeDuration > 0
- Check materials support transparency (_Color property)
- Check DOTween is imported

**Performance issues:**
- Reduce fadeDuration for faster transitions
- Use animate = false for instant show/hide
- Limit decorations per structure (<50 recommended)
