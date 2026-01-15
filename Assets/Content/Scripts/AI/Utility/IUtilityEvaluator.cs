using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.Utility {
  public interface IUtilityEvaluator {
    float Evaluate(IGoapAgent agent);
  }
  
  public interface IUtilityEvaluatorProvider {
    IUtilityEvaluator CopyEvaluator();
  }
}