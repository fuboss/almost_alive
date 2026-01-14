using UnityEngine;
using VContainer;

namespace Content.Scripts.Core.Simulation {
  /// <summary>
  /// Base class for MonoBehaviours that participate in simulation time.
  /// Automatically registers/unregisters with SimulationLoop.
  /// </summary>
  public abstract class SimulatableBehaviour : MonoBehaviour, ISimulatable {
    [Inject] protected SimulationLoop simLoop;
    [Inject] protected SimulationTimeController simTime;

    public virtual int tickPriority => 0;

    protected virtual void OnEnable() {
      simLoop?.Register(this);
    }

    protected virtual void OnDisable() {
      simLoop?.Unregister(this);
    }

    public abstract void SimTick(float simDeltaTime);

    /// <summary>
    /// Scaled speed for NavMeshAgent, Animator, etc.
    /// </summary>
    protected float timeScale => simTime?.timeScale ?? 1f;
  }
}
