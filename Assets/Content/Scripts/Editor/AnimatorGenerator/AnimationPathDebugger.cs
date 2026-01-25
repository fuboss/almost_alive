using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public static class AnimationPathDebugger {
    [MenuItem("Tools/Animation/Debug Animation Paths")]
    public static void DebugPaths() {
      var basePath = "Assets/ThrirdParty/KayKit/Characters/Animations/Animations/Rig_Medium";
      
      // Test 1: Direct path
      var testPath1 = $"{basePath}/General/Idle_A.anim";
      var clip1 = AssetDatabase.LoadAssetAtPath<AnimationClip>(testPath1);
      Debug.Log($"Test 1 - Direct path: {testPath1}");
      Debug.Log($"Result: {(clip1 != null ? "✓ FOUND" : "✗ NOT FOUND")}");
      
      // Test 2: Find all AnimationClips
      var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { basePath });
      Debug.Log($"\nFound {guids.Length} AnimationClips in {basePath}");
      
      if (guids.Length > 0) {
        for (int i = 0; i < Mathf.Min(5, guids.Length); i++) {
          var path = AssetDatabase.GUIDToAssetPath(guids[i]);
          var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
          Debug.Log($"  [{i}] {path} -> {(clip != null ? clip.name : "NULL")}");
        }
      }
      
      // Test 3: Load from exact known path
      var allClips = AssetDatabase.FindAssets("Idle_A t:AnimationClip");
      Debug.Log($"\nSearching for 'Idle_A': found {allClips.Length} results");
      foreach (var guid in allClips) {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        Debug.Log($"  Found at: {path}");
      }
    }
  }
}

