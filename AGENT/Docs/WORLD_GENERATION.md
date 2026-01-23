# World Generation

## Pipeline

```
Seed → WorldRandom → VoronoiGenerator → BiomeMap
→ TerrainSculptor → Heightmap
→ TerrainFeatureMap → Edge detection
→ SplatmapPainter → Textures
→ VegetationPainter → Grass/bushes
→ Scatter Spawning → Actors
```

Config: `Resources/Environment/WorldGeneratorConfig`

---

## Biomes

Types: Forest, Meadow, Lake, Desert, Hills

BiomeSO: textures, height params, scatters, vegetation

---

## Scatters

**ScatterRuleSO:** actorKey, density, minSpacing, clusterSize, slopeRange, heightRange, childScatters

**ScatterPlacement:** Any, FlatOnly, CliffEdge, CliffBase, Valley

**TerrainFeatureMap:** Sobel edge detection for cliff-aware placement

---

## Vegetation

Unity Terrain Details for grass/bushes.

**VegetationPrototypeSO** - texture/prefab definition  
**BiomeVegetationConfig** - per-biome layers  
**VegetationPainter** - generates DetailLayers

**VegetationManager** (runtime):
```csharp
ClearVegetationAt(pos)
StartFireAt(pos)
ExtinguishAt(pos)
```

Fire: density-based spread, region sync, O(burning cells)

---

## Notes

**WorldRandom** - isolated System.Random for determinism (UnityEngine.Random is global)

**Two-phase scatter generation** - positions first, transforms second (Editor/Runtime parity)
