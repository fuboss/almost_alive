using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Controls structure cutout effect via MaterialPropertyBlock.
  /// Supports plane-based cutout with configurable shape/falloff.
  /// </summary>
  public class StructureCutoutController : MonoBehaviour {
    [Title("Cutout Settings")] [SerializeField]
    private CutoutSettings _settings;

    [Title("Target Renderers")] [Tooltip("Wall/roof renderers to apply cutout. Auto-collect if empty")] [SerializeField]
    private Renderer[] _targetRenderers;

    [Title("Runtime State")] [ShowInInspector, ReadOnly]
    private bool _isActive;

    [ShowInInspector, ReadOnly] private float _currentIntensity;
    [SerializeField, Range(0f, 1f)] private float _targetIntensity;

    [Title("Cutout Plane")] [SerializeField]
    private Vector3 _cutoutCenter;

    [SerializeField] private float _cutoutRadius = 5f;

    private MaterialPropertyBlock _propBlock;
    private static readonly int CutoutEnabled = Shader.PropertyToID("_CutoutEnabled");
    private static readonly int CutoutCenter = Shader.PropertyToID("_CutoutCenter");
    private static readonly int CutoutRadius = Shader.PropertyToID("_CutoutRadius");
    private static readonly int CutoutFalloff = Shader.PropertyToID("_CutoutFalloff");
    private static readonly int CutoutIntensity = Shader.PropertyToID("_CutoutIntensity");

    #region Lifecycle

    private void Awake() {
      _propBlock = new MaterialPropertyBlock();

      if (_targetRenderers == null || _targetRenderers.Length == 0) {
        CollectRenderers();
      }

      // Enable cutout keyword on materials
      foreach (var r in _targetRenderers) {
        if (r != null && r.sharedMaterial != null) {
          r.sharedMaterial.EnableKeyword("_CUTOUT_ENABLED");
        }
      }

      if (_settings == null) {
        Debug.LogWarning($"[CutoutController] No settings on {name}", this);
      }
    }

    private void OnEnable() {
      if (_isActive) ApplyCutout();
    }

    private void LateUpdate() {
      if (!_isActive) return;

      if (Mathf.Abs(_currentIntensity - _targetIntensity) > 0.001f) {
        var speed = _settings != null ? _settings.transitionSpeed : 5f;
        _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * speed);

        ApplyCutout();
      }
    }

    #endregion

    #region Public API

    [Button("Enable Cutout"), HideInEditorMode]
    public void EnableCutout() {
      _isActive = true;
      _targetIntensity = 1f;
    }

    [Button("Disable Cutout"), HideInEditorMode]
    public void DisableCutout() {
      _isActive = false;
      _targetIntensity = 0f;
      ClearCutout();
    }

    public void SetCutoutIntensity(float intensity) {
      _targetIntensity = Mathf.Clamp01(intensity);
      if (!_isActive && _targetIntensity > 0f) _isActive = true;
    }

    public void SetCutoutCenter(Vector3 worldPos) {
      _cutoutCenter = worldPos;
      if (_isActive) ApplyCutout();
    }

    public void SetCutoutRadius(float radius) {
      _cutoutRadius = Mathf.Max(0.1f, radius);
      if (_isActive) ApplyCutout();
    }

    #endregion

    #region Internal

    private void ApplyCutout() {
      if (_targetRenderers == null || _targetRenderers.Length == 0) {
        return;
      }

      var enabled = _currentIntensity > 0.01f ? 1f : 0f;
      var falloff = _settings != null ? _settings.falloffCurve.Evaluate(_currentIntensity) : 0.5f;

      _propBlock.SetFloat(CutoutEnabled, enabled);
      _propBlock.SetVector(CutoutCenter, _cutoutCenter);
      _propBlock.SetFloat(CutoutRadius, _cutoutRadius);
      _propBlock.SetFloat(CutoutFalloff, falloff);
      _propBlock.SetFloat(CutoutIntensity, _currentIntensity);

      foreach (var r in _targetRenderers) {
        if (r != null) r.SetPropertyBlock(_propBlock);
      }
    }

    private void ClearCutout() {
      if (_targetRenderers == null) return;

      _propBlock.SetFloat(CutoutEnabled, 0f);

      foreach (var r in _targetRenderers) {
        if (r != null) r.SetPropertyBlock(_propBlock);
      }
    }

    private void CollectRenderers() {
      _targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected() {
      if (!_isActive) return;

      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(_cutoutCenter, _cutoutRadius);

      Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
      Gizmos.DrawSphere(_cutoutCenter, _cutoutRadius);
    }

    #endregion
  }
}