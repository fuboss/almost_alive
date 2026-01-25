# Resources & Prefabs Quick Reference

## Core Prefabs

### DI / Scope
- **GameScope.prefab** - `Assets/Resources/GameScope.prefab`
  - Root VContainer LifetimeScope
  - All service registrations
  - Script: `GameScope.cs`

### Building
- **Structure Ghost Material** - Assigned in GameScope
  - Used for placement preview
  - Keyed as "ghostMaterial"

## ScriptableObject Configs

### Building
- **BuildingManagerConfigSO** - Registered in GameScope
  - Building system configuration
  - Cell size, placement rules

- **StructureDefinitionSO** - Asset files
  - Location: `Assets/Content/Data/Buildings/Structures/`
  - Defines structure blueprints (footprint, walls, slots)
  
- **ModuleDefinitionSO** - Asset files
  - Location: `Assets/Content/Data/Buildings/Modules/`
  - Defines modules that go in structure slots

### Environment
- **EnvironmentSetupSO** - Registered in GameScope
  - World environment configuration

### Camera
- **CameraSettingsSO** - Registered in GameScope
  - Camera behavior settings

## UI Layers

Registered in GameScope.uiLayers:
- Created under "UI Root" GameObject
- DontDestroyOnLoad
- Singleton lifetime

## Addressables Labels

### Building Parts
- **StructureParts** - Group for wall/support prefabs
  - Solid walls
  - Doorway walls
  - Passage walls
  - Support pillars

### Structures (TODO)
- Label/group for structure foundation prefabs
- Tagged with StructureTag component

## Prefab Components

### Structure Foundation
- **StructureTag** - Marks foundation, defines footprint
- **StructureFoundationBuilder** - Editor tool for slot setup
- **NavMeshSurface** - For interior pathfinding
- **StructureDecoration** - On decorations for visibility control

### Building Parts
- **StructurePartDescription** - On wall/support prefabs
  - WallSegmentType (Solid, Doorway, Passage)
  - Part metadata

## Key Paths

```
Assets/
├── Resources/
│   └── GameScope.prefab          ✅ Root DI scope
├── Content/
│   ├── Data/
│   │   └── Buildings/
│   │       ├── Structures/       📁 StructureDefinitionSO files
│   │       └── Modules/          📁 ModuleDefinitionSO files
│   └── Scripts/
│       └── GameScope.cs          ✅ DI registration script
```

## Update Checklist

When adding new resources:
- [ ] Add to appropriate section above
- [ ] Note Addressable label if applicable
- [ ] Document component requirements
- [ ] Update PROJECT_INDEX.md if major change
