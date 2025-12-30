namespace Content.Scripts.AI.GOAP.Planning {
  public interface IPlannerFactory {
    IGoapPlanner CreatePlanner();
  }
}