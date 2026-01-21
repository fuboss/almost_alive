using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Actions {
  public interface IActionStrategy {
    bool canPerform { get; }
    bool complete { get; }

    void OnStart() {
    }

    void OnUpdate(float deltaTime) {
    }

    void OnStop() {
    }

    void OnComplete() {
    }

    IActionStrategy Create(IGoapAgentCore agent);
  }
}
