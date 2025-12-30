using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(SphereCollider))]
  public class VisionSensor : MonoBehaviour {
    // Event fired when an actor becomes visible
    public event Action<ActorDescription> OnActorEntered = delegate { };

    // Event fired when an actor is no longer visible
    public event Action<ActorDescription> OnActorExited = delegate { };

    [SerializeField] private float _detectionRadius = 5f;

    [SerializeField] [Tooltip("Field of view in degrees (cone centered on forward)")]
    private float _fieldOfViewAngle = 90f;

    [SerializeField] [Tooltip("How often to re-evaluate actors inside the trigger")]
    private float _checkInterval = 0.2f;

    private SphereCollider _trigger;
    private readonly HashSet<ActorDescription> _candidates = new();
    private readonly HashSet<ActorDescription> _visible = new();
    private float _timer;

    // Public read-only access to currently visible actors
    public IReadOnlyCollection<ActorDescription> VisibleActors => _visible;

    private void Awake() {
      _trigger = GetComponent<SphereCollider>();
      _trigger.isTrigger = true;
      _trigger.radius = _detectionRadius;
    }

    private void Update() {
      _timer += Time.deltaTime;
      if (_timer < _checkInterval) return;
      _timer = 0f;
      ReevaluateCandidates();
    }

    private void OnTriggerEnter(Collider other) {
      var actor = other.GetComponentInParent<ActorDescription>();
      if (actor == null) return;
      if (!_candidates.Add(actor)) return;
      if (!IsInFov(actor.transform.position)) return;
      if (_visible.Add(actor)) OnActorEntered.Invoke(actor);
    }

    private void OnTriggerExit(Collider other) {
      var actor = other.GetComponentInParent<ActorDescription>();
      if (actor == null) return;
      _candidates.Remove(actor);
      if (_visible.Remove(actor)) OnActorExited.Invoke(actor);
    }

    private void ReevaluateCandidates() {
      var snapshot = new List<ActorDescription>(_candidates);
      foreach (var actor in snapshot) {
        if (actor == null) {
          // cleanup destroyed objects
          _candidates.Remove(actor);
          if (_visible.Remove(actor)) OnActorExited.Invoke(actor);
          continue;
        }

        bool inFov = IsInFov(actor.transform.position);
        bool currentlyVisible = _visible.Contains(actor);

        if (inFov && !currentlyVisible) {
          _visible.Add(actor);
          OnActorEntered.Invoke(actor);
        }
        else if (!inFov && currentlyVisible) {
          _visible.Remove(actor);
          OnActorExited.Invoke(actor);
        }
      }
    }

    private bool IsInFov(Vector3 targetPosition) {
      Vector3 dir = (targetPosition - transform.position);
      if (dir.sqrMagnitude <= Mathf.Epsilon) return true;
      dir.Normalize();
      float halfAngle = _fieldOfViewAngle * 0.5f;
      return Vector3.Angle(transform.forward, dir) <= halfAngle;
    }

    public void SetDetectionRadius(float radius) {
      _detectionRadius = Mathf.Max(0f, radius);
      if (_trigger != null) _trigger.radius = _detectionRadius;
    }

    public void SetFieldOfView(float degrees) {
      _fieldOfViewAngle = Mathf.Clamp(degrees, 0f, 360f);
    }

    public IEnumerable<ActorDescription> ObjectsWithTagsInView(string[] tags) {
      foreach (var actor in _visible) {
        if (actor == null || actor.descriptionData == null) continue;
        if (!IsInFov(actor.transform.position)) continue;
        if (!tags.All(t => actor.descriptionData.tags.Contains(t))) continue;

        yield return actor;
      }
    }

    public bool HasObjectsWithTagsInView(string[] tags) {
      return ObjectsWithTagsInView(tags).Any();
    }
  }
}