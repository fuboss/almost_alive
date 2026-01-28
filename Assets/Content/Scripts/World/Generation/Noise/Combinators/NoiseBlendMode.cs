namespace Content.Scripts.World.Generation.Noise.Combinators {
  /// <summary>
  /// Blend modes for combining two noise values.
  /// </summary>
  public enum NoiseBlendMode {
    /// <summary>Linear interpolation: lerp(a, b, blend)</summary>
    Lerp,
    
    /// <summary>Addition: a + b</summary>
    Add,
    
    /// <summary>Subtraction: a - b</summary>
    Subtract,
    
    /// <summary>Multiplication: a * b</summary>
    Multiply,
    
    /// <summary>Minimum: min(a, b)</summary>
    Min,
    
    /// <summary>Maximum: max(a, b)</summary>
    Max,
    
    /// <summary>Screen blend: 1 - (1-a)*(1-b)</summary>
    Screen,
    
    /// <summary>Overlay: Photoshop-style overlay</summary>
    Overlay,
    
    /// <summary>Difference: abs(a - b)</summary>
    Difference,
    
    /// <summary>Use mask noise: a * mask + b * (1-mask)</summary>
    Mask
  }
}
