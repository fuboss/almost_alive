using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.Utility {
  public interface IUtilityEvaluator {
    float Evaluate(IGoapAgentCore agent);
  }
  
  public interface IUtilityEvaluatorProvider {
    IUtilityEvaluator CopyEvaluator();
  }
}
