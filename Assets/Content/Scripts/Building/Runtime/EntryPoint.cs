using Content.Scripts.Building.Data;
using Unity.AI.Navigation;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Runtime data for an entry point (stairs + doorway).
  /// </summary>
  public class EntryPoint {
    public WallSide side;
    public int segmentIndex;
    public Vector3 stairsPosition;    // world position for stairs
    public float stairsHeight;        // gap between terrain and structure floor
    public GameObject stairsInstance;
    public NavMeshLink navMeshLink;

    public EntryPoint(WallSide side, int segmentIndex, Vector3 stairsPosition, float stairsHeight) {
      this.side = side;
      this.segmentIndex = segmentIndex;
      this.stairsPosition = stairsPosition;
      this.stairsHeight = stairsHeight;
    }

    /// <summary>
    /// Cleanup instances.
    /// </summary>
    public void Destroy() {
      if (stairsInstance != null) {
        Object.Destroy(stairsInstance);
        stairsInstance = null;
      }
      if (navMeshLink != null) {
        Object.Destroy(navMeshLink);
        navMeshLink = null;
      }
    }
  }
}
