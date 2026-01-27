#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AAWizard {
  [Serializable]
  public class TagWizardComposite {
    private const string TAG_FILE = "Assets/Content/Scripts/AI/Tag.cs";
    private const string TAG_DEFINITIONS_FOLDER = "Assets/Content/Scripts/Descriptors/Tags";
    
    [HideInInspector] public bool _init;
    
    private Vector2 _scrollPos;
    private string _newTagName = "";
    private List<TagEntry> _tags = new();
    private string _searchFilter = "";

    [OnInspectorGUI]
    public void DrawGUI() {
      EditorGUILayout.Space(10);
      
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Tag Management", EditorStyles.boldLabel);
      if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshTags();
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.Space(5);
      _searchFilter = EditorGUILayout.TextField("Search", _searchFilter);
      EditorGUILayout.Space(10);
      
      DrawTagList();
      EditorGUILayout.Space(10);
      DrawAddSection();
      
      if (_tags.Count == 0) RefreshTags();
    }

    private void DrawTagList() {
      EditorGUILayout.LabelField($"Tags ({_tags.Count})", EditorStyles.boldLabel);
      
      _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(350));
      
      var filtered = string.IsNullOrEmpty(_searchFilter) 
        ? _tags 
        : _tags.Where(t => t.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

      foreach (var tag in filtered) {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        var statusIcon = tag.hasConstant && tag.hasDefinition ? "✓" : "⚠";
        var statusColor = tag.hasConstant && tag.hasDefinition ? Color.green : Color.yellow;
        var oldColor = GUI.color;
        GUI.color = statusColor;
        EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
        GUI.color = oldColor;
        
        EditorGUILayout.LabelField(tag.name, EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField(tag.hasConstant ? "const" : "no const", GUILayout.Width(60));
        EditorGUILayout.LabelField(tag.hasDefinition ? "class" : "no class", GUILayout.Width(60));
        
        GUILayout.FlexibleSpace();
        
        if (!tag.hasConstant || !tag.hasDefinition) {
          if (GUILayout.Button("Fix", GUILayout.Width(40))) {
            FixTag(tag);
          }
        }
        
        if (GUILayout.Button("×", GUILayout.Width(25))) {
          if (EditorUtility.DisplayDialog("Delete Tag", 
            $"Delete '{tag.name}'?\n\nRemoves:\n- Tag.cs constant\n- {tag.name}Tag.cs file", 
            "Delete", "Cancel")) {
            DeleteTag(tag);
          }
        }
        
        EditorGUILayout.EndHorizontal();
      }
      
      EditorGUILayout.EndScrollView();
    }

    private void DrawAddSection() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Add New Tag", EditorStyles.boldLabel);
      
      EditorGUILayout.BeginHorizontal();
      _newTagName = EditorGUILayout.TextField("Name", _newTagName);
      _newTagName = _newTagName.ToUpperInvariant().Replace(" ", "_");
      
      var canAdd = !string.IsNullOrWhiteSpace(_newTagName) && 
                   IsValidTagName(_newTagName) && 
                   !_tags.Any(t => t.name == _newTagName);
      
      GUI.enabled = canAdd;
      if (GUILayout.Button("Add", GUILayout.Width(60))) {
        AddTag(_newTagName);
        _newTagName = "";
      }
      GUI.enabled = true;
      EditorGUILayout.EndHorizontal();
      
      if (!string.IsNullOrWhiteSpace(_newTagName) && !canAdd) {
        var reason = !IsValidTagName(_newTagName) ? "Invalid name" : "Already exists";
        EditorGUILayout.HelpBox(reason, MessageType.Warning);
      }
      
      EditorGUILayout.EndVertical();
    }

    private void RefreshTags() {
      _tags.Clear();
      
      var constants = ParseTagConstants();
      var definitions = ScanTagDefinitions();
      
      var allNames = constants.Keys.Union(definitions.Keys).Distinct();
      foreach (var name in allNames) {
        _tags.Add(new TagEntry {
          name = name,
          hasConstant = constants.ContainsKey(name),
          hasDefinition = definitions.ContainsKey(name),
          definitionPath = definitions.GetValueOrDefault(name)
        });
      }
      
      _tags = _tags.OrderBy(t => t.name).ToList();
    }

    private Dictionary<string, bool> ParseTagConstants() {
      var result = new Dictionary<string, bool>();
      if (!File.Exists(TAG_FILE)) return result;
      
      var content = File.ReadAllText(TAG_FILE);
      var matches = Regex.Matches(content, @"public\s+const\s+string\s+(\w+)\s*=");
      
      foreach (Match m in matches) {
        result[m.Groups[1].Value] = true;
      }
      
      return result;
    }

    private Dictionary<string, string> ScanTagDefinitions() {
      var result = new Dictionary<string, string>();
      if (!Directory.Exists(TAG_DEFINITIONS_FOLDER)) return result;
      
      var files = Directory.GetFiles(TAG_DEFINITIONS_FOLDER, "*Tag.cs");
      foreach (var file in files) {
        var fileName = Path.GetFileNameWithoutExtension(file);
        if (fileName.EndsWith("Tag")) {
          var tagName = fileName.Substring(0, fileName.Length - 3).ToUpperInvariant();
          tagName = Regex.Replace(tagName, "([a-z])([A-Z])", "$1_$2").ToUpperInvariant();
          result[tagName] = file;
        }
      }
      
      return result;
    }

    private void AddTag(string tagName) {
      AddConstant(tagName);
      CreateDefinitionClass(tagName);
      AssetDatabase.Refresh();
      RefreshTags();
      Debug.Log($"[TagWizard] Added: {tagName}");
    }

    private void FixTag(TagEntry tag) {
      if (!tag.hasConstant) AddConstant(tag.name);
      if (!tag.hasDefinition) CreateDefinitionClass(tag.name);
      AssetDatabase.Refresh();
      RefreshTags();
      Debug.Log($"[TagWizard] Fixed: {tag.name}");
    }

    private void DeleteTag(TagEntry tag) {
      if (tag.hasConstant) RemoveConstant(tag.name);
      if (tag.hasDefinition && !string.IsNullOrEmpty(tag.definitionPath)) {
        File.Delete(tag.definitionPath);
        var metaPath = tag.definitionPath + ".meta";
        if (File.Exists(metaPath)) File.Delete(metaPath);
      }
      AssetDatabase.Refresh();
      RefreshTags();
      Debug.Log($"[TagWizard] Deleted: {tag.name}");
    }

    private void AddConstant(string tagName) {
      if (!File.Exists(TAG_FILE)) return;
      
      var lines = File.ReadAllLines(TAG_FILE).ToList();
      
      var insertIndex = -1;
      for (var i = 0; i < lines.Count; i++) {
        if (lines[i].Contains("public const string")) insertIndex = i + 1;
        if (lines[i].Contains("ALL_TAGS")) break;
      }
      
      if (insertIndex > 0) {
        lines.Insert(insertIndex, $"    public const string {tagName} = \"{tagName}\";");
      }
      
      UpdateAllTagsArray(lines, tagName, true);
      File.WriteAllLines(TAG_FILE, lines);
    }

    private void RemoveConstant(string tagName) {
      if (!File.Exists(TAG_FILE)) return;
      
      var lines = File.ReadAllLines(TAG_FILE).ToList();
      lines.RemoveAll(l => Regex.IsMatch(l, $@"public\s+const\s+string\s+{tagName}\s*="));
      UpdateAllTagsArray(lines, tagName, false);
      File.WriteAllLines(TAG_FILE, lines);
    }

    private void UpdateAllTagsArray(List<string> lines, string tagName, bool add) {
      var startIndex = lines.FindIndex(l => l.Contains("ALL_TAGS"));
      if (startIndex < 0) return;
      
      var endIndex = lines.FindIndex(startIndex, l => l.Contains("};"));
      if (endIndex < 0) return;

      if (add) {
        lines.Insert(endIndex, $"      {tagName},");
      } else {
        for (var i = startIndex; i <= endIndex; i++) {
          if (Regex.IsMatch(lines[i], $@"\b{tagName}\b,?")) {
            lines.RemoveAt(i);
            break;
          }
        }
      }
    }

    private void CreateDefinitionClass(string tagName) {
      var className = ToPascalCase(tagName) + "Tag";
      var filePath = Path.Combine(TAG_DEFINITIONS_FOLDER, $"{className}.cs");
      
      if (File.Exists(filePath)) return;
      
      var content = $@"namespace Content.Scripts.Game {{
  public class {className} : TagDefinition {{
    public override string Tag => AI.Tag.{tagName};
  }}
}}
";
      
      Directory.CreateDirectory(TAG_DEFINITIONS_FOLDER);
      File.WriteAllText(filePath, content);
    }

    private static string ToPascalCase(string input) {
      return string.Join("", input.Split('_')
        .Select(s => s.Length > 0 
          ? char.ToUpper(s[0]) + s.Substring(1).ToLower() 
          : ""));
    }

    private static bool IsValidTagName(string name) {
      return Regex.IsMatch(name, @"^[A-Z][A-Z0-9_]*$");
    }

    [Serializable]
    private class TagEntry {
      public string name;
      public bool hasConstant;
      public bool hasDefinition;
      public string definitionPath;
    }
  }
}
#endif