using System.Linq;
using Content.Scripts.Game;

namespace Content.Scripts.AI.GOAP.Agent {
  public class FilteredActorInventory : ActorInventory {
    public string[] availableTags;

    public override bool TryPutItemInInventory(ActorDescription target) {
      var filtered = availableTags.Any(t => target.descriptionData.tags.Contains(t));

      return filtered && base.TryPutItemInInventory(target);
    }
  }
}