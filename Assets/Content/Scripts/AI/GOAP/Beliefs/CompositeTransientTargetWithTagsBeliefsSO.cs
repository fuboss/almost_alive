using Content.Scripts.AI.GOAP.Beliefs.TransientTarget;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/TransientTargetWithTags", order = 0)]
  public class CompositeTransientTargetWithTagsBeliefsSO : CompositeBeliefSO<TransientTargetHasTagsBelief> {

    public bool inverse = false;

    protected override TransientTargetHasTagsBelief CreateBeliefForTag(string tag) {
      return new TransientTargetHasTagsBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
        inverse = inverse
      };
    }
  }
}