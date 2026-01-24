namespace Content.Scripts.Animation {
  public interface IAnimationController {
    void SetLocomotion(float speed, float direction = 0f, float vertical = 0f);
    void SetGrounded(bool grounded);
    void SetCrouching(bool crouching);
    void Jump();
    void TakeHit();
    void Die(int variant = 0);
    void Resurrect();
    float GetCurrentClipLength();
    bool IsInTransition(int layer = 0);
  }

  public interface IWorkAnimationController {
    void SetToolType(ToolAnimationType type);
    void SetWorking(bool working);
    void StartWork(ToolAnimationType tool);
    void StopWork();
  }

  public interface ICombatAnimationController {
    void SetWeaponType(WeaponAnimationType type);
    void SetInCombat(bool inCombat);
    void SetAiming(bool aiming);
    void SetBlocking(bool blocking);
    void Attack();
    void Dodge(UnityEngine.Vector2 direction);
    void Reload();
  }

  public interface ISocialAnimationController {
    void Interact();
    void UseItem();
    void Throw();
    void Wave();
    void Cheer();
    void Spawn();
  }

  public interface IRestAnimationController {
    void SetSitting(bool sitting, SitAnimationType type = SitAnimationType.Chair);
    void SetLying(bool lying);
  }
}

