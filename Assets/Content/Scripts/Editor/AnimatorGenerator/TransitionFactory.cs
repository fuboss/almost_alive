using Content.Scripts.Animation;
using UnityEditor.Animations;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class TransitionFactory {
    private readonly AnimatorGeneratorConfig _config;

    public TransitionFactory(AnimatorGeneratorConfig config) {
      _config = config;
    }

    public void AddFloatTransition(AnimatorState from, AnimatorState to, string param, float threshold, bool greater, int layer) {
      var transition = from.AddTransition(to);
      transition.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, param);
      transition.duration = _config.GetTransitionDuration(layer);
      transition.hasExitTime = false;
    }

    public void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, int layer) {
      var transition = from.AddTransition(to);
      transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
      transition.duration = _config.GetTransitionDuration(layer);
      transition.hasExitTime = false;
    }

    public void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger, int layer) {
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
      transition.duration = _config.GetTransitionDuration(layer);
      transition.hasExitTime = false;
    }

    public void AddExitTimeTransition(AnimatorState from, AnimatorState to, float exitTime, int layer) {
      var transition = from.AddTransition(to);
      transition.hasExitTime = true;
      transition.exitTime = _config.GetExitTime(exitTime);
      transition.duration = _config.GetTransitionDuration(layer);
    }

    public void AddAttackTransition(AnimatorState from, AnimatorState to, int weaponType) {
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
      transition.AddCondition(AnimatorConditionMode.Equals, weaponType, "WeaponType");
      transition.duration = _config.attackTransitionDuration;
      transition.hasExitTime = false;
    }

    public void AddBlockTransition(AnimatorState from, AnimatorState to) {
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "IsBlocking");
      transition.duration = _config.blockReactionTime;
      transition.hasExitTime = false;
    }
  }
}

