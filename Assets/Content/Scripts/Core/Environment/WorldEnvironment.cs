using System;
using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Core.Environment {
  /// <summary>
  /// Central environment service. Provides day/night cycle data and environment modifiers.
  /// Reads settings from EnvironmentSetupSO - changes apply at runtime.
  /// </summary>
  [Serializable]
  public class WorldEnvironment : ISimulatable, IStartable {
    public static WorldEnvironment instance { get; private set; }

    [Inject] private readonly SimulationLoop _simLoop;
    [Inject] private readonly EnvironmentSetupSO _setup;

    [ShowInInspector] private DayCycle _dayCycle = new();

    public EnvironmentSetupSO setup => _setup;
    public DayCycle dayCycle => _dayCycle;
    public int tickPriority => -100;

    static WorldEnvironment() {
      StaticResetRegistry.RegisterReset(() => instance = null);
    }

    public WorldEnvironment() {
      instance = this;
    }

    public void Start() {
      if (_setup == null) {
        Debug.LogError("[WorldEnvironment] EnvironmentSetupSO not injected!");
        return;
      }

      _dayCycle.Initialize(_setup);
      _simLoop.Register(this);

      Debug.Log($"[WorldEnvironment] Started. Day length: {_setup.dayLengthSeconds}s, " +
                $"Start time: {_dayCycle.hourOfDay:F1}h ({_dayCycle.currentPhase})");
    }

    public void SimTick(float simDeltaTime) {
      _dayCycle.Tick(simDeltaTime);
    }

    public float sleepinessModifier => _setup.EvaluateSleepiness(_dayCycle.normalizedTime);
    public float visionModifier => _setup.EvaluateVision(_dayCycle.normalizedTime);
    public float sunAngle => _dayCycle.normalizedTime * 360f - 90f;
  }
}
