using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(SphereCollider))]
  public class InteractionSensor : MonoBehaviour {
    [SerializeField] private float _detectionRadius = 1.5f;

    [SerializeField] [Tooltip("How often to re-evaluate actors inside the trigger")]
    private float _checkInterval = 0.2f;

    private readonly HashSet<ActorDescription> _candidates = new();
    [ShowInInspector] private readonly HashSet<ActorDescription> _visible = new();
    private float _timer;
    private SphereCollider _trigger;

    // Public read-only access to currently visible actors
    public IReadOnlyCollection<ActorDescription> ActorsInZone => _visible;

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
      if (_visible.Add(actor)) OnActorEntered.Invoke(actor);
    }

    private void OnTriggerExit(Collider other) {
      var actor = other.GetComponentInParent<ActorDescription>();
      if (actor == null) return;
      _candidates.Remove(actor);
      if (_visible.Remove(actor)) OnActorExited.Invoke(actor);
    }

    private void OnValidate() {
      // ensure collider radius and mesh update in editor when properties change
      if (TryGetComponent(out SphereCollider sc)) {
        sc.radius = Mathf.Max(0f, _detectionRadius);
        sc.isTrigger = true;
        _trigger = sc;
      }
    }

    // Event fired when an actor becomes visible
    public event Action<ActorDescription> OnActorEntered = delegate { };

    // Event fired when an actor is no longer visible
    public event Action<ActorDescription> OnActorExited = delegate { };

    private void ReevaluateCandidates() {
      var snapshot = new List<ActorDescription>(_candidates);
      foreach (var actor in snapshot) {
        if (actor == null) {
          // cleanup destroyed objects
          _candidates.Remove(actor);
          if (_visible.Remove(actor)) OnActorExited.Invoke(actor);
          continue;
        }


        if (_visible.Add(actor)) {
          OnActorEntered.Invoke(actor);
        }
        else {
          _visible.Remove(actor);
          OnActorExited.Invoke(actor);
        }
      }
    }

    public void SetDetectionRadius(float radius) {
      _detectionRadius = Mathf.Max(0f, radius);
      if (_trigger != null) _trigger.radius = _detectionRadius;
    }

    public IEnumerable<ActorDescription> ObjectsWithTagsInArea(string[] tags) {
      foreach (var actor in _visible) {
        if (actor == null || actor.descriptionData == null) continue;
        if (!tags.All(t => actor.descriptionData.tags.Contains(t))) continue;

        yield return actor;
      }
    }

    public bool HasObjectsWithTagsArea(string[] tags) {
      return ObjectsWithTagsInArea(tags).Any();
    }
  }
}