using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Deterministic random generator for world generation.
  /// Isolated from UnityEngine.Random to prevent external interference.
  /// </summary>
  public class WorldRandom {
    private readonly System.Random _random;

    public WorldRandom(int seed) {
      _random = new System.Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive) {
      return _random.Next(minInclusive, maxExclusive);
    }

    public float Range(float min, float max) {
      return min + (float)_random.NextDouble() * (max - min);
    }

    public float Value => (float)_random.NextDouble();

    public Vector2 InsideUnitCircle() {
      var angle = Range(0f, Mathf.PI * 2f);
      var radius = Mathf.Sqrt(Value); // sqrt for uniform distribution
      return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    public Vector3 RandomPointInBounds(Bounds bounds) {
      return new Vector3(
        Range(bounds.min.x, bounds.max.x),
        0,
        Range(bounds.min.z, bounds.max.z)
      );
    }

    /// <summary>
    /// Fisher-Yates shuffle for deterministic randomization.
    /// </summary>
    public void Shuffle<T>(System.Collections.Generic.IList<T> list) {
      for (var i = list.Count - 1; i > 0; i--) {
        var j = Range(0, i + 1);
        (list[i], list[j]) = (list[j], list[i]);
      }
    }
  }
}
