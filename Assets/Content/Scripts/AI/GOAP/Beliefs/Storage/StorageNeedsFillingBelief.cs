using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable]
  public class StorageNeedsFillingBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] acceptedTags;
    public bool inverse;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        var result = ActorRegistry<StorageActor>.all
          .Where(storage => storage.priority.isEnabled && !storage.isFull)
          .Any(storage => acceptedTags.Length <= 0 || (storage.AcceptsAnyTag(acceptedTags)));
        return !inverse 
          ? result 
          : !result;
      };
      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new StorageNeedsFillingBelief() {
        acceptedTags = acceptedTags,
        inverse = inverse,
        name = name
      };
    }
  }
}