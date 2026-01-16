using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Ui;
using ImprovedTimers;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class IdleStrategy : AgentStrategy {
    [Inject] private AgentUIModule _uiModule;
    
    public float duration;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new IdleStrategy(duration);
    }

    public IdleStrategy() {
      InitTimer(duration);
    }

    private IdleStrategy(float duration) {
      InitTimer(duration);
    }

    private void InitTimer(float d) {
      _timer = new CountdownTimer(d);
      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override bool canPerform => true; // Agent can always Idle
    public override bool complete { get; internal set; }

    public override void OnStart() {

      Debug.LogError($"Injection: {_uiModule != null}");
      if (_timer == null) {
        InitTimer(duration);
      }

      _timer!.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}