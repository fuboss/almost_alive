# Building System — Refactoring Plan: Unify with Actor System

> Goal: Make Structure and UnfinishedStructure part of Actor ecosystem for unified GOAP handling.

## Problem Statement

Current architecture has parallel hierarchies:
- **Actors**: ActorDescription + tags + transientTarget + ActorRegistry
- **Structures**: Structure + UnfinishedStructure + separate Registry

This causes:
1. **Belief duplication** — `UnfinishedNeedsResourcesBelief` vs `UnfinishedStructureNeedsResourcesBelief`
2. **Strategy duplication** — `DeliverToUnfinishedStrategy` vs `DeliverToStructureStrategy`
3. **TransientTarget incompatibility** — can't use existing tag-based beliefs for structures
4. **Camp coupling** — beliefs tightly coupled to ICampAgent instead of generic queries

---

## Proposed Solution

### Core Principle
**Structure IS-A Actor**. UnfinishedStructure IS-A UnfinishedActor with structural metadata.

### Phase R1: Deprecate Camp System ✅

Mark as `[Obsolete]`:
- `ICampAgent` interface
- `CampLocation`, `CampSetup`, `CampSpot`, `CampModule`
- `AI/GOAP/Beliefs/Camp/*`
- `AI/GOAP/Strategies/Camp/*`
- `AgentCampData`

**Migration**: Agent ownership → Memory-based structure ownership (see R4).

### Phase R2: Structure as Actor

#### R2.1 Add ActorDescription to Structure
```csharp
[RequireComponent(typeof(ActorDescription))]
public class Structure : MonoBehaviour {
  // Structure-specific data (walls, slots, entryPoints)
  // ActorDescription provides: actorKey, tags, WorldGrid registration
}
```

**Tags for Structure**:
- `"structure"` — base tag
- `"shelter"` — provides shelter
- `"production"` — has production slots
- etc.

#### R2.2 Reuse UnfinishedActor for structures
Instead of separate `UnfinishedStructure`, use `UnfinishedActor` with `StructureRecipeSO`:

```csharp
// New: StructureRecipeSO extends RecipeSO
[CreateAssetMenu(menuName = "Building/Structure Recipe")]
public class StructureRecipeSO : RecipeSO {
  public StructureDefinitionSO structureDefinition;
  // recipe.resultActorKey → structure prefab with Structure + ActorDescription
}
```

**UnfinishedActor changes**:
- Already has: inventory, progress, tags
- Add: optional `StructureDefinitionSO` reference (via recipe or direct)
- On complete: spawn Structure prefab (like any other actor)

### Phase R3: Unified Queries

#### R3.1 Merge UnfinishedQuery + UnfinishedStructureQuery
```csharp
public static class UnfinishedQuery {
  // Existing methods work with ALL unfinished (actors + structures)
  public static UnfinishedActor GetNeedingResources(Vector3? nearPos = null);
  public static UnfinishedActor GetNeedingWork(Vector3? nearPos = null);
  
  // New: filter by tag
  public static UnfinishedActor GetNeedingResourcesWithTag(string tag, Vector3? nearPos = null);
  public static IEnumerable<UnfinishedActor> GetAllWithTag(string tag);
}
```

#### R3.2 Remove camp-based queries
All queries become global or position-based, not camp-scoped.

### Phase R4: Ownership System

Replace `ICampAgent.camp` with memory-based ownership:

```csharp
// Agent stores owned structures in memory
public interface IOwnershipAgent {
  IEnumerable<ActorDescription> GetOwnedActors(string tag = null);
  void ClaimOwnership(ActorDescription actor);
  void ReleaseOwnership(ActorDescription actor);
}

// Memory key
public static class OwnershipKeys {
  public const string OWNED_STRUCTURES = "owned_structures";  // List<int> actorIds
}
```

**Ownership queries**:
```csharp
// "Agent owns at least one structure"
agent.GetOwnedActors("structure").Any()

// "Agent owns shelter"
agent.GetOwnedActors("shelter").Any()
```

### Phase R5: Unified Beliefs

#### Delete (replaced by generic versions):
- `UnfinishedStructureNeedsResourcesBelief`
- `UnfinishedStructureNeedsWorkBelief`
- `AgentHasResourceForStructureBelief`
- All camp-specific beliefs

#### Modify existing beliefs:
```csharp
// Before: camp-scoped
public class UnfinishedNeedsResourcesBelief : AgentBelief {
  if (agent is not ICampAgent campAgent) return () => false;
  return () => UnfinishedQuery.GetNeedingResources(campAgent.camp) != null;
}

// After: global or tag-filtered
public class UnfinishedNeedsResourcesBelief : AgentBelief {
  [Optional] public string filterTag;  // e.g., "structure" or null for all
  
  return () => UnfinishedQuery.GetNeedingResources(filterTag) != null;
}
```

#### New generic beliefs:
```csharp
// True when agent owns any actor with tag
public class OwnsActorWithTagBelief : AgentBelief {
  public string tag;  // "structure", "shelter", etc.
}

// True when any unfinished with tag needs resources
public class UnfinishedWithTagNeedsResourcesBelief : AgentBelief {
  public string tag;
}
```

### Phase R6: Unified Strategies

#### Merge strategies:
- `DeliverToUnfinishedStrategy` + `DeliverToStructureStrategy` → single `DeliverToUnfinishedStrategy`
- `WorkOnUnfinishedStrategy` + `WorkOnStructureStrategy` → single `WorkOnUnfinishedStrategy`

Strategies use:
- `transientTarget` if set (specific target)
- Global query if no transient (find nearest)

### Phase R7: Structure Completion

When `UnfinishedActor` with structure recipe completes:
1. Spawn Structure prefab (has ActorDescription + Structure components)
2. `StructuresModule.OnStructureSpawned()` — builds walls, slots, entries
3. Structure is now a regular actor in the world

```csharp
// In UnfinishedActor.TryComplete():
if (recipe is StructureRecipeSO structureRecipe) {
  // Spawn structure prefab
  var structure = _actorCreation.TrySpawnActor(recipe.resultActorKey, pos, out var actor);
  // StructuresModule hooks into spawn event to build
}
```

---

## Migration Checklist

### Phase R1: Deprecate Camp
- [ ] Add `[Obsolete]` to ICampAgent, CampLocation, CampSetup, CampSpot
- [ ] Add `[Obsolete]` to Camp beliefs and strategies
- [ ] Remove ICampAgent from GOAPAgent (keep property for migration)

### Phase R2: Structure as Actor
- [ ] Add `[RequireComponent(typeof(ActorDescription))]` to Structure
- [ ] Create structure prefabs with ActorDescription + Structure
- [ ] Create `StructureRecipeSO` or use convention (recipe.resultActorKey → structure)
- [ ] Update StructuresModule.PlaceBlueprint to create UnfinishedActor

### Phase R3: Unified Queries
- [ ] Refactor UnfinishedQuery — remove camp dependency
- [ ] Add tag-based filtering
- [ ] Delete UnfinishedStructureQuery

### Phase R4: Ownership System
- [ ] Create IOwnershipAgent interface
- [ ] Implement in GOAPAgent using memory
- [ ] Create ownership beliefs

### Phase R5: Unified Beliefs
- [ ] Refactor camp beliefs to use tags/ownership
- [ ] Delete duplicate structure beliefs
- [ ] Add filterTag to existing beliefs

### Phase R6: Unified Strategies
- [ ] Refactor DeliverToUnfinishedStrategy — use global query
- [ ] Refactor WorkOnUnfinishedStrategy — handle structure completion
- [ ] Delete duplicate structure strategies

### Phase R7: Integration
- [ ] Update GOAP feature sets
- [ ] Test full flow: place → deliver → work → complete
- [ ] Remove deprecated code after verification

---

## Benefits

1. **Single codebase** — one set of beliefs/strategies for all construction
2. **TransientTarget works** — structures are actors, can use tag-based beliefs
3. **Flexible ownership** — agent owns list of structures, not single camp
4. **Global queries** — any agent can help build any structure
5. **Future-proof** — modules, vehicles, etc. all follow same pattern

---

## Questions to Resolve

1. **Ghost visual** — keep on UnfinishedActor or move to StructuresModule?
2. **Structure prefab workflow** — Addressable with ActorDescription + Structure?
3. **Ownership persistence** — save/load owned structure IDs?
4. **Multiple owners** — can structure have multiple owners (colony)?

---

*Created: Phase 4 GOAP Integration*
