using Content.Scripts.AI.GOAP.Beliefs.Camp;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/CampHasModules", order = 0)]
  public class CompositeCampHasModulesTagsBeliefsSO : CompositeBeliefSO<CampHasModuleBelief> {
    protected override CampHasModuleBelief CreateBeliefForTag(string tag) {
      return new CampHasModuleBelief {
        name = $"{name}/{tag}",
        moduleTag = tag,
      };
    }
  }
}