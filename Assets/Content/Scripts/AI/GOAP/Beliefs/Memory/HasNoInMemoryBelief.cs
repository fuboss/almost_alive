using System;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Memory {
  /// <summary>
  /// Deprecated: Use HasInMemoryBelief with inverse=true instead.
  /// Kept for backward compatibility with existing SO assets.
  /// </summary>
  [Serializable, TypeInfoBox("DEPRECATED: Use HasInMemoryBelief with inverse=true. True when agent doesn't remember objects with specified tags.")]
  [Obsolete("Use HasInMemoryBelief with inverse=true")]
  public class HasNoInMemoryBelief : HasInMemoryBelief {
    public HasNoInMemoryBelief() {
      inverse = true;
    }

    public override AgentBelief Copy() {
      return new HasNoInMemoryBelief {
        tags = tags,
        name = name,
        minCount = minCount,
        checkDistance = checkDistance,
        maxDistance = maxDistance,
        inverse = true
      };
    }
  }
}
