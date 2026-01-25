using Content.Scripts.Building.Services.Visuals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime.Visuals {
  /// <summary>
  /// Component marking a decoration with visibility rules.
  /// Placed on decoration GameObjects in structure foundation prefab.
  /// </summary>
  public class StructureDecoration : MonoBehaviour {
    [Title("Visibility")]
    [Tooltip("When this decoration should be visible")]
    public DecorationVisibilityMode visibilityMode = DecorationVisibilityMode.Always;
    
    [ShowIf("visibilityMode", DecorationVisibilityMode.OnConstruction)]
    [Range(0f, 1f)]
    [Tooltip("Construction progress threshold (0-1) when decoration appears")]
    public float constructionThreshold = 0f;
    
    [ShowIf("visibilityMode", DecorationVisibilityMode.WithModule)]
    [Tooltip("Module tag required for this decoration to be visible")]
    public string requiredModuleTag;
    
    [Title("Animation")]
    [Tooltip("Animate show/hide transitions")]
    public bool animate = true;
    
    [ShowIf("animate")]
    [Tooltip("Animation duration in seconds")]
    [MinValue(0.1f)]
    public float fadeDuration = 0.5f;
    
    // Runtime state
    private bool _isVisible;
    private Renderer[] _renderers;

    public bool isVisible => _isVisible;
    public Renderer[] renderers => _renderers;

    private void Awake() {
      // Cache all renderers in children
      _renderers = GetComponentsInChildren<Renderer>(true);
      
      // Start hidden for non-Always modes
      if (visibilityMode != DecorationVisibilityMode.Always) {
        SetVisibleImmediate(false);
      }
    }

    /// <summary>
    /// Evaluate if decoration should be visible given current context.
    /// </summary>
    public bool ShouldBeVisible(VisualsContext context) {
      switch (visibilityMode) {
        case DecorationVisibilityMode.Always:
          return true;
          
        case DecorationVisibilityMode.OnConstruction:
          // Show when progress >= threshold
          return context.isUnfinished && context.constructionProgress >= constructionThreshold;
          
        case DecorationVisibilityMode.AfterCoreModule:
          // Show only after core is built
          return !context.isUnfinished && context.isCoreBuilt;
          
        case DecorationVisibilityMode.WithModule:
          // Show when module with required tag is installed
          if (string.IsNullOrEmpty(requiredModuleTag)) return false;
          return !context.isUnfinished && context.installedModuleTags.Contains(requiredModuleTag);
          
        case DecorationVisibilityMode.Custom:
          // Override in derived classes
          return EvaluateCustomVisibility(context);
          
        default:
          return false;
      }
    }

    /// <summary>
    /// Custom visibility evaluation. Override in derived classes.
    /// </summary>
    protected virtual bool EvaluateCustomVisibility(VisualsContext context) {
      return false;
    }

    /// <summary>
    /// Set visibility with optional animation via strategy.
    /// Called by StructureVisualsModule.
    /// </summary>
    public void SetVisible(bool visible, IDecorationAnimationStrategy animStrategy) {
      if (_isVisible == visible) return;
      
      _isVisible = visible;
      
      if (animate && animStrategy != null) {
        if (visible) {
          animStrategy.Show(this, fadeDuration);
        }
        else {
          animStrategy.Hide(this, fadeDuration);
        }
      }
      else {
        SetVisibleImmediate(visible);
      }
    }

    /// <summary>
    /// Set visibility immediately without animation.
    /// </summary>
    private void SetVisibleImmediate(bool visible) {
      if (_renderers == null || _renderers.Length == 0) return;
      
      foreach (var renderer in _renderers) {
        if (renderer != null) {
          renderer.enabled = visible;
        }
      }
    }

    #region Editor

#if UNITY_EDITOR
    [Button("Test Show")]
    [PropertyOrder(100)]
    private void EditorTestShow() {
      if (!Application.isPlaying) {
        Debug.LogWarning("Test only works in Play mode");
        return;
      }
      
      var strategy = new FadeAnimationStrategy();
      SetVisible(true, strategy);
    }

    [Button("Test Hide")]
    [PropertyOrder(100)]
    private void EditorTestHide() {
      if (!Application.isPlaying) {
        Debug.LogWarning("Test only works in Play mode");
        return;
      }
      
      var strategy = new FadeAnimationStrategy();
      SetVisible(false, strategy);
    }
#endif

    #endregion
  }
}
