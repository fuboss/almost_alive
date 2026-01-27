using UnityEngine;

namespace Content.Scripts.DebugPanel {
  /// <summary>
  /// Centralized style constants for DebugPanelUI.
  /// </summary>
  public static class DebugPanelStyles {
    // Colors
    public static readonly Color PanelColor = new(0.2f, 0.2f, 0.2f, 0.9f);
    public static readonly Color ButtonNormal = new(0.3f, 0.3f, 0.3f, 1f);
    public static readonly Color ButtonHover = Color.white;
    public static readonly Color ButtonActive = new(0.5f, 0.5f, 0.2f, 1f);
    public static readonly Color ButtonGroupColor = new(0.25f, 0.35f, 0.25f, 1f);
    public static readonly Color ButtonBackColor = new(0.35f, 0.25f, 0.25f, 1f);
    public static readonly Color TextColor = Color.white;
    public static readonly Color TextActive = Color.yellow;
    public static readonly Color TextMuted = new(0.6f, 0.6f, 0.6f, 1f);
    
    // Sizes
    public const float ToggleSize = 40f;
    public const float ButtonHeight = 30f;
    public const float OptionHeight = 28f;
    public const float ButtonWidth = 100f;
    public const float DropdownWidth = 280f;
    public const float DropdownOptionWidth = 270f;
    public const float StatusTextWidth = 180f;
    
    // Layout
    public const int FontSize = 14;
    public const float Padding = 5f;
    public const float Spacing = 5f;
    public const float DropdownSpacing = 2f;
    public const int DropdownPadding = 5;
    
    // Positioning
    public const float PanelTopOffset = -10f;
    public const float DropdownYOffset = -60f;
  }
}
