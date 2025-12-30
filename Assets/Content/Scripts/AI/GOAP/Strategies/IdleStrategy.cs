using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using ImprovedTimers;

namespace Content.Scripts.AI.GOAP.Strategies {
  // TODO Migrate Strategies, Beliefs, Actions and Goals to Scriptable Objects and create Node Editor for them

  [Serializable]
  public class IdleStrategy : IActionStrategy {
    public float duration;
    private CountdownTimer _timer;

    public IActionStrategy Create(IGoapAgent agent) {
      return new IdleStrategy(duration);
    }

    public IdleStrategy() {
      InitTimer(duration);
    }

    public IdleStrategy(float duration) {
      InitTimer(duration);
    }

    private void InitTimer(float d) {
      _timer = new CountdownTimer(d);
      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public bool canPerform => true; // Agent can always Idle
    public bool complete { get; private set; }

    public void Start() {
      if (_timer == null) {
        InitTimer(duration);
      }

      _timer!.Start();
    }

    public void Update(float deltaTime) {
      _timer.Tick();
    }
  }
}