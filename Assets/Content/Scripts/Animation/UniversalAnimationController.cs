using UnityEngine;

namespace Content.Scripts.Animation {
  public class UniversalAnimationController : MonoBehaviour,
    IAnimationController,
    IWorkAnimationController,
    ICombatAnimationController,
    ISocialAnimationController,
    IRestAnimationController {
    
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int AimAngleHash = Animator.StringToHash("AimAngle");

    private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
    private static readonly int ToolTypeHash = Animator.StringToHash("ToolType");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int SitTypeHash = Animator.StringToHash("SitType");
    private static readonly int DeathTypeHash = Animator.StringToHash("DeathType");

    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSneakingHash = Animator.StringToHash("IsSneaking");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int IsWorkingHash = Animator.StringToHash("IsWorking");
    private static readonly int IsSittingHash = Animator.StringToHash("IsSitting");
    private static readonly int IsLyingHash = Animator.StringToHash("IsLying");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int InCombatHash = Animator.StringToHash("InCombat");

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int DodgeTrigger = Animator.StringToHash("Dodge");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int HitTrigger = Animator.StringToHash("Hit");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int InteractTrigger = Animator.StringToHash("Interact");
    private static readonly int UseItemTrigger = Animator.StringToHash("UseItem");
    private static readonly int ThrowTrigger = Animator.StringToHash("Throw");
    private static readonly int SpawnTrigger = Animator.StringToHash("Spawn");
    private static readonly int CheerTrigger = Animator.StringToHash("Cheer");
    private static readonly int WaveTrigger = Animator.StringToHash("Wave");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");

    [SerializeField] private Animator animator;

    private int _currentAttackIndex;
    private float _comboResetTime;
    private const float ComboWindow = 1.5f;

    private AnimationAction _currentAction;

    public Animator Animator => animator;

    private void Awake() {
      if (animator == null)
        animator = GetComponentInChildren<Animator>();
    }

    private void Update() {
      if (_comboResetTime > 0) {
        _comboResetTime -= Time.deltaTime;
        if (_comboResetTime <= 0)
          ResetCombo();
      }
    }

    #region Action System

    public void ExecuteAction(AnimationAction action) {
      _currentAction?.Stop(this);
      _currentAction = action;
      _currentAction?.Execute(this);
    }

    public void StopCurrentAction() {
      _currentAction?.Stop(this);
      _currentAction = null;
    }

    #endregion

    #region Convenience Methods (for strategies)

    public void CutTree() => StartWork(ToolAnimationType.Axe);
    public void Mine() => StartWork(ToolAnimationType.Pickaxe);
    public void Dig() => StartWork(ToolAnimationType.Shovel);
    public void Fish() => StartWork(ToolAnimationType.FishingRod);
    public void Hammer() => StartWork(ToolAnimationType.Hammer);
    public void Saw() => StartWork(ToolAnimationType.Saw);

    public void PickUp() => Interact();
    public void DepositItem() => Interact();
    public void Eat() => UseItem();

    public void SitOnChair() => SetSitting(true, SitAnimationType.Chair);
    public void SitOnFloor() => SetSitting(true, SitAnimationType.Floor);
    public void StandUp() {
      SetSitting(false);
      SetLying(false);
    }

    public void LieDown() => SetLying(true);

    #endregion

    #region IAnimationController

    public void SetLocomotion(float speed, float direction = 0f, float vertical = 0f) {
      animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
      animator.SetFloat(DirectionHash, Mathf.Clamp(direction, -1f, 1f));
      animator.SetFloat(VerticalHash, Mathf.Clamp(vertical, -1f, 1f));
    }

    public void SetGrounded(bool grounded) => animator.SetBool(IsGroundedHash, grounded);
    public void SetCrouching(bool crouching) => animator.SetBool(IsCrouchingHash, crouching);

    public void Jump() => animator.SetTrigger(JumpTrigger);

    public void TakeHit() => animator.SetTrigger(HitTrigger);

    public void Die(int variant = 0) {
      animator.SetInteger(DeathTypeHash, variant);
      animator.SetBool(IsDeadHash, true);
      animator.SetTrigger(DieTrigger);
    }

    public void Resurrect() => animator.SetBool(IsDeadHash, false);

    public float GetCurrentClipLength() {
      var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
      return clipInfo.Length > 0 ? clipInfo[0].clip.length : 0f;
    }

    public float GetCurrentClipNormalizedTime() {
      var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
      return stateInfo.normalizedTime % 1f;
    }

    public bool IsInTransition(int layer = 0) => animator.IsInTransition(layer);

    #endregion

    #region IWorkAnimationController

    public void SetToolType(ToolAnimationType type) {
      Debug.Log($"[Animation] SetToolType: {type} ({(int)type})");
      animator.SetInteger(ToolTypeHash, (int)type);
    }
    
    public void SetWorking(bool working) {
      Debug.Log($"[Animation] SetWorking: {working}");
      animator.SetBool(IsWorkingHash, working);
    }

    public void StartWork(ToolAnimationType tool) {
      Debug.Log($"[Animation] StartWork: {tool}");
      SetToolType(tool);
      SetWorking(true);
    }

    public void StopWork() {
      Debug.Log("[Animation] StopWork");
      SetWorking(false);
      SetToolType(ToolAnimationType.None);
    }

    #endregion

    #region ICombatAnimationController

    public void SetWeaponType(WeaponAnimationType type) => animator.SetInteger(WeaponTypeHash, (int)type);
    public void SetInCombat(bool inCombat) => animator.SetBool(InCombatHash, inCombat);
    public void SetAiming(bool aiming) => animator.SetBool(IsAimingHash, aiming);
    public void SetBlocking(bool blocking) => animator.SetBool(IsBlockingHash, blocking);
    public void SetAimAngle(float angle) => animator.SetFloat(AimAngleHash, Mathf.Clamp(angle, -1f, 1f));

    public void Attack() {
      animator.SetInteger(AttackIndexHash, _currentAttackIndex);
      animator.SetTrigger(AttackTrigger);
      _currentAttackIndex = (_currentAttackIndex + 1) % 4;
      _comboResetTime = ComboWindow;
    }

    public void Dodge(Vector2 direction) {
      animator.SetFloat(DirectionHash, direction.x);
      animator.SetFloat(VerticalHash, direction.y);
      animator.SetTrigger(DodgeTrigger);
    }

    public void Reload() => animator.SetTrigger(ReloadTrigger);

    private void ResetCombo() => _currentAttackIndex = 0;

    #endregion

    #region ISocialAnimationController

    public void Interact() => animator.SetTrigger(InteractTrigger);
    public void UseItem() => animator.SetTrigger(UseItemTrigger);
    public void Throw() => animator.SetTrigger(ThrowTrigger);
    public void Wave() => animator.SetTrigger(WaveTrigger);
    public void Cheer() => animator.SetTrigger(CheerTrigger);
    public void Spawn() => animator.SetTrigger(SpawnTrigger);

    #endregion

    #region IRestAnimationController

    public void SetSitting(bool sitting, SitAnimationType type = SitAnimationType.Chair) {
      animator.SetBool(IsSittingHash, sitting);
      animator.SetInteger(SitTypeHash, (int)type);
    }

    public void SetLying(bool lying) => animator.SetBool(IsLyingHash, lying);
    public void SetSneaking(bool sneaking) => animator.SetBool(IsSneakingHash, sneaking);

    #endregion
  }

  public enum WeaponAnimationType {
    Unarmed = 0,
    OneHandMelee = 1,
    TwoHandMelee = 2,
    DualWield = 3,
    OneHandGun = 4,
    TwoHandRifle = 5,
    Bow = 6,
    Magic = 7
  }

  public enum ToolAnimationType {
    None = 0,
    Axe = 1,
    Pickaxe = 2,
    Shovel = 3,
    Hammer = 4,
    Saw = 5,
    FishingRod = 6
  }

  public enum SitAnimationType {
    None = 0,
    Chair = 1,
    Floor = 2
  }
}

