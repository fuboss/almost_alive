using System;
using UnityEngine;

namespace Content.Scripts.Animation {
  [Serializable]
  public abstract class AnimationAction {
    public abstract void Execute(UniversalAnimationController controller);
    public virtual void Stop(UniversalAnimationController controller) { }
  }

  [Serializable]
  public class WorkAction : AnimationAction {
    public ToolAnimationType toolType;

    public WorkAction(ToolAnimationType tool) {
      toolType = tool;
    }

    public override void Execute(UniversalAnimationController controller) {
      controller.SetToolType(toolType);
      controller.SetWorking(true);
    }

    public override void Stop(UniversalAnimationController controller) {
      controller.SetWorking(false);
      controller.SetToolType(ToolAnimationType.None);
    }
  }

  [Serializable]
  public class CombatAction : AnimationAction {
    public WeaponAnimationType weaponType;
    public bool enterCombat;

    public CombatAction(WeaponAnimationType weapon, bool enter = true) {
      weaponType = weapon;
      enterCombat = enter;
    }

    public override void Execute(UniversalAnimationController controller) {
      controller.SetWeaponType(weaponType);
      controller.SetInCombat(enterCombat);
    }

    public override void Stop(UniversalAnimationController controller) {
      controller.SetInCombat(false);
    }
  }

  [Serializable]
  public class RestAction : AnimationAction {
    public enum RestType { Sit, Lie }
    
    public RestType restType;
    public SitAnimationType sitType;

    public RestAction(RestType type, SitAnimationType sit = SitAnimationType.Chair) {
      restType = type;
      sitType = sit;
    }

    public override void Execute(UniversalAnimationController controller) {
      switch (restType) {
        case RestType.Sit:
          controller.SetSitting(true, sitType);
          break;
        case RestType.Lie:
          controller.SetLying(true);
          break;
      }
    }

    public override void Stop(UniversalAnimationController controller) {
      controller.SetSitting(false);
      controller.SetLying(false);
    }
  }

  [Serializable]
  public class TriggerAction : AnimationAction {
    public enum ActionType { Interact, UseItem, Throw, Wave, Cheer, Spawn, Jump, Hit }
    
    public ActionType actionType;

    public TriggerAction(ActionType type) {
      actionType = type;
    }

    public override void Execute(UniversalAnimationController controller) {
      switch (actionType) {
        case ActionType.Interact: controller.Interact(); break;
        case ActionType.UseItem: controller.UseItem(); break;
        case ActionType.Throw: controller.Throw(); break;
        case ActionType.Wave: controller.Wave(); break;
        case ActionType.Cheer: controller.Cheer(); break;
        case ActionType.Spawn: controller.Spawn(); break;
        case ActionType.Jump: controller.Jump(); break;
        case ActionType.Hit: controller.TakeHit(); break;
      }
    }
  }
}

