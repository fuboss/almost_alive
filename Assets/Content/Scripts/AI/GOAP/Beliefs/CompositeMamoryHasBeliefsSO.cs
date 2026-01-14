using Content.Scripts.AI.GOAP.Beliefs.Memory;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/HasInMemory", order = 0)]
  public class CompositeMamoryHasBeliefsSO : CompositeBeliefSO<HasInMemoryBelief> {
    public bool checkDistance = true;
    public float maxDistance = 50;

    protected override HasInMemoryBelief CreateBeliefForTag(string tag) {
      return new HasInMemoryBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
        checkDistance = checkDistance,
        maxDistance = maxDistance
      };
    }
  }
}