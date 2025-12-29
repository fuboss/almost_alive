using Content.Scripts.AI.GOAP.Core;
using ImprovedTimers;

namespace Content.Scripts.AI.GOAP.Strategies {
  // TODO Migrate Strategies, Beliefs, Actions and Goals to Scriptable Objects and create Node Editor for them

  public class IdleStrategy : IActionStrategy {
    private readonly CountdownTimer _timer;

    public IdleStrategy(float duration) {
      _timer = new CountdownTimer(duration);
      _timer.OnTimerStart += () => Complete = false;
      _timer.OnTimerStop += () => Complete = true;
    }

    public bool CanPerform => true; // Agent can always Idle
    public bool Complete { get; private set; }

    public void Start() {
      _timer.Start();
    }

    public void Update(float deltaTime) {
      _timer.Tick();
    }
  }
}