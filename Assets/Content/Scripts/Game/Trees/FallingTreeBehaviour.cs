using System;
using Content.Scripts.Game.Effects;
using Content.Scripts.Game.Interaction;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.Game.Trees {
  public enum TreeFallState {
    Falling,
    Settling,
    Settled
  }

  public class FallingTreeBehaviour : MonoBehaviour {
    private TreeFallConfigSO _config;
    private EffectsModule _effectsModule;
    private Action<FallingTreeBehaviour> _onSettled;

    private Rigidbody _rigidbody;
    private float _fallStartTime;
    private float _firstImpactTime;
    private bool _hadFirstImpact;
    private TreeFallState _state = TreeFallState.Falling;

    private Bounds _originalBounds;
    private Material _originalMaterial;
    private Transform _crownPosition;

    public TreeFallState state => _state;
    public Rigidbody treeRigidbody => _rigidbody;
    public Bounds originalBounds => _originalBounds;
    public Material originalMaterial => _originalMaterial;

    public void Initialize(
      TreeFallConfigSO config,
      EffectsModule effectsModule,
      Bounds bounds,
      Material material,
      Transform crownPos,
      Action<FallingTreeBehaviour> onSettled
    ) {
      _config = config;
      _effectsModule = effectsModule;
      _originalBounds = bounds;
      _originalMaterial = material;
      _crownPosition = crownPos;
      _onSettled = onSettled;
      _fallStartTime = Time.time;
      _rigidbody = GetComponent<Rigidbody>();
    }

    public void ApplyFallImpulse(Vector3 direction) {
      if (_rigidbody == null) return;

      // Unlock Y constraint now that we're applying impulse
      _rigidbody.constraints = RigidbodyConstraints.None;

      var torqueAxis = Vector3.Cross(Vector3.up, direction).normalized;
      var torque = torqueAxis * _config.initialTorqueMultiplier * _rigidbody.mass;
      _rigidbody.AddTorque(torque, ForceMode.Impulse);

      var pushForce = direction * _config.initialTorqueMultiplier * 0.1f * _rigidbody.mass;
      _rigidbody.AddForce(pushForce, ForceMode.Impulse);
    }

    private void FixedUpdate() {
      if (_state == TreeFallState.Settled) return;

      float elapsed = Time.time - _fallStartTime;
      if (elapsed > _config.maxFallDuration) {
        TransitionToSettled();
        return;
      }

      if (_state == TreeFallState.Settling) {
        CheckSettled();
      }
    }

    private void OnCollisionEnter(Collision collision) {
      if (_state == TreeFallState.Settled) return;

      if (!_hadFirstImpact) {
        _hadFirstImpact = true;
        _firstImpactTime = Time.time;
        _state = TreeFallState.Settling;
      }

      TryDealImpactDamage(collision);
    }

    private void TryDealImpactDamage(Collision collision) {
      if (_rigidbody == null) return;

      float velocity = _rigidbody.linearVelocity.magnitude;
      if (velocity < _config.minVelocityForDamage) return;

      var receiver = collision.gameObject.GetComponentInParent<IImpactReceiver>();
      if (receiver == null || !receiver.canReceiveImpact) return;

      float damage = velocity * _rigidbody.mass * _config.impactDamageMultiplier;
      var contact = collision.contacts[0];
      receiver.ReceiveImpact(damage, contact.point, contact.normal);

      Debug.Log($"[FallingTree] Impact damage {damage:F1} to {collision.gameObject.name}");
    }

    private void CheckSettled() {
      float timeSinceImpact = Time.time - _firstImpactTime;
      if (timeSinceImpact < _config.settledCheckDelay) return;

      if (_rigidbody == null) {
        TransitionToSettled();
        return;
      }

      float linearVel = _rigidbody.linearVelocity.magnitude;
      float angularVel = _rigidbody.angularVelocity.magnitude;

      if (linearVel > _config.settledVelocityThreshold) return;
      if (angularVel > _config.settledAngularThreshold) return;

      float angleFromVertical = Vector3.Angle(transform.up, Vector3.up);
      if (angleFromVertical < _config.settledAngleFromVertical) return;

      TransitionToSettled();
    }

    private void TransitionToSettled() {
      if (_state == TreeFallState.Settled) return;
      _state = TreeFallState.Settled;

      SpawnLeafBurst();
      _onSettled?.Invoke(this);
    }

    private void SpawnLeafBurst() {
      if (_effectsModule == null) return;

      var spawnPos = _crownPosition != null ? _crownPosition.position : GetCrownPosition();
      
      if (_config.leafBurstPrefab != null) {
        _effectsModule.SpawnAt(_config.leafBurstPrefab, spawnPos, _config.leafBurstDuration);
      } else {
        var tempEffect = LeafBurstFactory.CreateLeafBurstPrefab();
        tempEffect.transform.position = spawnPos;
        var ps = tempEffect.GetComponent<ParticleSystem>();
        ps.Play();
        Object.Destroy(tempEffect, _config.leafBurstDuration);
      }
    }

    private Vector3 GetCrownPosition() {
      return transform.position + transform.up * _originalBounds.extents.y;
    }
  }
}
