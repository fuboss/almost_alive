using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Samplers {
  /// <summary>
  /// Cellular/Voronoi/Worley noise using Unity.Mathematics.
  /// Creates cell-like patterns, great for biome boundaries, cracks, organic shapes.
  /// Output range: [0, 1] (distance-based)
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Cellular", fileName = "Noise_Cellular")]
  public class CellularNoiseSO : NoiseSO {
    
    public enum CellularType {
      F1,           // Distance to nearest point
      F2,           // Distance to second nearest
      F2MinusF1,    // Cell edges (F2 - F1)
      F1PlusF2      // Smooth blend
    }

    [BoxGroup("Cellular")]
    [Tooltip("Which distance metric to use")]
    public CellularType cellType = CellularType.F1;

    [BoxGroup("Cellular")]
    [Tooltip("Randomness of cell centers (0 = grid, 1 = random)")]
    [Range(0f, 1f)]
    public float jitter = 1f;

    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      var seedOffset2D = new float2(seed * 0.31f, seed * 0.47f);
      var pos = new float2(x, y) + seedOffset2D;
      
      // Unity.Mathematics cellular returns float2(F1, F2)
      var cell = noise.cellular(pos);
      
      // Apply jitter by lerping between grid and random
      if (jitter < 1f) {
        var gridCell = GetGridCellDistance(pos);
        cell = math.lerp(gridCell, cell, jitter);
      }

      return cellType switch {
        CellularType.F1 => cell.x,
        CellularType.F2 => cell.y,
        CellularType.F2MinusF1 => cell.y - cell.x,
        CellularType.F1PlusF2 => (cell.x + cell.y) * 0.5f,
        _ => cell.x
      };
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      var seedOffset3D = new float3(seed * 0.31f, seed * 0.47f, seed * 0.59f);
      var pos = new float3(x, y, z) + seedOffset3D;
      var cell = noise.cellular(pos);
      
      return cellType switch {
        CellularType.F1 => cell.x,
        CellularType.F2 => cell.y,
        CellularType.F2MinusF1 => cell.y - cell.x,
        CellularType.F1PlusF2 => (cell.x + cell.y) * 0.5f,
        _ => cell.x
      };
    }

    private float2 GetGridCellDistance(float2 pos) {
      var cell = math.floor(pos);
      var frac = pos - cell;
      
      // Distance to cell center
      var toCenter = frac - 0.5f;
      var f1 = math.length(toCenter);
      
      // Approximate F2 as distance to nearest edge
      var toEdge = 0.5f - math.abs(toCenter);
      var f2 = math.min(toEdge.x, toEdge.y) + f1;
      
      return new float2(f1, f2);
    }

    protected override int GetParameterHash() {
      return HashCode.Combine(base.GetParameterHash(), cellType, jitter);
    }
  }
}
