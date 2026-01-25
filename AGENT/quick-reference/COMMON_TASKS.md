# Common Tasks - Quick Reference

## DI / Services

### Add New Service

**1. Create service class:**
```csharp
// Building/Services/MyService.cs
using VContainer;

namespace Content.Scripts.Building.Services {
  public class MyService {
    [Inject] private OtherService _other;
    
    public void DoSomething() {
      // Implementation
    }
  }
}
```

**2. Register in GameScope.cs:**
```csharp
// In appropriate section (Building, AI, etc)
builder.Register<MyService>(Lifetime.Singleton).AsSelf();
```

**3. Inject where needed:**
```csharp
public class Consumer {
  [Inject] private MyService _myService;
}
```

### Add ITickable Service

**1. Implement ITickable:**
```csharp
using VContainer.Unity;

public class MyModule : ITickable {
  public void Tick() {
    // Called every frame
  }
}
```

**2. Register as EntryPoint:**
```csharp
builder.RegisterEntryPoint<MyModule>().AsSelf();
// Or with interfaces:
builder.Register<MyModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
```

### Add Keyed Dependency

**1. Register with key:**
```csharp
builder.RegisterInstance(myMaterial).Keyed("myKey").AsSelf();
```

**2. Inject with key:**
```csharp
[Inject] [Key("myKey")] private Material _material;
```

## Building System

### Create New StructureDefinitionSO

**1. Right-click in Project:**
`Create > Building > Structure Definition`

**2. Configure:**
- `structureId` - unique identifier
- `structureType` - Enclosed or Open
- `footprint` - grid size (e.g. 3x3)
- `foundationPrefab` - prefab with StructureTag
- `slots` - copy from prefab or manual setup
- `coreModule` - required module (optional)

**3. Assign prefabs:**
- `solidWallPrefab` - from StructureParts addressables
- `doorwayWallPrefab` - for entries
- `passageWallPrefab` - for expansions
- `stairsPrefab` - for terrain height differences

### Create Foundation Prefab

**1. Create GameObject hierarchy:**
```
FoundationPrefab
├── Visual (mesh/model)
├── Slots (empty GOs at slot positions)
│   ├── Slot_Interior_0
│   ├── Slot_Exterior_0
│   └── ...
├── Decorations (for Open structures)
│   ├── Campfire (+ StructureDecoration)
│   └── Props (+ StructureDecoration)
└── View (+ NavMeshSurface)
```

**2. Add StructureTag component:**
- Set footprint to match definition
- Set entryDirections

**3. Add StructureFoundationBuilder (Editor only):**
- Auto-scan slots from hierarchy
- Set slot types/tags

**4. For Open structures:**
- Add StructureDecoration to decoration GOs
- Configure visibility modes

### Add Decoration to Structure

**1. Add GameObject in foundation:**
```
Decorations/
└── MyDecoration (+ StructureDecoration)
    └── Visual (mesh/model)
```

**2. Configure StructureDecoration:**
- `visibilityMode`:
  - Always - always visible
  - OnConstruction - show at progress threshold
  - AfterCoreModule - show after core built
  - WithModule - show when module with tag installed
- `constructionThreshold` - 0-1 (for OnConstruction)
- `requiredModuleTag` - tag string (for WithModule)
- `animate` - fade in/out
- `fadeDuration` - animation time

**3. Test in Play mode:**
- Use inspector buttons: "Test Show" / "Test Hide"

## Prefabs & Addressables

### Add Wall Prefab

**1. Create prefab:**
```
WallSegment
├── Visual (1x3m mesh)
└── StructurePartDescription component
```

**2. Configure StructurePartDescription:**
- `wallType` - Solid, Doorway, or Passage
- `description` - shown in dropdown

**3. Add to Addressables:**
- Group: StructureParts
- Label: StructureParts

**4. Reference in StructureDefinitionSO:**
- Dropdown will auto-populate from group

### Create Module Prefab

**1. Create prefab:**
```
ModulePrefab
├── Visual
└── Module component
```

**2. Create ModuleDefinitionSO:**
- `moduleId` - unique identifier
- `slotFootprint` - grid size (1x1, 2x1, etc)
- `tags` - for decoration dependencies
- `clearanceRadius` - spacing from other modules

**3. Assign in StructureDefinitionSO:**
- Set as coreModule if required

## Testing

### Test Structure Placement

**1. Play mode**
**2. Find terrain**
**3. Create structure:**
```csharp
// Via ActorCreationModule
_actorCreation.TrySpawnActor("structure_id", position, out var result);
```

### Test Construction Progress

**1. Get UnfinishedStructureActor**
**2. Call AddWork:**
```csharp
unfinished.AddWork(10f); // Adds 10 units of work
```
**3. Observe decorations appearing at thresholds**

### Test Module Placement

**1. Get built structure**
**2. Find compatible slot:**
```csharp
var slots = structure.FindSlotsForModule(moduleDef);
```
**3. Assign module:**
```csharp
structure.AssignModuleToSlots(moduleDef);
```

## Debug

### Check DI Registration

**1. Play mode**
**2. Check console for VContainer errors**
**3. Verify injection:**
```csharp
Debug.Log(_injectedService != null); // Should be true
```

### Check Addressables

**1. Window > Asset Management > Addressables > Groups**
**2. Verify asset in correct group**
**3. Build > New Build > Default Build Script**

### Check Decorations

**1. Play mode**
**2. Select structure in hierarchy**
**3. Expand Decorations folder**
**4. Check StructureDecoration inspector:**
- `isVisible` - current state
- Use Test Show/Hide buttons

## Performance

### Profile Building System

**1. Window > Analysis > Profiler**
**2. Look for:**
- `StructureVisualsModule.Tick()`
- `StructureConstructionService.BuildStructure()`
**3. Optimize:**
- Reduce decorations per structure
- Use animate=false for instant show
- Batch structure placements

### Profile NavMesh

**1. Check NavMeshSurface.BuildNavMesh() time**
**2. Reduce voxel size if slow**
**3. Use async building if available**
