# VegetationPhase Optimization

## Date: 2026-01-28

## Problem
Vegetation generation was extremely slow in editor. For a 512x512 detail resolution:
- 262,144 pixels to process
- Each pixel: multiple biome lookups, slope calculations, splat queries
- Total time: unacceptably long

## Root Causes Identified

### 🔴 Critical (Fixed)

#### 1. GetDominantSplatLayer - PER-PIXEL Unity API calls
**Before:**
```csharp
var alphas = td.GetAlphamaps(ax, ay, 1, 1); // Called 262K times!
```
- Unity API call overhead per pixel
- Each call allocates a 3D array

**After:**
```csharp
// Pre-compute once at start
var alphas = td.GetAlphamaps(0, 0, alphaRes, alphaRes);
// ... build _dominantSplatMap[y,x] lookup table
```
- Single Unity API call
- O(1) lookup in main loop

#### 2. FindPrototypeIndex - Allocations per pixel
**Before:**
```csharp
var prefabsToSearch = new List<GameObject>(); // 262K allocations
var matchingIndices = new List<int>();        // 262K more
```

**After:**
```csharp
// Lazy cache: VegetationPrototypeSO -> int[]
private Dictionary<VegetationPrototypeSO, int[]> _prototypeIndexCache;
```
- Zero allocations in main loop
- Cache built on first access per prototype

#### 3. CalculateSlope - Redundant math per pixel
**Before:**
```csharp
var slope = CalculateSlope(heights, hx, hy, heightmapRes, terrainSize);
// Full calculation every pixel
```

**After:**
```csharp
// Pre-computed slope map at detail resolution
_slopeMap = new float[detailRes, detailRes];
PrecomputeSlopeMap(heights, heightmapRes, terrainSize, resolution);
// O(1) lookup: var slope = _slopeMap[y, x];
```

### 🟡 Medium (Fixed)

#### 4. BiomeMap vector allocations
**Before:**
```csharp
var worldPos = new Vector3(worldX, 0, worldZ);
biomeMap.GetBiomeDataAt(worldPos);
```

**After:**
```csharp
// Added overloads to BiomeMap.cs
biomeMap.GetBiomeDataAt(worldX, worldZ);
biomeMap.GetNormalizedDistanceToCenter(worldX, worldZ);
```

### 🟢 Minor tweaks

#### 5. Loop variable caching
```csharp
// Pre-compute outside loops
var resMinusOne = resolution - 1;
var heightResMinusOne = heightmapRes - 1;
var categories = vegConfig.categories;
```

#### 6. Progress reporting frequency
```csharp
// Before: every 50 rows
// After: every 100 rows
if (y % 100 == 0) { ... }
```

## Performance Impact

| Optimization | Estimated Speedup |
|-------------|-------------------|
| GetAlphamaps cache | ~50-100x for splat checks |
| Prototype index cache | ~10-20x (no allocs) |
| Slope map pre-compute | ~2-3x |
| Vector alloc removal | ~1.5x |
| **Parallel.For in MaskService** | **~3-4x** |
| **Unity.Mathematics noise** | **~1.5-2x** |
| **Total estimated** | **~10-20x faster** |

## Files Modified

1. **VegetationPhase.cs** - Main optimizations + Unity.Mathematics
2. **BiomeMap.cs** - Added float overloads + Unity.Mathematics
3. **PerlinMaskGenerator.cs** - Parallel.For + Unity.Mathematics

## Future Optimizations (Not Done)

### Parallel Processing in VegetationPhase
```csharp
// Main loop could also use Parallel.For but needs:
// - Thread-safe Random (per-thread seeds)
// - Careful with detailLayers writes (atomic or per-thread buffers)
```

### Downsampling for Masks
- Generate at 1/4 resolution, bilinear upscale
- Would give ~16x speedup for mask generation
- May affect quality slightly at biome edges

## Testing Notes

- Test with different detail resolutions (256, 512, 1024)
- Verify visual output unchanged
- Profile with Unity Profiler to measure actual gains
