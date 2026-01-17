using Content.Scripts.AI.GOAP.Beliefs.Memory;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/Has No InMemory", order = 0)]
  public class CompositeMemoryHasNoBeliefsSO : CompositeBeliefSO<HasNoInMemoryBelief> {
    protected override HasNoInMemoryBelief CreateBeliefForTag(string tag) {
      return new HasNoInMemoryBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
      };
    }
  }
}