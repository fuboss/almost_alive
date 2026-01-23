# Camp System

## Components

| Component | Description |
|-----------|-------------|
| CampLocation | Scene marker, registry |
| CampSetup | Prefab with CampSpot[] |
| CampSpot | Build spot with preferredTags[] |
| CampModule | VContainer, Addressables |

---

## Flow

```
1. Find unclaimed CampLocation
2. agent.TryClaim(location)
3. CampModule.InstantiateRandomSetup(location)
4. Store in persistentMemory["personal_camp"]
5. Build at CampSpots via recipes
```

---

## Access

```csharp
agent.camp         // CampLocation or null
agent.campData     // AgentCampData (cached resources)
camp.setup         // CampSetup
setup.GetEmptySpot(tag)
```

---

## Beliefs

```
HasCamp, NeedsCamp
CampNeedsBuilding, CampFullyBuilt
CampHasModule (moduleTag)
```

---

## TODO

- [ ] ClaimCamp, BuildAtCamp actions
- [ ] EstablishCamp, BuildCamp goals
- [ ] Camp_FeatureSet.asset
