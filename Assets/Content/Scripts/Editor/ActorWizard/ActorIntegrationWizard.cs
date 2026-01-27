#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Scripts.AI;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Game;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Content.Scripts.Editor.ActorWizard {
  public class ActorIntegrationWizard : EditorWindow {
    private const string ACTORS_FOLDER = "Assets/Content/Prefabs/Actors";
    private const string ACTORS_GROUP = "Actors";
    private const string ACTORS_LABEL = "Actors";

    private GameObject _sourcePrefab;
    private string _actorKey = "";
    private bool _isSelectable = true;
    private float _rememberDuration = 300f;

    private readonly HashSet<string> _selectedTags = new();
    private Vector2 _scrollPos;
    
    // ItemTag specific
    private bool _isItem = false;
    private float _itemWeight = 1f;
    private int _stackMax = 10;

    [MenuItem("GOAP/Actor Integration Wizard", priority = 3)]
    public static void Open() {
      var window = GetWindow<ActorIntegrationWizard>("Actor Wizard");
      window.minSize = new Vector2(450, 600);
    }

    private void OnGUI() {
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Actor Integration Wizard", EditorStyles.boldLabel);
      EditorGUILayout.Space(5);
      
      DrawSourcePrefabSection();
      EditorGUILayout.Space(10);
      
      if (_sourcePrefab != null) {
        DrawConfigSection();
        EditorGUILayout.Space(10);
        DrawTagsSection();
        EditorGUILayout.Space(10);
        DrawItemSection();
        EditorGUILayout.Space(10);
        DrawCreateButton();
      }
    }

    private void DrawSourcePrefabSection() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Source Prefab", EditorStyles.boldLabel);
      
      var newPrefab = (GameObject)EditorGUILayout.ObjectField(_sourcePrefab, typeof(GameObject), false);
      if (newPrefab != _sourcePrefab) {
        _sourcePrefab = newPrefab;
        if (_sourcePrefab != null) {
          _actorKey = _sourcePrefab.name.ToLowerInvariant().Replace(" ", "_");
        }
      }
      
      EditorGUILayout.EndVertical();
    }

    private void DrawConfigSection() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
      
      _actorKey = EditorGUILayout.TextField("Actor Key", _actorKey);
      _isSelectable = EditorGUILayout.Toggle("Is Selectable", _isSelectable);
      _rememberDuration = EditorGUILayout.FloatField("Remember Duration", _rememberDuration);
      
      EditorGUILayout.EndVertical();
    }

    private void DrawTagsSection() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
      
      _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
      
      foreach (var tag in Tag.ALL_TAGS) {
        var isSelected = _selectedTags.Contains(tag);
        var newSelected = EditorGUILayout.ToggleLeft(tag, isSelected);
        
        if (newSelected != isSelected) {
          if (newSelected) {
            _selectedTags.Add(tag);
            // Auto-enable item section if ITEM tag selected
            if (tag == Tag.ITEM) _isItem = true;
          } else {
            _selectedTags.Remove(tag);
            if (tag == Tag.ITEM) _isItem = false;
          }
        }
      }
      
      EditorGUILayout.EndScrollView();
      EditorGUILayout.EndVertical();
    }

    private void DrawItemSection() {
      if (!_selectedTags.Contains(Tag.ITEM)) return;
      
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Item Properties", EditorStyles.boldLabel);
      
      _itemWeight = EditorGUILayout.FloatField("Weight", _itemWeight);
      _stackMax = EditorGUILayout.IntField("Stack Max", _stackMax);
      
      EditorGUILayout.EndVertical();
    }

    private void DrawCreateButton() {
      var valid = ValidateInput(out var error);
      
      if (!string.IsNullOrEmpty(error)) {
        EditorGUILayout.HelpBox(error, MessageType.Warning);
      }
      
      GUI.enabled = valid;
      if (GUILayout.Button("Create Actor Prefab", GUILayout.Height(40))) {
        CreateActorPrefab();
      }
      GUI.enabled = true;
    }

    private bool ValidateInput(out string error) {
      error = "";
      
      if (_sourcePrefab == null) {
        error = "Source prefab is required";
        return false;
      }
      
      if (string.IsNullOrWhiteSpace(_actorKey)) {
        error = "Actor key cannot be empty";
        return false;
      }
      
      if (!IsValidActorKey(_actorKey)) {
        error = "Actor key must be lowercase with underscores only";
        return false;
      }
      
      var targetPath = GetTargetPrefabPath();
      if (File.Exists(targetPath)) {
        error = $"Prefab already exists at {targetPath}";
        return false;
      }
      
      return true;
    }

    private void CreateActorPrefab() {
      try {
        // Ensure folder exists
        Directory.CreateDirectory(ACTORS_FOLDER);
        
        // Instantiate source prefab
        var instance = Instantiate(_sourcePrefab);
        instance.name = _actorKey;
        
        // Add ActorId
        if (instance.GetComponent<ActorId>() == null) {
          instance.AddComponent<ActorId>();
        }
        
        // Add ActorDescription
        var description = instance.GetComponent<ActorDescription>();
        if (description == null) {
          description = instance.AddComponent<ActorDescription>();
        }
        
        // Configure description
        var descData = new DescriptionData {
          tags = _selectedTags.ToArray(),
          rememberDuration = _rememberDuration
        };
        
        var descType = typeof(ActorDescription);
        var field = descType.GetField("_descriptionData", 
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(description, descData);
        
        var actorKeyField = descType.GetField("actorKey", 
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        actorKeyField?.SetValue(description, _actorKey);
        
        var selectableField = descType.GetField("isSelectable", 
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        selectableField?.SetValue(description, _isSelectable);
        
        // Add TagDefinition components
        AddTagComponents(instance);
        
        // Save prefab
        var targetPath = GetTargetPrefabPath();
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
        DestroyImmediate(instance);
        
        // Register in Addressables
        RegisterInAddressables(targetPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select created prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"[ActorWizard] Created actor prefab: {_actorKey} at {targetPath}");
        
        // Reset wizard
        _sourcePrefab = null;
        _selectedTags.Clear();
        _actorKey = "";
        
      } catch (Exception e) {
        EditorUtility.DisplayDialog("Error", $"Failed to create actor prefab:\n{e.Message}", "OK");
        Debug.LogError($"[ActorWizard] Error: {e}");
      }
    }

    private void AddTagComponents(GameObject instance) {
      foreach (var tag in _selectedTags) {
        var tagType = GetTagDefinitionType(tag);
        if (tagType == null) {
          Debug.LogWarning($"[ActorWizard] No TagDefinition class found for tag: {tag}");
          continue;
        }
        
        var component = instance.GetComponent(tagType);
        if (component == null) {
          component = instance.AddComponent(tagType);
        }
        
        // Configure ItemTag if needed
        if (tag == Tag.ITEM && component is ItemTag itemTag) {
          var weightField = typeof(ItemTag).GetField("weight", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
          weightField?.SetValue(itemTag, _itemWeight);
          
          var stackField = typeof(ItemTag).GetField("stackData", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
          stackField?.SetValue(itemTag, new StackData { max = _stackMax, current = 1 });
        }
      }
    }

    private Type GetTagDefinitionType(string tag) {
      var className = ToPascalCase(tag) + "Tag";
      var assembly = typeof(TagDefinition).Assembly;
      return assembly.GetTypes().FirstOrDefault(t => t.Name == className && typeof(TagDefinition).IsAssignableFrom(t));
    }

    private void RegisterInAddressables(string assetPath) {
      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) {
        Debug.LogError("[ActorWizard] Addressables settings not found");
        return;
      }
      
      // Find or create Actors group
      var group = settings.FindGroup(ACTORS_GROUP);
      if (group == null) {
        Debug.LogError($"[ActorWizard] Addressables group '{ACTORS_GROUP}' not found");
        return;
      }
      
      // Create entry
      var guid = AssetDatabase.AssetPathToGUID(assetPath);
      var entry = settings.CreateOrMoveEntry(guid, group, false, false);
      entry.address = _actorKey;
      
      // Add label
      settings.AddLabel(ACTORS_LABEL);
      entry.SetLabel(ACTORS_LABEL, true);
      
      settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private string GetTargetPrefabPath() {
      return Path.Combine(ACTORS_FOLDER, $"{_actorKey}.prefab");
    }

    private static bool IsValidActorKey(string key) {
      return System.Text.RegularExpressions.Regex.IsMatch(key, @"^[a-z][a-z0-9_]*$");
    }

    private static string ToPascalCase(string input) {
      return string.Join("", input.Split('_')
        .Select(s => s.Length > 0 
          ? char.ToUpper(s[0]) + s.Substring(1).ToLower() 
          : ""));
    }
  }
}
#endif
