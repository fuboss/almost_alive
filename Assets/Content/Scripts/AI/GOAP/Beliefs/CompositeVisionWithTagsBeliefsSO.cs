using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/SeeWithTags", order = 0)]
  public class CompositeVisionWithTagsBeliefsSO : CompositeBeliefSO<VisionBelief> {
    
    protected override VisionBelief CreateBeliefForTag(string tag) {
      return new VisionBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
      };
    }
  }
}