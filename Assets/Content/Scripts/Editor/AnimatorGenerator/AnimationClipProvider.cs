using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class AnimationClipProvider {
    private readonly string _basePath;
    private readonly Dictionary<string, string> _folderMapping;
    private readonly Dictionary<string, AnimationClip> _cache = new();
    private readonly Dictionary<string, AnimationClip> _allClipsByName = new();
    private bool _isScanned;

    public AnimationClipProvider(string basePath, Dictionary<string, string> folderMapping) {
      _basePath = basePath;
      _folderMapping = folderMapping;
    }

    private void EnsureScanned() {
      if (_isScanned) return;
      
      Debug.Log($"[AnimationClipProvider] Starting scan...");
      Debug.Log($"[AnimationClipProvider] Base path: {_basePath}");
      
      // Check if path exists in AssetDatabase
      var pathExists = AssetDatabase.IsValidFolder(_basePath);
      Debug.Log($"[AnimationClipProvider] Path valid: {pathExists}");
      
      var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { _basePath });
      Debug.Log($"[AnimationClipProvider] Found {guids.Length} animation clip GUIDs");
      
      int loadedCount = 0;
      foreach (var guid in guids) {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        
        if (clip != null) {
          _allClipsByName[clip.name] = clip;
          loadedCount++;
          
          if (loadedCount <= 5) {
            Debug.Log($"[AnimationClipProvider] Loaded: {clip.name} from {path}");
          }
        } else {
          Debug.LogWarning($"[AnimationClipProvider] Failed to load clip at: {path}");
        }
      }
      
      _isScanned = true;
      Debug.Log($"[AnimationClipProvider] Scan complete! Loaded {_allClipsByName.Count} clips into cache");
      
      if (_allClipsByName.Count > 0) {
        Debug.Log($"[AnimationClipProvider] Sample clips: {string.Join(", ", _allClipsByName.Keys.Take(10))}");
      }
    }

    public AnimationClip Get(string folder, string clipName) {
      EnsureScanned();
      
      var key = $"{folder}/{clipName}";
      
      if (_cache.TryGetValue(key, out var cached)) {
        return cached;
      }

      // Try to get from pre-scanned clips by name
      if (_allClipsByName.TryGetValue(clipName, out var clip)) {
        _cache[key] = clip;
        return clip;
      }

      // Fallback: try direct path
      var folderPath = _folderMapping.GetValueOrDefault(folder, folder);
      var path = $"{_basePath}/{folderPath}/{clipName}.anim";
      clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
      
      if (clip != null) {
        _cache[key] = clip;
        _allClipsByName[clipName] = clip;
      } else {
        Debug.LogWarning($"[AnimationClipProvider] Clip not found: {clipName} (tried path: {path})");
      }
      
      return clip;
    }

    public bool Exists(string folder, string clipName) {
      return Get(folder, clipName) != null;
    }

    public void ClearCache() {
      _cache.Clear();
      _allClipsByName.Clear();
      _isScanned = false;
    }
    
    public int GetTotalClipsCount() {
      EnsureScanned();
      return _allClipsByName.Count;
    }
    
    public IEnumerable<string> GetAllClipNames() {
      EnsureScanned();
      return _allClipsByName.Keys;
    }
  }
}

