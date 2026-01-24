using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Scripts.Animation;
using Content.Scripts.Editor.AnimatorGenerator;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class UniversalAnimatorGeneratorWindow : OdinEditorWindow {
    private const string AnimationsPath = "Assets/ThrirdParty/KayKit/Characters/Animations/Animations/Rig_Medium";
    private const string OutputPath = "Assets/Content/Animations";
    private const string ConfigPath = "Assets/Content/Animations/AnimatorGeneratorConfig.asset";

    private static readonly Dictionary<string, string> FolderMapping = new() {
      { "CombatMelee", "Combat Melee" },
      { "CombatRanged", "Combat Ranged" },
      { "General", "General" },
      { "MovementAdvanced", "Movement Advanced" },
      { "MovementBasic", "Movement Basic" },
      { "Simulation", "Simulation" },
      { "Special", "Special" },
      { "Tools", "Tools" }
    };

    [TitleGroup("Configuration")]
    [InlineEditor(InlineEditorModes.FullEditor)]
    [Required("Create a config first!")]
    public AnimatorGeneratorConfig config;

    [TitleGroup("Validation", Order = 100)]
    [ShowInInspector, ReadOnly, ProgressBar(0, 100, ColorGetter = "ValidationColor")]
    [LabelText("Clips Found")]
    private float ValidationProgress => _validationResult?.Progress ?? 0f;

    [TitleGroup("Validation")]
    [ShowInInspector, ReadOnly]
    [HideIf("@_validationResult?.Missing?.Count == 0")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true, NumberOfItemsPerPage = 10)]
    private List<string> MissingClips => _validationResult?.Missing ?? new List<string>();

    [TitleGroup("Animation Clips", Order = 110)]
    [ShowInInspector, ReadOnly]
    [LabelText("General")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, NumberOfItemsPerPage = 5)]
    private List<string> GeneralClips => GetClipsByCategory("General");

    [TitleGroup("Animation Clips")]
    [ShowInInspector, ReadOnly]
    [LabelText("Movement")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, NumberOfItemsPerPage = 5)]
    private List<string> MovementClips => GetClipsByCategory("Movement");

    [TitleGroup("Animation Clips")]
    [ShowInInspector, ReadOnly]
    [LabelText("Combat")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, NumberOfItemsPerPage = 5)]
    private List<string> CombatClips => GetClipsByCategory("Combat");

    [TitleGroup("Animation Clips")]
    [ShowInInspector, ReadOnly]
    [LabelText("Tools")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, NumberOfItemsPerPage = 5)]
    private List<string> ToolsClips => GetClipsByCategory("Tools");

    [TitleGroup("Animation Clips")]
    [ShowInInspector, ReadOnly]
    [LabelText("Social")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, NumberOfItemsPerPage = 5)]
    private List<string> SocialClips => GetClipsByCategory("Social");

    private List<string> GetClipsByCategory(string category) {
      if (_validationResult?.ByCategory == null) return new List<string>();
      if (!_validationResult.ByCategory.TryGetValue(category, out var clips)) return new List<string>();
      return clips.Select(c => $"{(c.Exists ? "✓" : "✗")} {c.Name} → {c.UsedIn}").ToList();
    }

    private ClipValidationResult _validationResult;
    private ClipValidator _validator;
    private AnimationClipProvider _clipProvider;

    private Color ValidationColor => ValidationProgress >= 100 ? Color.green :
                                     ValidationProgress >= 80 ? Color.yellow : Color.red;

    [MenuItem("Tools/Animation/Generate Universal Animator")]
    public static void ShowWindow() {
      var window = GetWindow<UniversalAnimatorGeneratorWindow>();
      window.titleContent = new GUIContent("Animator Generator");
      window.minSize = new Vector2(450, 600);
    }

    protected override void OnEnable() {
      base.OnEnable();
      LoadOrCreateConfig();
      InitializeServices();
      RunValidation();
    }

    private void LoadOrCreateConfig() {
      config = AssetDatabase.LoadAssetAtPath<AnimatorGeneratorConfig>(ConfigPath);
      if (config == null) {
        EnsureOutputDirectory();
        config = CreateInstance<AnimatorGeneratorConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);
        AssetDatabase.SaveAssets();
      }
    }

    private void InitializeServices() {
      _clipProvider = new AnimationClipProvider(AnimationsPath, FolderMapping);
      _validator = new ClipValidator(_clipProvider);
    }

    [TitleGroup("Actions", Order = 50)]
    [HorizontalGroup("Actions/Buttons")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.PlayFill), GUIColor(0.4f, 0.8f, 0.4f)]
    public void GenerateAnimator() {
      if (config == null) {
        EditorUtility.DisplayDialog("Error", "Config is missing!", "OK");
        return;
      }

      RunValidation();
      if (_validationResult.Found == 0) {
        EditorUtility.DisplayDialog("Error", "No animation clips found!", "OK");
        return;
      }

      EnsureOutputDirectory();

      var service = new AnimatorGeneratorService(config, AnimationsPath, OutputPath, FolderMapping);
      service.Generate();

      EditorUtility.DisplayDialog("Success",
        $"Animator Controller generated!\n\nClips: {_validationResult.Found}/{_validationResult.Total}\nMissing: {_validationResult.Missing.Count}",
        "OK");
    }

    [HorizontalGroup("Actions/Buttons")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.ArrowRepeat)]
    public void ValidateClips() {
      _clipProvider?.ClearCache();
      RunValidation();
    }

    private void RunValidation() {
      _validationResult = _validator?.Validate() ?? new ClipValidationResult(0, 0, new List<string>(), new List<string>(), new Dictionary<string, List<ClipInfo>>());
    }

    [HorizontalGroup("Actions/Buttons")]
    [Button(ButtonSizes.Large, Icon = SdfIconType.Mask)]
    public void GenerateMask() {
      EnsureOutputDirectory();

      var mask = new AvatarMask { name = "UpperBodyMask" };

      for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) {
        var part = (AvatarMaskBodyPart)i;
        bool include = part switch {
          AvatarMaskBodyPart.Body => true,
          AvatarMaskBodyPart.Head => true,
          AvatarMaskBodyPart.LeftArm => true,
          AvatarMaskBodyPart.RightArm => true,
          AvatarMaskBodyPart.LeftFingers => true,
          AvatarMaskBodyPart.RightFingers => true,
          _ => false
        };
        mask.SetHumanoidBodyPartActive(part, include);
      }

      AssetDatabase.CreateAsset(mask, $"{OutputPath}/UpperBodyMask.mask");
      AssetDatabase.SaveAssets();

      EditorUtility.DisplayDialog("Success", "Upper Body Mask generated!", "OK");
    }

    [TitleGroup("Quick Actions", Order = 90)]
    [ButtonGroup("Quick Actions/Row1")]
    [Button("Open Output Folder", Icon = SdfIconType.Folder)]
    private void OpenOutputFolder() {
      EnsureOutputDirectory();
      var obj = AssetDatabase.LoadAssetAtPath<Object>(OutputPath);
      if (obj != null) {
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
      }
    }

    [ButtonGroup("Quick Actions/Row1")]
    [Button("Select Controller", Icon = SdfIconType.FileEarmarkPlay)]
    private void SelectController() {
      var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{OutputPath}/UniversalAnimator.controller");
      if (controller != null) {
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
      } else {
        EditorUtility.DisplayDialog("Not Found", "Controller not generated yet!", "OK");
      }
    }

    [ButtonGroup("Quick Actions/Row1")]
    [Button("Export Config", Icon = SdfIconType.Download)]
    private void ExportConfig() {
      if (config == null) return;
      var json = JsonUtility.ToJson(config, true);
      var path = EditorUtility.SaveFilePanel("Export Config", "", "AnimatorConfig", "json");
      if (!string.IsNullOrEmpty(path)) {
        File.WriteAllText(path, json);
      }
    }

    [ButtonGroup("Quick Actions/Row1")]
    [Button("Import Config", Icon = SdfIconType.Upload)]
    private void ImportConfig() {
      var path = EditorUtility.OpenFilePanel("Import Config", "", "json");
      if (!string.IsNullOrEmpty(path) && config != null) {
        var json = File.ReadAllText(path);
        JsonUtility.FromJsonOverwrite(json, config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
      }
    }

    private void EnsureOutputDirectory() {
      if (!AssetDatabase.IsValidFolder(OutputPath)) {
        var parent = Path.GetDirectoryName(OutputPath)?.Replace("\\", "/") ?? "Assets";
        var folderName = Path.GetFileName(OutputPath);
        AssetDatabase.CreateFolder(parent, folderName);
      }
    }
  }
}

