using System.Collections.Generic;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Result of validating a single biome's height settings.
  /// </summary>
  public class BiomeHeightValidation {
    public string biomeName;
    public float maxHeight;
    public float minHeight;
    public float heightRange;
    public float recommendedMaxStep;
    
    public List<string> warnings = new();
    public List<string> errors = new();
    
    public bool isValid => errors.Count == 0;
    public bool hasWarnings => warnings.Count > 0;
    
    public void AddWarning(string msg) => warnings.Add(msg);
    public void AddError(string msg) => errors.Add(msg);
  }

  /// <summary>
  /// Result of validating transition between two biomes.
  /// </summary>
  public class BiomeTransitionValidation {
    public string fromBiome;
    public string toBiome;
    public float blendDistance;
    public float worstCaseHeightDiff;
    public float resultingSlope;
    
    public List<string> warnings = new();
    public List<string> errors = new();
    
    public bool isValid => errors.Count == 0;
    public bool hasWarnings => warnings.Count > 0;
    
    public void AddWarning(string msg) => warnings.Add(msg);
    public void AddError(string msg) => errors.Add(msg);
  }

  /// <summary>
  /// Complete validation result for all biomes in a configuration.
  /// </summary>
  public class BiomeConfigValidation {
    public List<BiomeHeightValidation> biomeResults = new();
    public List<BiomeTransitionValidation> transitionResults = new();
    
    public int errorCount;
    public int warningCount;
    
    public bool isValid => errorCount == 0;
    public bool hasWarnings => warningCount > 0;
    
    public void Add(BiomeHeightValidation result) {
      biomeResults.Add(result);
      errorCount += result.errors.Count;
      warningCount += result.warnings.Count;
    }
    
    public void Add(BiomeTransitionValidation result) {
      transitionResults.Add(result);
      errorCount += result.errors.Count;
      warningCount += result.warnings.Count;
    }
  }
}
