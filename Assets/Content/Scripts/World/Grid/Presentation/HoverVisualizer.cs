using UnityEngine;

namespace Content.Scripts.World.Grid.Presentation {
  /// <summary>
  /// Visualizer for single grid cell hover highlight.
  /// Uses TileMeshRenderer for efficient terrain-adaptive rendering.
  /// </summary>
  public class HoverVisualizer : MonoBehaviour, IHoverVisualizer {
    private WorldGridPresentationConfigSO _config;
    private TileMeshRenderer _tileRenderer;
    
    private bool _isVisible;
    private float _pulseTime;
    private bool _isValid;
    private GroundCoord _currentCoord;

    public void Initialize(WorldGridPresentationConfigSO config) {
      _config = config;
      
      // Create tile renderer
      _tileRenderer = gameObject.AddComponent<TileMeshRenderer>();
      _tileRenderer.Initialize(config.Data.tileMaterial);
      
      Debug.Log("[HoverVisualizer] Initialized with TileMeshRenderer");
    }

    public void ShowHover(GroundCoord coord, bool isValid) {
      _isVisible = true;
      _isValid = isValid;
      _currentCoord = coord;
      _pulseTime = 0f;
      
      // Show tile with appropriate color
      var color = isValid ? _config.Data.hoverColorValid : _config.Data.hoverColorInvalid;
      _tileRenderer.HideAll();
      _tileRenderer.ShowTile(coord, color, borderOnly: true);
    }

    public void Hide() {
      _isVisible = false;
      _tileRenderer?.HideAll();
    }

    private void Update() {
      if (!_isVisible || _tileRenderer == null) return;
      
      // Pulse animation
      _pulseTime += Time.deltaTime * _config.Data.hoverPulseSpeed;
      var pulseValue = _config.Data.hoverPulseCurve.Evaluate(_pulseTime % 1f);
      
      // Update color with pulse
      var baseColor = _isValid ? _config.Data.hoverColorValid : _config.Data.hoverColorInvalid;
      var pulsedColor = baseColor;
      pulsedColor.a *= pulseValue;
      
      _tileRenderer.UpdateTileColor(_currentCoord, pulsedColor);
    }
  }
}
