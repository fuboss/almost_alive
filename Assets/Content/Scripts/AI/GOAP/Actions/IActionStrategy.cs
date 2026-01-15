using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Actions {
  public interface IActionStrategy {
    bool canPerform { get; }
    bool complete { get; }

    void OnStart() {
      // noop
    }

    void OnUpdate(float deltaTime) {
      // noop
    }

    void OnStop() {
      // noop
    }

    IActionStrategy Create(IGoapAgent agent);

    void OnComplete() {
    }
  }
}