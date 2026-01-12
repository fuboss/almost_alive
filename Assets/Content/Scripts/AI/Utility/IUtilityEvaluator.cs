using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.Utility {
  public interface IUtilityEvaluator {
    float Evaluate(IGoapAgent agent);
  }

  public abstract class UtilitySO : SerializedScriptableObject, IUtilityEvaluator {
    public abstract float Evaluate(IGoapAgent agent);
#if UNITY_EDITOR
    public List<string> Tags() {
      return GOAPEditorHelper.GetTags();
    }
#endif
  }
}