using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.Animation {
  public abstract class AnimationController : MonoBehaviour {
    private const float KCrossfadeDuration = 0.1f;

    [HideInInspector] public int locomotionClip = Animator.StringToHash("Locomotion");
    [HideInInspector] public int speedHash = Animator.StringToHash("Speed");
    [HideInInspector] public int attackClip = Animator.StringToHash("Attack");

    private float _animationLength;

    private Animator _animator;
    private CountdownTimer _timer;

    private void Awake() {
      _animator = GetComponentInChildren<Animator>();
      SetLocomotionClip();
      SetAttackClip();
      SetSpeedHash();
    }

    private void Update() {
      _timer?.Tick();
    }

    public void SetSpeed(float speed) {
      _animator.SetFloat(speedHash, speed);
    }

    public void Attack() {
      PlayAnimationUsingTimer(attackClip);
    }

    private void PlayAnimationUsingTimer(int clipHash) {
      _timer = new CountdownTimer(GetAnimationLength(clipHash));
      _timer.OnTimerStart += () => _animator.CrossFade(clipHash, KCrossfadeDuration);
      _timer.OnTimerStop += () => _animator.CrossFade(locomotionClip, KCrossfadeDuration);
      _timer.Start();
    }

    public float GetAnimationLength(int hash) {
      if (_animationLength > 0) return _animationLength;

      foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        if (Animator.StringToHash(clip.name) == hash) {
          _animationLength = clip.length;
          return clip.length;
        }

      return -1f;
    }

    protected abstract void SetLocomotionClip();
    protected abstract void SetAttackClip();
    protected abstract void SetSpeedHash();
  }
}