using System;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  /// <summary>
  /// Deprecated: Use HasInInventoryBelief with inverse=true instead.
  /// Kept for backward compatibility with existing SO assets.
  /// </summary>
  [Serializable, TypeInfoBox("DEPRECATED: Use HasInInventoryBelief with inverse=true. True when agent has no item with specified tags.")]
  [Obsolete("Use HasInInventoryBelief with inverse=true")]
  public class HasNoInInventoryBelief : HasInInventoryBelief {
    public HasNoInInventoryBelief() {
      inverse = true;
    }

    public override AgentBelief Copy() {
      return new HasNoInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
        inverse = true
      };
    }
  }
}
