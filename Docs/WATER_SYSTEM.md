# Water System Design

## Overview

Water system in Almost Alive is controlled by a single **WaterPlane** GameObject placed in the scene at a fixed height (e.g., 3m). The terrain generation creates geometry **below** this level for water bodies (lakes, rivers) and **above** for land masses.

This document describes the height calculation guarantees and configuration parameters.

---

## Core Concept: Height Guarantees

The system enforces strict height relationships to ensure visual correctness:

```
┌─────────────────────────────────────┐
│      Land Biomes (above water)      │ ← baseHeight + noise + minClearanceAboveWater
├─────────────────────────────────────┤
│         WaterPlane (3.0m)            │ ← Synced from scene
├─────────────────────────────────────┤
│      Rivers (flowing water)          │ ← waterLevel - riverCenterDepth
├─────────────────────────────────────┤
│      Lakes (standing water)          │ ← waterLevel - waterBodyFloorDepth
└─────────────────────────────────────┘
```

---

## Water Bodies (isWaterBody = true)

Lakes, ponds, and other standing water features.

### Height Calculation

```csharp
floorHeight = waterLevel - waterBodyFloorDepth;
shoreHeight = waterLevel - 0.01m; // Just below surface

// Blend from shore (edge) to floor (center)
finalHeight = Lerp(shoreHeight, floorHeight, distanceToCenterProfile);
```

### Configuration Parameters

| Parameter | Type | Range | Description |
|-----------|------|-------|-------------|
| `waterBodyFloorDepth` | float | 0.5-10m | Depth of lake floor **below** water surface |
| `waterBodyShoreSteepness` | float | 0-1 | Shore gradient (0=cliff, 1=beach) |
| `shoreGradient` | float | 0-1 | Legacy, kept for compatibility |

### Guarantees

✅ **Floor always below water surface**  
✅ **Shore blends smoothly to surrounding terrain**  
✅ **WaterPlane always visible in lake center**

---

## Land Biomes (isWaterBody = false)

Desert, forest, meadow, hills – any non-water terrain.

### Height Calculation

```csharp
height = biome.baseHeight + noise + globalNoise;

// Enforce minimum clearance above water
minHeight = waterLevel + biome.minClearanceAboveWater;
height = Max(height, minHeight);
```

### Configuration Parameters

| Parameter | Type | Range | Description |
|-----------|------|-------|-------------|
| `baseHeight` | float | 0-50m | Base elevation of the biome |
| `heightVariation` | float | 0-20m | Max noise amplitude |
| `minClearanceAboveWater` | float | 0.1-5m | Minimum height **above** water level |

### Guarantees

✅ **Never submerges below waterLevel + clearance**  
✅ **Allows smooth beach-like transitions to water**  
✅ **WaterPlane never shows through land**

---

## Rivers (generateRivers = true)

Rivers carve channels along biome boundaries.

### Height Calculation

```csharp
riverCenterHeight = waterLevel - riverCenterDepth;
riverShoreHeight = blendWithSurroundingTerrain;

// Profile: 0 = outside river, 1 = river center
height = Lerp(terrainHeight, riverCenterHeight, riverProfile);
```

### Configuration Parameters

| Parameter | Type | Range | Description |
|-----------|------|-------|-------------|
| `riverCenterDepth` | float | 0.2-3m | Depth of river bed **below** water surface |
| `riverWidth` | float | 2-20m | Width of main river channel |
| `riverBorderChance` | float | 0-1 | Probability of river spawning at biome border |

### Biome-Specific Overrides

Each biome can customize river appearance:

| Parameter | Description |
|-----------|-------------|
| `riverShoreStyle` | Natural, Soft, Rocky, Marshy, Terraced |
| `riverShoreWidth` | Transition zone from river to terrain |
| `riverShoreGradient` | Shore steepness (0=abrupt, 1=gentle) |
| `rockyIrregularity` | Noise amplitude for rocky shores |

### Guarantees

✅ **River center always below water**  
✅ **Borders follow biome boundaries**  
✅ **Probabilistic spawning prevents over-saturation**

---

## Slope Control

Slope limiting ensures NavMesh compatibility but can conflict with water features.

### Configuration

```csharp
limitSlopes = true;
maxSlopeAngle = 20°;            // General terrain
maxSlopeAngleNearWater = 60°;   // Relaxed near water
protectWaterSlopes = true;       // Enable water protection
```

### Behavior

- **Land areas:** Limited to `maxSlopeAngle` for walkability
- **Near water (±5% of waterLevel):** Uses `maxSlopeAngleNearWater` to allow steeper shores
- **Water bodies:** Exempt from slope limiting entirely

---

## Debugging & Visualization

### Biome Gizmo Labels

Displays biome names floating above terrain in Scene view.

**Settings:** (in `WorldGeneratorDebugSettings`)
```csharp
drawBiomeGizmos = true;
biomeLabelHeight = 2f;  // Offset above terrain surface
gizmoResolution = 20;   // Grid density for labels
```

### River Visualization

- **Blue circles:** Mark river spawn points along biome borders
- **Opacity:** Indicates river probability/strength
- **Gizmo labels:** Show which biomes the river connects

**Toggle:** Controlled by `drawBiomeGizmos` flag

### Water Level Sync

At generation start, `TerrainSculptPhase` logs:
```
[TerrainSculpt] Synced waterLevel from WaterPlane: 3.00m
```

If WaterPlane is missing, uses `Config.waterLevel` as fallback.

---

## Configuration Reference

### WorldGeneratorConfig

#### Water Settings
```csharp
[Range(0f, 50f)] waterLevel;           // Auto-synced from WaterPlane
[Range(0.2f, 3f)] riverCenterDepth;
[Range(2f, 20f)] riverWidth;
[Range(0f, 1f)] riverBorderChance;
```

#### Terrain Sculpting
```csharp
[Range(0f, 50f)] globalNoiseAmplitude;
[Range(0.001f, 0.1f)] globalNoiseScale;
```

#### Slope Control
```csharp
bool limitSlopes;
[Range(5f, 45f)] maxSlopeAngle;
[Range(45f, 90f)] maxSlopeAngleNearWater;
bool protectWaterSlopes;
```

### BiomeSO

#### Height (Land Biomes)
```csharp
[Range(0f, 50f)] baseHeight;
[Range(0f, 20f)] heightVariation;
[Range(0.1f, 5f)] minClearanceAboveWater;
```

#### Height (Water Bodies)
```csharp
bool isWaterBody;
[Range(0.5f, 10f)] waterBodyFloorDepth;
[Range(0f, 1f)] waterBodyShoreSteepness;
```

#### River Appearance
```csharp
RiverShoreStyle riverShoreStyle;
[Range(0f, 1f)] riverShoreGradient;
[Range(2f, 20f)] riverShoreWidth;
[Range(0f, 1f)] rockyIrregularity;
```

---

## Migration Notes

### From Old System (waterDepth → waterBodyFloorDepth)

**No automatic migration.** Manually adjust assets:

1. Open each water biome SO (e.g., `Biome_Lake`)
2. Set `waterBodyFloorDepth = old waterDepth value`
3. Set `waterBodyShoreSteepness = old shoreGradient value`
4. Verify `isWaterBody = true`

### Typical Values

**Small Lake:** floorDepth=1.5m, steepness=0.7  
**Deep Lake:** floorDepth=4m, steepness=0.4  
**Pond:** floorDepth=0.8m, steepness=0.9  

**River:** centerDepth=0.5m, width=6m, chance=0.3  

**Desert:** baseHeight=5m, clearance=0.5m  
**Hills:** baseHeight=15m, clearance=1m  

---

## Implementation Details

### TerrainSculptPhase Pipeline

```
1. Sync waterLevel from WaterPlane (mandatory)
2. Calculate base heights per biome
3. Apply global noise (skipped for water bodies)
4. Enforce minClearanceAboveWater for land
5. Carve rivers along biome borders
6. Smooth water edges (3 passes)
7. Limit slopes (protect water areas)
8. Apply to terrain
```

### BiomeGizmoDrawer

Custom Gizmo drawer that:
- Samples terrain height at biome cell centers
- Draws biome labels at `terrainY + biomeLabelHeight`
- Renders river borders with distinct color
- Only active when `drawBiomeGizmos = true`

---

## Troubleshooting

### Water shows through land
→ Increase `minClearanceAboveWater` for affected biomes

### Rivers too shallow
→ Increase `riverCenterDepth` in WorldGeneratorConfig

### Lakes not filling with water
→ Check `waterBodyFloorDepth < waterLevel`  
→ Verify WaterPlane is at correct height in scene

### Steep cliffs at shoreline
→ Increase `waterBodyShoreSteepness` (lake)  
→ Increase `riverShoreGradient` (river)  
→ Disable/reduce `limitSlopes`

### Biome labels not visible
→ Enable `drawBiomeGizmos` in debug settings  
→ Increase `biomeLabelHeight` offset

---

*Last updated: 2026-01-28*
