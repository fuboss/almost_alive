namespace Content.Scripts.Building.Runtime.Visuals {
  /// <summary>
  /// Defines when a decoration should be visible.
  /// </summary>
  public enum DecorationVisibilityMode {
    /// <summary>Always visible (e.g. foundation props)</summary>
    Always,
    
    /// <summary>Show progressively during construction at threshold</summary>
    OnConstruction,
    
    /// <summary>Show only after core module is built</summary>
    AfterCoreModule,
    
    /// <summary>Show when specific module with tag is installed</summary>
    WithModule,
    
    /// <summary>Custom evaluation logic (for special cases)</summary>
    Custom
  }
}
