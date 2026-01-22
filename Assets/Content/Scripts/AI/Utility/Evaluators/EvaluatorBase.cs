using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public abstract class EvaluatorBase : IUtilityEvaluator {
    public abstract float Evaluate(IGoapAgentCore agent);
    
        
#if UNITY_EDITOR
    public List<string> Tags() {
      return GOAPEditorHelper.GetTags();
    }
#endif
  }
}
