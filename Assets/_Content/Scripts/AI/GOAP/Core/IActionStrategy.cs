namespace _Content.Scripts.AI.GOAP.Strategies {
  public interface IActionStrategy {
    bool CanPerform { get; }
    bool Complete { get; }

    void Start() {
      // noop
    }

    void Update(float deltaTime) {
      // noop
    }

    void Stop() {
      // noop
    }
  }
}