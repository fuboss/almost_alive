using System;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// State of vegetation in a single cell.
  /// Used for runtime manipulation (fire, clearing, etc.)
  /// </summary>
  [Serializable]
  public struct VegetationCell {
    /// <summary>Vegetation density per prototype layer (0-32)</summary>
    public byte[] densities;
    
    /// <summary>Burn progress 0-1 (0=healthy, 1=fully burned)</summary>
    public float burnProgress;
    
    /// <summary>Is currently on fire</summary>
    public bool isOnFire;
    
    /// <summary>Cell has been modified since last sync</summary>
    public bool isDirty;

    public static VegetationCell Create(int layerCount) {
      return new VegetationCell {
        densities = new byte[layerCount],
        burnProgress = 0f,
        isOnFire = false,
        isDirty = false
      };
    }

    public void SetDensity(int layer, byte value) {
      if (layer < 0 || layer >= densities.Length) return;
      if (densities[layer] == value) return;
      densities[layer] = value;
      isDirty = true;
    }

    public byte GetDensity(int layer) {
      if (layer < 0 || layer >= densities.Length) return 0;
      return densities[layer];
    }

    /// <summary>
    /// Apply burn damage, reducing density based on burnProgress.
    /// </summary>
    public void ApplyBurn(float damage) {
      burnProgress = Mathf.Clamp01(burnProgress + damage);
      
      // Reduce all densities based on burn progress
      for (var i = 0; i < densities.Length; i++) {
        var original = densities[i];
        var reduced = (byte)Mathf.RoundToInt(original * (1f - burnProgress));
        if (reduced != original) {
          densities[i] = reduced;
          isDirty = true;
        }
      }
      
      // Extinguish if fully burned
      if (burnProgress >= 1f) {
        isOnFire = false;
      }
    }

    /// <summary>
    /// Clear all vegetation in this cell.
    /// </summary>
    public void Clear() {
      for (var i = 0; i < densities.Length; i++) {
        if (densities[i] > 0) {
          densities[i] = 0;
          isDirty = true;
        }
      }
      burnProgress = 1f;
      isOnFire = false;
    }

    public bool HasVegetation {
      get {
        for (var i = 0; i < densities.Length; i++) {
          if (densities[i] > 0) return true;
        }
        return false;
      }
    }

    public int TotalDensity {
      get {
        var total = 0;
        for (var i = 0; i < densities.Length; i++) {
          total += densities[i];
        }
        return total;
      }
    }
  }
}
