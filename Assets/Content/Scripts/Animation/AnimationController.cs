using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.Animation {
  public abstract class AnimationController : MonoBehaviour {
    private const float KCrossfadeDuration = 0.1f;

    private readonly int speedHash = Animator.StringToHash("speed");
    private readonly int rotationHash = Animator.StringToHash("rotation");
    private readonly int idleHash = Animator.StringToHash("isIdle");

    private float _animationLength;

    private Animator _animator;
    private CountdownTimer _timer;

    private void Awake() {
      _animator = GetComponentInChildren<Animator>();
      SetClipNames();
    }

    private void Update() {
      _timer?.Tick();
    }

    public void SetParams(float speed, float rotation, bool isIdle) {
      _animator.SetFloat(speedHash, Mathf.Clamp01(speed));

      _animator.SetFloat(rotationHash, Mathf.Clamp01(rotation));
      _animator.SetBool(idleHash, isIdle);
      //Debug.Log($"set({_animator.name}) speed:{speed:f3}, rotation:{rotation}, isIdle:{isIdle}", _animator);
    }


    private void PlayAnimationUsingTimer(int clipHash) {
      _timer = new CountdownTimer(GetAnimationLength(clipHash));
      _timer.OnTimerStart += () => _animator.CrossFade(clipHash, KCrossfadeDuration);
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

    protected abstract void SetClipNames();

    public void Eat() {
      Debug.LogError("Eat animation not implemented", this);
    }

    public void PickUp() {
      Debug.LogError("PickUp animation not implemented", this);
    }
  }
}