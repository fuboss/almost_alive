namespace Content.Scripts.DebugPanel {
  public interface IDebugAction {
    string displayName { get; }
    DebugCategory category { get; }
    DebugActionType actionType { get; }
    void Execute(DebugActionContext context);
  }
}