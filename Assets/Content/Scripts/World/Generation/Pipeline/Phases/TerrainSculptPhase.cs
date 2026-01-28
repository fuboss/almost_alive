using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 2: Sculpt terrain heightmap based on biome settings.
  /// Applies height noise from each biome with proper blending.
  /// Supports: global noise, slope limiting, river carving, lake biomes.
  /// </summary>
  public class TerrainSculptPhase : GenerationPhaseBase {
    
    public override string Name => "Terrain Sculpt";
    public override string Description => "Apply heightmap based on biomes";

    protected override bool ValidateContext(GenerationContext ctx) {
      if (ctx?.BiomeMap == null) {
        Debug.LogError("[TerrainSculpt] BiomeMap not generated");
        return false;
      }
      if (!ctx.Config.sculptTerrain) {
        Debug.Log("[TerrainSculpt] Skipped (sculptTerrain disabled)");
        return false;
      }
      return true;
    }

    protected override void ExecuteInternal(GenerationContext ctx) {
      ReportProgress(0f, "Sculpting terrain...");
      
      var terrain = ctx.Terrain;
      var td = terrain.terrainData;
      var resolution = td.heightmapResolution;
      var heights = new float[resolution, resolution];
      var biomeMap = ctx.BiomeMap;
      var bounds = ctx.Bounds;
      var config = ctx.Config;
      var terrainHeight = td.size.y;
      var terrainPos = terrain.transform.position;
      
      // Sync water level from scene WaterPlane if exists
      SyncWaterLevelFromScene(ctx);
      
      float waterLevelNorm = config.waterLevel / terrainHeight;
      
      // ═══════════════════════════════════════════════════════════════
      // PASS 1: Base heights + biome heights + global noise + lakes
      // ═══════════════════════════════════════════════════════════════
      
      ReportProgress(0.05f, "Calculating base heights...");
      
      for (int y = 0; y < resolution; y++) {
        if (y % 100 == 0) {
          ReportProgress(0.05f + 0.35f * y / resolution, $"Height pass: {y}/{resolution}");
        }
        
        for (int x = 0; x < resolution; x++) {
          // Convert heightmap coords to world position
          var nx = x / (float)(resolution - 1);
          var ny = y / (float)(resolution - 1);
          
          var worldX = bounds.min.x + nx * bounds.size.x;
          var worldZ = bounds.min.z + ny * bounds.size.z;
          var worldPos = new Vector3(worldX, 0, worldZ);
          
          // Get biome query (includes blend info)
          var query = biomeMap.QueryBiome(worldPos);
          var biome = query.primaryData;
          
          float height = 0f;
          
          if (biome != null) {
            // Check if this is a water body biome (lake)
            if (biome.isWaterBody) {
              height = CalculateLakeHeight(query, config, terrainHeight);
            } else {
              // Normal biome height calculation
              height = biome.baseHeight / terrainHeight;
              
              // Biome-specific noise
              if (biome.heightNoise != null) {
                biome.heightNoise.SetSeed(ctx.Seed);
                var noiseValue = biome.heightNoise.Sample(worldX, worldZ);
                height += noiseValue * (biome.heightVariation / terrainHeight);
              }
            }
          }
          
          // Global noise (independent of biomes, skip for water bodies)
          if (config.useGlobalNoise && (biome == null || !biome.isWaterBody)) {
            // Large-scale terrain variation
            float globalNoise = SamplePerlin(worldX, worldZ, config.globalNoiseScale, ctx.Seed);
            height += globalNoise * (config.globalNoiseAmplitude / terrainHeight);
            
            // Fine detail
            float detailNoise = SamplePerlin(worldX, worldZ, config.detailNoiseScale, ctx.Seed + 12345);
            height += detailNoise * (config.detailNoiseAmplitude / terrainHeight);
          }
          
          heights[y, x] = Mathf.Clamp01(height);
        }
      }
      
      // ═══════════════════════════════════════════════════════════════
      // PASS 2: River carving along biome borders
      // ═══════════════════════════════════════════════════════════════
      
      if (config.generateRivers) {
        ReportProgress(0.45f, "Carving rivers...");
        CarveRivers(heights, resolution, biomeMap, bounds, config, terrainHeight, ctx.Seed, ctx);
      }
      
      // ═══════════════════════════════════════════════════════════════
      // PASS 3: Smooth water edges (lakes + rivers)
      // ═══════════════════════════════════════════════════════════════
      
      ReportProgress(0.6f, "Smoothing shorelines...");
      SmoothWaterEdges(heights, resolution, waterLevelNorm, passes: 3);
      
      // ═══════════════════════════════════════════════════════════════
      // PASS 4: Slope limiting (for NavMesh compatibility)
      // ═══════════════════════════════════════════════════════════════
      
      if (config.limitSlopes) {
        ReportProgress(0.75f, "Limiting slopes...");
        LimitSlopes(heights, resolution, config.maxSlopeAngle, td.size, config.slopeSmoothingPasses);
      }
      
      ReportProgress(0.95f, "Applying heightmap...");
      
      // Store for potential rollback
      ctx.Heightmap = heights;
      
      // Apply to terrain
      td.SetHeights(0, 0, heights);
      
      // Update WaterPlane position in scene
      UpdateWaterPlaneInScene(ctx);
      
      ReportProgress(1f);
      ClearProgressBar();
    }

    // ═══════════════════════════════════════════════════════════════
    // WATER PLANE SYNC
    // ═══════════════════════════════════════════════════════════════

    private void SyncWaterLevelFromScene(GenerationContext ctx) {
      var waterPlane = GameObject.Find("WaterPlane");
      if (waterPlane != null) {
        var terrainY = ctx.Terrain.transform.position.y;
        var planeY = waterPlane.transform.position.y;
        var waterLevel = planeY - terrainY;
        
        if (Mathf.Abs(waterLevel - ctx.Config.waterLevel) > 0.01f) {
          Debug.Log($"[TerrainSculpt] Syncing waterLevel from WaterPlane: {waterLevel:F2}m");
          ctx.ConfigSO.Data.waterLevel = waterLevel;
        }
      }
    }

    private void UpdateWaterPlaneInScene(GenerationContext ctx) {
      var waterPlane = GameObject.Find("WaterPlane");
      if (waterPlane != null) {
        var terrainPos = ctx.Terrain.transform.position;
        var targetY = terrainPos.y + ctx.Config.waterLevel;
        
        if (Mathf.Abs(waterPlane.transform.position.y - targetY) > 0.01f) {
          var pos = waterPlane.transform.position;
          pos.y = targetY;
          waterPlane.transform.position = pos;
          Debug.Log($"[TerrainSculpt] Updated WaterPlane Y to {targetY:F2}");
        }
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // LAKE HEIGHT CALCULATION
    // ═══════════════════════════════════════════════════════════════

    private float CalculateLakeHeight(BiomeMap.BiomeQuery query, WorldGeneratorConfig config, float terrainHeight) {
      var biome = query.primaryData;
      float waterLevelNorm = config.waterLevel / terrainHeight;
      float lakeBottomNorm = (config.waterLevel - biome.waterDepth) / terrainHeight;
      
      // Use distance to center for depth profile
      // primaryWeight: 1 = center of biome, decreasing towards edges
      float centerWeight = query.primaryWeight;
      
      // Shore gradient affects how steep the transition is
      float gradient = biome.shoreGradient;
      
      // Remap t based on gradient (expand the transition zone)
      float t = Mathf.Clamp01(centerWeight);
      float expandedT = Mathf.Lerp(t, t * t, 1f - gradient);
      float smoothT = Smootherstep(expandedT);
      
      // Interpolate: edge = just below water, center = lake bottom
      float shoreHeight = waterLevelNorm - 0.01f; // Just below water surface at shore
      float height = Mathf.Lerp(shoreHeight, lakeBottomNorm, smoothT);
      
      return Mathf.Max(0.001f, height);
    }

    // ═══════════════════════════════════════════════════════════════
    // NOISE SAMPLING
    // ═══════════════════════════════════════════════════════════════

    private float SamplePerlin(float x, float z, float scale, int seed) {
      float offsetX = (seed % 10000) * 0.1f;
      float offsetZ = ((seed / 10000) % 10000) * 0.1f;
      
      float value = Mathf.PerlinNoise(x * scale + offsetX, z * scale + offsetZ);
      return value - 0.5f;
    }

    // ═══════════════════════════════════════════════════════════════
    // RIVER CARVING (with soft shores)
    // ═══════════════════════════════════════════════════════════════

    private void CarveRivers(float[,] heights, int resolution, BiomeMap biomeMap, 
                            Bounds bounds, WorldGeneratorConfig config, float terrainHeight, 
                            int seed, GenerationContext ctx) {
      float waterLevelNorm = config.waterLevel / terrainHeight;
      float riverBedNorm = (config.waterLevel - config.riverBedDepth) / terrainHeight;
      
      var riverMask = new float[resolution, resolution];
      int riverPixels = 0;
      
      for (int y = 0; y < resolution; y++) {
        for (int x = 0; x < resolution; x++) {
          var nx = x / (float)(resolution - 1);
          var ny = y / (float)(resolution - 1);
          
          var worldX = bounds.min.x + nx * bounds.size.x;
          var worldZ = bounds.min.z + ny * bounds.size.z;
          var worldPos = new Vector3(worldX, 0, worldZ);
          
          float borderDist = biomeMap.GetDistanceToBorder(worldPos);
          
          // Get biome for shore style
          var query = biomeMap.QueryBiome(worldPos);
          var biome = query.primaryData;
          
          // Skip water body biomes - they handle their own depth
          if (biome != null && biome.isWaterBody) continue;
          
          // Get biome-specific shore settings
          float shoreWidth = biome?.riverShoreWidth ?? config.riverWidth * 0.4f;
          float shoreGradient = biome?.riverShoreGradient ?? 0.5f;
          var shoreStyle = biome?.riverShoreStyle ?? RiverShoreStyle.Natural;
          float rockyIrregularity = biome?.rockyIrregularity ?? 0f;
          
          float totalWidth = config.riverWidth + shoreWidth;
          
          if (borderDist < totalWidth) {
            // River presence noise
            float riverNoise = Mathf.PerlinNoise(worldX * 0.008f + seed * 0.001f, worldZ * 0.008f);
            
            if (riverNoise < config.riverBorderChance) {
              float riverProfile = CalculateRiverProfile(
                borderDist, config.riverWidth, shoreWidth, 
                shoreGradient, shoreStyle, rockyIrregularity,
                worldX, worldZ, seed
              );
              
              if (riverProfile > 0.02f) {
                riverMask[y, x] = riverProfile;
                riverPixels++;
                
                // Target height based on profile
                float targetHeight = Mathf.Lerp(waterLevelNorm - 0.005f, riverBedNorm, riverProfile);
                
                // Blend with existing terrain
                float blendFactor = Mathf.Clamp01(riverProfile * 1.5f);
                heights[y, x] = Mathf.Lerp(heights[y, x], targetHeight, blendFactor);
                
                // Ensure below water for river center
                if (riverProfile > 0.3f) {
                  heights[y, x] = Mathf.Min(heights[y, x], waterLevelNorm - 0.002f);
                }
              }
            }
          }
        }
      }
      
      ctx.RiverMask = riverMask;
      Debug.Log($"[TerrainSculpt] Carved {riverPixels} river pixels");
    }

    /// <summary>
    /// Calculate river cross-section profile based on biome shore style.
    /// Returns 0-1 where 1 = river center (deepest), 0 = outside river.
    /// </summary>
    private float CalculateRiverProfile(float borderDist, float riverWidth, float shoreWidth,
                                        float shoreGradient, RiverShoreStyle style, float rockyIrregularity,
                                        float worldX, float worldZ, int seed) {
      float profile;
      
      if (borderDist < riverWidth) {
        // Inside river channel
        float t = borderDist / riverWidth;
        
        switch (style) {
          case RiverShoreStyle.Rocky:
            // Sharp transition with noise for irregular rocky edges
            float rockNoise = Mathf.PerlinNoise(worldX * 0.1f + seed, worldZ * 0.1f) * rockyIrregularity;
            float rockyT = Mathf.Clamp01(t + rockNoise * 0.3f);
            // Steep falloff
            profile = 1f - Mathf.Pow(rockyT, 0.5f + shoreGradient);
            break;
            
          case RiverShoreStyle.Marshy:
            // Very gradual, almost flat transition
            float marshyT = t * t; // Squared for even more gradual
            profile = 1f - Smootherstep(marshyT);
            // Add slight waviness for marsh texture
            float marshNoise = Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f) * 0.1f;
            profile = Mathf.Clamp01(profile - marshNoise);
            break;
            
          case RiverShoreStyle.Terraced:
            // Step-like profile (3 terraces)
            float terracedT = t * 3f;
            float step = Mathf.Floor(terracedT);
            float frac = terracedT - step;
            // Smooth within each terrace
            profile = 1f - (step + Smootherstep(frac)) / 3f;
            break;
            
          case RiverShoreStyle.Soft:
            // Beach-like, very smooth
            profile = 1f - Smootherstep(Smootherstep(t)); // Double smoothstep
            break;
            
          case RiverShoreStyle.Natural:
          default:
            // Standard smootherstep with gradient control
            float adjustedT = Mathf.Lerp(t, t * t, 1f - shoreGradient);
            profile = 1f - Smootherstep(adjustedT);
            break;
        }
      } else {
        // Shore zone - gradual blend to terrain
        float shoreT = (borderDist - riverWidth) / shoreWidth;
        
        switch (style) {
          case RiverShoreStyle.Rocky:
            // Abrupt end with some noise
            float rockEdgeNoise = Mathf.PerlinNoise(worldX * 0.15f + seed * 2, worldZ * 0.15f) * rockyIrregularity;
            profile = Mathf.Max(0, (1f - shoreT * 2f) * 0.2f - rockEdgeNoise * 0.1f);
            break;
            
          case RiverShoreStyle.Marshy:
            // Extended wet zone
            profile = (1f - Smootherstep(shoreT)) * 0.4f;
            break;
            
          case RiverShoreStyle.Terraced:
            // One more step outside
            profile = shoreT < 0.5f ? 0.15f : 0f;
            break;
            
          case RiverShoreStyle.Soft:
          case RiverShoreStyle.Natural:
          default:
            // Gradual fadeout
            float baseInfluence = 0.3f * shoreGradient;
            profile = (1f - Smootherstep(shoreT)) * baseInfluence;
            break;
        }
      }
      
      // Add universal meander waviness
      float waveNoise = Mathf.PerlinNoise(worldX * 0.03f + seed * 0.5f, worldZ * 0.03f) * 0.12f;
      return Mathf.Max(0, profile - waveNoise);
    }

    /// <summary>Quintic smootherstep for C2 continuity.</summary>
    private float Smootherstep(float t) {
      t = Mathf.Clamp01(t);
      return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    // ═══════════════════════════════════════════════════════════════
    // WATER EDGE SMOOTHING
    // ═══════════════════════════════════════════════════════════════

    private void SmoothWaterEdges(float[,] heights, int resolution, float waterLevel, int passes) {
      for (int pass = 0; pass < passes; pass++) {
        var temp = (float[,])heights.Clone();
        
        for (int y = 1; y < resolution - 1; y++) {
          for (int x = 1; x < resolution - 1; x++) {
            float h = heights[y, x];
            
            // Only smooth near water level
            if (Mathf.Abs(h - waterLevel) > 0.05f) continue;
            
            // Gaussian-weighted neighbors
            float sum = h * 4f;
            sum += heights[y - 1, x] + heights[y + 1, x];
            sum += heights[y, x - 1] + heights[y, x + 1];
            sum += (heights[y - 1, x - 1] + heights[y - 1, x + 1] +
                   heights[y + 1, x - 1] + heights[y + 1, x + 1]) * 0.5f;
            
            temp[y, x] = sum / 10f;
          }
        }
        
        System.Array.Copy(temp, heights, heights.Length);
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // SLOPE LIMITING
    // ═══════════════════════════════════════════════════════════════

    private void LimitSlopes(float[,] heights, int resolution, float maxAngle, Vector3 terrainSize, int smoothPasses) {
      float maxSlope = Mathf.Tan(maxAngle * Mathf.Deg2Rad);
      float cellSizeX = terrainSize.x / (resolution - 1);
      float cellSizeZ = terrainSize.z / (resolution - 1);
      float maxHeightDiffX = maxSlope * cellSizeX / terrainSize.y;
      float maxHeightDiffZ = maxSlope * cellSizeZ / terrainSize.y;
      
      for (int pass = 0; pass < smoothPasses + 1; pass++) {
        var temp = (float[,])heights.Clone();
        
        for (int y = 1; y < resolution - 1; y++) {
          for (int x = 1; x < resolution - 1; x++) {
            float h = heights[y, x];
            
            float hL = heights[y, x - 1];
            float hR = heights[y, x + 1];
            float hD = heights[y - 1, x];
            float hU = heights[y + 1, x];
            
            float minAllowed = Mathf.Max(hL - maxHeightDiffX, hR - maxHeightDiffX,
                                         hD - maxHeightDiffZ, hU - maxHeightDiffZ);
            float maxAllowed = Mathf.Min(hL + maxHeightDiffX, hR + maxHeightDiffX,
                                         hD + maxHeightDiffZ, hU + maxHeightDiffZ);
            
            if (h < minAllowed) {
              temp[y, x] = Mathf.Lerp(h, minAllowed, 0.5f);
            } else if (h > maxAllowed) {
              temp[y, x] = Mathf.Lerp(h, maxAllowed, 0.5f);
            }
          }
        }
        
        System.Array.Copy(temp, heights, heights.Length);
      }
    }

    protected override void RollbackInternal(GenerationContext ctx) {
      if (ctx.OriginalHeightmap != null) {
        ctx.Terrain.terrainData.SetHeights(0, 0, ctx.OriginalHeightmap);
      }
      ctx.Heightmap = null;
      ctx.RiverMask = null;
    }

    protected override Material CreateDebugMaterial(GenerationContext ctx) {
      return null;
    }
  }
}
