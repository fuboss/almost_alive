using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(NavMeshAgent))]
  public class NavigationVisualizer : MonoBehaviour {
    [SerializeField] private LineRenderer _lineRenderer;
    public bool isEnabled = true;
    
    [Header("Terrain Projection")]
    [Tooltip("Distance between raycast sample points")]
    [SerializeField] private float _sampleDistance = 1f;
    [Tooltip("Height offset above terrain")]
    [SerializeField] private float _heightOffset = 0.3f;
    [Tooltip("Raycast height above expected ground")]
    [SerializeField] private float _raycastHeight = 50f;
    [Tooltip("Layer mask for terrain raycast")]
    [SerializeField] private LayerMask _terrainMask = ~0;
    
    private NavMeshAgent _navMeshAgent;
    private readonly Vector3[] _corners = new Vector3[48];
    private readonly List<Vector3> _projectedPoints = new(128);

    private void Awake() {
      _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate() {
      if (isEnabled) {
        DebugDrawPath();
        return;
      }

      if (_lineRenderer.enabled) {
        DisableLine();
      }
    }

    private void DisableLine() {
      _lineRenderer.positionCount = 0;
      _lineRenderer.enabled = false;
    }

    private void DebugDrawPath() {
      if (!_navMeshAgent.hasPath) {
        DisableLine();
        return;
      }

      if (!_lineRenderer.enabled) _lineRenderer.enabled = true;

      var path = _navMeshAgent.path;
      var cornerCount = path.GetCornersNonAlloc(_corners);
      
      if (cornerCount == 0) {
        DisableLine();
        return;
      }

      _projectedPoints.Clear();
      
      // Sample segments between corners
      for (int i = 0; i < cornerCount - 1; i++) {
        var start = _corners[i];
        var end = _corners[i + 1];
        SampleSegment(start, end);
      }
      
      // Add final point
      AddProjectedPoint(_corners[cornerCount - 1]);

      _lineRenderer.positionCount = _projectedPoints.Count;
      _lineRenderer.SetPositions(_projectedPoints.ToArray());
    }

    private void SampleSegment(Vector3 start, Vector3 end) {
      var direction = end - start;
      var distance = direction.magnitude;
      var normalizedDir = direction / distance;
      
      var sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / _sampleDistance));
      var step = distance / sampleCount;

      for (int i = 0; i < sampleCount; i++) {
        var point = start + normalizedDir * (step * i);
        AddProjectedPoint(point);
      }
    }

    private void AddProjectedPoint(Vector3 point) {
      var rayOrigin = new Vector3(point.x, point.y + _raycastHeight, point.z);
      
      if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, _raycastHeight * 2f, _terrainMask)) {
        _projectedPoints.Add(hit.point + Vector3.up * _heightOffset);
      } else {
        // Fallback if raycast misses
        _projectedPoints.Add(point + Vector3.up * _heightOffset);
      }
    }
  }
}