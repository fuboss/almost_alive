using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Animals {
  /// <summary>
  /// Invisible point that drifts slowly across the world.
  /// Deer cohesion pulls them toward this anchor, creating organic herd migration.
  /// </summary>
  public class HerdAnchor : MonoBehaviour, ISimulatable {
    [SerializeField] private float _driftSpeed = 0.2f;
    [SerializeField] private float _directionChangeInterval = 45f;
    [SerializeField] private float _worldRadius = 100f;

    [ShowInInspector, ReadOnly] private Vector3 _driftDirection;
    [ShowInInspector, ReadOnly] private float _directionChangeTimer;

    public int tickPriority => 20;
    public Vector3 position => transform.position;

    private void Awake() {
      PickNewDirection();
      _directionChangeTimer = _directionChangeInterval;
    }


    public void SetWorldRadius(float radius) {
      _worldRadius = radius;
      
    }
    public void SimTick(float simDeltaTime) {
      // Drift
      var newPos = transform.position + _driftDirection * (_driftSpeed * simDeltaTime);
      
      // Clamp to world bounds
      newPos = ClampToWorld(newPos);
      transform.position = newPos;

      // Direction change
      _directionChangeTimer -= simDeltaTime;
      if (_directionChangeTimer <= 0f) {
        PickNewDirection();
        _directionChangeTimer = _directionChangeInterval + Random.Range(-10f, 10f);
      }
    }

    private void PickNewDirection() {
      var randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
      _driftDirection = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
    }

    private Vector3 ClampToWorld(Vector3 pos) {
      var center = Vector3.zero;
      var offset = pos - center;
      
      if (offset.magnitude > _worldRadius) {
        // Bounce: reverse direction component pointing outward
        _driftDirection = -_driftDirection;
        pos = center + offset.normalized * (_worldRadius - 1f);
      }
      
      return pos;
    }

   

#if UNITY_EDITOR
    private void OnDrawGizmos() {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, 2f);
      Gizmos.DrawRay(transform.position, _driftDirection * 5f);
    }
#endif
  }
}
