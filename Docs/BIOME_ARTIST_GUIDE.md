# Biome Configuration Guide for Artists

> Technical documentation for configuring biomes in Almost Alive world generator.
> Last updated: 2026-01-28

---

## Table of Contents

1. [Overview: How Biomes Work](#overview-how-biomes-work)
2. [Identity Settings](#identity-settings)
3. [Height & Terrain Sculpting](#height--terrain-sculpting)
4. [Water Bodies (Lakes)](#water-bodies-lakes)
5. [River Shore Interaction](#river-shore-interaction)
6. [Textures & Splatmaps](#textures--splatmaps)
7. [Vegetation](#vegetation)
8. [Common Recipes](#common-recipes)
9. [Troubleshooting](#troubleshooting)

---

## Overview: How Biomes Work

The world generator uses Voronoi cells to distribute biomes across terrain. Each biome is a ScriptableObject (`BiomeSO`) containing all settings that control:

- **Height** — how high/low the terrain is, how bumpy
- **Textures** — what ground materials appear (grass, rock, sand)
- **Vegetation** — trees, bushes, grass placement
- **Water interaction** — lakes, river shores

**Generation order:**
1. Biome placement (Voronoi cells based on `weight`)
2. Terrain sculpting (heights based on biome settings)
3. River carving (along biome borders)
4. Texture painting (based on slope and biome textures)
5. Vegetation spawning (based on category masks)

---

## Identity Settings

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Type** | enum | Biome identifier (Forest, Desert, Lake, etc.) |
| **Debug Color** | Color | Visualization color in Scene view gizmos |
| **Weight** | 0.1 – 10 | How often this biome appears relative to others |

### Weight explained

Weight controls biome distribution during Voronoi generation:
- `weight = 1.0` — normal frequency
- `weight = 0.5` — half as common (rarer)
- `weight = 2.0` — twice as common
- `weight = 0.1` — very rare (special/unique biomes)

**Example:** If Forest has `weight = 2` and Desert has `weight = 1`, Forest will cover ~66% of land, Desert ~33%.

---

## Height & Terrain Sculpting

Controls how the terrain mesh is shaped within this biome.

### Base Parameters

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Base Height** | 0 – 100m | Average elevation above terrain base |
| **Height Variation** | 0 – 50m | Maximum deviation from base (noise amplitude) |
| **Min Clearance Above Water** | 0.1 – 5m | Prevents terrain from going underwater |
| **Height Profile** | AnimationCurve | Height falloff from biome center to edge |

### Height Profile Curve

**X-axis:** 0 = biome center, 1 = biome edge
**Y-axis:** Height multiplier (0 – 1+)

| Curve Shape | Result |
|-------------|--------|
| Flat line at 1.0 | Uniform height across biome |
| Descending 1→0.5 | Higher in center, lower at edges |
| Ascending 0.5→1 | Lower in center, higher at edges (bowl) |
| Bell curve | Plateau in middle, slopes at edges |

**Tip:** Use this to create natural transitions between biomes. A Forest with descending profile will blend smoothly into lower Meadow.

### Height Noise

Controls the bumpiness/texture of terrain surface.

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Template** | NoiseSO | Source to copy settings from (read-only) |
| **Frequency** | 0.001 – 0.5 | Feature size. Lower = larger hills, Higher = fine detail |
| **Amplitude** | 0 – 2 | Output strength multiplier |
| **Offset** | Vector2 | Shifts the noise pattern (for variation) |
| **Seed Offset** | int | Unique seed for this biome's noise |

#### Output Processing

| Parameter | Effect |
|-----------|--------|
| **Normalize** | Maps output to 0–1 range (recommended ON) |
| **Invert** | Flips valleys to hills and vice versa |
| **Power** | < 1 = flatter peaks, > 1 = sharper peaks |

#### FBM (Fractal Brownian Motion)

Adds natural-looking detail by layering multiple noise frequencies.

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Use FBM** | bool | Enable fractal layering |
| **Octaves** | 1 – 8 | Number of detail layers. More = richer detail, slower |
| **Persistence** | 0.1 – 0.9 | Each octave's strength. 0.5 = each layer half as strong |
| **Lacunarity** | 1.5 – 3 | Frequency multiplier between octaves. 2 = each layer twice as fine |

**Visual guide for FBM settings:**

```
Octaves=1, Persistence=0.5:  Smooth, simple hills
Octaves=4, Persistence=0.5:  Natural terrain with detail
Octaves=6, Persistence=0.7:  Very detailed, slightly noisy
Octaves=2, Persistence=0.3:  Smooth rolling hills
```

**Frequency reference:**

| Frequency | Typical Feature Size | Use Case |
|-----------|---------------------|----------|
| 0.005 | ~200m | Large mountain ranges |
| 0.015 | ~65m | Hills, major terrain features |
| 0.03 | ~33m | Small hills, undulations |
| 0.05 | ~20m | Ground texture, micro-terrain |
| 0.1+ | <10m | Fine surface detail only |

---

## Water Bodies (Lakes)

For biomes that should be submerged (lakes, ponds).

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Is Water Body** | bool | Marks this biome as a lake |
| **Floor Depth** | 0.5 – 15m | Depth below water surface at center |
| **Shore Steepness** | 0 – 1 | 0 = cliff edges, 1 = gentle beach |

### Shore Steepness Visual

```
Steepness = 0.0:    ▐▀▀▀▌    Sharp cliff into water
Steepness = 0.3:    ▐▀▀▄▌    Moderate slope
Steepness = 0.5:    ▐▀▄▃▌    Natural beach
Steepness = 0.8:    ▐▁▂▃▌    Very gradual, wide beach
Steepness = 1.0:    ▔▔▔▁▁▁   Almost flat transition
```

**Tip:** Combine with low `weight` (0.2–0.5) to make lakes less common.

---

## River Shore Interaction

Controls how rivers carve through this biome when flowing along borders.

### Shore Style

| Style | Description | Best For |
|-------|-------------|----------|
| **Natural** | Smooth transition, respects gradient setting | Most biomes |
| **Soft** | Extra smooth, beach-like | Meadows, sandy areas |
| **Rocky** | Steep, jagged with noise irregularity | Mountains, rocky hills |
| **Marshy** | Very gradual, wet/muddy transition | Swamps, wetlands |
| **Terraced** | Step-like descent (3 terraces) | Geological formations |

### Shore Parameters

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Gradient** | 0 – 1 | Slope angle. 0 = vertical cliff, 1 = very gentle |
| **Width** | 1 – 15m | How far from river edge the shore extends |
| **Rocky Irregularity** | 0 – 1 | (Rocky style only) How jagged the edge is |

### Examples

| Biome Type | Style | Gradient | Width | Notes |
|------------|-------|----------|-------|-------|
| Forest | Natural | 0.5 | 4m | Standard balanced |
| Desert | Soft | 0.7 | 6m | Wide sandy banks |
| Rocky Hills | Rocky | 0.3 | 3m | Steep with irregularity 0.6 |
| Swamp | Marshy | 0.8 | 10m | Wide muddy transition |
| Mountains | Rocky | 0.2 | 2m | Steep cliffs |

---

## Textures & Splatmaps

Controls terrain texture painting. Uses 4 fixed slots that blend based on terrain conditions.

### Texture Slots

| Slot | Purpose | Blending Rule |
|------|---------|---------------|
| **Base** | Primary ground texture | Always present |
| **Detail** | Secondary variation | Noise-blended with base |
| **Slope** | Angled surfaces | Applied in slope range |
| **Cliff** | Steep faces | Applied above threshold |

### Base Layer
Always-on ground texture. Choose from the global Terrain Palette.

### Detail Layer

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Layer Name** | dropdown | Texture from palette |
| **Strength** | 0 – 1 | How much detail shows through (0 = none) |
| **Noise Scale** | 0.001 – 0.1 | Patch size. Lower = larger patches |

**Use case:** Add dirt patches to grass, or rock spots to sand.

### Slope Layer

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Layer Name** | dropdown | Texture for slopes |
| **Slope Range** | 0° – 90° | Min/max angle where texture appears |

**Example:** `slopeRange = (25°, 45°)` applies rock texture on moderate slopes.

### Cliff Layer

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Layer Name** | dropdown | Texture for cliffs |
| **Threshold** | 0° – 90° | Slope angle above which cliff texture appears |

**Example:** `threshold = 55°` applies cliff texture on steep faces.

### Texture Stacking Order

```
Priority (highest first):
1. Cliff (if slope > threshold)
2. Slope (if slope in range)
3. Detail (noise-blended)
4. Base (always)
```

---

## Vegetation

Controls tree, bush, and grass placement. Uses a **category-based** system where vegetation is grouped by size.

### Global Settings

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Global Density** | 0 – 3 | Master multiplier for all vegetation |
| **Max Density Per Cell** | 8 – 255 | Cap on grass instances per terrain cell |

### Categories

Vegetation is organized into categories by size:

| Category | Typical Content | Default Noise Scale |
|----------|-----------------|---------------------|
| **Small** | Ground cover, small grass | 0.05 (small patches) |
| **Medium** | Bushes, flowers, tall grass | 0.025 (medium clusters) |
| **Large** | Trees, large shrubs | 0.01 (large groves) |

### Category Settings

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Name** | string | Display name |
| **Size** | enum | Small/Medium/Large (affects defaults) |
| **Enabled** | bool | Toggle entire category |
| **Density Multiplier** | 0 – 2 | Category-wide density scale |

### Placement Noise

Controls where vegetation patches appear vs bare ground.

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Mode** | enum | Noise algorithm (Perlin, Cellular, etc.) |
| **Scale** | 0.001 – 0.2 | Patch size. Lower = larger forest areas |
| **Octaves** | 1 – 6 | Detail layers |
| **Persistence** | 0.1 – 0.9 | Octave falloff |
| **Threshold** | 0 – 1 | Cutoff. Higher = sparser vegetation |
| **Blend** | 0 – 0.5 | Edge softness |
| **Use Stochastic** | bool | Add random variation |

**Threshold explained:**

```
Threshold = 0.3:  ████████░░  Dense forest, some gaps
Threshold = 0.5:  ████░░████  Medium density, visible patches
Threshold = 0.7:  ██░░░░░░██  Sparse, isolated clusters
Threshold = 0.9:  ░░░░██░░░░  Very sparse, rare trees
```

### Terrain Filters (per category)

AnimationCurves that reduce density based on terrain conditions.

| Filter | X-Axis | Y-Axis | Typical Use |
|--------|--------|--------|-------------|
| **Biome Edge Falloff** | 0–1 (center→edge) | 0–1 multiplier | Less vegetation at biome borders |
| **Slope Falloff** | 0°–90° slope | 0–1 multiplier | No trees on cliffs |
| **Height Falloff** | 0–200m height | 0–1 multiplier | Treeline effect |

**Example Slope Falloff for Trees:**
```
0° → 1.0   Full density on flat ground
15° → 0.8  Slight reduction on gentle slopes
30° → 0.3  Sparse on moderate slopes
45° → 0.0  No trees on steep slopes
```

### Vegetation Layers

Each category contains layers — specific plant types.

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Prototype** | VegetationPrototypeSO | The plant/tree asset |
| **Density** | 0 – 1 | Base placement density |
| **Weight** | 0.1 – 5 | Relative frequency vs other layers |

#### Per-Layer Noise (optional)

| Parameter | Range | Effect |
|-----------|-------|--------|
| **Use Layer Noise** | bool | Add extra variation to this layer |
| **Layer Noise Scale** | 0.001 – 0.1 | Variation patch size |
| **Layer Noise Strength** | 0 – 1 | How much variation |

#### Terrain Overrides (optional)

| Parameter | Effect |
|-----------|--------|
| **Override Slope Falloff** | Use custom slope curve for this layer |
| **Allowed Terrain Layers** | Only place on specific textures |

---

## Common Recipes

### Lush Forest

```
Height:
  Base Height: 12m
  Height Variation: 8m
  Noise Frequency: 0.012
  FBM Octaves: 5
  Persistence: 0.55

River Shore:
  Style: Natural
  Gradient: 0.6
  Width: 5m

Vegetation:
  Global Density: 1.5
  Trees Category:
    Threshold: 0.4 (dense)
    Scale: 0.008 (large groves)
  Bushes Category:
    Threshold: 0.35
```

### Rocky Mountains

```
Height:
  Base Height: 35m
  Height Variation: 25m
  Noise Frequency: 0.008
  FBM Octaves: 6
  Persistence: 0.6
  Power: 1.3 (sharper peaks)

River Shore:
  Style: Rocky
  Gradient: 0.2
  Width: 2m
  Rocky Irregularity: 0.7

Textures:
  Base: Rock
  Slope: (15°, 35°) Stone
  Cliff: 40° Mountain Rock

Vegetation:
  Global Density: 0.4
  Trees:
    Slope Falloff: 0°→1.0, 25°→0.0
```

### Desert/Arid

```
Height:
  Base Height: 8m
  Height Variation: 3m
  Noise Frequency: 0.02
  FBM Octaves: 3
  Persistence: 0.4 (smooth dunes)

River Shore:
  Style: Soft
  Gradient: 0.8
  Width: 8m

Textures:
  Base: Sand
  Detail: Dirt (strength: 0.2, scale: 0.03)

Vegetation:
  Global Density: 0.2
  Trees Category:
    Enabled: false
  Bushes Category:
    Threshold: 0.75 (very sparse)
```

### Lake

```
Identity:
  Weight: 0.3 (uncommon)

Water Body:
  Is Water Body: true
  Floor Depth: 4m
  Shore Steepness: 0.6

Vegetation:
  Global Density: 0 (no underwater plants)
```

### Swamp/Wetland

```
Height:
  Base Height: 6m (low)
  Height Variation: 2m (flat)
  Min Clearance Above Water: 0.2m

River Shore:
  Style: Marshy
  Gradient: 0.9
  Width: 12m

Textures:
  Base: Mud
  Detail: Wet Grass (strength: 0.4)

Vegetation:
  Trees:
    Slope Falloff: flat (swamp trees ok on any slope)
  Small:
    Threshold: 0.2 (very dense reeds)
```

---

## Troubleshooting

### Terrain is too flat
- Increase `Height Variation`
- Lower `Noise Frequency` (bigger features)
- Add more `FBM Octaves`
- Increase `Persistence`

### Terrain is too noisy/chaotic
- Reduce `FBM Octaves`
- Lower `Persistence`
- Increase `Power` > 1 to smooth peaks

### Biome borders look too sharp
- Adjust `Height Profile` curve to fade at edges
- Use similar `Base Height` on neighboring biomes
- The generator auto-smooths borders, but big height differences still show

### Vegetation looks unnatural (too uniform)
- Lower category `Threshold` for denser patches
- Enable `Use Stochastic` for random variation
- Add `Per-Layer Noise` to individual plants
- Adjust `Biome Edge Falloff` curve

### Trees appear on cliffs
- Steepen `Slope Falloff` curve for Large category
- Add layer-specific `Override Slope Falloff`

### Lakes have vertical walls
- Increase `Shore Steepness` toward 1.0
- This creates more gradual beach-like transition

### Rivers don't look natural
- Match `River Shore Style` to biome type
- Adjust `Gradient` for slope steepness
- Increase `Width` for wider shore zones

---

## Quick Reference Card

| Want... | Adjust... |
|---------|-----------|
| Bigger hills | ↓ Noise Frequency |
| More detail | ↑ Octaves |
| Smoother terrain | ↓ Persistence, ↑ Power |
| Sharper peaks | ↑ Power > 1.5 |
| Higher biome | ↑ Base Height |
| More texture variation | ↑ Detail Strength |
| Rock on slopes | Set Slope Range (25°–45°) |
| Denser forest | ↓ Vegetation Threshold |
| Larger forest patches | ↓ Vegetation Scale |
| No trees on slopes | Edit Slope Falloff curve |
| Rare biome | ↓ Weight (0.2–0.5) |
| Gentle lake shores | ↑ Shore Steepness → 1.0 |

---

*Document generated for Almost Alive world generator v0.3*
