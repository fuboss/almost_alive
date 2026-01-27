using System.Collections.Generic;

namespace Content.Scripts.World.Vegetation.Mask {
  public static class MaskCache {
    private static readonly Dictionary<string, float[,]> s_cache = new Dictionary<string, float[,]>();

    public static bool TryGet(string key, out float[,] mask) {
      return s_cache.TryGetValue(key, out mask);
    }

    public static void Add(string key, float[,] mask) {
      if (s_cache.ContainsKey(key)) return;
      s_cache[key] = mask;
    }

    public static void Clear() {
      s_cache.Clear();
    }
  }
}
