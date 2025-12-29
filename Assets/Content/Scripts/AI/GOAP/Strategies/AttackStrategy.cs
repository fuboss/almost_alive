using Content.Scripts.AI.GOAP.Core;
using Content.Scripts.Animation;
using ImprovedTimers;

namespace Content.Scripts.AI.GOAP.Strategies {
  public class AttackStrategy : IActionStrategy {
    private readonly AnimationController _animations;

    private readonly CountdownTimer _timer;

    public AttackStrategy(AnimationController animations) {
      _animations = animations;
      // _timer = new CountdownTimer(animations.GetAnimationLength(animations.attackClip));
      // _timer.OnTimerStart += () => Complete = false;
      // _timer.OnTimerStop += () => Complete = true;
    }

    public bool CanPerform => true; // Agent can always attack
    public bool Complete { get; private set; }

    public void Start() {
      _timer.Start();
     // _animations.Attack();
    }

    public void Update(float deltaTime) {
      _timer.Tick();
    }
  }
}