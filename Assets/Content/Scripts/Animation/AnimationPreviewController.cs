using Content.Scripts.Animation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Animation {
  [ExecuteAlways]
  public class AnimationPreviewController : MonoBehaviour {
    // Animator parameter names
    const string ParamIsBlocking = "IsBlocking";
    const string ParamIsAiming = "IsAiming";
    const string ParamIsCrouching = "IsCrouching";
    const string ParamIsSneaking = "IsSneaking";
    const string ParamIsSitting = "IsSitting";
    const string ParamIsLying = "IsLying";
    const string ParamIsWorking = "IsWorking";
    const string ParamToolType = "ToolType";
    const string ParamIsGrounded = "IsGrounded";

    [TitleGroup("Target")]
    [Required]
    public UniversalAnimationController animationController;

    [TitleGroup("Scenario")]
    [EnumToggleButtons]
    [HideLabel]
    [OnValueChanged("ApplyScenario")]
    public PreviewScenario scenario = PreviewScenario.Idle;

    [TitleGroup("Locomotion")]
    [Range(0f, 1f)]
    [OnValueChanged("ApplyLocomotion")]
    public float speed;

    [TitleGroup("Locomotion")]
    [Range(-1f, 1f)]
    [OnValueChanged("ApplyLocomotion")]
    public float direction;

    [TitleGroup("Locomotion")]
    [Range(-1f, 1f)]
    [OnValueChanged("ApplyLocomotion")]
    public float vertical;

    [TitleGroup("Locomotion")]
    [OnValueChanged("ApplyGrounded")]
    public bool isGrounded = true;

    [TitleGroup("Work")]
    [EnumToggleButtons]
    [OnValueChanged("ApplyWork")]
    public ToolAnimationType toolType = ToolAnimationType.None;

    [TitleGroup("Work/Buttons")]
    [HorizontalGroup("Work/Buttons/Row")]
    [Button("Start Work")]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void StartWork() {
      if (animationController == null) return;
      animationController.StartWork(toolType);
      Debug.Log($"[Preview] StartWork: {toolType}");
    }

    [HorizontalGroup("Work/Buttons/Row")]
    [Button("Stop Work")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void StopWork() {
      if (animationController == null) return;
      animationController.StopWork();
      Debug.Log("[Preview] StopWork");
    }

    [TitleGroup("Combat")]
    [EnumToggleButtons]
    [OnValueChanged("ApplyCombat")]
    public WeaponAnimationType weaponType = WeaponAnimationType.Unarmed;

    [TitleGroup("Combat/Buttons1")]
    [HorizontalGroup("Combat/Buttons1/Row")]
    [Button("Enter Combat")]
    [GUIColor(0.8f, 0.6f, 0.2f)]
    public void EnterCombat() {
      if (animationController == null) return;
      animationController.SetWeaponType(weaponType);
      animationController.SetInCombat(true);
      Debug.Log($"[Preview] EnterCombat: {weaponType}");
    }

    [HorizontalGroup("Combat/Buttons1/Row")]
    [Button("Exit Combat")]
    public void ExitCombat() {
      if (animationController == null) return;
      animationController.SetInCombat(false);
      Debug.Log("[Preview] ExitCombat");
    }

    [TitleGroup("Combat/Buttons2")]
    [HorizontalGroup("Combat/Buttons2/Row")]
    [Button("Attack")]
    [GUIColor(1f, 0.3f, 0.3f)]
    public void Attack() {
      if (animationController == null) return;
      animationController.Attack();
      Debug.Log("[Preview] Attack");
    }

    [HorizontalGroup("Combat/Buttons2/Row")]
    [Button("Block")]
    public void ToggleBlock() {
      if (animationController == null) return;
      var current = animationController.Animator.GetBool(ParamIsBlocking);
      animationController.SetBlocking(!current);
      Debug.Log($"[Preview] Block: {!current}");
    }

    [HorizontalGroup("Combat/Buttons2/Row")]
    [Button("Aim")]
    public void ToggleAim() {
      if (animationController == null) return;
      var current = animationController.Animator.GetBool(ParamIsAiming);
      animationController.SetAiming(!current);
      Debug.Log($"[Preview] Aim: {!current}");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row1")]
    [Button("Jump")]
    [GUIColor(0.5f, 0.8f, 1f)]
    public void Jump() {
      if (animationController == null) return;
      isGrounded = false;
      animationController.SetGrounded(false);
      animationController.Jump();
      Debug.Log("[Preview] Jump - set IsGrounded=false, use 'Land' to return");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row1")]
    [Button("Land")]
    public void Land() {
      if (animationController == null) return;
      isGrounded = true;
      animationController.SetGrounded(true);
      Debug.Log("[Preview] Land - IsGrounded=true");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row1")]
    [Button("Dodge")]
    public void Dodge() {
      if (animationController == null) return;
      animationController.Dodge(new Vector2(direction, vertical));
      Debug.Log($"[Preview] Dodge ({direction}, {vertical})");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row1")]
    [Button("Hit")]
    [GUIColor(1f, 0.5f, 0.2f)]
    public void Hit() {
      if (animationController == null) return;
      animationController.TakeHit();
      Debug.Log("[Preview] Hit");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row2")]
    [Button("Die")]
    [GUIColor(0.3f, 0.3f, 0.3f)]
    public void Die() {
      if (animationController == null) return;
      animationController.Die();
      Debug.Log("[Preview] Die");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row2")]
    [Button("Interact")]
    public void Interact() {
      if (animationController == null) return;
      animationController.Interact();
      Debug.Log("[Preview] Interact");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row2")]
    [Button("Use Item")]
    public void UseItem() {
      if (animationController == null) return;
      animationController.UseItem();
      Debug.Log("[Preview] UseItem");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row2")]
    [Button("Throw")]
    public void Throw() {
      if (animationController == null) return;
      animationController.Throw();
      Debug.Log("[Preview] Throw");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row3")]
    [Button("Wave")]
    public void Wave() {
      if (animationController == null) return;
      animationController.Wave();
      Debug.Log("[Preview] Wave");
    }

    [TitleGroup("Triggers")]
    [HorizontalGroup("Triggers/Row3")]
    [Button("Cheer")]
    public void Cheer() {
      if (animationController == null) return;
      animationController.Cheer();
      Debug.Log("[Preview] Cheer");
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row1")]
    [Button("Crouch")]
    public void ToggleCrouch() {
      if (animationController == null) return;
      var current = animationController.Animator.GetBool(ParamIsCrouching);
      animationController.SetCrouching(!current);
      Debug.Log($"[Preview] Crouch: {!current}");
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row1")]
    [Button("Sneak")]
    public void ToggleSneak() {
      if (animationController == null) return;
      var current = animationController.Animator.GetBool(ParamIsSneaking);
      animationController.SetSneaking(!current);
      Debug.Log($"[Preview] Sneak: {!current}");
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row2")]
    [Button("Sit Chair")]
    public void ToggleSitChair() {
      if (animationController == null) return;
      var sitting = animationController.Animator.GetBool(ParamIsSitting);
      if (sitting) {
        animationController.StandUp();
        Debug.Log("[Preview] StandUp from Chair");
      }
      else {
        animationController.SitOnChair();
        Debug.Log("[Preview] SitOnChair");
      }
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row2")]
    [Button("Sit Floor")]
    public void ToggleSitFloor() {
      if (animationController == null) return;
      var sitting = animationController.Animator.GetBool(ParamIsSitting);
      if (sitting) {
        animationController.StandUp();
        Debug.Log("[Preview] StandUp from Floor");
      }
      else {
        animationController.SitOnFloor();
        Debug.Log("[Preview] SitOnFloor");
      }
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row2")]
    [Button("Lie Down")]
    public void ToggleLie() {
      if (animationController == null) return;
      var lying = animationController.Animator.GetBool(ParamIsLying);
      if (lying) {
        animationController.StandUp();
        Debug.Log("[Preview] StandUp from Lying");
      }
      else {
        animationController.LieDown();
        Debug.Log("[Preview] LieDown");
      }
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row3")]
    [Button("Resurrect")]
    [GUIColor(0.4f, 1f, 0.4f)]
    public void Resurrect() {
      if (animationController == null) return;
      animationController.Resurrect();
      Debug.Log("[Preview] Resurrect");
    }

    [TitleGroup("State")]
    [HorizontalGroup("State/Row3")]
    [Button("Reset All")]
    [GUIColor(1f, 1f, 0.5f)]
    public void ResetAll() {
      if (animationController == null) return;

      speed = 0;
      direction = 0;
      vertical = 0;
      isGrounded = true;
      toolType = ToolAnimationType.None;
      weaponType = WeaponAnimationType.Unarmed;
      scenario = PreviewScenario.Idle;

      animationController.SetLocomotion(0, 0, 0);
      animationController.SetGrounded(true);
      animationController.StopWork();
      animationController.SetInCombat(false);
      animationController.SetBlocking(false);
      animationController.SetAiming(false);
      animationController.SetCrouching(false);
      animationController.SetSneaking(false);
      animationController.StandUp();
      animationController.Resurrect();

      Debug.Log("[Preview] Reset All");
    }

    [TitleGroup("Debug Info")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Base Layer")]
    private string CurrentBaseState {
      get {
        if (animationController?.Animator == null) return "N/A";
        var info = animationController.Animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Idle")) return "Idle";
        if (info.IsName("Locomotion")) return "Locomotion";
        if (info.IsName("Crouch")) return "Crouch";
        if (info.IsName("JumpStart")) return "JumpStart";
        if (info.IsName("JumpIdle")) return "JumpIdle";
        if (info.IsName("JumpLand")) return "JumpLand";
        if (info.IsName("Dodge")) return "Dodge";
        if (info.IsName("Death")) return "Death";
        if (info.IsName("DeathPose")) return "DeathPose";
        return $"Other ({info.shortNameHash})";
      }
    }

    [ShowInInspector]
    [ReadOnly]
    [LabelText("UpperBody Layer")]
    private string CurrentUpperBodyState {
      get {
        if (animationController?.Animator == null) return "N/A";
        if (animationController.Animator.layerCount <= 2) return "No Layer";
        var info = animationController.Animator.GetCurrentAnimatorStateInfo(2);
        if (info.IsName("Empty")) return "Empty";
        if (info.IsName("Work_Axe")) return "Work_Axe";
        if (info.IsName("Work_Pickaxe")) return "Work_Pickaxe";
        if (info.IsName("Work_Shovel")) return "Work_Shovel";
        if (info.IsName("Work_Hammer")) return "Work_Hammer";
        if (info.IsName("Work_Saw")) return "Work_Saw";
        if (info.IsName("Work_Fishing")) return "Work_Fishing";
        if (info.IsName("Interact")) return "Interact";
        if (info.IsName("UseItem")) return "UseItem";
        if (info.IsName("Throw")) return "Throw";
        if (info.IsName("Wave")) return "Wave";
        if (info.IsName("Cheer")) return "Cheer";
        return $"Other ({info.shortNameHash})";
      }
    }

    [ShowInInspector]
    [ReadOnly]
    [LabelText("IsWorking")]
    private bool DebugIsWorking => animationController?.Animator?.GetBool(ParamIsWorking) ?? false;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("ToolType")]
    private int DebugToolType => animationController?.Animator?.GetInteger(ParamToolType) ?? 0;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("IsCrouching")]
    private bool DebugIsCrouching => animationController?.Animator?.GetBool(ParamIsCrouching) ?? false;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("IsSitting")]
    private bool DebugIsSitting => animationController?.Animator?.GetBool(ParamIsSitting) ?? false;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("IsLying")]
    private bool DebugIsLying => animationController?.Animator?.GetBool(ParamIsLying) ?? false;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("IsGrounded")]
    private bool DebugIsGrounded => animationController?.Animator?.GetBool(ParamIsGrounded) ?? false;

    private void ApplyScenario() {
      if (animationController == null) return;

      ResetAll();

      switch (scenario) {
        case PreviewScenario.Idle:
          break;

        case PreviewScenario.Walk:
          speed = 0.3f;
          ApplyLocomotion();
          break;

        case PreviewScenario.Run:
          speed = 1f;
          ApplyLocomotion();
          break;

        case PreviewScenario.WorkAxe:
          toolType = ToolAnimationType.Axe;
          ApplyWork();
          animationController.SetWorking(true);
          break;

        case PreviewScenario.WorkPickaxe:
          toolType = ToolAnimationType.Pickaxe;
          ApplyWork();
          animationController.SetWorking(true);
          break;

        case PreviewScenario.WorkAndWalk:
          speed = 0.3f;
          toolType = ToolAnimationType.Axe;
          ApplyLocomotion();
          ApplyWork();
          animationController.SetWorking(true);
          break;

        case PreviewScenario.Combat:
          weaponType = WeaponAnimationType.OneHandMelee;
          ApplyCombat();
          animationController.SetInCombat(true);
          break;

        case PreviewScenario.CombatCombo:
          weaponType = WeaponAnimationType.OneHandMelee;
          ApplyCombat();
          animationController.SetInCombat(true);
          animationController.Attack();
          break;

        case PreviewScenario.Crouch:
          animationController.SetCrouching(true);
          break;

        case PreviewScenario.CrouchMove:
          speed = 0.5f;
          animationController.SetCrouching(true);
          ApplyLocomotion();
          break;
      }
    }

    private void ApplyLocomotion() {
      if (animationController == null) return;
      animationController.SetLocomotion(speed, direction, vertical);
    }

    private void ApplyWork() {
      if (animationController == null) return;
      animationController.SetToolType(toolType);
    }

    private void ApplyCombat() {
      if (animationController == null) return;
      animationController.SetWeaponType(weaponType);
    }

    private void ApplyGrounded() {
      if (animationController == null) return;
      animationController.SetGrounded(isGrounded);
    }

    private void OnValidate() {
      if (animationController == null) {
        animationController = GetComponent<UniversalAnimationController>();
        if (animationController == null) {
          animationController = GetComponentInChildren<UniversalAnimationController>();
        }
      }
    }
  }

  public enum PreviewScenario {
    Idle,
    Walk,
    Run,
    Crouch,
    CrouchMove,
    WorkAxe,
    WorkPickaxe,
    WorkAndWalk,
    Combat,
    CombatCombo
  }
}
