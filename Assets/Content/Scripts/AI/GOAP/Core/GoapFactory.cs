using Content.Scripts.AI.GOAP.Planning;
using UnityEngine;

namespace Content.Scripts.AI.GOAP {
  public class GoapFactory : IPlannerFactory {
    public GoapFactory() {
      Debug.LogError("GOAP FACTORY created");
    }

    public IGoapPlanner CreatePlanner() {
      return new GOAPPlanner();
    }
  }

  public interface IPlannerFactory {
    IGoapPlanner CreatePlanner();
  }
}