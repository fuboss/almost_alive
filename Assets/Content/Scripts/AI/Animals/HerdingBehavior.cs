using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.Animals {
  /// <summary>
  /// Applies flocking steering forces to modify animal movement.
  /// Cohesion, separation, alignment + anchor pull for migration.
  /// </summary>
  public class HerdingBehavior : MonoBehaviour {
    [FoldoutGroup("Weights")]
    [SerializeField] private float _cohesionWeight = 1f;
    [FoldoutGroup("Weights")]
    [SerializeField] private float _separationWeight = 1.5f;
    [FoldoutGroup("Weights")]
    [SerializeField] private float _alignmentWeight = 0.5f;
    [FoldoutGroup("Weights")]
    [SerializeField] private float _anchorWeight = 0.8f;

    [FoldoutGroup("Distances")]
    [SerializeField] private float _neighborRadius = 12f;
    [FoldoutGroup("Distances")]
    [SerializeField] private float _separationRadius = 3f;
    [FoldoutGroup("Distances")]
    [SerializeField] private float _maxSteeringMagnitude = 8f;

    [ShowInInspector, ReadOnly] private HerdAnchor _anchor;

    [Inject] private AnimalsModule _animalsModule;
    private AnimalAgent _self;
    private readonly List<IGoapAnimalAgent> _neighborsCache = new();

    public HerdAnchor anchor => _anchor;

    public void Initialize( HerdAnchor anchor) {
      _anchor = anchor;
      _self = GetComponent<AnimalAgent>();
    }

    /// <summary>
    /// Modifies a destination with herd steering.
    /// Call from strategies when picking movement targets.
    /// </summary>
    public Vector3 GetModifiedDestination(Vector3 originalDestination) {
      if (_animalsModule == null || _anchor == null) return originalDestination;

      var steering = CalculateSteering();
      return originalDestination + steering;
    }

    /// <summary>
    /// Returns pure steering offset. Use for wander-style movement.
    /// </summary>
    public Vector3 GetSteeringOffset() {
      if (_animalsModule == null || _anchor == null) return Vector3.zero;
      return CalculateSteering();
    }

    private Vector3 CalculateSteering() {
      CacheNeighbors();

      var steering = Vector3.zero;
      steering += CalculateCohesion() * _cohesionWeight;
      steering += CalculateSeparation() * _separationWeight;
      steering += CalculateAlignment() * _alignmentWeight;
      steering += CalculateAnchorPull() * _anchorWeight;

      // Clamp magnitude
      if (steering.sqrMagnitude > _maxSteeringMagnitude * _maxSteeringMagnitude) {
        steering = steering.normalized * _maxSteeringMagnitude;
      }

      return steering;
    }

    private void CacheNeighbors() {
      _neighborsCache.Clear();

      var members = _animalsModule.GetHerdMembers(_anchor);
      var myPos = transform.position;
      var sqrRadius = _neighborRadius * _neighborRadius;

      foreach (var member in members) {
        if (member == _self || member == null) continue;

        var sqrDist = (member.transform.position - myPos).sqrMagnitude;
        if (sqrDist < sqrRadius) {
          _neighborsCache.Add(member);
        }
      }
    }

    /// <summary>
    /// Steer toward average position of neighbors.
    /// </summary>
    private Vector3 CalculateCohesion() {
      if (_neighborsCache.Count == 0) return Vector3.zero;

      var center = Vector3.zero;
      foreach (var n in _neighborsCache) {
        center += n.transform.position;
      }
      center /= _neighborsCache.Count;

      var toCenter = center - transform.position;
      return toCenter.normalized;
    }

    /// <summary>
    /// Steer away from neighbors that are too close.
    /// </summary>
    private Vector3 CalculateSeparation() {
      var separation = Vector3.zero;
      var myPos = transform.position;
      var sqrSepRadius = _separationRadius * _separationRadius;

      foreach (var n in _neighborsCache) {
        var toNeighbor = n.transform.position - myPos;
        var sqrDist = toNeighbor.sqrMagnitude;

        if (sqrDist < sqrSepRadius && sqrDist > 0.001f) {
          // Inverse strength: closer = stronger repulsion
          var strength = 1f - Mathf.Sqrt(sqrDist) / _separationRadius;
          separation -= toNeighbor.normalized * strength;
        }
      }

      return separation;
    }

    /// <summary>
    /// Steer toward average heading of neighbors.
    /// </summary>
    private Vector3 CalculateAlignment() {
      if (_neighborsCache.Count == 0) return Vector3.zero;

      var avgHeading = Vector3.zero;
      var count = 0;

      foreach (var n in _neighborsCache) {
        var nav = n.navMeshAgent;
        if (nav == null) continue;

        var vel = nav.velocity;
        if (vel.sqrMagnitude > 0.01f) {
          avgHeading += vel.normalized;
          count++;
        }
      }

      if (count == 0) return Vector3.zero;
      return (avgHeading / count).normalized;
    }

    /// <summary>
    /// Steer toward herd anchor (migration pull).
    /// Strength increases with distance.
    /// </summary>
    private Vector3 CalculateAnchorPull() {
      if (_anchor == null) return Vector3.zero;

      var toAnchor = _anchor.position - transform.position;
      var dist = toAnchor.magnitude;

      // Stronger pull when far
      var strength = Mathf.Clamp01(dist / (_neighborRadius * 2f));
      return toAnchor.normalized * strength;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
      // Anchor connection
      if (_anchor != null) {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, _anchor.position);
      }

      // Neighbor radius
      Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
      Gizmos.DrawWireSphere(transform.position, _neighborRadius);

      // Separation radius
      Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
      Gizmos.DrawWireSphere(transform.position, _separationRadius);
    }
#endif
  }
}
