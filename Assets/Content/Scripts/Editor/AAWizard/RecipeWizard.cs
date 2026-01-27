#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AAWizard {
  [Serializable]
  public class RecipeWizard {
    private const string RECIPES_FOLDER = "Assets/Content/Resources/Recipes";

    [HideInInspector] public bool _dummy; // Нужно для Odin

    private string _recipeId = "";
    private string _resultActorKey = "";
    private string _category = "Other";
    private string _displayName = "";
    private Sprite _icon;
    private int _buildPriority = 50;
    
    private float _craftTime = 5f;
    private CraftStationType _stationType = CraftStationType.None;
    private ushort _outputCount = 1;
    private float _workRequired = 10f;
    
    private List<ResourceEntry> _resources = new() { new ResourceEntry() };
    private Vector2 _scrollPos;

    [OnInspectorGUI]
    public void DrawGUI() {
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Recipe Wizard", EditorStyles.boldLabel);
      EditorGUILayout.Space(5);
      
      DrawBasicInfo();
      EditorGUILayout.Space(10);
      DrawCraftSettings();
      EditorGUILayout.Space(10);
      DrawResources();
      EditorGUILayout.Space(10);
      DrawCreateButton();
    }

    private void DrawBasicInfo() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
      
      _recipeId = EditorGUILayout.TextField("Recipe ID", _recipeId);
      
      var actorKeys = GOAPEditorHelper.GetActorKeys().ToList();
      var currentIndex = actorKeys.IndexOf(_resultActorKey);
      var newIndex = EditorGUILayout.Popup("Result Actor", currentIndex, actorKeys.ToArray());
      if (newIndex >= 0 && newIndex < actorKeys.Count) {
        _resultActorKey = actorKeys[newIndex];
      }
      
      _category = EditorGUILayout.TextField("Category", _category);
      _displayName = EditorGUILayout.TextField("Display Name", _displayName);
      _icon = (Sprite)EditorGUILayout.ObjectField("Icon", _icon, typeof(Sprite), false);
      _buildPriority = EditorGUILayout.IntSlider("Build Priority", _buildPriority, 0, 100);
      
      EditorGUILayout.EndVertical();
    }

    private void DrawCraftSettings() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Craft Settings", EditorStyles.boldLabel);
      
      _craftTime = EditorGUILayout.FloatField("Craft Time (sec)", _craftTime);
      _stationType = (CraftStationType)EditorGUILayout.EnumPopup("Station Type", _stationType);
      _outputCount = (ushort)EditorGUILayout.IntField("Output Count", _outputCount);
      _workRequired = EditorGUILayout.FloatField("Work Required", _workRequired);
      
      EditorGUILayout.EndVertical();
    }

    private void DrawResources() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Required Resources", EditorStyles.boldLabel);
      
      _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));
      
      for (int i = 0; i < _resources.Count; i++) {
        EditorGUILayout.BeginHorizontal();
        
        var resource = _resources[i];
        var tagIndex = Array.IndexOf(Tag.ALL_TAGS, resource.tag);
        var newTagIndex = EditorGUILayout.Popup("Tag", tagIndex, Tag.ALL_TAGS);
        if (newTagIndex >= 0) {
          resource.tag = Tag.ALL_TAGS[newTagIndex];
        }
        
        resource.count = (ushort)EditorGUILayout.IntField("Count", resource.count, GUILayout.Width(100));
        
        if (GUILayout.Button("×", GUILayout.Width(25))) {
          _resources.RemoveAt(i);
          i--;
        }
        
        EditorGUILayout.EndHorizontal();
      }
      
      EditorGUILayout.EndScrollView();
      
      if (GUILayout.Button("+ Add Resource")) {
        _resources.Add(new ResourceEntry());
      }
      
      EditorGUILayout.EndVertical();
    }

    private void DrawCreateButton() {
      var valid = ValidateInput(out var error);
      
      if (!string.IsNullOrEmpty(error)) {
        EditorGUILayout.HelpBox(error, MessageType.Warning);
      }
      
      GUI.enabled = valid;
      if (GUILayout.Button("Create Recipe", GUILayout.Height(40))) {
        CreateRecipe();
      }
      GUI.enabled = true;
    }

    private bool ValidateInput(out string error) {
      error = "";
      
      if (string.IsNullOrWhiteSpace(_recipeId)) {
        error = "Recipe ID cannot be empty";
        return false;
      }
      
      if (string.IsNullOrWhiteSpace(_resultActorKey)) {
        error = "Result Actor must be selected";
        return false;
      }
      
      if (_craftTime <= 0) {
        error = "Craft time must be positive";
        return false;
      }
      
      if (_outputCount <= 0) {
        error = "Output count must be positive";
        return false;
      }
      
      var assetPath = GetAssetPath();
      if (System.IO.File.Exists(assetPath)) {
        error = $"Recipe already exists: {assetPath}";
        return false;
      }
      
      return true;
    }

    private void CreateRecipe() {
      try {
        var recipe = ScriptableObject.CreateInstance<RecipeSO>();
        
        var recipeDataField = typeof(RecipeSO).GetField("data", 
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var recipeData = new RecipeData {
          resultActorKey = _resultActorKey,
          craftTime = _craftTime,
          stationType = _stationType,
          outputCount = _outputCount,
          workRequired = _workRequired
        };
        
        foreach (var res in _resources) {
          recipeData.requiredResources.Add(new RecipeRequiredResource {
            tag = res.tag,
            count = res.count
          });
        }
        
        recipeDataField?.SetValue(recipe, recipeData);
        
        typeof(RecipeSO).GetField("category")?.SetValue(recipe, _category);
        typeof(RecipeSO).GetField("displayName")?.SetValue(recipe, _displayName);
        typeof(RecipeSO).GetField("icon")?.SetValue(recipe, _icon);
        typeof(RecipeSO).GetField("buildPriority")?.SetValue(recipe, _buildPriority);
        
        var assetPath = GetAssetPath();
        AssetDatabase.CreateAsset(recipe, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Selection.activeObject = recipe;
        EditorGUIUtility.PingObject(recipe);
        
        Debug.Log($"[RecipeWizard] Created: {_recipeId}");
        Reset();
        
      } catch (Exception e) {
        EditorUtility.DisplayDialog("Error", $"Failed:\n{e.Message}", "OK");
        Debug.LogError($"[RecipeWizard] Error: {e}");
      }
    }

    private string GetAssetPath() => $"{RECIPES_FOLDER}/recipe_{_recipeId}.asset";

    private void Reset() {
      _recipeId = "";
      _resultActorKey = "";
      _category = "Other";
      _displayName = "";
      _icon = null;
      _buildPriority = 50;
      _craftTime = 5f;
      _stationType = CraftStationType.None;
      _outputCount = 1;
      _workRequired = 10f;
      _resources.Clear();
      _resources.Add(new ResourceEntry());
    }

    [Serializable]
    private class ResourceEntry {
      public string tag = Tag.WOOD;
      public ushort count = 1;
    }
  }
}
#endif