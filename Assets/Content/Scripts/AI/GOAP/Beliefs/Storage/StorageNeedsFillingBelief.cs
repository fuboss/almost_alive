using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when any enabled storage with matching tags needs filling.")]
  public class StorageNeedsFillingBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] acceptedTags;
    public bool inverse;
    
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var result = ActorRegistry<StorageActor>.all
          .Where(storage => storage.priority.isEnabled && !storage.isFull)
          .Any(storage => acceptedTags.Length <= 0 || (storage.AcceptsAnyTag(acceptedTags)));
        return !inverse ? result : !result;
      };
    }

    public override AgentBelief Copy() {
      return new StorageNeedsFillingBelief {
        acceptedTags = acceptedTags,
        inverse = inverse,
        name = name
      };
    }
  }
}
