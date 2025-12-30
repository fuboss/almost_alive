using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Actions {
  public interface IActionStrategy {
    bool canPerform { get; }
    bool complete { get; }

    void Start() {
      // noop
    }

    void Update(float deltaTime) {
      // noop
    }

    void Stop() {
      // noop
    }

    IActionStrategy Create(IGoapAgent agent);
  }
}