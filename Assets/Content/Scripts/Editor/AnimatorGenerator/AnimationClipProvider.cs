using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class AnimationClipProvider {
    private readonly string _basePath;
    private readonly Dictionary<string, string> _folderMapping;
    private readonly Dictionary<string, AnimationClip> _cache = new();

    public AnimationClipProvider(string basePath, Dictionary<string, string> folderMapping) {
      _basePath = basePath;
      _folderMapping = folderMapping;
    }

    public AnimationClip Get(string folder, string clipName) {
      var key = $"{folder}/{clipName}";
      
      if (_cache.TryGetValue(key, out var cached)) {
        return cached;
      }

      var folderPath = _folderMapping.GetValueOrDefault(folder, folder);
      var path = $"{_basePath}/{folderPath}/{clipName}.anim";
      var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
      
      if (clip != null) {
        _cache[key] = clip;
      }
      
      return clip;
    }

    public bool Exists(string folder, string clipName) {
      return Get(folder, clipName) != null;
    }

    public void ClearCache() {
      _cache.Clear();
    }
  }
}

