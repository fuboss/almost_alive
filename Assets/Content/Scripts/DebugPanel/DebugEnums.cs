namespace Content.Scripts.DebugPanel {
  public enum DebugState {
    Idle,           // panel hidden
    Browsing,       // panel open, browsing actions
    ReadyToApply    // action selected, waiting for application
  }

  public enum DebugCategory {
    Spawn,
    Structure,
    SpawnTemplates,
    Destroy,
    Environment,
    Events
  }

  public enum DebugActionType {
    Instant,                // executes immediately
    RequiresWorldPosition,  // requires world click
    RequiresActor,          // requires actor click
    RequiresStructure       // requires structure click
  }
}

