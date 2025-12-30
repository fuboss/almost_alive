using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class GoapPlanFactory : IPlannerFactory {
    public GoapPlanFactory() {
      Debug.LogError("GOAP FACTORY created");
    }

    public IGoapPlanner CreatePlanner() {
      return new GOAPPlanner();
    }
  }
}