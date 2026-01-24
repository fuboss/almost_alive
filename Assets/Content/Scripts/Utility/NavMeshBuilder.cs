using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;

namespace Content.Scripts.Utility {
  //[RequireComponent(typeof(NavMeshSurface))]
  public class NavMeshBuilder : MonoBehaviour {
    [SerializeField] private NavMeshSurface _meshSurface;

    [Button]
    public void Build() {
      if (_meshSurface == null) {
        _meshSurface = GetComponentInChildren<NavMeshSurface>();
        if (_meshSurface == null) {
          Debug.LogError("No NavMeshSurface found!", this);
          return;
        }
      }

      _meshSurface.BuildNavMesh();
      Debug.LogError("build", this);
    }
  }
}