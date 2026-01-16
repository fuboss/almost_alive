using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(NavMeshAgent))]
  public class NavigationVisualizer : MonoBehaviour {
    [SerializeField] private LineRenderer _lineRenderer;
    public bool isEnabled = true;
    private NavMeshAgent _navMeshAgent;
    private readonly Vector3[] _corners = new Vector3[48];

    private void Awake() {
      _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate() {
      if (!isEnabled) return;
      DebugDrawPath();
    }

    private void DebugDrawPath() {
      if (!_navMeshAgent.hasPath) {
        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;
        return;
      }

      if (!_lineRenderer.enabled)
        _lineRenderer.enabled = true;

      var path = _navMeshAgent.path;
      var count = path.GetCornersNonAlloc(_corners);
      _lineRenderer.positionCount = count;
      for (int i = 0; i < count; i++) {
        _lineRenderer.SetPosition(i, _corners[i] + Vector3.up * 0.1f);
      }
    }
  }
}