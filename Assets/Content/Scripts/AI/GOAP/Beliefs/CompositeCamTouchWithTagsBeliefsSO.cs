using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/CanTouchWithTags", order = 0)]
  public class CompositeCamTouchWithTagsBeliefsSO : CompositeBeliefSO<InteractionBelief> {
    
    protected override InteractionBelief CreateBeliefForTag(string tag) {
      return new InteractionBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
      };
    }
  }
}