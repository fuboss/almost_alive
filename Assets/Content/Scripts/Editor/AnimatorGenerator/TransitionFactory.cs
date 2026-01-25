using Content.Scripts.Animation;
using UnityEditor.Animations;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class TransitionFactory {
    private readonly AnimatorGeneratorConfig _config;

    public TransitionFactory(AnimatorGeneratorConfig config) {
      _config = config;
    }

    public void AddFloatTransition(AnimatorState from, AnimatorState to, string param, float threshold, bool greater, int layer) {
      if (from == null || to == null) {
        UnityEngine.Debug.LogWarning($"[TransitionFactory] Skipping transition({param}) - from or to state is null");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, param);
      transition.duration = _config.GetTransitionDuration(layer);
      transition.hasExitTime = false;
    }

    public void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, int layer) {
      if (from == null || to == null) {
        UnityEngine.Debug.LogWarning($"[TransitionFactory] Skipping Bool({param}) transition - from or to state is null");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
      transition.duration = _config.GetTransitionDuration(layer);
      transition.hasExitTime = false;
    }

    public void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger, int layer) {
      if (from == null || to == null) {
        UnityEngine.Debug.LogWarning($"[TransitionFactory] Skipping Trigger({trigger}) transition - from or to state is null in layer {layer}");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
      transition.duration = _config.GetTransitionDuration(layer);
      transition.hasExitTime = false;
    }

    public void AddExitTimeTransition(AnimatorState from, AnimatorState to, float exitTime, int layer) {
      if (from == null || to == null) {
        UnityEngine.Debug.LogWarning($"[TransitionFactory] Skipping ExitTime transition - from or to state is null in layer {layer}");
        return;
      }
      var transition = from.AddTransition(to);
      transition.hasExitTime = true;
      transition.exitTime = _config.GetExitTime(exitTime);
      transition.duration = _config.GetTransitionDuration(layer);
    }

    public void AddAttackTransition(AnimatorState from, AnimatorState to, int weaponType) {
      if (from == null || to == null) {
        UnityEngine.Debug.LogWarning($"[TransitionFactory] Skipping Attack transition - from or to state is null for weapon type {weaponType}");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
      transition.AddCondition(AnimatorConditionMode.Equals, weaponType, "WeaponType");
      transition.duration = _config.attackTransitionDuration;
      transition.hasExitTime = false;
    }

    public void AddBlockTransition(AnimatorState from, AnimatorState to) {
      if (from == null || to == null) {
        UnityEngine.Debug.LogWarning($"[TransitionFactory] Skipping Block transition - from or to state is null");
        return;
      }
      var transition = from.AddTransition(to);
      transition.AddCondition(AnimatorConditionMode.If, 0, "IsBlocking");
      transition.duration = _config.blockReactionTime;
      transition.hasExitTime = false;
    }
  }
}

