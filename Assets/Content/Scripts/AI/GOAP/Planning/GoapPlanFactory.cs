using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class GoapPlanFactory : IPlannerFactory {
    public GoapPlanFactory() {
    }

    public IGoapPlanner CreatePlanner() {
      return new GOAPPlanner();
    }
  }
}