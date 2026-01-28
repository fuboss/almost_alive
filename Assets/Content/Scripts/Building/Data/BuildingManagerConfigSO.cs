using System;
using Content.Scripts.Utility;
using UnityEngine;

namespace Content.Scripts.Building.Data {
  
  /// <summary>
  /// Configuration data for building manager.
  /// Currently empty - add fields as needed.
  /// </summary>
  [Serializable]
  public class BuildingManagerConfig {
    // TODO: Add building manager settings here
  }

  /// <summary>
  /// ScriptableObject container for BuildingManagerConfig.
  /// </summary>
  [CreateAssetMenu(fileName = "BuildingManagerConfig", menuName = "Building/Main Config")]
  public class BuildingManagerConfigSO : ScriptableConfig<BuildingManagerConfig> {
    
    public static BuildingManagerConfigSO GetFromResources() {
      return Resources.Load<BuildingManagerConfigSO>("BuildingManagerConfig");
    }
  }
}
