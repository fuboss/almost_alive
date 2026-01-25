using UnityEngine;

namespace Content.Scripts.Building.Data {
  [CreateAssetMenu(fileName = "BuildingManagerConfig", menuName = "Building/Main Config")]
  public class BuildingManagerConfigSO : ScriptableObject {
    
    public static BuildingManagerConfigSO GetFromResources() {
      return Resources.Load<BuildingManagerConfigSO>("BuildingManagerConfig");
    }
  }
}