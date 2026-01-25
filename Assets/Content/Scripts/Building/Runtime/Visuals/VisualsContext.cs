using System.Collections.Generic;
using Content.Scripts.Building.Runtime;

namespace Content.Scripts.Building.Runtime.Visuals {
  /// <summary>
  /// Context data for evaluating decoration visibility.
  /// </summary>
  public struct VisualsContext {
    /// <summary>Construction progress 0-1 (from UnfinishedStructureActor)</summary>
    public float constructionProgress;
    
    /// <summary>True if core module is built</summary>
    public bool isCoreBuilt;
    
    /// <summary>Tags of all installed modules</summary>
    public HashSet<string> installedModuleTags;
    
    /// <summary>True if structure is under construction (UnfinishedStructureActor)</summary>
    public bool isUnfinished;

    /// <summary>
    /// Create context for built structure.
    /// </summary>
    public static VisualsContext ForStructure(Structure structure) {
      var tags = new HashSet<string>();
      
      if (structure?.slots != null) {
        foreach (var slot in structure.slots) {
          if (slot.builtModule?.definition?.tags != null) {
            foreach (var tag in slot.builtModule.definition.tags) {
              tags.Add(tag);
            }
          }
        }
      }
      
      return new VisualsContext {
        constructionProgress = 1f,
        isCoreBuilt = structure?.isCoreBuilt ?? false,
        installedModuleTags = tags,
        isUnfinished = false
      };
    }

    /// <summary>
    /// Create context for structure under construction.
    /// </summary>
    public static VisualsContext ForUnfinished(UnfinishedStructureActor unfinished, float progress) {
      return new VisualsContext {
        constructionProgress = progress,
        isCoreBuilt = false,
        installedModuleTags = new HashSet<string>(),
        isUnfinished = true
      };
    }
  }
}
