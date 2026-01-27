namespace Content.Scripts.World.Vegetation.Mask {
  public enum MaskMode { None = 0, Perlin = 1, Voronoi = 2 }

  [System.Serializable]
  public class MaskSettings {
    public MaskMode mode = MaskMode.Perlin;

    // Perlin / FBM
    public float scale = 0.02f;
    public int fbmOctaves = 3;
    public float fbmPersistence = 0.5f;
    public float fbmLacunarity = 2f;

    // Thresholding to get harder islands
    public float threshold = 0.6f;
    public float blend = 0.12f;

    // Voronoi
    public int voronoiSites = 64;
    public float voronoiJitter = 0.7f;
    public int voronoiDownsample = 4;
    public float voronoiFalloff = 0.35f;

    // Stochastic culling
    public bool useStochasticCulling = false;
    public float stochasticBlend = 0f; // 0..1

    public bool cacheEnabled = true;
    public int seedOffset = 1000;

    // default constructor
    public MaskSettings() { }
  }
}
