using System.Collections.Generic;
using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Game.Decay {
  /// <summary>
  /// Ticks decay for all loose items and destroys expired ones.
  /// </summary>
  public class DecayModule : ITickable {
    [Inject] private readonly SimulationTimeController _simTime;

    private readonly List<DecayableActor> _toDestroy = new();
    
    [ShowInInspector] private float _cleanupInterval = 5f;
    [ShowInInspector] private float _timer;
    [ShowInInspector] private int _destroyedCount;

    public int destroyedCount => _destroyedCount;
    public int activeCount => ActorRegistry<DecayableActor>.count;

    public void Tick() {
      if (_simTime == null || _simTime.isPaused) return;

      var simDelta = Time.deltaTime * _simTime.timeScale;

      // Tick all decay
      foreach (var actor in ActorRegistry<DecayableActor>.all) {
        if (actor == null) continue;
        actor.TickDecay(simDelta);
      }

      // Periodic cleanup
      _timer += simDelta;
      if (_timer >= _cleanupInterval) {
        CleanupExpired();
        _timer = 0f;
      }
    }

    private void CleanupExpired() {
      _toDestroy.Clear();

      foreach (var actor in ActorRegistry<DecayableActor>.all) {
        if (actor != null && actor.isExpired) {
          _toDestroy.Add(actor);
        }
      }

      foreach (var actor in _toDestroy) {
        if (actor == null) continue;
        
        Debug.Log($"[Decay] Destroying expired: {actor.gameObject.name}", actor);
        _destroyedCount++;
        
        // Could fire event here for VFX/sound
        Object.Destroy(actor.gameObject);
      }

      _toDestroy.Clear();
    }

    public void SetCleanupInterval(float interval) {
      _cleanupInterval = Mathf.Max(0.5f, interval);
    }
  }
}
