namespace Content.Scripts.World.Generation.Noise {
  /// <summary>
  /// Interface for noise sampling algorithms.
  /// Implementations provide deterministic noise values based on coordinates.
  /// </summary>
  public interface INoiseSampler {
    /// <summary>
    /// Sample noise at 2D coordinates.
    /// </summary>
    /// <returns>Noise value, typically in range [0, 1] or [-1, 1] depending on implementation</returns>
    float Sample(float x, float y);
    
    /// <summary>
    /// Sample noise at 3D coordinates.
    /// </summary>
    float Sample(float x, float y, float z);
    
    /// <summary>
    /// Set the seed for deterministic generation.
    /// </summary>
    void SetSeed(int seed);
  }
}
