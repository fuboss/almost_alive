namespace Content.Scripts.Core.Simulation {
  public interface ISimulatable {
    void SimTick(float simDeltaTime);
    int tickPriority => 0;
  }
}
