using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/UtilityDB")]
  public class UtilityDB : SerializedScriptableObject {
    [SerializeField] private List<UtilitySO> _utilities = new List<UtilitySO>();

    public IEnumerable<UtilitySO> GetAllUtilities() {
      return _utilities;
    }

    public IEnumerable<string> GetAllUtilityNames() {
      return _utilities.Select(u => u.name);
    }

    public static UtilityDB Load() {
      return Resources.Load<UtilityDB>("GOAP/UtilityEvaluators/Utility DB");
    }

    public static IEnumerable<string> Names() {
      return Load().GetAllUtilityNames();
    }

    [Button]
    private void LoadAllUtilities() {
      _utilities.Clear();
      var myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
      myPath = myPath.Substring(0, myPath.LastIndexOf('/'));

      _utilities.AddRange(UnityEditor.AssetDatabase.FindAssets("t:UtilitySO", new[] { myPath })
        .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
        .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<UtilitySO>)
        .Where(so => !UnityEditor.AssetDatabase.IsSubAsset(so))
      );

      _utilities.AddRange(UnityEditor.AssetDatabase.FindAssets("t:CompositeUtilitySO", new[] { myPath })
        .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
        .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<CompositeUtilitySO>)
        .Cast<IUtilityCompositeEvaluatorProvider>()
        .SelectMany(p => p.Get())
        .Cast<UtilitySO>());
    }
  }
}