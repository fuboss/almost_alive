using DG.Tweening;
using UnityEngine;

namespace Content.Scripts.Building.Services.Visuals {
  /// <summary>
  /// Fade-only animation strategy using DOTween.
  /// </summary>
  public class FadeAnimationStrategy : IDecorationAnimationStrategy {
    public void Show(Runtime.Visuals.StructureDecoration decoration, float duration) {
      if (decoration == null) return;

      var renderers = decoration.renderers;
      if (renderers == null || renderers.Length == 0) return;

      foreach (var renderer in renderers) {
        if (renderer == null) continue;

        // Enable renderer first
        renderer.enabled = true;

        // Get all materials and fade them
        foreach (var mat in renderer.materials) {
          if (mat == null) continue;

          // Check if material supports transparency
          if (!mat.HasProperty("_Color")) continue;

          var color = mat.color;
          color.a = 0f;
          mat.color = color;

          mat.DOFade(1f, duration).SetEase(Ease.OutQuad);
        }
      }
    }

    public void Hide(Runtime.Visuals.StructureDecoration decoration, float duration) {
      if (decoration == null) return;

      var renderers = decoration.renderers;
      if (renderers == null || renderers.Length == 0) return;

      foreach (var renderer in renderers) {
        if (renderer == null) continue;

        // Fade out all materials
        foreach (var mat in renderer.materials) {
          if (mat == null) continue;

          if (!mat.HasProperty("_Color")) continue;

          mat.DOFade(0f, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
              if (renderer != null) renderer.enabled = false;
            });
        }
      }
    }
  }
}
