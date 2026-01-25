using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Marks decoration object that should snap to terrain on structure placement.
  /// Place on decorations in foundation prefab (campfire, logs, props).
  /// </summary>
  public class TerrainSnapDecoration : MonoBehaviour {
    [Tooltip("Vertical offset from terrain surface (negative = sink into ground)")]
    public float yOffset = 0f;
    
    [Tooltip("Use raycast down to find terrain (otherwise use terrain.SampleHeight)")]
    public bool useRaycast = true;
    
    [Tooltip("Max raycast distance")]
    public float maxRaycastDistance = 10f;
    
    [Tooltip("Layer mask for raycast (terrain layer)")]
    public LayerMask terrainMask = 1 << 0; // Default layer

    /// <summary>
    /// Snap this decoration to terrain at current XZ position.
    /// </summary>
    public void SnapToTerrain(Terrain terrain) {
      if (terrain == null) {
        Debug.LogWarning($"[TerrainSnapDecoration] No terrain provided for {name}");
        return;
      }
      
      var pos = transform.position;
      
      if (useRaycast) {
        // Raycast down from current position
        var ray = new Ray(pos + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out var hit, maxRaycastDistance, terrainMask)) {
          pos.y = hit.point.y + yOffset;
        }
        else {
          // Fallback to terrain sample
          pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y + yOffset;
        }
      }
      else {
        // Use terrain sampling
        pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y + yOffset;
      }
      
      transform.position = pos;
    }
  }
}
