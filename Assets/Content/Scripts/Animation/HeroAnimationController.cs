using UnityEngine;

namespace Content.Scripts.Animation {
  public class HeroAnimationController : AnimationController {
    protected override void SetLocomotionClip() {
      locomotionClip = Animator.StringToHash("Locomotion");
    }

    protected override void SetAttackClip() {
      attackClip = Animator.StringToHash("Attack01_MagicWand");
    }

    protected override void SetSpeedHash() {
      speedHash = Animator.StringToHash("Speed");
    }
  }
}