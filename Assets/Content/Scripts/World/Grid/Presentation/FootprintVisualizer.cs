using UnityEngine;

namespace Content.Scripts.World.Grid.Presentation {
  /// <summary>
  /// Visualizer for multi-cell structure footprint preview.
  /// Uses TileMeshRenderer for efficient terrain-adaptive rendering.
  /// </summary>
  public class FootprintVisualizer : MonoBehaviour, IFootprintVisualizer {
    private WorldGridPresentationConfigSO _config;
    private TileMeshRenderer _tileRenderer;

    private bool _isVisible;
    private bool _isValid;

    public void Initialize(WorldGridPresentationConfigSO config) {
      _config = config;

      // Create tile renderer
      _tileRenderer = gameObject.AddComponent<TileMeshRenderer>();
      _tileRenderer.Initialize(config.tileMaterial);

      Debug.Log("[FootprintVisualizer] Initialized with TileMeshRenderer");
    }

    public void ShowFootprint(GroundCoord origin, Vector2Int footprint, bool isValid) {
      _isVisible = true;
      _isValid = isValid;

      var color = isValid ? _config.footprintColorValid : _config.footprintColorInvalid;

      _tileRenderer.HideAll();
      _tileRenderer.ShowFootprint(origin, footprint, color, borderOnly: false);

      Debug.Log($"[FootprintVisualizer] Showing footprint {footprint} at {origin}");
    }

    public void ShowFootprintAtWorldPos(Vector3 worldPos, Vector2Int footprint, bool isValid) {
      _isVisible = true;
      _isValid = isValid;

      var color = isValid ? _config.footprintColorValid : _config.footprintColorInvalid;

      _tileRenderer.HideAll();

      // For structures, we want tiles starting from the raycast point
      // Calculate origin coord from world position
      var cellSize = WorldGrid.cellSize;
      var originCoord = GroundCoord.FromWorld(worldPos);

      // Show each cell in the footprint
      for (int x = 0; x < footprint.x; x++) {
        for (int z = 0; z < footprint.y; z++) {
          var cellCoord = new GroundCoord(originCoord.x + x, originCoord.z + z);
          _tileRenderer.ShowTile(cellCoord, color, borderOnly: false);
        }
      }
    }

    public void Hide() {
      _isVisible = false;
      _tileRenderer?.HideAll();
    }

    /// <summary>
    /// Ensure pool has enough capacity (for compatibility).
    /// TileMeshRenderer handles this automatically.
    /// </summary>
    public void EnsureQuadPool(int requiredCount) {
      // No-op: TileMeshRenderer handles pooling internally
    }
  }
}