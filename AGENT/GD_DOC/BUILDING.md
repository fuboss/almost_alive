# Building System — Smart Blueprints

> Modular base building with autonomous agent construction.

## Status: 🟡 Design Phase

---

## Core Concept

**Smart Blueprints** — игрок размещает Foundation структуры, которая содержит фиксированные слоты. Агенты заполняют слоты модулями автономно или по указанию игрока.

### Отличия от RimWorld

| RimWorld | Almost Alive |
|----------|--------------|
| Tile-by-tile placement | Structure-based placement |
| Player places each object | Player places Foundation, agents fill slots |
| Static blueprints | Living structures with expansion |
| Flat grid | 3D terrain integration |

---

## Key Entities

### Structure (Foundation)

Физическое строение, размещаемое игроком.

```
Structure
├── StructureDefinitionSO — data asset
├── Foundation — built first (walls, roof, core)
├── Slots[] — fixed positions for modules
├── Expansions[] — available upgrades
└── State: Blueprint → UnderConstruction → Built → Damaged
```

**Стены и крыша** — часть Foundation, строятся автоматически вместе с каркасом.

### Slot

Позиция внутри Structure для размещения Module.

```
Slot
├── SlotType: Sleeping | Production | Storage | Utility
├── AcceptedTags[]: какие модули можно ставить
├── State: Empty | Assigned | Built
├── AssignedModule: что здесь будет/есть
└── Priority: Low | Normal | High | Critical
```

### Module

Функциональный объект, размещаемый в Slot.

```
Module
├── ModuleDefinitionSO — data asset
├── Tags[]: bed, workbench, storage, medical...
├── Recipe: требуемые ресурсы
├── Prefab: визуал
└── SlotRequirements: какой SlotType нужен
```

---

## Player Flow

### Placing Structure

```
1. Open Build Menu → Structures
2. Select structure type (Shelter, Workshop, Barracks...)
3. See ghost preview with footprint
4. Validate terrain (flatness, obstacles, NavMesh)
5. Confirm placement
6. Foundation Blueprint appears (semi-transparent)
```

### Structure Construction

```
1. Blueprint created → agents see "Build Foundation" task
2. Haulers deliver resources to site
3. ONE Builder constructs Foundation
4. Foundation complete → walls/roof appear → slots activate
5. Structure enters "Built" state, ready for modules
```

### Module Assignment

**Autonomous (default):**
```
1. Agent evaluates personal needs + colony needs
2. Finds empty slot matching need
3. Picks appropriate module from available recipes
4. Creates Module Blueprint in slot
5. Gathers resources → builds
```

**Player-directed:**
```
1. Click on Structure → see slot overview
2. Click empty Slot → see available modules
3. Select module → set priority
4. Module Blueprint created
5. Agents build based on priority
```

### Expansion

```
1. Structure signals "expansion available" (all slots filled OR player request)
2. Player clicks Structure → Expand tab
3. Select expansion type (Wing, Floor, Patio...)
4. Ghost shows snap position
5. Confirm → Expansion Blueprint attached
6. Agents build → new slots available
```

---

## Construction Rules

| Rule | Value |
|------|-------|
| Builders per module | 1 |
| Haulers per blueprint | Unlimited |
| Foundation first | Required before any modules |
| Slot assignment | Player OR agent (configurable priority) |

---

## Structure States

```
[Blueprint] → [UnderConstruction] → [Built] → [Damaged] → [Destroyed]
                                      ↓
                                 [Expanding]
```

| State | Description |
|-------|-------------|
| Blueprint | Ghost, awaiting resources |
| UnderConstruction | Builder actively working |
| Built | Functional, slots available |
| Damaged | Partial HP, needs repair |
| Expanding | Adding new wing/floor |
| Destroyed | Gone, can rebuild |

---

## Data Structures

### StructureDefinitionSO

```csharp
[CreateAssetMenu]
public class StructureDefinitionSO : ScriptableObject {
    public string structureId;
    public Vector2Int footprint;           // 3x3, 4x4, etc.
    public GameObject foundationPrefab;    // includes walls, roof
    public RecipeData foundationRecipe;    // materials for foundation
    
    public SlotDefinition[] slots;
    public ExpansionDefinition[] expansions;
    
    public TerrainRequirements terrain;    // slope tolerance, water, etc.
}
```

### SlotDefinition

```csharp
[Serializable]
public class SlotDefinition {
    public string slotId;
    public SlotType type;                  // Sleeping, Production, Storage, Utility
    public Vector3 localPosition;
    public Quaternion localRotation;
    public string[] acceptedModuleTags;
    public bool startsLocked;              // requires upgrade to unlock
}
```

### ModuleDefinitionSO

```csharp
[CreateAssetMenu]
public class ModuleDefinitionSO : ScriptableObject {
    public string moduleId;
    public string[] tags;                  // bed, workbench, heater...
    public SlotType[] compatibleSlots;
    public RecipeData recipe;
    public GameObject prefab;
}
```

---

## UI Requirements

### Build Menu
- Category tabs: Structures | Zones (later) | Orders
- Structure preview with footprint, cost, slot overview
- Ghost placement with terrain validation feedback

### Structure Panel (on click)
- Overview: name, HP, state
- Slots grid: visual layout, filled/empty status
- Per-slot: click to assign module, set priority
- Expand button (if available)

### Module Assignment Popup
- List of compatible modules for selected slot
- Each shows: icon, name, resource cost
- Priority selector: Low/Normal/High/Critical

---

## Agent Integration

### New Beliefs
```
Structure_NeedsFoundation(structureId)
Structure_NeedsModule(structureId, slotId)
Structure_NeedsRepair(structureId)
```

### New Goals
```
goal_BuildFoundation — priority based on structure type
goal_BuildModule — priority from slot assignment
goal_RepairStructure — triggers when HP < threshold
```

### New Actions
```
action_BuildFoundation — requires: at structure, has resources
action_BuildModule — requires: foundation complete, slot empty, has resources
action_RepairStructure — requires: structure damaged, has resources
```

---

## Migration Path

### From Current System

Current:
```
CampLocation (scatter) → CampSetup (prefab) → CampSpot (slot)
```

New:
```
Player places Structure → Foundation built → Slots available
```

**CampSetup → StructureDefinitionSO**: convert existing setups to structure definitions
**CampSpot → SlotDefinition**: map preferredTags to SlotType + acceptedModuleTags
**RecipeSO**: keep as-is, link to ModuleDefinitionSO

### Transition Strategy

1. Keep CampLocation for AI-only test scenarios
2. Add Structure system in parallel
3. Player uses Structures, AI can use either
4. Eventually deprecate CampLocation

---

## Resolved Questions

### Expansion Snapping
**Decision:** Predefined snap points.

StructureDefinitionSO contains explicit `snapPoints[]` — positions + directions where expansions attach. Gives control over visuals, simplifies validation, avoids wall-merging edge cases.

### Interior vs Exterior Slots
**Decision:** `isInterior` flag in SlotDefinition.

```csharp
public class SlotDefinition {
    // ...
    public bool isInterior;  // true = under roof, protected
}
```

- Interior: protected from weather, uses structure temperature
- Exterior: exposed, ambient temperature
- Some modules require interior (bed, medical), some prefer exterior (campfire, drying rack)
- Weather effects deferred to temperature system implementation

### Structure Ownership
**Decision:** Hybrid — shared by default, assignable.

```csharp
public class Module {
    public IGoapAgent owner;  // null = colony-shared
    
    public bool CanUse(IGoapAgent agent) {
        return owner == null || owner == agent;
    }
}
```

- Default: any colonist can use
- Player or agent can assign ownership (bed → "John's bed")
- Owner has priority, others can use if owner not claiming
- Mood impact: "slept in own bed" vs "slept in someone's bed"

### Deconstruction
**Decision:** Explicit player action, 50% resource return.

**Module deconstruction:**
1. Player marks module for deconstruction
2. Agent deconstructs
3. Returns 50% resources (configurable per ModuleDefinitionSO)
4. Slot becomes Empty

**Structure deconstruction:**
1. Player marks structure
2. All modules deconstructed first (queued)
3. Foundation deconstructed last
4. Footprint freed

**Damaged items:** return proportional to HP (50% HP = 25% resources)

---

## Development Plan

### Phase 1: Data Foundation
**Goal:** Core data structures, no runtime yet.

```
1.1 StructureDefinitionSO
    - scriptable object
    - footprint, foundationPrefab placeholder
    - slots[] as SlotDefinition[]
    - foundationRecipe (reuse existing RecipeData)

1.2 SlotDefinition
    - serializable class
    - slotId, slotType enum, localPosition/rotation
    - acceptedModuleTags[], isInterior, startsLocked

1.3 ModuleDefinitionSO
    - scriptable object
    - moduleId, tags[], compatibleSlotTypes[]
    - recipe (RecipeData), prefab reference
    - deconstructReturnPercent (default 0.5f)

1.4 SlotType enum
    - Sleeping, Production, Storage, Utility
    - extensible later

1.5 Create test assets
    - StructureDefinition_BasicShelter.asset
    - ModuleDefinition_Bedroll.asset, _Chest.asset
```

### Phase 2: Runtime Structure
**Goal:** Structures exist in world, no construction yet.

```
2.1 Structure (MonoBehaviour)
    - reference to StructureDefinitionSO
    - runtime slots[] (Slot instances)
    - state machine: Blueprint → UnderConstruction → Built → Damaged
    - HP, damage/repair interface

2.2 Slot (class, not MonoBehaviour)
    - runtime state: Empty, Assigned, Built
    - assignedModuleDef (what should be here)
    - builtModule (actual instance)
    - priority enum
    - owner (IGoapAgent, nullable)

2.3 Module (MonoBehaviour)
    - reference to ModuleDefinitionSO
    - owner (IGoapAgent, nullable)
    - HP for damage system
    - CanUse(agent) check

2.4 StructureRegistry
    - static Registry<Structure> pattern (like CampLocation)
    - queries: GetAll(), GetByState(), GetNeedingWork()

2.5 Test: spawn Structure via code, verify slots initialize
```

### Phase 3: Placement System
**Goal:** Player can place structure blueprints.

```
3.1 StructurePlacementController
    - enters placement mode with selected StructureDefinitionSO
    - spawns ghost preview (semi-transparent)
    - follows cursor, snaps to grid

3.2 Placement Validation
    - terrain check: flatness tolerance, no water
    - obstacle check: no overlap with existing structures
    - navmesh check: doesn't block critical paths
    - visual feedback: green valid, red invalid

3.3 Placement Confirmation
    - click to confirm
    - spawns Structure in Blueprint state
    - ghost becomes real (but transparent/unbuilt visual)

3.4 Input integration
    - ESC cancels placement
    - rotation hotkey (R?)
```

### Phase 4: Construction Flow
**Goal:** Agents build structures and modules.

```
4.1 StructureConstructionSite (component on Structure)
    - tracks required resources for foundation
    - tracks delivered resources
    - progress 0-1

4.2 Resource Delivery
    - Structure in Blueprint state → agents haul resources
    - reuse existing hauling system if possible
    - delivered resources stored at site

4.3 Foundation Building
    - new GOAP action: action_BuildFoundation
    - precondition: resources delivered, structure in Blueprint state
    - effect: structure → Built state
    - single builder, progress over time

4.4 Module Building
    - slot assigned (by player or agent) → module blueprint
    - new GOAP action: action_BuildModule
    - precondition: foundation complete, slot resources delivered
    - effect: module spawned, slot → Built

4.5 Beliefs & Goals
    - belief: Structure_NeedsFoundation, Structure_NeedsModule
    - goal: goal_BuildStructure (foundation + modules)
    - utility evaluator: colony needs vs current structures
```

### Phase 5: Player Module Assignment
**Goal:** Player can assign modules to slots.

```
5.1 Structure Selection
    - click structure → select it
    - show structure info panel

5.2 Slot Visualization
    - highlight slots on selected structure
    - color code: empty (yellow), assigned (blue), built (green)

5.3 Assignment UI
    - click slot → popup with compatible modules
    - show module: icon, name, cost
    - select → slot.AssignModule(def, priority)

5.4 Priority Control
    - dropdown or buttons: Low/Normal/High/Critical
    - affects agent task selection
```

### Phase 6: Agent Autonomous Building
**Goal:** Agents decide what to build without player input.

```
6.1 Need Evaluation
    - agent checks: do I have bed? does colony have storage?
    - maps needs to module tags

6.2 Slot Selection
    - find structures with empty compatible slots
    - prefer slots in own "home" structure

6.3 Auto-Assignment
    - if slot empty and agent decides to build
    - agent assigns module themselves
    - lower priority than player-assigned

6.4 Balancing Autonomy
    - config: allow agent auto-build? (per structure or global)
    - player can lock slots to prevent auto-assignment
```

### Phase 7: Ownership & Usage
**Goal:** Modules can be owned, affects AI decisions.

```
7.1 Ownership Assignment
    - player assigns via UI
    - agent claims on first use (bed)

7.2 Usage Priority
    - owner always has priority
    - non-owners use only if owner not claiming

7.3 Mood Integration
    - "slept in own bed" mood buff
    - "slept in stranger's bed" mood debuff (mild)
    - integrate with existing mood system
```

### Phase 8: Deconstruction
**Goal:** Player can remove modules and structures.

```
8.1 Mark for Deconstruction
    - player selects module/structure
    - "Deconstruct" button
    - visual indicator (X overlay or tint)

8.2 Deconstruction Action
    - new GOAP action: action_Deconstruct
    - agent goes to site, deconstructs over time

8.3 Resource Return
    - on complete: spawn resources at site
    - amount = base * HP% * returnPercent

8.4 Structure Deconstruction
    - marks all modules for deconstruct first
    - foundation last
    - frees footprint when done
```

### Phase 9: Expansion System
**Goal:** Structures can be expanded with new wings.

```
9.1 ExpansionDefinition
    - add to StructureDefinitionSO
    - snapPointIndex, additional slots[], recipe

9.2 Expansion UI
    - "Expand" button on structure panel
    - shows available expansions with costs

9.3 Expansion Placement
    - ghost preview at snap point
    - confirm → creates expansion blueprint

9.4 Expansion Construction
    - same flow as foundation
    - on complete: new slots added to structure
```

### Phase 10: Polish & Integration
**Goal:** System feels complete, edge cases handled.

```
10.1 Visual Polish
    - construction progress visuals
    - slot highlight effects
    - damaged state visuals

10.2 Audio
    - construction sounds
    - completion jingle

10.3 Save/Load
    - serialize structure state, slot assignments, module HP
    - rebuild on load

10.4 Migration
    - convert existing CampSetup prefabs to StructureDefinitions
    - deprecate CampLocation spawn system

10.5 Debug Tools
    - inspector for structure state
    - quick-build cheat
    - slot/module visualizer
```

---

## References

- [CAMP.md](../Assets/Content/Scripts/Docs/CAMP.md) — current camp system
- [RecipeSO](../Assets/Content/Scripts/AI/Craft/RecipeSO.cs) — existing recipe system

---

*Last updated: Session with GD*
