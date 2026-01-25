# DI Registration - TO BE DOCUMENTED

## Current Status: UNKNOWN

The project uses VContainer for dependency injection, but the registration location is not documented.

## What We Know

Services use `[Inject]` attribute:
```csharp
public class StructureConstructionService {
    [Inject] private StructurePlacementService _placement;
}
```

## What We Need

**Please update this file with:**

1. **Root Scope Location**
   - File path to main LifetimeScope or ProjectContext
   - Scene name where it lives

2. **Registration Pattern**
   - Example of how services are registered
   - Code snippet showing Configure method

3. **New Service Registration**
   - Step-by-step instructions
   - Example for StructureVisualsModule

## Temporary Instructions

For StructureVisualsModule registration, add to your DI Configure method:

```csharp
// Strategies
builder.Register<IConstructionProgressionStrategy, LinearProgressionStrategy>(Lifetime.Singleton);
builder.Register<IDecorationAnimationStrategy, FadeAnimationStrategy>(Lifetime.Singleton);

// Module (ITickable)
builder.RegisterEntryPoint<StructureVisualsModule>(Lifetime.Singleton);
```

---

**TODO:** Replace this file with actual DI documentation once location is confirmed.
