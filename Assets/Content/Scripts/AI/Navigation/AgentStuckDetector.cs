using System;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.Navigation {
  /// <summary>
  /// Detects when NavMeshAgent is stuck (trying to move but making no progress).
  /// </summary>
  [Serializable]
  public class AgentStuckDetector {
    [Tooltip("Seconds without progress before considered stuck")]
    public float stuckThreshold = 2f;
    
    [Tooltip("Minimum distance to count as progress")]
    public float minMovement = 0.3f;
    
    [Tooltip("Cooldown after replan before checking again")]
    public float replanCooldown = 3f;

    private Vector3 _lastPosition;
    private float _stuckTime;
    private float _cooldownRemaining;
    private bool _isStuck;

    public bool isStuck => _isStuck;
    public float stuckDuration => _stuckTime;
    
    public event Action OnStuck;

    public void Initialize(Vector3 startPosition) {
      _lastPosition = startPosition;
      _stuckTime = 0f;
      _cooldownRemaining = 0f;
      _isStuck = false;
    }

    /// <summary>
    /// Call every frame. Returns true if agent just became stuck this frame.
    /// </summary>
    public bool Update(NavMeshAgent agent, float deltaTime) {
      if (agent == null) return false;

      // Cooldown after replan
      if (_cooldownRemaining > 0f) {
        _cooldownRemaining -= deltaTime;
        return false;
      }

      var currentPos = agent.transform.position;
      var moved = Vector3.Distance(currentPos, _lastPosition);

      // Made progress - reset
      if (moved > minMovement) {
        _lastPosition = currentPos;
        _stuckTime = 0f;
        _isStuck = false;
        return false;
      }

      // Not trying to move - not stuck
      if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance) {
        _stuckTime = 0f;
        _isStuck = false;
        return false;
      }

      // Path is still being calculated
      if (agent.pathPending) {
        return false;
      }

      // Path is partial (can't reach destination)
      if (agent.pathStatus == NavMeshPathStatus.PathPartial) {
        _stuckTime += deltaTime * 2f; // faster detection for partial paths
      } else {
        _stuckTime += deltaTime;
      }

      // Check if stuck
      if (_stuckTime >= stuckThreshold && !_isStuck) {
        _isStuck = true;
        OnStuck?.Invoke();
        return true;
      }

      return false;
    }

    /// <summary>
    /// Call when agent starts a new plan/action to reset detection.
    /// </summary>
    public void OnNewAction(Vector3 position) {
      _lastPosition = position;
      _stuckTime = 0f;
      _isStuck = false;
    }

    /// <summary>
    /// Call after handling stuck to prevent immediate re-trigger.
    /// </summary>
    public void StartCooldown() {
      _cooldownRemaining = replanCooldown;
      _stuckTime = 0f;
      _isStuck = false;
    }

    public void Reset() {
      _stuckTime = 0f;
      _cooldownRemaining = 0f;
      _isStuck = false;
    }
  }
}
