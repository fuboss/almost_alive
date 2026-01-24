#if UNITY_EDITOR
using Content.Scripts.Game.Camera.Settings;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Editor {
  /// <summary>
  /// Editor window for real-time camera calibration
  /// </summary>
  public class CameraCalibrationWindow : OdinEditorWindow {
    [MenuItem("Tools/Game/Camera Calibrator")]
    private static void OpenWindow() {
      GetWindow<CameraCalibrationWindow>("Camera Calibrator").Show();
    }

    [Title("Camera Settings Asset")]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Boxed)]
    [SerializeField]
    private CameraSettingsSO settings;

    [Title("Live Preview")]
    [ShowIf("@settings != null")]
    [Range(0f, 1f)]
    [OnValueChanged("UpdatePreview")]
    [SerializeField]
    private float previewZoom = 0.5f;

    [ShowIf("@settings != null")]
    [ReadOnly]
    [SerializeField]
    private float previewHeight;

    [ShowIf("@settings != null")]
    [ReadOnly]
    [SerializeField]
    private float previewPitch;

    [ShowIf("@settings != null")]
    [ReadOnly]
    [SerializeField]
    private float previewPanSpeed;

    [Title("Quick Presets")]
    [ShowIf("@settings != null")]
    [ButtonGroup("Presets")]
    private void ApplyRTSClassic() {
      if (settings == null) return;
      
      settings.heightRange = new Vector2(8f, 60f);
      settings.zoomToPitchCurve = AnimationCurve.EaseInOut(0f, 40f, 1f, 70f);
      settings.basePanSpeed = 25f;
      settings.zoomSpeed = 4f;
      EditorUtility.SetDirty(settings);
      UpdatePreview();
    }

    [ShowIf("@settings != null")]
    [ButtonGroup("Presets")]
    private void ApplyTactical() {
      if (settings == null) return;
      
      settings.heightRange = new Vector2(5f, 40f);
      settings.zoomToPitchCurve = AnimationCurve.EaseInOut(0f, 30f, 1f, 80f);
      settings.basePanSpeed = 20f;
      settings.zoomSpeed = 3f;
      EditorUtility.SetDirty(settings);
      UpdatePreview();
    }

    [ShowIf("@settings != null")]
    [ButtonGroup("Presets")]
    private void ApplyCinematic() {
      if (settings == null) return;
      
      settings.heightRange = new Vector2(3f, 25f);
      settings.zoomToPitchCurve = AnimationCurve.EaseInOut(0f, 20f, 1f, 55f);
      settings.basePanSpeed = 15f;
      settings.zoomSpeed = 2f;
      EditorUtility.SetDirty(settings);
      UpdatePreview();
    }

    [Title("Curve Editor")]
    [ShowIf("@settings != null")]
    [Button(ButtonSizes.Large)]
    private void ResetZoomCurve() {
      if (settings == null) return;
      
      settings.zoomToPitchCurve = AnimationCurve.EaseInOut(0f, 35f, 1f, 75f);
      EditorUtility.SetDirty(settings);
      UpdatePreview();
    }

    protected override void OnEnable() {
      base.OnEnable();
      
      // Try to find existing settings asset
      if (settings == null) {
        var guids = AssetDatabase.FindAssets("t:CameraSettingsSO");
        if (guids.Length > 0) {
          var path = AssetDatabase.GUIDToAssetPath(guids[0]);
          settings = AssetDatabase.LoadAssetAtPath<CameraSettingsSO>(path);
        }
      }
      
      UpdatePreview();
    }

    private void UpdatePreview() {
      if (settings == null) return;
      
      previewHeight = settings.GetHeightForZoom(previewZoom);
      previewPitch = settings.GetPitchForZoom(previewZoom);
      previewPanSpeed = settings.basePanSpeed * settings.GetPanSpeedMultiplier(previewZoom);
    }

    [Title("Create New Settings")]
    [HideIf("@settings != null")]
    [Button(ButtonSizes.Large)]
    private void CreateNewSettings() {
      var path = EditorUtility.SaveFilePanelInProject(
        "Create Camera Settings",
        "CameraSettings",
        "asset",
        "Choose location for camera settings"
      );

      if (string.IsNullOrEmpty(path)) return;

      settings = CreateInstance<CameraSettingsSO>();
      AssetDatabase.CreateAsset(settings, path);
      AssetDatabase.SaveAssets();
      UpdatePreview();
    }
  }
}
#endif

